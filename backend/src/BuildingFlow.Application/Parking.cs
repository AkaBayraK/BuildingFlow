using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public enum ParkingRole { MANAGE, GUARD }
public sealed record ParkingPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record ParkingVersionRequest(long Version);

public sealed record MobileParkingContext(Guid PropertyId, Guid? UnitId, string Role, string? ContextKind = null, Guid? ContextId = null, int Page = 1);
public sealed record MobileParkingAllocation(Guid Id, string SpaceCode, DateOnly StartDate, DateOnly? EndDate);
public sealed record MobileParkingSnapshot(string UserId, string ContextKey, Guid PropertyId, Guid? UnitId, string Role, DateTimeOffset AsOf, int Page,
    int VehicleTotal, int AllocationTotal, int GuestTotal, int UnitTotal, int SpaceTotal,
    IReadOnlyList<ResidentVehicleResponse> Vehicles, IReadOnlyList<MobileParkingAllocation> Allocations, IReadOnlyList<GuestVehicleResponse> Guests,
    IReadOnlyList<ParkingUnitOption> Units, IReadOnlyList<ParkingSpaceResponse> Spaces, bool PolicyConfigured, int GuestMaxHours);
public sealed record ParkingGuestPolicyResponse(bool Configured, int GuestMaxHours);
public sealed record ParkingPolicyRequest(int MaxVehiclesPerUnit, int MaxVehiclesPerPerson, int MaxSpacesPerUnit, int GuestMaxHours, int RetentionDays, long Version = 0);
public sealed record ParkingPolicyResponse(Guid PropertyId, bool Configured, int MaxVehiclesPerUnit, int MaxVehiclesPerPerson, int MaxSpacesPerUnit, int GuestMaxHours, int RetentionDays, long Version);

public sealed record ParkingFacilityRequest(string? Name, Guid? BuildingId, long Version = 0);
public sealed record ParkingLevelRequest(Guid FacilityId, string? Name, int SortOrder, long Version = 0);
public sealed record ParkingLevelResponse(Guid Id, Guid FacilityId, string Name, int SortOrder, long Version);
public sealed record ParkingFacilityResponse(Guid Id, Guid? BuildingId, string? BuildingName, string Name, int SpaceCount, IReadOnlyList<ParkingLevelResponse> Levels, long Version);

public sealed record ParkingSpaceRequest(Guid FacilityId, Guid? LevelId, string? Code, string? Type, long Version = 0);
public sealed record ParkingSpaceQuery(Guid? FacilityId = null, Guid? LevelId = null, string? Type = null, int Page = 1, int PageSize = 20);
public sealed record ParkingSpaceResponse(Guid Id, Guid FacilityId, Guid? LevelId, string Code, string Type, Guid? AllocatedUnitId, string? AllocatedUnitNumber, long Version, bool IsClosed = false);

public sealed record VehicleRequest(Guid RequestId, Guid PersonId, Guid UnitId, string? Plate, string? Type, string? Brand, string? Model, string? Color);
public sealed record ResidentVehicleRequest(Guid RequestId, Guid PropertyId, Guid UnitId, string? Plate, string? Type, string? Brand, string? Model, string? Color);
public sealed record VehicleEditRequest(string? Type, string? Brand, string? Model, string? Color, long Version);
public sealed record VehicleReviewRequest(long Version, string? Reason = null);
public sealed record VehicleQuery(string? Status = null, string? Search = null, Guid? UnitId = null, int Page = 1, int PageSize = 20);
public sealed record VehicleResponse(Guid Id, Guid PersonId, string PersonName, Guid UnitId, string UnitNumber, string Plate, string Type, string? Brand, string? Model,
    string? Color, string Status, DateTimeOffset? ReviewedAt, string? ReviewReason, long Version, DateTimeOffset CreatedAt);
public sealed record ResidentVehicleResponse(Guid Id, Guid UnitId, string Plate, string Type, string? Brand, string? Model, string? Color, string Status, string? ReviewReason, long Version);
public sealed record PlateResponse(string Plate, string UnitNumber, string? BuildingName, string Type);
public sealed record ParkingUnitOption(Guid Id, string Number, string? BuildingName);
public sealed record ParkingResidentOption(Guid PersonId, string Name, string Relation);

