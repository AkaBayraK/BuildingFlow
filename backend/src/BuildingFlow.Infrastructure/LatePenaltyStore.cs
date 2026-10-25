using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class LatePenaltyStore(BuildingFlowDbContext db, LatePenaltyEngine engine, TimeProvider clock) : ILatePenaltyStore, ILatePenaltyScheduler
{
    private static string Money(decimal value) => DuesService.Money(value);
    private IQueryable<Property> Properties(AccessScope access)
    {
        var parents = db.Properties.Where(value => !value.IsDeleted);
        return access.ReadAll ? parents : parents.Where(value => db.PropertyMemberships.Any(member =>
            !member.IsDeleted && member.PropertyId == value.Id && member.UserId == access.UserId && member.IsEnabled));
    }
    private async Task<Property?> LockProperty(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var parent = await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token);
        if (parent is { IsEnabled: false }) throw new HierarchyConflictException(ErrorCatalog.DuesErrorInactive.Create());
        return parent;
    }
    private void Touch(BaseEntity entity) => db.Entry(entity).Property(value => value.IsEnabled).IsModified = true;
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());

    private static LatePenaltySwitchResponse Switch(LatePenaltySwitch value, bool replayed = false) =>
        new(value.Id, value.EffectiveFrom, value.Apply, value.Reason, value.CreatedBy, value.CreatedAt, replayed);
    private static LatePenaltyPolicyResponse Policy(LatePenaltyPolicy value, bool replayed = false) =>
        new(value.Id, value.Year, value.ValidFrom, value.ValidTo, LatePenaltyService.Rate(value.MonthlyRate), value.Method.ToString(),
            value.LegalBasis.ToString(), value.LegalReference, value.ApprovalDocumentId, value.Status.ToString(), value.ApprovedBy,
            value.ApprovedAt, value.StatusReason, value.CreatedBy, value.CreatedAt, value.Version, replayed);
    private static LatePenaltySegmentResponse Segment(PenaltySegmentResult value) =>
        new(value.From, value.To, value.Days, Money(value.Principal), LatePenaltyService.Rate(value.MonthlyRate), value.Method.ToString(),
            value.PolicyId, value.Amount.ToString("0.000000", CultureInfo.InvariantCulture));
    private static LatePenaltyAccrualResponse Accrual(LatePenaltyAccrual value) =>
        new(value.Id, value.FromDate, value.ToDate, Money(value.Amount), Money(value.PaidAmount), Money(value.Balance), value.Status.ToString(),
            value.StatusReason, value.StatusChangedAt, value.Version, value.Segments.OrderBy(item => item.FromDate)
                .Select(item => Segment(new(item.FromDate, item.ToDate, item.Days, item.Principal, item.MonthlyRate, item.Method, item.PolicyId, item.Amount))).ToArray());

    private async Task EnsureNotCrystallized(Guid property, DateOnly from, CancellationToken token)
    {
        var through = await engine.CrystallizedThroughAsync(property, token);
        if (through is not null && from <= through) throw Conflict(ErrorCatalog.LatePenaltyCrystallized);
    }

    public async Task<LatePenaltySettings?> SettingsAsync(Guid property, LatePenaltyActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var switches = await db.LatePenaltySwitches.AsNoTracking().Where(value => value.PropertyId == property).OrderByDescending(value => value.EffectiveFrom).ToListAsync(token);
        var policies = await db.LatePenaltyPolicies.AsNoTracking().Where(value => value.PropertyId == property)
            .OrderByDescending(value => value.ValidFrom).ThenByDescending(value => value.CreatedAt).ToListAsync(token);
        var through = await engine.CrystallizedThroughAsync(property, token);
        await transaction.CommitAsync(token);
        var today = BusinessDate.Today(clock);
        return new(property, switches.FirstOrDefault(value => value.EffectiveFrom <= today)?.Apply ?? false,
            LatePenaltyService.Rate(LatePenaltyRules.StatutoryMonthlyRate), LatePenaltyRules.StatutoryReference, LatePenaltyRules.DefaultMethod.ToString(),
            through, actor.CanDraft, actor.CanApprove, switches.Select(value => Switch(value)).ToArray(), policies.Select(value => Policy(value)).ToArray());
    }

    public async Task<LatePenaltySwitchResponse?> SwitchAsync(Guid property, LatePenaltySwitchRequest request, LatePenaltyActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        var previous = await db.LatePenaltySwitches.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == request.RequestId, token);
        if (previous is not null)
        {
            if (previous.EffectiveFrom != request.EffectiveFrom || previous.Apply != request.Apply || previous.Reason != request.Reason)
                throw Conflict(ErrorCatalog.LatePenaltyRequestConflict);
            await transaction.CommitAsync(token); return Switch(previous, true);
        }
        var from = request.EffectiveFrom!.Value;
        if (from < BusinessDate.Today(clock)) throw new RequestValidationException(new() { ["effectiveFrom"] = [ErrorCatalog.LatePenaltySwitchInvalid.Create()] });
        await EnsureNotCrystallized(property, from, token);
        if (await db.LatePenaltySwitches.AnyAsync(value => value.PropertyId == property && value.EffectiveFrom == from, token))
            throw Conflict(ErrorCatalog.LatePenaltyState);
        var value = new LatePenaltySwitch(property, request.RequestId, from, request.Apply, request.Reason!);
        db.LatePenaltySwitches.Add(value); Touch(parent);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw Conflict(ErrorCatalog.LatePenaltyState); }
        await transaction.CommitAsync(token); return Switch(value);
    }

    private async Task EnsureDocument(Guid property, Guid? document, CancellationToken token)
    {
        if (document is not null && !await db.StoredDocuments.AnyAsync(value => value.PropertyId == property && value.Id == document, token))
            throw new RequestValidationException(new() { ["approvalDocumentId"] = [ErrorCatalog.LatePenaltyDocument.Create()] });
    }
    private static bool Same(LatePenaltyPolicy value, LatePenaltyPolicyCommand command) =>
        value.ValidFrom == command.ValidFrom && value.ValidTo == command.ValidTo && value.MonthlyRate == command.MonthlyRate && value.Method == command.Method &&
        value.LegalBasis == command.LegalBasis && value.LegalReference == command.LegalReference && value.ApprovalDocumentId == command.ApprovalDocumentId;

    public async Task<LatePenaltyPolicyResponse?> CreatePolicyAsync(Guid property, LatePenaltyPolicyCommand command, LatePenaltyActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        var previous = await db.LatePenaltyPolicies.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == command.RequestId, token);
        if (previous is not null)
        {
            if (!Same(previous, command)) throw Conflict(ErrorCatalog.LatePenaltyRequestConflict);
            await transaction.CommitAsync(token); return Policy(previous, true);
        }
        await EnsureDocument(property, command.ApprovalDocumentId, token);
        var value = new LatePenaltyPolicy(property, command.RequestId, command.ValidFrom, command.ValidTo, command.MonthlyRate,
            command.Method, command.LegalBasis, command.LegalReference, command.ApprovalDocumentId);
        db.LatePenaltyPolicies.Add(value); Touch(parent);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return Policy(value);
    }

    private async Task<(Property Parent, LatePenaltyPolicy Policy)?> LoadPolicy(Guid property, Guid policy, long version, AccessScope access, CancellationToken token)
    {
        var parent = await LockProperty(property, access, token); if (parent is null) return null;
        var value = await db.LatePenaltyPolicies.SingleOrDefaultAsync(item => item.PropertyId == property && item.Id == policy, token);
        if (value is null) return null;
        if (value.Version != version) throw Conflict(ErrorCatalog.RecordChanged);
        return (parent, value);
    }
    private async Task<LatePenaltyPolicyResponse?> Mutate(Guid property, Guid policy, long version, LatePenaltyActor actor,
        Func<Property, LatePenaltyPolicy, Task> change, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var loaded = await LoadPolicy(property, policy, version, actor.Access, token); if (loaded is null) return null;
        var (parent, value) = loaded.Value;
        await change(parent, value); Touch(parent);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return Policy(value);
    }

    public Task<LatePenaltyPolicyResponse?> UpdatePolicyAsync(Guid property, Guid policy, LatePenaltyPolicyCommand command, LatePenaltyActor actor, CancellationToken token) =>
        Mutate(property, policy, command.Version, actor, async (_, value) => {
            if (value.Status != LatePenaltyPolicyStatus.DRAFT) throw Conflict(ErrorCatalog.LatePenaltyState);
            await EnsureDocument(property, command.ApprovalDocumentId, token);
            value.UpdateDraft(command.ValidFrom, command.ValidTo, command.MonthlyRate, command.Method, command.LegalBasis, command.LegalReference, command.ApprovalDocumentId);
        }, token);

    public Task<LatePenaltyPolicyResponse?> ApprovePolicyAsync(Guid property, Guid policy, long version, LatePenaltyActor actor, CancellationToken token) =>
        Mutate(property, policy, version, actor, async (_, value) => {
            if (value.Status != LatePenaltyPolicyStatus.DRAFT) throw Conflict(ErrorCatalog.LatePenaltyState);
            await EnsureNotCrystallized(property, value.ValidFrom, token);
            var approved = await db.LatePenaltyPolicies.Where(item => item.PropertyId == property && item.Id != value.Id &&
                item.Status == LatePenaltyPolicyStatus.APPROVED).ToListAsync(token);
            if (approved.Any(value.Overlaps)) throw Conflict(ErrorCatalog.LatePenaltyOverlap);
            try { value.Approve(actor.Access.UserId, clock.GetUtcNow()); }
            catch (InvalidOperationException) { throw Conflict(ErrorCatalog.LatePenaltyApproval); }
        }, token);

    public Task<LatePenaltyPolicyResponse?> CancelPolicyAsync(Guid property, Guid policy, LatePenaltyStatusRequest request, LatePenaltyActor actor, CancellationToken token) =>
        Mutate(property, policy, request.Version, actor, (_, value) => {
            if (value.Status != LatePenaltyPolicyStatus.DRAFT) throw Conflict(ErrorCatalog.LatePenaltyState);
            value.Cancel(request.Reason!); return Task.CompletedTask;
        }, token);

    public Task<LatePenaltyPolicyResponse?> EndPolicyAsync(Guid property, Guid policy, LatePenaltyStatusRequest request, LatePenaltyActor actor, CancellationToken token) =>
        Mutate(property, policy, request.Version, actor, async (_, value) => {
            var validTo = request.ValidTo!.Value;
            if (value.Status != LatePenaltyPolicyStatus.APPROVED || validTo < value.ValidFrom || (value.ValidTo is not null && validTo >= value.ValidTo))
                throw Conflict(ErrorCatalog.LatePenaltyState);
            await EnsureNotCrystallized(property, validTo.AddDays(1), token);
            value.EndOn(validTo, request.Reason!);
        }, token);

    private sealed class ChargeRow
    {
        public required DuesCharge Charge { get; init; }
        public required string UnitNumber { get; init; }
        public string? BuildingName { get; init; }
    }
    private IQueryable<ChargeRow> ChargeRows(IQueryable<DuesCharge> charges, Guid property) =>
        from charge in charges
        join unit in db.Units.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on charge.UnitId equals unit.Id
        join building in db.Buildings.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property)
            on unit.BuildingId equals (Guid?)building.Id into parents
        from building in parents.DefaultIfEmpty()
        select new ChargeRow { Charge = charge, UnitNumber = unit.Number, BuildingName = building == null ? null : building.Name };

    public async Task<LatePenaltyPage?> OverdueAsync(Guid property, LatePenaltyQuery query, LatePenaltyActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var today = BusinessDate.Today(clock);
        var accruals = db.LatePenaltyAccruals.AsNoTracking().Where(value => value.PropertyId == property);
        var charges = db.DuesCharges.AsNoTracking().Where(value => value.PropertyId == property && !value.IsCancelled && value.DueDate < today &&
            (value.PaidAmount < value.Amount || accruals.Any(item => item.ChargeId == value.Id && item.Status == LatePenaltyAccrualStatus.ACTIVE && item.PaidAmount < item.Amount)));
        if (query.UnitId is not null) charges = charges.Where(value => value.UnitId == query.UnitId);
        if (query.Year is not null) charges = charges.Where(value => value.Period.Year == query.Year);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            charges = charges.Where(value => db.Units.IgnoreQueryFilters().Any(unit => unit.PropertyId == property && unit.Id == value.UnitId &&
                (EF.Functions.ILike(unit.Number, pattern, "\\") || db.Buildings.IgnoreQueryFilters().Any(building =>
                    building.PropertyId == property && building.Id == unit.BuildingId && EF.Functions.ILike(building.Name, pattern, "\\")))));
        }
        var count = await charges.CountAsync(token);
        var principal = await charges.SumAsync(value => (decimal?)(value.Amount - value.PaidAmount), token) ?? 0m;
        var scoped = accruals.Where(item => charges.Any(charge => charge.Id == item.ChargeId));
        var outstanding = await scoped.Where(item => item.Status == LatePenaltyAccrualStatus.ACTIVE).SumAsync(item => (decimal?)(item.Amount - item.PaidAmount), token) ?? 0m;
        var collected = await scoped.Where(item => item.Status != LatePenaltyAccrualStatus.SUPERSEDED).SumAsync(item => (decimal?)item.PaidAmount, token) ?? 0m;
        var waived = await scoped.Where(item => item.Status == LatePenaltyAccrualStatus.WAIVED).SumAsync(item => (decimal?)item.Amount, token) ?? 0m;
        var rows = await ChargeRows(charges, property).OrderBy(value => value.Charge.DueDate).ThenBy(value => value.Charge.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        var inputs = await engine.InputsAsync(property, token);
        var states = await engine.StatesAsync(property, rows.Select(value => value.Charge).ToArray(), null, false, token);
        await transaction.CommitAsync(token);
        var items = rows.Select(row => {
            var state = states[row.Charge.Id];
            var penalty = state.Accruals.Sum(value => value.Balance);
            var pending = LatePenaltyCalculator.Round(LatePenaltyEngine.Pending(state, inputs, today).Sum(value => value.Amount));
            return new LatePenaltyRow(row.Charge.Id, row.Charge.UnitId, row.UnitNumber, row.BuildingName, Period(row.Charge.Period), row.Charge.DueDate,
                today.DayNumber - row.Charge.DueDate.DayNumber, Money(row.Charge.Balance), Money(penalty), Money(pending), Money(row.Charge.Balance + penalty + pending));
        }).ToArray();
        return new(items, query.Page, query.PageSize, count, new(Money(principal), Money(outstanding), Money(collected), Money(waived), "TRY"));
    }
    private static string Period(DateOnly value) => value.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public async Task<LatePenaltyChargeDetail?> ChargeAsync(Guid property, Guid charge, LatePenaltyActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var row = await ChargeRows(db.DuesCharges.AsNoTracking().Where(value => value.PropertyId == property && value.Id == charge), property).SingleOrDefaultAsync(token);
        if (row is null) return null;
        var history = await db.LatePenaltyAccruals.AsNoTracking().Include(value => value.Segments)
            .Where(value => value.PropertyId == property && value.ChargeId == charge).OrderBy(value => value.FromDate).ThenBy(value => value.CreatedAt).ToListAsync(token);
        var inputs = await engine.InputsAsync(property, token);
        var state = (await engine.StatesAsync(property, [row.Charge], null, false, token))[charge];
        await transaction.CommitAsync(token);
        var today = BusinessDate.Today(clock);
        var pending = LatePenaltyEngine.Pending(state, inputs, today);
        var live = history.Where(value => value.Status != LatePenaltyAccrualStatus.SUPERSEDED).ToArray();
        var balance = live.Sum(value => value.Balance); var pendingAmount = LatePenaltyCalculator.Round(pending.Sum(value => value.Amount));
        return new(charge, row.Charge.UnitId, row.UnitNumber, Period(row.Charge.Period), row.Charge.DueDate, Money(row.Charge.Amount),
            Money(row.Charge.PaidAmount), Money(row.Charge.Balance), Money(live.Sum(value => value.Amount)), Money(live.Sum(value => value.PaidAmount)),
            Money(live.Where(value => value.Status == LatePenaltyAccrualStatus.WAIVED).Sum(value => value.Amount)), Money(balance), Money(pendingAmount),
            Money(row.Charge.Balance + balance + pendingAmount), today, inputs.AppliedOn(today), history.Select(Accrual).ToArray(), pending.Select(Segment).ToArray());
    }

    public async Task<LatePenaltyPreview?> PreviewAsync(Guid property, Guid unit, DateOnly paidDate, decimal? amount, LatePenaltyActor actor, CancellationToken token)
    {
        if (paidDate > BusinessDate.Today(clock)) throw new RequestValidationException(new() { ["paidDate"] = [ErrorCatalog.PaymentsErrorFutureDate.Create()] });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        if (!await db.Units.IgnoreQueryFilters().AnyAsync(value => value.PropertyId == property && value.Id == unit, token)) return null;
        var charges = await db.DuesCharges.AsNoTracking().Where(value => value.PropertyId == property && value.UnitId == unit && !value.IsCancelled)
            .OrderBy(value => value.DueDate).ThenBy(value => value.Period).ThenBy(value => value.Id).ToListAsync(token);
        var inputs = await engine.InputsAsync(property, token);
        var states = await engine.StatesAsync(property, charges, null, false, token);
        await transaction.CommitAsync(token);
        var (suggested, remaining) = LatePenaltyEngine.Suggest(charges, states, inputs, paidDate, amount ?? 0m);
        var lines = suggested.Select(value => new LatePenaltyPreviewLine(value.Charge.Id, Period(value.Charge.Period), value.Charge.DueDate, value.Charge.Version,
            Money(value.Charge.Balance), Money(value.PenaltyDue), Money(value.Penalty), Money(value.Principal))).ToArray();
        return new(unit, paidDate, inputs.AppliedOn(paidDate), Money(charges.Sum(value => value.Balance)),
            Money(suggested.Sum(value => value.PenaltyDue)), Money(remaining), lines);
    }

    public async Task<LatePenaltyRunResult?> RunAsync(Guid property, DateOnly through, LatePenaltyActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        if (through > BusinessDate.Today(clock)) throw new RequestValidationException(new() { ["through"] = [ErrorCatalog.LatePenaltyDate.Create()] });
        var result = await engine.AccrueOutstandingAsync(property, through, token);
        if (result.Accruals > 0) { Touch(parent); await db.SaveChangesAsync(token); }
        await transaction.CommitAsync(token);
        return new(through, result.Charges, result.Accruals, Money(result.Amount));
    }

    public async Task<LatePenaltyAccrualResponse?> WaiveAsync(Guid property, Guid accrual, LatePenaltyWaiveRequest request, LatePenaltyActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        var value = await db.LatePenaltyAccruals.Include(item => item.Segments).SingleOrDefaultAsync(item => item.PropertyId == property && item.Id == accrual, token);
        if (value is null) return null;
        if (value.Version != request.Version) throw Conflict(ErrorCatalog.RecordChanged);
        if (value.Status != LatePenaltyAccrualStatus.ACTIVE || value.PaidAmount > 0) throw Conflict(ErrorCatalog.LatePenaltyWaive);
        value.Waive(request.Reason!, clock.GetUtcNow()); Touch(parent);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return Accrual(value);
    }

    public async Task<LatePenaltyReport?> ReportAsync(Guid property, LatePenaltyActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var accrued = await db.LatePenaltyAccruals.AsNoTracking().Where(value => value.PropertyId == property && value.Status != LatePenaltyAccrualStatus.SUPERSEDED)
            .GroupBy(value => value.FromDate.Year).Select(group => new {
                Year = group.Key, Amount = group.Sum(value => value.Amount),
                Waived = group.Sum(value => value.Status == LatePenaltyAccrualStatus.WAIVED ? value.Amount : 0m),
                Outstanding = group.Sum(value => value.Status == LatePenaltyAccrualStatus.ACTIVE ? value.Amount - value.PaidAmount : 0m) }).ToListAsync(token);
        var collected = await (from allocation in db.PaymentAllocations.AsNoTracking()
                               join payment in db.Payments.AsNoTracking() on allocation.PaymentId equals payment.Id
                               where allocation.PropertyId == property && allocation.Kind == AllocationKind.PENALTY && !payment.IsCancelled
                               group allocation by payment.PaidDate.Year into grouped
                               select new { Year = grouped.Key, Amount = grouped.Sum(value => value.Amount) }).ToListAsync(token);
        var policies = await db.LatePenaltyPolicies.AsNoTracking().Where(value => value.PropertyId == property && value.Status == LatePenaltyPolicyStatus.APPROVED)
            .OrderBy(value => value.ValidFrom).ToListAsync(token);
        var switches = await db.LatePenaltySwitches.AsNoTracking().Where(value => value.PropertyId == property).OrderBy(value => value.EffectiveFrom).ToListAsync(token);
        await transaction.CommitAsync(token);
        var years = accrued.Select(value => value.Year).Union(collected.Select(value => value.Year)).Order().Select(year => {
            var a = accrued.SingleOrDefault(value => value.Year == year);
            return new LatePenaltyReportYear(year, Money(a?.Amount ?? 0m), Money(collected.SingleOrDefault(value => value.Year == year)?.Amount ?? 0m),
                Money(a?.Waived ?? 0m), Money(a?.Outstanding ?? 0m));
        }).ToArray();
        return new(property, "TRY", years, policies.Select(value => Policy(value)).ToArray(), switches.Select(value => Switch(value)).ToArray());
    }

    // Scheduled month-end crystallization; reruns find nothing new for the same month.
    public async Task<int> AccrueThroughPreviousMonthAsync(CancellationToken token)
    {
        var today = BusinessDate.Today(clock); var through = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        var properties = await db.LatePenaltySwitches.AsNoTracking().Where(value => value.Apply && value.EffectiveFrom <= through &&
                db.Properties.Any(parent => parent.Id == value.PropertyId && parent.IsEnabled))
            .Select(value => value.PropertyId).Distinct().ToListAsync(token);
        var created = 0;
        foreach (var property in properties)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            var parent = await LockProperty(property, new("system", true), token);
            if (parent is null) continue;
            var result = await engine.AccrueOutstandingAsync(property, through, token);
            if (result.Accruals > 0) { Touch(parent); await db.SaveChangesAsync(token); }
            await transaction.CommitAsync(token); created += result.Accruals;
            db.ChangeTracker.Clear();
        }
        return created;
    }
}
