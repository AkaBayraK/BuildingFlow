using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class PeopleStore(BuildingFlowDbContext db, TimeProvider clock) : IPeopleStore
{
    private DateOnly Today => BusinessDate.Today(clock);
    // Explicit filters also apply in the historical join, whose IgnoreQueryFilters
    // annotation affects every entity in the composed EF query.
    private IQueryable<Property> Properties(AccessScope access) => db.Properties.Where(value => !value.IsDeleted && (access.ReadAll ||
        db.PropertyMemberships.Any(member => !member.IsDeleted && member.PropertyId == value.Id && member.UserId == access.UserId && member.IsEnabled)));
    private IQueryable<Person> People(Guid property, AccessScope access) => db.People.Where(value => value.PropertyId == property && Properties(access).Any(parent => parent.Id == property));
    private void Touch(BaseEntity entity) => db.Entry(entity).Property(value => value.IsEnabled).IsModified = true;
    private async Task<Property?> LockProperty(Guid property,AccessScope access,CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property,token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE",token);
        return await Properties(access).SingleOrDefaultAsync(value => value.Id == property,token);
    }
    private static void Expected(BaseEntity entity, long version)
    { if (entity.Version != version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    private static void Active(BaseEntity entity)
    { if (!entity.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.OccupancyParentInactive.Create()); }
    private async Task Save(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new HierarchyConflictException(ErrorCatalog.OccupancyOverlap.Create()); }
    }
    private IQueryable<PersonResponse> Project(IQueryable<Person> query)
    {
        var today = Today;
        return query.Select(value => new PersonResponse(value.Id, value.PropertyId, value.FirstName, value.LastName, value.PhoneNumber,
            value.Email, value.IsEnabled, value.Version, value.CreatedAt, db.UnitOccupancies.Count(link => link.PersonId == value.Id && link.StartDate <= today && (link.EndDate == null || link.EndDate >= today))));
    }
    private static string Pattern(string text) => "%" + text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    public async Task<PagedRows<PersonResponse>?> ListAsync(Guid property, PeopleQuery request, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        var query = People(property, access).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search)) { var pattern = Pattern(request.Search); query = query.Where(value => EF.Functions.ILike(value.FirstName + " " + value.LastName, pattern, "\\")); }
        var count = await query.CountAsync(token);
        var sorted = request.SortBy == "createdAt" ? (request.SortDirection == "desc" ? query.OrderByDescending(value => value.CreatedAt) : query.OrderBy(value => value.CreatedAt))
            : (request.SortDirection == "desc" ? query.OrderByDescending(value => value.LastName).ThenByDescending(value => value.FirstName) : query.OrderBy(value => value.LastName).ThenBy(value => value.FirstName));
        return new(await Project(sorted.ThenBy(value => value.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)).ToListAsync(token), request.Page, request.PageSize, count);
    }
    public Task<PersonResponse?> GetAsync(Guid property, Guid person, AccessScope access, CancellationToken token) => Project(People(property, access).AsNoTracking().Where(value => value.Id == person)).SingleOrDefaultAsync(token);
    public async Task<PersonResponse?> CreateAsync(Guid property, CreatePersonRequest request, AccessScope access, CancellationToken token)
    {
        var parent = await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token); if (parent is null) return null;
        Active(parent); var person = new Person(property, request.FirstName!, request.LastName!, request.PhoneNumber, request.Email);
        db.People.Add(person); Touch(parent); await Save(token); return await GetAsync(property, person.Id, access, token);
    }
    public async Task<PersonResponse?> UpdateAsync(Guid property, Guid personId, UpdatePersonRequest request, AccessScope access, CancellationToken token)
    {
        var parent = await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token);
        var person = await People(property, access).SingleOrDefaultAsync(value => value.Id == personId, token); if (parent is null || person is null) return null;
        Active(parent); Expected(person, request.Version);
        if (request.IsEnabled == false && await HasUnfinished(personId, null, token)) throw new HierarchyConflictException(ErrorCatalog.PersonHasOccupancy.Create());
        person.Update(request.FirstName!, request.LastName!, request.PhoneNumber, request.Email, request.IsEnabled!.Value);
        Touch(person); Touch(parent); await Save(token); return await GetAsync(property, personId, access, token);
    }
    private Task<bool> HasUnfinished(Guid? person, Guid? unit, CancellationToken token)
    { var today = Today; return db.UnitOccupancies.AnyAsync(value => (person == null || value.PersonId == person) && (unit == null || value.UnitId == unit) && (value.EndDate == null || value.EndDate >= today), token); }
    public async Task<bool> DeleteAsync(Guid property, Guid personId, long version, AccessScope access, CancellationToken token)
    {
        var parent = await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token);
        var person = await People(property, access).SingleOrDefaultAsync(value => value.Id == personId, token); if (parent is null || person is null) return false;
        Active(parent); Expected(person, version);
        if (await HasUnfinished(personId, null, token)) throw new HierarchyConflictException(ErrorCatalog.PersonDeleteHasOccupancy.Create());
        db.Remove(person); Touch(parent); await Save(token); return true;
    }
    public async Task<PagedRows<UnitChoice>?> UnitChoicesAsync(Guid property, PeopleQuery request, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        var query = from unit in db.Units.Where(value => value.PropertyId == property)
                    join building in db.Buildings on unit.BuildingId equals (Guid?)building.Id into buildings
                    from building in buildings.DefaultIfEmpty()
                    where unit.BuildingId == null || building != null
                    select new { Unit = unit, Building = building };
        if (!string.IsNullOrWhiteSpace(request.Search)) { var pattern = Pattern(request.Search); query = query.Where(value => EF.Functions.ILike((value.Building == null ? "" : value.Building.Name) + " " + value.Unit.Number, pattern, "\\")); }
        var count = await query.CountAsync(token);
        var sorted = request.SortDirection == "desc" ? query.OrderByDescending(value => value.Building == null ? null : value.Building.Name).ThenByDescending(value => value.Unit.Number) : query.OrderBy(value => value.Building == null ? null : value.Building.Name).ThenBy(value => value.Unit.Number);
        return new(await sorted.ThenBy(value => value.Unit.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(value => new UnitChoice(value.Unit.Id, value.Unit.BuildingId, value.Unit.Number, value.Building == null ? null : value.Building.Name, value.Unit.IsEnabled && (value.Building == null || value.Building.IsEnabled))).ToListAsync(token), request.Page, request.PageSize, count);
    }
    private sealed class HistoryRow
    {
        public UnitOccupancy Link { get; init; } = null!;
        public Person Person { get; init; } = null!;
        public Unit Unit { get; init; } = null!;
        public Building? Building { get; init; }
    }
    private IQueryable<HistoryRow> OccupancyRows(Guid property, AccessScope access)
    {
        // Deleted people/units are retained for authorized historical display.
        return from link in db.UnitOccupancies.AsNoTracking().Where(value => !value.IsDeleted && value.PropertyId == property && Properties(access).Any(parent => parent.Id == property))
               join person in db.People.IgnoreQueryFilters() on new { link.PropertyId, Id = link.PersonId } equals new { person.PropertyId, person.Id }
               join unit in db.Units.IgnoreQueryFilters() on new { link.PropertyId, Id = link.UnitId } equals new { unit.PropertyId, unit.Id }
               join building in db.Buildings.IgnoreQueryFilters() on unit.BuildingId equals (Guid?)building.Id into buildings
               from building in buildings.DefaultIfEmpty()
               select new HistoryRow { Link = link, Person = person, Unit = unit, Building = building };
    }
    private IQueryable<OccupancyResponse> ProjectHistory(IQueryable<HistoryRow> query)
    {
        var today = Today;
        return query.Select(value => new OccupancyResponse(value.Link.Id, value.Link.PropertyId, value.Link.PersonId, value.Link.UnitId, value.Unit.BuildingId,
            value.Person.FirstName + " " + value.Person.LastName, value.Unit.Number, value.Building == null ? null : value.Building.Name, value.Link.Type.ToString(),
            value.Link.StartDate, value.Link.EndDate, value.Link.IsPrimaryContact, value.Link.StartDate > today ? "UPCOMING" : value.Link.EndDate != null && value.Link.EndDate < today ? "ENDED" : "ACTIVE", value.Link.Version));
    }
    public async Task<PagedRows<OccupancyResponse>?> OccupanciesAsync(Guid property, Guid? person, Guid? unit, PeopleQuery request, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        if (person is not null && !await db.People.IgnoreQueryFilters().AnyAsync(value => value.PropertyId == property && value.Id == person, token)) return null;
        if (unit is not null && !await db.Units.IgnoreQueryFilters().AnyAsync(value => value.PropertyId == property && value.Id == unit, token)) return null;
        var query = OccupancyRows(property, access).Where(value => (person == null || value.Link.PersonId == person) && (unit == null || value.Link.UnitId == unit));
        if (!string.IsNullOrWhiteSpace(request.Search)) { var pattern = Pattern(request.Search); query = query.Where(value => EF.Functions.ILike(value.Person.FirstName + " " + value.Person.LastName, pattern, "\\") || EF.Functions.ILike(value.Unit.Number, pattern, "\\")); }
        var count = await query.CountAsync(token);
        return new(await ProjectHistory(query.OrderByDescending(value => value.Link.StartDate).ThenBy(value => value.Link.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)).ToListAsync(token), request.Page, request.PageSize, count);
    }
    public Task<OccupancyResponse?> GetOccupancyAsync(Guid property, Guid occupancy, AccessScope access, CancellationToken token) => ProjectHistory(OccupancyRows(property, access).Where(value => value.Link.Id == occupancy)).SingleOrDefaultAsync(token);
    public async Task<OccupancyResponse?> CreateOccupancyAsync(Guid property, CreateOccupancyRequest request, AccessScope access, CancellationToken token)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property,access,token);
        var person = await People(property, access).SingleOrDefaultAsync(value => value.Id == request.PersonId, token);
        var unit = await db.Units.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == request.UnitId, token);
        if (parent is null || person is null || unit is null) return null;
        var building = unit.BuildingId is null ? null : await db.Buildings.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == unit.BuildingId, token);
        if (unit.BuildingId is not null && building is null) return null;
        Active(parent); Active(person); Active(unit); if (building is not null) Active(building);
        var type = Enum.Parse<OccupancyType>(request.Type); var start = request.StartDate!.Value; var end = request.EndDate;
        var overlapping = db.UnitOccupancies.Where(value => value.UnitId == unit.Id && (end == null || value.StartDate <= end) && (value.EndDate == null || value.EndDate >= start));
        if (await overlapping.AnyAsync(value => value.PersonId == person.Id && value.Type == type, token)) throw new HierarchyConflictException(ErrorCatalog.PersonOccupancyOverlap.Create());
        if (request.IsPrimaryContact && await overlapping.AnyAsync(value => value.IsPrimaryContact, token)) throw new HierarchyConflictException(ErrorCatalog.PrimaryContactOverlap.Create());
        var link = new UnitOccupancy(property, unit.Id, person.Id, type, start, end, request.IsPrimaryContact);
        db.UnitOccupancies.Add(link); Touch(parent); Touch(person); Touch(unit); if (building is not null) Touch(building);
        await Save(token); await transaction.CommitAsync(token); return await GetOccupancyAsync(property, link.Id, access, token);
    }
    private IQueryable<DuesCharge> FutureTenantCharges(UnitOccupancy link,DateOnly end) => db.DuesCharges.Where(value =>
        link.Type == OccupancyType.TENANT && value.PropertyId == link.PropertyId && value.UnitId == link.UnitId && !value.IsCancelled && value.Period > end &&
        db.DuesChargeParties.Any(party => party.PropertyId == link.PropertyId && party.ChargeId == value.Id && party.OccupancyId == link.Id && party.IsDebtor && party.Type == OccupancyType.TENANT));
    private IQueryable<DuesCharge> UnpaidTenantCharges(UnitOccupancy link,DateOnly end) => FutureTenantCharges(link,end).Where(value => value.PaidAmount == 0 &&
        !db.PaymentAllocations.Any(part => part.PropertyId == link.PropertyId && part.ChargeId == value.Id));
    public async Task<TenantMoveOutPreview?> EndPreviewAsync(Guid property,Guid occupancy,DateOnly endDate,long version,AccessScope access,CancellationToken token)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        if (!await Properties(access).AnyAsync(value => value.Id == property,token)) return null;
        var link=await db.UnitOccupancies.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == occupancy,token);
        if (link is null) return null; Expected(link,version);
        if (link.EndDate is not null) throw new HierarchyConflictException(ErrorCatalog.OccupancyAlreadyEnded.Create());
        if (endDate < link.StartDate) throw new RequestValidationException(new() { ["endDate"] = [ErrorCatalog.UiTheEndDateCannotPrecedeTheStartDate.Create()] });
        var query=UnpaidTenantCharges(link,endDate);var count=await query.CountAsync(token);
        var total=await query.SumAsync(value => (decimal?)value.Amount,token) ?? 0;
        var skipped=await FutureTenantCharges(link,endDate).CountAsync(token)-count;
        var records=count<=500 ? await query.OrderBy(value => value.Period).ThenBy(value => value.Id).ToListAsync(token) : [];
        await transaction.CommitAsync(token);
        return new(link.Id,link.Version,endDate,count,DuesService.Money(total),skipped,records.Select(value => new TenantChargeReview(value.Id,value.Version,value.Period.ToString("yyyy-MM"),DuesService.Money(value.Amount),value.DueDate)).ToArray(),count<=500);
    }
    public async Task<OccupancyResponse?> EndOccupancyAsync(Guid property, Guid occupancy, EndOccupancyRequest request, AccessScope access, CancellationToken token)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property,access,token);
        if (parent is null) return null;
        var link = await db.UnitOccupancies.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == occupancy, token); if (link is null) return null;
        Active(parent); Expected(link, request.Version);
        if (link.EndDate is not null) throw new HierarchyConflictException(ErrorCatalog.OccupancyAlreadyEnded.Create());
        if (request.EndDate < link.StartDate) throw new RequestValidationException(new() { ["endDate"] = [ErrorCatalog.UiTheEndDateCannotPrecedeTheStartDate.Create()] });
        var end=request.EndDate!.Value;
        var eligible=await UnpaidTenantCharges(link,end).ToListAsync(token);
        var futureCount=await FutureTenantCharges(link,end).CountAsync(token);
        var approvals=request.CancellationApprovals ?? [];
        if (approvals.Any(approval => !eligible.Any(charge => charge.Id == approval.ChargeId && charge.Version == approval.Version)))
            throw new HierarchyConflictException(ErrorCatalog.DuesErrorStaleApproval.Create());
        if (approvals.Count > 0)
        {
            var reason=await db.DuesCancellationReasons.SingleOrDefaultAsync(value => value.Id == DuesCancellationReason.TenantMovedOutId && value.IsEnabled,token);
            if(reason is null) throw new HierarchyConflictException(ErrorCatalog.DuesErrorMoveOutReason.Create());
            foreach (var approval in approvals)
                eligible.Single(charge => charge.Id == approval.ChargeId).Cancel($"İlişki bitiş tarihi: {end:yyyy-MM-dd}.",clock.GetUtcNow(),reason.Id,reason.Code,reason.Name,link.Id);
        }
        // No silent extension: an open interval already owns all later dates.
        link.End(request.EndDate!.Value); Touch(link); Touch(parent);
        var person = await db.People.SingleAsync(value => value.Id == link.PersonId, token); Touch(person);
        var unit = await db.Units.SingleAsync(value => value.Id == link.UnitId, token); Touch(unit);
        await Save(token); await transaction.CommitAsync(token);
        var response=await GetOccupancyAsync(property, link.Id, access, token);
        return response is null ? null : response with {TenantDebtResult=new(approvals.Count,eligible.Count-approvals.Count,futureCount-eligible.Count)};
    }
}
