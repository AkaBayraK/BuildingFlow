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

public sealed class DuesStore(BuildingFlowDbContext db, TimeProvider clock) : IDuesStore
{
    private IQueryable<Property> Properties(AccessScope access)
    {
        // IgnoreQueryFilters used by historical joins affects the whole EF query.
        // Keep parent and membership deletion guards explicit, including SUPER_ADMIN.
        var parents = db.Properties.Where(value => !value.IsDeleted);
        return access.ReadAll ? parents : parents.Where(value => db.PropertyMemberships.Any(member =>
            !member.IsDeleted && member.PropertyId == value.Id && member.UserId == access.UserId && member.IsEnabled));
    }
    private IQueryable<DuesCharge> Charges(Guid property, AccessScope access) =>
        db.DuesCharges.AsNoTracking().Where(value => !value.IsDeleted && value.PropertyId == property &&
            Properties(access).Any(parent => parent.Id == property));
    private static void Active(BaseEntity entity)
    {
        if (!entity.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.DuesErrorInactive.Create());
    }
    private void Touch(BaseEntity entity) => db.Entry(entity).Property(value => value.IsEnabled).IsModified = true;
    private static string Pattern(string value) => "%" + value.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    private static string Fingerprint(AssessmentCommand command)
    {
        var canonical = JsonSerializer.Serialize(new {
            units = command.UnitIds.Order().ToArray(), period = command.Period.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            amount = DuesService.Money(command.Amount), command.Currency,
            dueDate = command.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), command.Description
        });
        // Preserve the fingerprint of existing UNIT requests; old UUID retries still replay.
        if (command.DebtorType != "UNIT") canonical += "\nDebtorType=" + command.DebtorType;
        if (command.Kind != ChargeKind.DUES) canonical += "\nKind=" + command.Kind + ";Category=" + command.Category;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
    private static AssessmentResponse Assessment(DuesAssessment value, bool replayed) =>
        new(value.Id, value.PropertyId, value.RequestId, value.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            DuesService.Money(value.Amount), value.Currency, value.DueDate, value.Description, value.Charges.Count,
            DuesService.Money(value.Amount * value.Charges.Count), value.Charges.Select(charge => charge.UnitId).Order().ToArray(), replayed,
            value.Kind.ToString(), value.Category?.ToString());