public sealed record ParkingAllocationRequest(Guid SpaceId, Guid UnitId, Guid? VehicleId, DateOnly StartDate, DateOnly? EndDate = null);
public sealed record ParkingAllocationEndRequest(DateOnly EndDate, long Version);
public sealed record ParkingAllocationQuery(Guid? UnitId = null, Guid? SpaceId = null, bool ActiveOnly = true, int Page = 1, int PageSize = 20);
public sealed record ParkingAllocationResponse(Guid Id, Guid SpaceId, string SpaceCode, Guid UnitId, string UnitNumber, Guid? VehicleId, string? Plate, DateOnly StartDate, DateOnly? EndDate, long Version);

public sealed record GuestVehicleRequest(Guid RequestId, Guid HostUnitId, string? Plate, string? GuestName, DateTimeOffset StartsAt, DateTimeOffset EndsAt, Guid? SpaceId, string? Note);
public sealed record GuestVehicleQuery(DateTimeOffset? From = null, DateTimeOffset? To = null, string? Status = null, int Page = 1, int PageSize = 20);
public sealed record GuestVehicleResponse(Guid Id, Guid HostUnitId, string UnitNumber, Guid? SpaceId, string? SpaceCode, string Plate, string? GuestName, string? Note,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, string Status, DateTimeOffset? ArrivedAt, DateTimeOffset? DepartedAt, long Version);
public enum GuestAction { ARRIVE, DEPART, CANCEL }
public enum VehicleAction { APPROVE, REJECT, DEACTIVATE }

public sealed record ParkingClosureRequest(Guid RequestId, Guid SpaceId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Reason);
public sealed record ParkingClosureCancelRequest(long Version, string? Reason);
public sealed record ParkingClosureResponse(Guid Id, Guid SpaceId, string SpaceCode, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    string Reason, DateTimeOffset? CancelledAt, string? CancellationReason, long Version);

public sealed record ParkingReportDay(DateOnly Day, int Entries, int Exits, int Reservations);
public sealed record ParkingReport(Guid PropertyId, DateOnly From, DateOnly Through, DateTimeOffset AsOf,
    int Capacity, int AllocatedSpaces, int ReservedGuestSpaces, int ManualGuestsInside, int OverdueGuests,
    bool ScheduledOperationsEnabled, bool HardwareEnabled, IReadOnlyList<ParkingReportDay> Days);
