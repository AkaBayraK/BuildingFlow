using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class PaymentStore(BuildingFlowDbContext db, LatePenaltyEngine penalties, TimeProvider clock) : IPaymentStore
{
    private IQueryable<Property> Properties(AccessScope access)
    {
        var parents = db.Properties.Where(value => !value.IsDeleted);
        return access.ReadAll ? parents : parents.Where(value => db.PropertyMemberships.Any(member =>
            !member.IsDeleted && member.PropertyId == value.Id && member.UserId == access.UserId && member.IsEnabled));
    }
    private IQueryable<Payment> Payments(Guid property, AccessScope access) => db.Payments.AsNoTracking().Where(value =>
        !value.IsDeleted && value.PropertyId == property && Properties(access).Any(parent => parent.Id == property));
    private async Task<Property?> LockProperty(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        return await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token);
    }
    private void Touch(BaseEntity entity) => db.Entry(entity).Property(value => value.IsEnabled).IsModified = true;
    private static void Active(Property property)
    {
        if (!property.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.PaymentsInactiveProperty.Create());
    }
    private static string Fingerprint(PaymentCommand command)
    {
        var content = JsonSerializer.Serialize(new {
            command.UnitId, amount = DuesService.Money(command.Amount), date = command.PaidDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            method = command.Method.ToString(), command.Reference, command.Description,
            allocations = command.Allocations.Where(value => value.Kind == AllocationKind.PRINCIPAL).OrderBy(value => value.ChargeId)
                .Select(value => new { value.ChargeId, amount = DuesService.Money(value.Amount), value.Version })
        });
        // Requests without penalty lines keep their original fingerprint, so old UUID retries still replay.
        var penalty = command.Allocations.Where(value => value.Kind == AllocationKind.PENALTY).OrderBy(value => value.ChargeId)
            .Select(value => new { value.ChargeId, amount = DuesService.Money(value.Amount), value.Version }).ToArray();
        if (penalty.Length > 0) content += "\nPenalty=" + JsonSerializer.Serialize(penalty);
        if (command.BankAccountId is not null) content += "\nBankAccount=" + command.BankAccountId;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
    public async Task<PaymentResponse?> CreateAsync(Guid property, PaymentCommand command, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, access, token); if (parent is null) return null;
        var fingerprint = Fingerprint(command);
        var previous = await db.Payments.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == command.RequestId, token);
        if (previous is not null)
        {
            if (previous.RequestFingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.PaymentsErrorRequestConflict.Create());
            await transaction.CommitAsync(token);
            var replay = await GetAsync(property, previous.Id, access, token); return replay is null ? null : replay with { Replayed = true };
        }
        Active(parent);
        if (command.PaidDate > BusinessDate.Today(clock)) throw new RequestValidationException(new() { ["paidDate"] = [ErrorCatalog.PaymentsErrorFutureDate.Create()] });
        var payment = await RecordAsync(parent, command, fingerprint, true, PaymentLedgerType.COLLECTION, null, token);
        if (payment is null) return null;
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new HierarchyConflictException(ErrorCatalog.PaymentsErrorUnique.Create()); }
        await transaction.CommitAsync(token);
        return await GetAsync(property, payment.Id, access, token);
    }
    public static string FingerprintOf(PaymentCommand command) => Fingerprint(command);
    // Shared by manual and verified online collections. The caller holds the Property lock and saves.
    // `checkVersions` is false for online settlement: the server, not a stale client form, chose the lines.
    internal async Task<Payment?> RecordAsync(Property parent, PaymentCommand command, string fingerprint, bool checkVersions,
        PaymentLedgerType? ledger, Guid? intentId, CancellationToken token)
    {
        var property = parent.Id;
        // Historical charges of disabled/deleted units can be settled. Property scope remains explicit.
        if (!await db.Units.IgnoreQueryFilters().AnyAsync(value => value.PropertyId == property && value.Id == command.UnitId, token)) return null;
        var ids = command.Allocations.Select(value => value.ChargeId).Distinct().ToArray();
        var charges = await db.DuesCharges.Where(value => value.PropertyId == property && value.UnitId == command.UnitId && ids.Contains(value.Id)).ToListAsync(token);
        if (charges.Count != ids.Length) return null;
        foreach (var allocation in command.Allocations)
        {
            var charge = charges.Single(value => value.Id == allocation.ChargeId);
            if (checkVersions && charge.Version != allocation.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            if (charge.IsCancelled || (allocation.Kind == AllocationKind.PRINCIPAL && allocation.Amount > charge.Balance))
                throw new HierarchyConflictException(ErrorCatalog.PaymentsErrorBalance.Create());
        }
        PaymentBankAccount? account = null;
        if (command.BankAccountId is not null)
        {
            account = await db.PaymentBankAccounts.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == command.BankAccountId, token);
            if (account is null || !account.UsableOn(command.PaidDate)) throw new HierarchyConflictException(ErrorCatalog.PaymentBankAccountUnusable.Create());
        }
        var states = await SettlePenaltiesAsync(property, command, charges, token);
        var payment = new Payment(property, command.UnitId, command.RequestId, fingerprint, command.Amount, "TRY", command.PaidDate, command.Method, command.Reference, command.Description);
        if (account is not null) payment.ReceivedInto(account);
        foreach (var allocation in command.Allocations)
        {
            var charge = charges.Single(value => value.Id == allocation.ChargeId);
            if (allocation.Kind == AllocationKind.PRINCIPAL) { payment.Allocate(charge, allocation.Amount); continue; }
            try { payment.AllocatePenalty(charge, states[charge.Id].Accruals, allocation.Amount); }
            catch (InvalidOperationException) { throw new HierarchyConflictException(ErrorCatalog.LatePenaltyBalance.Create()); }
        }
        payment.ValidateComplete(); db.Payments.Add(payment); Touch(parent);
        await IssueReceiptAsync(payment, token);
        if (ledger is not null)
            db.PaymentLedgerEntries.Add(new(property, payment.UnitId, ledger.Value, payment.Amount, payment.PaidDate, payment.Method.ToString(), payment.Id, intentId));
        return payment;
    }
    // Numbers are sequential per Property and year; the Property lock held by every caller serializes them.
    private async Task IssueReceiptAsync(Payment payment, CancellationToken token)
    {
        var prefix = await db.PropertyPaymentSettings.Where(value => value.PropertyId == payment.PropertyId).Select(value => value.ReceiptPrefix).SingleOrDefaultAsync(token) ?? "BF";
        var year = BusinessDate.Today(clock).Year;
        var last = await db.PaymentReceipts.Where(value => value.PropertyId == payment.PropertyId && value.Year == year).MaxAsync(value => (int?)value.Sequence, token) ?? 0;
        last = Math.Max(last, db.PaymentReceipts.Local.Where(value => value.PropertyId == payment.PropertyId && value.Year == year).Select(value => value.Sequence).DefaultIfEmpty().Max());
        db.PaymentReceipts.Add(new(payment, prefix, year, last + 1));
    }
    // Penalty up to the payment date is crystallized before principal changes. A back-dated
    // principal payment recalculates later unpaid accruals instead of leaving them stale.
    private async Task<Dictionary<Guid, LatePenaltyEngine.ChargeState>> SettlePenaltiesAsync(Guid property, PaymentCommand command, List<DuesCharge> charges, CancellationToken token)
    {
        var inputs = await penalties.InputsAsync(property, token);
        var states = await penalties.StatesAsync(property, charges, null, true, token);
        foreach (var state in states.Values)
        {
            var principal = command.Allocations.Where(value => value.ChargeId == state.Charge.Id && value.Kind == AllocationKind.PRINCIPAL).Sum(value => value.Amount);
            if (principal > 0)
            {
                state.Events.Add(new(command.PaidDate, principal));
                await penalties.RebuildAsync(state, inputs, command.PaidDate, "Geriye dönük tahsilat ile yeniden hesaplandı.", token);
            }
            penalties.Accrue(state, inputs, command.PaidDate);
        }
        return states;
    }
    private sealed class Row
    {
        public required Payment Payment { get; init; }
        public required string UnitNumber { get; init; }
        public Guid? BuildingId { get; init; }
        public string? BuildingName { get; init; }
        public string? ReceiptNumber { get; init; }
        public string? BankAccountIban { get; init; }
    }
    private IQueryable<Row> Rows(IQueryable<Payment> payments, Guid property) =>
        from payment in payments
        join unit in db.Units.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on payment.UnitId equals unit.Id
        join building in db.Buildings.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on unit.BuildingId equals (Guid?)building.Id into parents
        from building in parents.DefaultIfEmpty()
        select new Row { Payment = payment, UnitNumber = unit.Number, BuildingId = unit.BuildingId, BuildingName = building == null ? null : building.Name,
            ReceiptNumber = db.PaymentReceipts.Where(receipt => receipt.PaymentId == payment.Id).Select(receipt => receipt.Number).FirstOrDefault(),
            BankAccountIban = db.PaymentBankAccounts.Where(account => account.PropertyId == property && account.Id == payment.BankAccountId).Select(account => account.Iban).FirstOrDefault() };
    private sealed record AllocationRow(Guid PaymentId, Guid Id, Guid ChargeId, DateOnly Period, decimal Amount, AllocationKind Kind, Guid? AccrualId,
        ChargeKind ChargeKind, ChargeCategory? ChargeCategory);
    private async Task<List<AllocationRow>> Allocations(Guid property, Guid[] payments, CancellationToken token) => await (
        from allocation in db.PaymentAllocations.AsNoTracking().Where(value => value.PropertyId == property && payments.Contains(value.PaymentId))
        join charge in db.DuesCharges.AsNoTracking().Where(value => value.PropertyId == property) on allocation.ChargeId equals charge.Id
        select new AllocationRow(allocation.PaymentId, allocation.Id, allocation.ChargeId, charge.Period, allocation.Amount, allocation.Kind, allocation.AccrualId,
            charge.Kind, charge.Category)).ToListAsync(token);
    private static PaymentResponse Response(Row row, IEnumerable<AllocationRow> allocations)
    {
        var value = row.Payment;
        return new(value.Id, value.PropertyId, value.UnitId, row.BuildingId, row.UnitNumber, row.BuildingName, value.RequestId,
            DuesService.Money(value.Amount), value.Currency, value.PaidDate, value.Method.ToString(), value.Reference, value.Description,
            value.IsCancelled ? "CANCELLED" : "RECORDED", value.Version, value.CreatedAt, value.CancellationReason, value.CancelledAt,
            allocations.Where(part => part.PaymentId == value.Id).OrderBy(part => part.Period).ThenBy(part => part.ChargeId).ThenBy(part => part.Kind).ThenBy(part => part.Id)
                .Select(part => new PaymentAllocationResponse(part.Id, part.ChargeId, part.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    DuesService.Money(part.Amount), part.Kind.ToString(), part.AccrualId, part.ChargeKind.ToString(), part.ChargeCategory?.ToString())).ToArray(), false,
            row.ReceiptNumber, value.BankAccountId, row.BankAccountIban);
    }
    public async Task<PaymentResponse?> GetAsync(Guid property, Guid payment, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        var row = await Rows(Payments(property, access).Where(value => value.Id == payment), property).SingleOrDefaultAsync(token);
        if (row is null) return null;
        var parts = await Allocations(property, [payment], token);
        await transaction.CommitAsync(token); return Response(row, parts);
    }
    public async Task<PaymentPage?> ListAsync(Guid property, PaymentQuery query, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        var payments = Payments(property, access);
        if (query.UnitId is not null) payments = payments.Where(value => value.UnitId == query.UnitId);
        if (query.ChargeId is not null) payments = payments.Where(value => db.PaymentAllocations.Any(part => part.PropertyId == property && part.PaymentId == value.Id && part.ChargeId == query.ChargeId));
        if (query.From is not null) payments = payments.Where(value => value.PaidDate >= query.From);
        if (query.To is not null) payments = payments.Where(value => value.PaidDate <= query.To);
        if (query.Status == "RECORDED") payments = payments.Where(value => !value.IsCancelled);
        if (query.Status == "CANCELLED") payments = payments.Where(value => value.IsCancelled);
        if (!string.IsNullOrEmpty(query.Method)) { var method = Enum.Parse<PaymentMethod>(query.Method); payments = payments.Where(value => value.Method == method); }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            payments = payments.Where(value => EF.Functions.ILike(value.Description, pattern, "\\") || (value.Reference != null && EF.Functions.ILike(value.Reference, pattern, "\\")) ||
                db.Units.IgnoreQueryFilters().Any(unit => unit.PropertyId == property && unit.Id == value.UnitId && (EF.Functions.ILike(unit.Number, pattern, "\\") ||
                    db.Buildings.IgnoreQueryFilters().Any(building => building.PropertyId == property && building.Id == unit.BuildingId && EF.Functions.ILike(building.Name, pattern, "\\")))));
        }
        var count = await payments.CountAsync(token);
        var sums = await payments.GroupBy(_ => 1).Select(group => new {
            Recorded = group.Sum(value => value.IsCancelled ? 0m : value.Amount), Cancelled = group.Sum(value => value.IsCancelled ? value.Amount : 0m),
            RecordedCount = group.Count(value => !value.IsCancelled), CancelledCount = group.Count(value => value.IsCancelled)
        }).SingleOrDefaultAsync(token);
        var rows = Rows(payments, property); var desc = query.SortDirection == "desc";
        var ordered = query.SortBy switch {
            "amount" => desc ? rows.OrderByDescending(value => value.Payment.Amount) : rows.OrderBy(value => value.Payment.Amount),
            "createdAt" => desc ? rows.OrderByDescending(value => value.Payment.CreatedAt) : rows.OrderBy(value => value.Payment.CreatedAt),
            _ => desc ? rows.OrderByDescending(value => value.Payment.PaidDate) : rows.OrderBy(value => value.Payment.PaidDate)
        };
        var items = await ordered.ThenBy(value => value.Payment.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        var parts = await Allocations(property, items.Select(value => value.Payment.Id).ToArray(), token);
        await transaction.CommitAsync(token);
        return new(items.Select(value => Response(value, parts)).ToArray(), query.Page, query.PageSize, count,
            new(DuesService.Money(sums?.Recorded ?? 0m), DuesService.Money(sums?.Cancelled ?? 0m), sums?.RecordedCount ?? 0, sums?.CancelledCount ?? 0, "TRY"));
    }
    public async Task<PaymentResponse?> CancelAsync(Guid property, Guid payment, CancelChargeRequest request, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, access, token); if (parent is null) return null;
        Active(parent);
        var value = await db.Payments.Include(item => item.Allocations).SingleOrDefaultAsync(item => item.PropertyId == property && item.Id == payment, token);
        if (value is null) return null;
        if (value.Version != request.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if (value.IsCancelled) throw new HierarchyConflictException(ErrorCatalog.PaymentsErrorCancelled.Create());
        if (value.Method == PaymentMethod.ONLINE_CARD) throw new HierarchyConflictException(ErrorCatalog.PaymentOnlineCancel.Create());
        await ReverseAsync(parent, value, request.Reason!, token);
        db.PaymentLedgerEntries.Add(new(property, value.UnitId, PaymentLedgerType.COLLECTION_REVERSAL, -value.Amount, BusinessDate.Today(clock), value.Method.ToString(), value.Id));
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return await GetAsync(property, payment, access, token);
    }
    // Reverses allocations and rebuilds late penalty; used by manual cancellation, online refunds and chargebacks.
    internal async Task ReverseAsync(Property parent, Payment value, string reason, CancellationToken token)
    {
        var property = parent.Id;
        if (!db.Entry(value).Collection(item => item.Allocations).IsLoaded) await db.Entry(value).Collection(item => item.Allocations).LoadAsync(token);
        var ids = value.Allocations.Select(part => part.ChargeId).ToArray();
        var charges = await db.DuesCharges.Where(charge => charge.PropertyId == property && charge.UnitId == value.UnitId && ids.Contains(charge.Id)).ToListAsync(token);
        var accrualIds = value.Allocations.Where(part => part.AccrualId != null).Select(part => part.AccrualId!.Value).ToArray();
        var accruals = await db.LatePenaltyAccruals.Where(item => item.PropertyId == property && accrualIds.Contains(item.Id)).ToListAsync(token);
        foreach (var part in value.Allocations)
            if (part.Kind == AllocationKind.PRINCIPAL) charges.Single(charge => charge.Id == part.ChargeId).ReversePayment(part.Amount);
            else accruals.Single(item => item.Id == part.AccrualId).ReversePayment(part.Amount);
        value.Cancel(reason, clock.GetUtcNow()); Touch(parent);
        var principalCharges = charges.Where(charge => value.Allocations.Any(part => part.ChargeId == charge.Id && part.Kind == AllocationKind.PRINCIPAL)).ToArray();
        if (principalCharges.Length > 0)
        {
            var inputs = await penalties.InputsAsync(property, token);
            foreach (var state in (await penalties.StatesAsync(property, principalCharges, value.Id, true, token)).Values)
                await penalties.RebuildAsync(state, inputs, value.PaidDate, "Ödeme iptali ile yeniden hesaplandı.", token);
        }
    }
}