    // Transactions serialize dues commands on the real Property row. Re-read after
    // waiting so concurrent identical requests replay the committed batch, rather
    // than creating a second charge or using a stale parent version.
    private async Task<Property?> LockProperty(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        return await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token);
    }
    public async Task<AssessmentResponse?> CreateAsync(Guid property, AssessmentCommand command, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, access, token);
        if (parent is null) return null;
        var fingerprint = Fingerprint(command);
        var existing = await db.DuesAssessments.AsNoTracking().Include(value => value.Charges)
            .SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == command.RequestId, token);
        if (existing is not null)
        {
            if (existing.RequestFingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.DuesErrorRequestConflict.Create());
            await transaction.CommitAsync(token);
            return Assessment(existing, true);
        }
        Active(parent);
        var units = await db.Units.Where(value => value.PropertyId == property && command.UnitIds.Contains(value.Id)).ToListAsync(token);
        if (units.Count != command.UnitIds.Count) return null;
        foreach (var unit in units) Active(unit);
        var buildingIds = units.Where(value => value.BuildingId != null).Select(value => value.BuildingId!.Value).Distinct().ToArray();
        var buildings = await db.Buildings.Where(value => value.PropertyId == property && buildingIds.Contains(value.Id)).ToListAsync(token);
        if (buildings.Count != buildingIds.Length) throw new HierarchyConflictException(ErrorCatalog.DuesErrorBuilding.Create());
        foreach (var building in buildings) Active(building);
        // Only monthly dues are unique per unit and period; several extra charges may share a month.
        if (command.Kind == ChargeKind.DUES && await db.DuesCharges.AnyAsync(value => value.PropertyId == property && command.UnitIds.Contains(value.UnitId) &&
            value.Period == command.Period && !value.IsCancelled && value.Kind == ChargeKind.DUES, token))
            throw new HierarchyConflictException(ErrorCatalog.DuesErrorDuplicate.Create());
        var assessment = new DuesAssessment(property, command.RequestId, fingerprint, command.Period,
            command.Amount, command.Currency, command.DueDate, command.Description, command.DebtorType, command.Kind, command.Category);
        var links = await (from link in db.UnitOccupancies.IgnoreQueryFilters().AsNoTracking()
                .Where(value => !value.IsDeleted && value.IsEnabled && value.PropertyId == property && command.UnitIds.Contains(value.UnitId) &&
                    value.StartDate <= command.Period && (value.EndDate == null || value.EndDate >= command.Period))
            join person in db.People.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property)
                on link.PersonId equals person.Id
            select new {Link=link,Person=person}).ToListAsync(token);
        foreach (var unit in units)
        {
            var related = links.Where(value => value.Link.UnitId == unit.Id).ToArray();
            var debtors = related.Where(value => value.Link.Type.ToString() == command.DebtorType).ToArray();
            if (command.DebtorType != "UNIT" && debtors.Length != 1)
                throw new RequestValidationException(new() { ["debtorType"] = [ErrorCatalog.DuesErrorAmbiguousDebtor.Create()] });
            var charge = assessment.AddCharge(unit.Id);
            charge.RecordParties(related.Select(value => new DuesChargeParty(charge,value.Link,value.Person,
                command.DebtorType != "UNIT" && value.Link.Id == debtors[0].Link.Id)));
            Touch(unit);
        }
        db.DuesAssessments.Add(assessment);
        Touch(parent); foreach (var building in buildings) Touch(building);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new HierarchyConflictException(ErrorCatalog.DuesErrorUnique.Create()); }
        await transaction.CommitAsync(token);
        return Assessment(assessment, false);
    }
    public async Task<AssessmentResponse?> GetAssessmentAsync(Guid property, Guid assessment, AccessScope access, CancellationToken token)
    {
        var value = await db.DuesAssessments.AsNoTracking().Include(item => item.Charges)
            .SingleOrDefaultAsync(item => item.PropertyId == property && item.Id == assessment &&
                Properties(access).Any(parent => parent.Id == property), token);
        return value is null ? null : Assessment(value, false);
    }

    private sealed class Row
    {
        public required DuesCharge Charge { get; init; }
        public required string UnitNumber { get; init; }
        public Guid? BuildingId { get; init; }
        public string? BuildingName { get; init; }
        public required string Description { get; init; }
        public required string DebtorType { get; init; }
        public decimal PenaltyBalance { get; init; }
    }
    private IQueryable<Row> Rows(IQueryable<DuesCharge> charges, Guid property) =>
        from charge in charges
        join assessment in db.DuesAssessments.AsNoTracking().Where(value => value.PropertyId == property) on charge.AssessmentId equals assessment.Id
        // Historical unit/building names remain readable, without disabling the
        // Property/active membership scope enforced by Charges().
        join unit in db.Units.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on charge.UnitId equals unit.Id
        join building in db.Buildings.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property)
            on unit.BuildingId equals (Guid?)building.Id into parents
        from building in parents.DefaultIfEmpty()
        select new Row { Charge = charge, UnitNumber = unit.Number, BuildingId = unit.BuildingId,
            BuildingName = building == null ? null : building.Name, Description = assessment.Description, DebtorType = assessment.DebtorType,
            PenaltyBalance = db.LatePenaltyAccruals.Where(accrual => accrual.PropertyId == property && accrual.ChargeId == charge.Id &&
                accrual.Status == LatePenaltyAccrualStatus.ACTIVE).Sum(accrual => (decimal?)(accrual.Amount - accrual.PaidAmount)) ?? 0m };
    private DuesChargeResponse Response(Row row, AccessScope access, IReadOnlyList<DuesPartyResponse> parties)
    {
        var charge = row.Charge;
        return new(charge.Id, charge.PropertyId, charge.AssessmentId, charge.UnitId, row.BuildingId, row.UnitNumber,
            row.BuildingName, charge.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture), DuesService.Money(charge.Amount),
            DuesService.Money(charge.Balance), charge.Currency, charge.DueDate, row.Description,
            charge.Status(BusinessDate.Today(clock)), charge.Version, charge.CreatedAt, charge.CancellationReason, charge.CancelledAt,
            charge.PartiesRecorded, access.ReadFinancialParties, row.DebtorType, parties, charge.CancellationReasonCode, charge.CancellationReasonName,
            DuesService.Money(row.PenaltyBalance), charge.Kind.ToString(), charge.Category?.ToString());
    }
    private async Task<IReadOnlyList<DuesPartyResponse>> PartyResponses(Guid property, Guid[] charges, AccessScope access, CancellationToken token)
    {
        if (!access.ReadFinancialParties) return [];
        return await db.DuesChargeParties.AsNoTracking().Where(value => value.PropertyId == property && charges.Contains(value.ChargeId))
            .OrderBy(value => value.Type).ThenBy(value => value.PersonName).ThenBy(value => value.Id)
            .Select(value => new DuesPartyResponse(value.ChargeId,value.PersonId,value.OccupancyId,value.PersonName,value.Type.ToString(),value.StartDate,value.EndDate,value.IsDebtor)).ToListAsync(token);
    }
    public async Task<DuesChargeResponse?> GetAsync(Guid property, Guid charge, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        var row = await Rows(Charges(property, access).Where(value => value.Id == charge), property).SingleOrDefaultAsync(token);
        if (row is null) return null;
        var parties = await PartyResponses(property,[charge],access,token);
        await transaction.CommitAsync(token); return Response(row,access,parties);
    }
    public async Task<DuesPage?> ListAsync(Guid property, DuesQuery query, AccessScope access, CancellationToken token)
    {
        // Count, filtered summary and page share one snapshot.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        var today = BusinessDate.Today(clock);
        var charges = Charges(property, access);
        if (query.UnitId is not null) charges = charges.Where(value => value.UnitId == query.UnitId);
        if (query.Kind is not null) { var kind = Enum.Parse<ChargeKind>(query.Kind); charges = charges.Where(value => value.Kind == kind); }
        var partyRole = query.PartyRole is null ? (OccupancyType?)null : Enum.Parse<OccupancyType>(query.PartyRole);
        if (query.PersonId is not null || partyRole is not null) charges = charges.Where(value => db.DuesChargeParties.Any(party =>
            party.PropertyId == property && party.ChargeId == value.Id && (query.PersonId == null || party.PersonId == query.PersonId) &&
            (partyRole == null || party.Type == partyRole)));
        if (!string.IsNullOrEmpty(query.Period)) { DuesService.TryPeriod(query.Period, out var period); charges = charges.Where(value => value.Period == period); }
        if (query.DueFrom is not null) charges = charges.Where(value => value.DueDate >= query.DueFrom);
        if (query.DueTo is not null) charges = charges.Where(value => value.DueDate <= query.DueTo);
        charges = query.Status switch {
            "OPEN" => charges.Where(value => !value.IsCancelled && value.PaidAmount == 0 && value.DueDate >= today),
            "PARTIALLY_PAID" => charges.Where(value => !value.IsCancelled && value.PaidAmount > 0 && value.PaidAmount < value.Amount && value.DueDate >= today),
            "PAID" => charges.Where(value => !value.IsCancelled && value.PaidAmount == value.Amount),
            "OUTSTANDING" => charges.Where(value => !value.IsCancelled && value.PaidAmount < value.Amount),
            "OVERDUE" => charges.Where(value => !value.IsCancelled && value.PaidAmount < value.Amount && value.DueDate < today),
            "CANCELLED" => charges.Where(value => value.IsCancelled), _ => charges
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = Pattern(query.Search);
            charges = charges.Where(value =>
                (access.ReadFinancialParties && db.DuesChargeParties.Any(party => party.PropertyId == property && party.ChargeId == value.Id && EF.Functions.ILike(party.PersonName,pattern,"\\"))) ||
                db.DuesAssessments.Any(batch => batch.Id == value.AssessmentId && EF.Functions.ILike(batch.Description, pattern, "\\")) ||
                db.Units.IgnoreQueryFilters().Any(unit => unit.PropertyId == property && unit.Id == value.UnitId && EF.Functions.ILike(unit.Number, pattern, "\\")) ||
                db.Units.IgnoreQueryFilters().Any(unit => unit.PropertyId == property && unit.Id == value.UnitId &&
                    db.Buildings.IgnoreQueryFilters().Any(building => building.PropertyId == property && building.Id == unit.BuildingId && EF.Functions.ILike(building.Name, pattern, "\\"))));
        }
        var count = await charges.CountAsync(token);
        var sums = await charges.GroupBy(_ => 1).Select(group => new {
            Outstanding = group.Sum(value => value.IsCancelled ? 0m : value.Amount - value.PaidAmount),
            Overdue = group.Sum(value => !value.IsCancelled && value.PaidAmount < value.Amount && value.DueDate < today ? value.Amount - value.PaidAmount : 0m),
            Cancelled = group.Sum(value => value.IsCancelled ? value.Amount : 0m),
            OpenCount = group.Count(value => !value.IsCancelled && value.PaidAmount < value.Amount && value.DueDate >= today),
            OverdueCount = group.Count(value => !value.IsCancelled && value.PaidAmount < value.Amount && value.DueDate < today),
            CancelledCount = group.Count(value => value.IsCancelled)
        }).SingleOrDefaultAsync(token);
        var penalty = await db.LatePenaltyAccruals.Where(accrual => accrual.PropertyId == property && accrual.Status == LatePenaltyAccrualStatus.ACTIVE &&
            charges.Any(charge => charge.Id == accrual.ChargeId)).SumAsync(accrual => (decimal?)(accrual.Amount - accrual.PaidAmount), token) ?? 0m;
        var summary = new DuesSummary(DuesService.Money(sums?.Outstanding ?? 0m), DuesService.Money(sums?.Overdue ?? 0m),
            DuesService.Money(sums?.Cancelled ?? 0m), sums?.OpenCount ?? 0, sums?.OverdueCount ?? 0, sums?.CancelledCount ?? 0, "TRY", DuesService.Money(penalty));
        var rows = Rows(charges, property); var desc = query.SortDirection == "desc";
        var ordered = query.SortBy switch {
            "period" => desc ? rows.OrderByDescending(value => value.Charge.Period) : rows.OrderBy(value => value.Charge.Period),
            "amount" => desc ? rows.OrderByDescending(value => value.Charge.Amount) : rows.OrderBy(value => value.Charge.Amount),
            "createdAt" => desc ? rows.OrderByDescending(value => value.Charge.CreatedAt) : rows.OrderBy(value => value.Charge.CreatedAt),
            _ => desc ? rows.OrderByDescending(value => value.Charge.DueDate) : rows.OrderBy(value => value.Charge.DueDate)
        };
        var items = await ordered.ThenBy(value => value.Charge.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        var parties = await PartyResponses(property,items.Select(value => value.Charge.Id).ToArray(),access,token);
        await transaction.CommitAsync(token);
        return new(items.Select(value => Response(value,access,parties.Where(party => party.ChargeId == value.Charge.Id).ToArray())).ToArray(), query.Page, query.PageSize, count, summary);
    }
    public async Task<DuesChargeResponse?> CancelAsync(Guid property, Guid charge, CancelChargeRequest request, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, access, token); if (parent is null) return null;
        var value = await db.DuesCharges.SingleOrDefaultAsync(item => item.PropertyId == property && item.Id == charge, token);
        if (value is null) return null;
        Active(parent);
        if (value.Version != request.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if (value.IsCancelled) throw new HierarchyConflictException(ErrorCatalog.DuesErrorCancelled.Create());
        if (value.PaidAmount > 0) throw new HierarchyConflictException(ErrorCatalog.PaymentsDebtHasPayments.Create());
        var accruals = await db.LatePenaltyAccruals.Where(item => item.PropertyId == property && item.ChargeId == charge &&
            item.Status == LatePenaltyAccrualStatus.ACTIVE).ToListAsync(token);
        if (accruals.Any(item => item.PaidAmount > 0)) throw new HierarchyConflictException(ErrorCatalog.LatePenaltyChargePaid.Create());
        foreach (var accrual in accruals) accrual.Supersede("Ana borç iptal edildi.", clock.GetUtcNow());
        var reason = await db.DuesCancellationReasons.SingleOrDefaultAsync(item => item.Id == (request.ReasonId ?? DuesCancellationReason.OtherId) && item.IsEnabled, token);
        if (reason is null) throw new RequestValidationException(new() { ["reasonId"] = [ErrorCatalog.DuesChooseReason.Create()] });
        if (reason.RequiresExplanation && string.IsNullOrWhiteSpace(request.Reason))
            throw new RequestValidationException(new() { ["reason"] = [ErrorCatalog.DuesOtherReasonRequired.Create()] });
        value.Cancel(string.IsNullOrWhiteSpace(request.Reason) ? reason.Name : request.Reason.Trim(), clock.GetUtcNow(), reason.Id, reason.Code, reason.Name); Touch(parent);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        await transaction.DisposeAsync();
        return await GetAsync(property, charge, access, token);
    }
    public async Task<PagedRows<UnitChoice>?> UnitChoicesAsync(Guid property, PeopleQuery query, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        var units = db.Units.AsNoTracking().Where(value => value.PropertyId == property && value.IsEnabled &&
            Properties(access).Any(parent => parent.Id == property && parent.IsEnabled) &&
            (value.BuildingId == null || db.Buildings.Any(building => building.PropertyId == property && building.Id == value.BuildingId && building.IsEnabled)));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = Pattern(query.Search);
            units = units.Where(value => EF.Functions.ILike(value.Number, pattern, "\\") ||
                db.Buildings.Any(building => building.Id == value.BuildingId && EF.Functions.ILike(building.Name, pattern, "\\")));
        }
        var count = await units.CountAsync(token);
        var items = await units.OrderBy(value => value.Number).ThenBy(value => value.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(value => new UnitChoice(value.Id, value.BuildingId, value.Number,
                db.Buildings.Where(building => building.Id == value.BuildingId).Select(building => building.Name).FirstOrDefault(), value.IsEnabled)).ToListAsync(token);
        return new(items, query.Page, query.PageSize, count);
    }
    public async Task<PagedRows<DuesPersonChoice>?> PersonChoicesAsync(Guid property,PeopleQuery request,AccessScope access,CancellationToken token)
    {
        if (!access.ReadFinancialParties) throw new UnauthorizedAccessException();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        if (!await Properties(access).AnyAsync(value => value.Id == property,token)) return null;
        var parties = db.DuesChargeParties.AsNoTracking().Where(value => value.PropertyId == property && Charges(property,access).Any(charge => charge.Id == value.ChargeId));
        if (!string.IsNullOrWhiteSpace(request.Search)) { var pattern=Pattern(request.Search); parties=parties.Where(value => EF.Functions.ILike(value.PersonName,pattern,"\\")); }
        var choices = parties.GroupBy(value => value.PersonId).Select(group => new {Id=group.Key,Name=
            group.OrderByDescending(value => value.CreatedAt).ThenBy(value => value.Id).Select(value => value.PersonName).First()});
        var count=await choices.CountAsync(token);
        var items=await choices.OrderBy(value => value.Name).ThenBy(value => value.Id).Skip((request.Page-1)*request.PageSize).Take(request.PageSize).ToListAsync(token);
        await transaction.CommitAsync(token);return new(items.Select(value => new DuesPersonChoice(value.Id,value.Name,property)).ToArray(),request.Page,request.PageSize,count);
    }
    public async Task<IReadOnlyList<CancellationReasonResponse>?> CancellationReasonsAsync(Guid property,AccessScope access,CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property,token)) return null;
        return await db.DuesCancellationReasons.AsNoTracking().Where(value => value.IsEnabled).OrderBy(value => value.Code)
            .Select(value => new CancellationReasonResponse(value.Id,value.Code,value.Name,value.RequiresExplanation)).ToListAsync(token);
    }
}