public interface IParkingOperations
{
    Task<Guid?> RunBatch(Guid? afterProperty, CancellationToken token);
}
public interface IParkingStore
{
    Task<ParkingReport?> Report(Guid property, DateOnly from, DateOnly through, AccessScope access, CancellationToken token);
    Task<MobileParkingSnapshot> MobileRead(MobileParkingContext context, CancellationToken token);
    Task<ResidentVehicleResponse> MobileRegister(MobileParkingContext context, ResidentVehicleRequest request, VehicleType type, CancellationToken token);
    Task<ParkingPage<ParkingClosureResponse>?> Closures(Guid property, int page, int pageSize, AccessScope access, CancellationToken token);
    Task<ParkingClosureResponse?> CreateClosure(Guid property, ParkingClosureRequest request, AccessScope access, CancellationToken token);
    Task<ParkingClosureResponse?> CancelClosure(Guid property, Guid id, ParkingClosureCancelRequest request, AccessScope access, CancellationToken token);
    Task<ParkingPolicyResponse?> Policy(Guid property, AccessScope access, CancellationToken token);
    Task<ParkingPolicyResponse?> SavePolicy(Guid property, ParkingPolicyRequest request, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<ParkingFacilityResponse>?> Facilities(Guid property, AccessScope access, CancellationToken token);
    Task<ParkingFacilityResponse?> SaveFacility(Guid property, Guid? id, ParkingFacilityRequest request, AccessScope access, CancellationToken token);
    Task<bool> DeleteFacility(Guid property, Guid id, long version, AccessScope access, CancellationToken token);
    Task<ParkingLevelResponse?> SaveLevel(Guid property, Guid? id, ParkingLevelRequest request, AccessScope access, CancellationToken token);
    Task<bool> DeleteLevel(Guid property, Guid id, long version, AccessScope access, CancellationToken token);
    Task<ParkingPage<ParkingSpaceResponse>?> Spaces(Guid property, ParkingSpaceQuery query, AccessScope access, CancellationToken token);
    Task<ParkingSpaceResponse?> SaveSpace(Guid property, Guid? id, ParkingSpaceRequest request, ParkingSpaceType type, AccessScope access, CancellationToken token);
    Task<bool> DeleteSpace(Guid property, Guid id, long version, AccessScope access, CancellationToken token);
    Task<ParkingPage<VehicleResponse>?> Vehicles(Guid property, VehicleQuery query, AccessScope access, CancellationToken token);
    Task<VehicleResponse?> CreateVehicle(Guid property, VehicleRequest request, VehicleType type, AccessScope access, CancellationToken token);
    Task<VehicleResponse?> EditVehicle(Guid property, Guid id, VehicleEditRequest request, VehicleType type, AccessScope access, CancellationToken token);
    Task<VehicleResponse?> ReviewVehicle(Guid property, Guid id, VehicleAction action, VehicleReviewRequest request, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<PlateResponse>?> Plates(Guid property, string search, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<ParkingUnitOption>?> Units(Guid property, string? search, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<ParkingResidentOption>?> Residents(Guid property, Guid unit, AccessScope access, CancellationToken token);
    Task<ParkingPage<ParkingAllocationResponse>?> Allocations(Guid property, ParkingAllocationQuery query, AccessScope access, CancellationToken token);
    Task<ParkingAllocationResponse?> Allocate(Guid property, ParkingAllocationRequest request, AccessScope access, CancellationToken token);
    Task<ParkingAllocationResponse?> EndAllocation(Guid property, Guid id, ParkingAllocationEndRequest request, AccessScope access, CancellationToken token);
    Task<ParkingPage<GuestVehicleResponse>?> Guests(Guid property, GuestVehicleQuery query, AccessScope access, CancellationToken token);
    Task<GuestVehicleResponse?> CreateGuest(Guid property, GuestVehicleRequest request, AccessScope access, CancellationToken token, MobileParkingContext? mobile = null);
    Task<GuestVehicleResponse?> ChangeGuest(Guid property, Guid id, GuestAction action, long version, AccessScope access, CancellationToken token, MobileParkingContext? mobile = null);
    Task<IReadOnlyList<ResidentVehicleResponse>> MyVehicles(Guid property, Guid unit, string userId, CancellationToken token);
    Task<ResidentVehicleResponse> RegisterMyVehicle(ResidentVehicleRequest request, VehicleType type, string userId, CancellationToken token);
    Task<ResidentVehicleResponse?> DeactivateMyVehicle(Guid property, Guid id, long version, string userId, CancellationToken token);
}

public sealed class ParkingService(IParkingStore store, ICurrentUserService user)
{
    public const int MaxPageSize = 100, MaxSearch = 50, MinPlateSearch = 2;
    private static readonly string[] Managers = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER"];
    public static readonly string[] Guards = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "SECURITY"];

    public Task<ParkingReport?> Report(Guid property, DateOnly from, DateOnly through, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE);
        if (from.Year < 2000 || through.Year > 9998 || through < from || through.DayNumber - from.DayNumber > 365)
            throw new RequestValidationException(new() { ["range"] = [ErrorCatalog.ParkingInvalid.Create()] });
        return store.Report(property, from, through, access, token);
    }
    private AccessScope Access(ParkingRole role)
    {
        var roles = role == ParkingRole.MANAGE ? Managers : Guards;
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !roles.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    private string Resident()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) ||
            !Managers.Concat(new[] { "OWNER", "TENANT", "RESIDENT" }).Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return user.UserId;
    }
    private static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.ParkingInvalid.Create()] });
    private static void Page(int page, int size)
    {
        if (page is < 1 or > 1000000 || size is < 1 or > MaxPageSize) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] });
    }
    private static void Version(long version, bool create) { if (create ? version != 0 : version < 1) Invalid("version"); }
    private static T Parse<T>(string? value, string field) where T : struct, Enum
    {
        if (value is null || !Enum.GetNames<T>().Contains(value)) Invalid(field);
        return Enum.Parse<T>(value!);
    }
    private static void Plate(string? plate)
    {
        try { VehiclePlate.Normalize(plate!); } catch (ArgumentException) { Invalid("plate"); }
    }
    private static void Text(string? value, int max, string field) { if (value?.Trim().Length > max) Invalid(field); }
    private static void Details(string? brand, string? model, string? color)
    {
        Text(brand, Vehicle.TextLength, "brand"); Text(model, Vehicle.TextLength, "model"); Text(color, Vehicle.TextLength, "color");
    }

    private static void MobileContext(MobileParkingContext q)
    {
        Page(q.Page, 20);
        if(q.PropertyId == Guid.Empty || q.UnitId == Guid.Empty || q.Role is not ("SUPER_ADMIN" or "SITE_ADMIN" or "MANAGER" or "ACCOUNTANT" or "OWNER" or "TENANT" or "RESIDENT")) Invalid("role");
        if((q.Role is "OWNER" or "TENANT" or "RESIDENT") != q.UnitId.HasValue) Invalid("unitId");
    }
    public Task<MobileParkingSnapshot> MobileRead(MobileParkingContext q, CancellationToken token)
    { MobileContext(q); return store.MobileRead(q, token); }
    public Task<ResidentVehicleResponse> MobileRegister(MobileParkingContext q, ResidentVehicleRequest r, CancellationToken token)
    {
        MobileContext(q);
        if(q.UnitId is null || r.PropertyId != q.PropertyId || r.UnitId != q.UnitId || r.RequestId == Guid.Empty) Invalid("unitId");
        Plate(r.Plate); Details(r.Brand,r.Model,r.Color);
        return store.MobileRegister(q,r,Parse<VehicleType>(r.Type,"type"),token);
    }
    public Task<GuestVehicleResponse?> MobileGuest(MobileParkingContext q, GuestVehicleRequest r, CancellationToken token)
    {
        MobileContext(q); var access = Access(ParkingRole.MANAGE);
        ValidateGuest(r);
        return store.CreateGuest(q.PropertyId,r with {StartsAt=r.StartsAt.ToUniversalTime(),EndsAt=r.EndsAt.ToUniversalTime()},access,token,q);
    }
    public Task<GuestVehicleResponse?> MobileGuestAction(MobileParkingContext q, Guid id, GuestAction action, long version, CancellationToken token)
    { MobileContext(q); var access=Access(ParkingRole.MANAGE); Version(version,false); return store.ChangeGuest(q.PropertyId,id,action,version,access,token,q); }

    public Task<ParkingPage<ParkingClosureResponse>?> Closures(Guid property, int page, int pageSize, CancellationToken token)
    { var access = Access(ParkingRole.GUARD); Page(page, pageSize); return store.Closures(property, page, pageSize, access, token); }
    public Task<ParkingClosureResponse?> CreateClosure(Guid property, ParkingClosureRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE);
        if (request.RequestId == Guid.Empty || request.SpaceId == Guid.Empty) Invalid("spaceId");
        if (request.StartsAt.Year is < 2000 or > 2100 || request.EndsAt.Year > 2100 || request.EndsAt <= request.StartsAt) Invalid("endsAt");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) Invalid("reason");
        return store.CreateClosure(property, request with { StartsAt = request.StartsAt.ToUniversalTime(), EndsAt = request.EndsAt.ToUniversalTime() }, access, token);
    }
    public Task<ParkingClosureResponse?> CancelClosure(Guid property, Guid id, ParkingClosureCancelRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Version(request.Version, false);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) Invalid("reason");
        return store.CancelClosure(property, id, request, access, token);
    }

    public async Task<ParkingGuestPolicyResponse?> GuestPolicy(Guid property, CancellationToken token)
    {
        var value = await store.Policy(property, Access(ParkingRole.GUARD), token);
        return value is null ? null : new(value.Configured, value.GuestMaxHours);
    }
    public Task<ParkingPage<ParkingSpaceResponse>?> GuestSpaces(Guid property, int page, CancellationToken token)
    { var access = Access(ParkingRole.GUARD); Page(page, MaxPageSize); return store.Spaces(property, new(Type: "VISITOR", Page: page, PageSize: MaxPageSize), access, token); }

    public Task<ParkingPolicyResponse?> Policy(Guid property, CancellationToken token) => store.Policy(property, Access(ParkingRole.MANAGE), token);
    public Task<ParkingPolicyResponse?> SavePolicy(Guid property, ParkingPolicyRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE);
        if (request.Version < 0) Invalid("version");
        try { _ = new ParkingPolicy(property == Guid.Empty ? Guid.NewGuid() : property, request.MaxVehiclesPerUnit, request.MaxVehiclesPerPerson, request.MaxSpacesPerUnit, request.GuestMaxHours, request.RetentionDays); }
        catch (ArgumentException) { Invalid("policy"); }
        return store.SavePolicy(property, request, access, token);
    }

    public Task<IReadOnlyList<ParkingFacilityResponse>?> Facilities(Guid property, CancellationToken token) => store.Facilities(property, Access(ParkingRole.MANAGE), token);
    public Task<ParkingFacilityResponse?> SaveFacility(Guid property, Guid? id, ParkingFacilityRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Version(request.Version, id is null);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > ParkingFacility.NameLength) Invalid("name");
        if (request.BuildingId == Guid.Empty) Invalid("buildingId");
        return store.SaveFacility(property, id, request, access, token);
    }
    public Task<bool> DeleteFacility(Guid property, Guid id, long version, CancellationToken token)
    { var access = Access(ParkingRole.MANAGE); Version(version, false); return store.DeleteFacility(property, id, version, access, token); }
    public Task<ParkingLevelResponse?> SaveLevel(Guid property, Guid? id, ParkingLevelRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Version(request.Version, id is null);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > ParkingLevel.NameLength) Invalid("name");
        if (request.SortOrder is < ParkingLevel.MinOrder or > ParkingLevel.MaxOrder) Invalid("sortOrder");
        return store.SaveLevel(property, id, request, access, token);
    }
    public Task<bool> DeleteLevel(Guid property, Guid id, long version, CancellationToken token)
    { var access = Access(ParkingRole.MANAGE); Version(version, false); return store.DeleteLevel(property, id, version, access, token); }

    public Task<ParkingPage<ParkingSpaceResponse>?> Spaces(Guid property, ParkingSpaceQuery query, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Page(query.Page, query.PageSize);
        if (query.Type is not null) Parse<ParkingSpaceType>(query.Type, "type");
        return store.Spaces(property, query, access, token);
    }
    public Task<ParkingSpaceResponse?> SaveSpace(Guid property, Guid? id, ParkingSpaceRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Version(request.Version, id is null);
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > ParkingSpace.CodeLength) Invalid("code");
        if (request.LevelId == Guid.Empty) Invalid("levelId");
        return store.SaveSpace(property, id, request, Parse<ParkingSpaceType>(request.Type, "type"), access, token);
    }
    public Task<bool> DeleteSpace(Guid property, Guid id, long version, CancellationToken token)
    { var access = Access(ParkingRole.MANAGE); Version(version, false); return store.DeleteSpace(property, id, version, access, token); }

    public Task<ParkingPage<VehicleResponse>?> Vehicles(Guid property, VehicleQuery query, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Page(query.Page, query.PageSize);
        if (query.Status is not null) Parse<VehicleStatus>(query.Status, "status");
        Text(query.Search, MaxSearch, "search");
        return store.Vehicles(property, query, access, token);
    }
    public Task<VehicleResponse?> CreateVehicle(Guid property, VehicleRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE);
        if (request.RequestId == Guid.Empty) Invalid("requestId");
        if (request.PersonId == Guid.Empty) Invalid("personId");
        if (request.UnitId == Guid.Empty) Invalid("unitId");
        Plate(request.Plate); Details(request.Brand, request.Model, request.Color);
        return store.CreateVehicle(property, request, Parse<VehicleType>(request.Type, "type"), access, token);
    }
    public Task<VehicleResponse?> EditVehicle(Guid property, Guid id, VehicleEditRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Version(request.Version, false); Details(request.Brand, request.Model, request.Color);
        return store.EditVehicle(property, id, request, Parse<VehicleType>(request.Type, "type"), access, token);
    }
    public Task<VehicleResponse?> ReviewVehicle(Guid property, Guid id, VehicleAction action, VehicleReviewRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Version(request.Version, false);
        if (action == VehicleAction.REJECT && (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > Vehicle.ReasonLength)) Invalid("reason");
        return store.ReviewVehicle(property, id, action, request, access, token);
    }
    public Task<IReadOnlyList<PlateResponse>?> Plates(Guid property, string? search, CancellationToken token)
    {
        var access = Access(ParkingRole.GUARD);
        var normalized = new string((search ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (normalized.Length is < MinPlateSearch or > VehiclePlate.MaxLength) Invalid("search");
        return store.Plates(property, normalized, access, token);
    }

    public Task<IReadOnlyList<ParkingUnitOption>?> Units(Guid property, string? search, CancellationToken token)
    {
        var access = Access(ParkingRole.GUARD); Text(search, MaxSearch, "search");
        return store.Units(property, search?.Trim(), access, token);
    }
    public Task<IReadOnlyList<ParkingResidentOption>?> Residents(Guid property, Guid unit, CancellationToken token) =>
        store.Residents(property, unit, Access(ParkingRole.MANAGE), token);

    public Task<ParkingPage<ParkingAllocationResponse>?> Allocations(Guid property, ParkingAllocationQuery query, CancellationToken token)
    { var access = Access(ParkingRole.MANAGE); Page(query.Page, query.PageSize); return store.Allocations(property, query, access, token); }
    public Task<ParkingAllocationResponse?> Allocate(Guid property, ParkingAllocationRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE);
        if (request.SpaceId == Guid.Empty) Invalid("spaceId");
        if (request.UnitId == Guid.Empty) Invalid("unitId");
        if (request.VehicleId == Guid.Empty) Invalid("vehicleId");
        if (request.StartDate.Year is < 2000 or > 2100 || request.EndDate < request.StartDate || request.EndDate?.Year > 2100) Invalid("startDate");
        return store.Allocate(property, request, access, token);
    }
    public Task<ParkingAllocationResponse?> EndAllocation(Guid property, Guid id, ParkingAllocationEndRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.MANAGE); Version(request.Version, false);
        if (request.EndDate.Year is < 2000 or > 2100) Invalid("endDate");
        return store.EndAllocation(property, id, request, access, token);
    }

    public Task<ParkingPage<GuestVehicleResponse>?> Guests(Guid property, GuestVehicleQuery query, CancellationToken token)
    {
        var access = Access(ParkingRole.GUARD); Page(query.Page, query.PageSize);
        if (query.Status is not null) Parse<GuestVehicleStatus>(query.Status, "status");
        if (query.From > query.To) Invalid("from");
        return store.Guests(property, query with { From = query.From?.ToUniversalTime(), To = query.To?.ToUniversalTime() }, access, token);
    }
    public Task<GuestVehicleResponse?> CreateGuest(Guid property, GuestVehicleRequest request, CancellationToken token)
    {
        var access = Access(ParkingRole.GUARD);
        ValidateGuest(request);
        return store.CreateGuest(property, request with { StartsAt = request.StartsAt.ToUniversalTime(), EndsAt = request.EndsAt.ToUniversalTime() }, access, token);
    }
    private static void ValidateGuest(GuestVehicleRequest request)
    {
        if (request.RequestId == Guid.Empty) Invalid("requestId");
        if (request.HostUnitId == Guid.Empty) Invalid("hostUnitId");
        if (request.SpaceId == Guid.Empty) Invalid("spaceId");
        if (request.EndsAt <= request.StartsAt || request.StartsAt.Year is < 2000 or > 2100 || request.EndsAt.Year > 2100) Invalid("endsAt");
        Plate(request.Plate); Text(request.GuestName, GuestVehicle.NameLength, "guestName"); Text(request.Note, GuestVehicle.NoteLength, "note");
    }
    public Task<GuestVehicleResponse?> ChangeGuest(Guid property, Guid id, GuestAction action, long version, CancellationToken token)
    { var access = Access(ParkingRole.GUARD); Version(version, false); return store.ChangeGuest(property, id, action, version, access, token); }

    public Task<IReadOnlyList<ResidentVehicleResponse>> MyVehicles(Guid property, Guid unit, CancellationToken token)
    {
        var userId = Resident();
        if (property == Guid.Empty || unit == Guid.Empty) Invalid("unitId");
        return store.MyVehicles(property, unit, userId, token);
    }
    public Task<ResidentVehicleResponse> RegisterMyVehicle(ResidentVehicleRequest request, CancellationToken token)
    {
        var userId = Resident();
        if (request.RequestId == Guid.Empty) Invalid("requestId");
        if (request.PropertyId == Guid.Empty || request.UnitId == Guid.Empty) Invalid("unitId");
        Plate(request.Plate); Details(request.Brand, request.Model, request.Color);
        return store.RegisterMyVehicle(request, Parse<VehicleType>(request.Type, "type"), userId, token);
    }
    public Task<ResidentVehicleResponse?> DeactivateMyVehicle(Guid property, Guid id, long version, CancellationToken token)
    { var userId = Resident(); Version(version, false); return store.DeactivateMyVehicle(property, id, version, userId, token); }
}
