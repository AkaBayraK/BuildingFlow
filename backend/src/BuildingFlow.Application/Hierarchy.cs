using System.Text.Json.Serialization;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record AccessScope(string UserId, bool ReadAll, bool ReadFinancialParties = false);
public sealed record HierarchyQuery(int Page = 1, int PageSize = 10, string? Search = null, string SortBy = "name", string SortDirection = "asc", Guid? UnitId = null);
public sealed record PagedRows<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record CreateBuildingRequest(string? Name, int? FloorCount = null, int? ConstructionYear = null, string? HeatingType = null, bool? HasElevator = null, string? ParkingType = null)
{
    [JsonIgnore] public BuildingFeatureInput Features => new(ConstructionYear, HeatingType, HasElevator, ParkingType);
}
public sealed record UpdateBuildingRequest(string? Name, bool? IsEnabled, long Version, int? FloorCount = null, int? ConstructionYear = null, string? HeatingType = null, bool? HasElevator = null, string? ParkingType = null)
{
    [JsonIgnore] public BuildingFeatureInput Features => new(ConstructionYear, HeatingType, HasElevator, ParkingType);
}
public sealed record CreateUnitRequest(string? Number, int? Floor, decimal? GrossSquareMeters, string Type = "APARTMENT",
    decimal? NetSquareMeters = null, string? RoomLayout = null, string? FloorPosition = null, bool? IsMezzanine = null, int? BathroomCount = null, string? KitchenType = null, bool? HasBalcony = null, string? HeatingType = null)
{
    [JsonIgnore] public UnitFeatureInput Features => new(NetSquareMeters, RoomLayout, FloorPosition, IsMezzanine, BathroomCount, KitchenType, HasBalcony, HeatingType);
}
public sealed record UpdateUnitRequest(string? Number, int? Floor, decimal? GrossSquareMeters, bool? IsEnabled, long Version, string Type = "APARTMENT",
    decimal? NetSquareMeters = null, string? RoomLayout = null, string? FloorPosition = null, bool? IsMezzanine = null, int? BathroomCount = null, string? KitchenType = null, bool? HasBalcony = null, string? HeatingType = null)
{
    [JsonIgnore] public UnitFeatureInput Features => new(NetSquareMeters, RoomLayout, FloorPosition, IsMezzanine, BathroomCount, KitchenType, HasBalcony, HeatingType);
}
public sealed record UpdatePropertyRequest(string? Name, string? City, string? District, string? Address, bool? IsEnabled, long Version, int? FloorCount = null, string? Type = null,
    string? Neighborhood = null, int? ConstructionYear = null, string? HeatingType = null, bool? HasElevator = null, string? ParkingType = null)
{
    [JsonIgnore] public BuildingFeatureInput Features => new(ConstructionYear, HeatingType, HasElevator, ParkingType);
}
public sealed record BuildingResponse(Guid Id, Guid PropertyId, string Name, long Version, DateTimeOffset CreatedAt, bool IsEnabled, int UnitCount, int? FloorCount,
    int? ConstructionYear = null, string? HeatingType = null, bool? HasElevator = null, string? ParkingType = null);
public sealed record UnitResponse(Guid Id, Guid PropertyId, Guid? BuildingId, string Number, int Floor, decimal GrossSquareMeters, long Version, DateTimeOffset CreatedAt, bool IsEnabled, string Type,
    decimal? NetSquareMeters = null, string? RoomLayout = null, string? FloorPosition = null, bool? IsMezzanine = null, int? BathroomCount = null, string? KitchenType = null, bool? HasBalcony = null, string? HeatingType = null);
public sealed class HierarchyConflictException(ApiErrorMessage error) : Exception(error.Message)
{ public ApiErrorMessage Error { get; } = error; }

public interface IHierarchyStore
{
    Task<PropertyResponse?> UpdatePropertyAsync(Guid siteId, UpdatePropertyRequest request, AccessScope access, CancellationToken token);
    Task<bool> DeletePropertyAsync(Guid siteId, long version, AccessScope access, CancellationToken token);
    Task<PagedRows<BuildingResponse>?> ListBuildingsAsync(Guid siteId, HierarchyQuery query, AccessScope access, CancellationToken token);
    Task<BuildingResponse?> GetBuildingAsync(Guid siteId, Guid blockId, AccessScope access, CancellationToken token);
    Task<BuildingResponse?> CreateBuildingAsync(Guid siteId, CreateBuildingRequest request, AccessScope access, CancellationToken token);
    Task<BuildingResponse?> UpdateBuildingAsync(Guid siteId, Guid blockId, UpdateBuildingRequest request, AccessScope access, CancellationToken token);
    Task<bool> DeleteBuildingAsync(Guid siteId, Guid blockId, long version, AccessScope access, CancellationToken token);
    Task<PagedRows<UnitResponse>?> ListUnitsAsync(Guid siteId, Guid? blockId, HierarchyQuery query, AccessScope access, CancellationToken token);
    Task<UnitResponse?> GetUnitAsync(Guid siteId, Guid? blockId, Guid apartmentId, AccessScope access, CancellationToken token);
    Task<UnitResponse?> CreateUnitAsync(Guid siteId, Guid? blockId, CreateUnitRequest request, AccessScope access, CancellationToken token);
    Task<UnitResponse?> UpdateUnitAsync(Guid siteId, Guid? blockId, Guid apartmentId, UpdateUnitRequest request, AccessScope access, CancellationToken token);
    Task<bool> DeleteUnitAsync(Guid siteId, Guid? blockId, Guid apartmentId, long version, AccessScope access, CancellationToken token);
}

// HTTP and persistence concerns stay outside the application layer.
public sealed class HierarchyService(IHierarchyStore store, ICurrentUserService user, TimeProvider? clock = null)
{
    private AccessScope Access(bool write = false, bool siteWrite = false)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) throw new UnauthorizedAccessException();
        var global = user.IsInRole("SUPER_ADMIN");
        if ((siteWrite && !global) || (write && !global && !user.IsInRole("SITE_ADMIN") && !user.IsInRole("MANAGER")))
            throw new UnauthorizedAccessException();
        return new(user.UserId, global);
    }
    private static void Text(Dictionary<string, ApiErrorMessage[]> errors, string field, string? value, string label, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[field] = [ErrorCatalog.FieldRequired.Create(("field", ErrorCatalog.FieldKey(label)))];
        else if (value.Trim().Length > max) errors[field] = [ErrorCatalog.FieldTooLong.Create(("field", ErrorCatalog.FieldKey(label)), ("max", max))];
    }
    private static void Version(Dictionary<string, ApiErrorMessage[]> errors, long version, bool? enabled = true)
    {
        if (version < 1) errors["version"] = [ErrorCatalog.VersionRequired.Create()];
        if (enabled is null) errors["isEnabled"] = [ErrorCatalog.EnabledRequired.Create()];
    }
    private static void Check(Dictionary<string, ApiErrorMessage[]> errors)
    {
        if (errors.Count > 0) throw new RequestValidationException(errors);
    }
    private int CurrentYear => PropertyDetails.CurrentYear(clock ?? TimeProvider.System);
    private static void Unit(Dictionary<string, ApiErrorMessage[]> errors, string? number, int? floor, decimal? area, UnitFeatureInput features)
    {
        PropertyDetails.Validate(errors, features, floor, area is > 0 ? area : null);
        Text(errors, "number", number, "Daire numarası", 30);
        if (floor is null) errors["floor"] = [ErrorCatalog.ErrorsFloorRequired.Create()];
        if (area is null or <= 0 or > 99999999.99m || decimal.Round(area.GetValueOrDefault(), 2) != area)
            errors["grossSquareMeters"] = [ErrorCatalog.ErrorsAreaBounds.Create()];
    }
    private static void Query(HierarchyQuery query, bool apartment)
    {
        Dictionary<string, ApiErrorMessage[]> errors = [];
        if (query.Page is < 1 or > 1000000) errors["page"] = [ErrorCatalog.InvalidPage.Create()];
        if (apartment && query.UnitId == Guid.Empty) errors["unitId"] = [ErrorCatalog.InvalidRequest.Create()];
        if (query.PageSize is < 1 or > 100) errors["pageSize"] = [ErrorCatalog.PageSizeBounds.Create()];
        if (query.Search?.Trim().Length > 200) errors["search"] = [ErrorCatalog.ErrorsSearchBounds.Create()];
        if (query.SortDirection is not ("asc" or "desc")) errors["sortDirection"] = [ErrorCatalog.InvalidSortDirection.Create()];
        if (!(apartment ? new[] { "number", "floor", "grossSquareMeters", "createdAt" } : new[] { "name", "createdAt" }).Contains(query.SortBy))
            errors["sortBy"] = [ErrorCatalog.InvalidSortField.Create()];
        Check(errors);
    }
    public Task<PropertyResponse?> UpdatePropertyAsync(Guid id, UpdatePropertyRequest request, CancellationToken token)
    {
        var access = Access(siteWrite: true); Dictionary<string, ApiErrorMessage[]> errors = [];
        Text(errors,"name",request.Name,"Yönetim alanı adı",200); Text(errors,"city",request.City,"Şehir",100);
        Text(errors,"district",request.District,"İlçe",100); Text(errors,"address",request.Address,"Adres",500);
        if(request.FloorCount is <=0)errors["floorCount"]=[ErrorCatalog.UiTheNumberOfFloorsMustBePositive.Create()];
        PropertyDetails.ValidateNeighborhood(errors,request.Neighborhood); PropertyDetails.Validate(errors,request.Features,CurrentYear);
        if(request.Type is not null)
        {
            if(!Enum.TryParse<PropertyType>(request.Type,out var type) || !Enum.IsDefined(type) || request.Type!=type.ToString())errors["type"]=[ErrorCatalog.ErrorsValidPropertyType.Create()];
            else if(type!=PropertyType.APARTMENT && request.FloorCount is not null)errors["floorCount"]=[ErrorCatalog.ErrorsApartmentFloorsOnly.Create()];
            else if(type!=PropertyType.APARTMENT && !request.Features.IsEmpty)errors["buildingFeatures"]=[ErrorCatalog.BuildingFeaturesApartmentOnly.Create()];
        }
        Version(errors,request.Version,request.IsEnabled); Check(errors);
        return store.UpdatePropertyAsync(id,request,access,token);
    }
    public Task<bool> DeletePropertyAsync(Guid id, long version, CancellationToken token)
    {
        var access=Access(siteWrite:true); Dictionary<string,ApiErrorMessage[]> errors=[]; Version(errors,version); Check(errors);
        return store.DeletePropertyAsync(id,version,access,token);
    }
    public Task<PagedRows<BuildingResponse>?> ListBuildingsAsync(Guid id, HierarchyQuery query, CancellationToken token)
    { var access=Access(); Query(query,false); return store.ListBuildingsAsync(id,query,access,token); }
    public Task<BuildingResponse?> GetBuildingAsync(Guid id, Guid block, CancellationToken token) => store.GetBuildingAsync(id,block,Access(),token);
    public Task<BuildingResponse?> CreateBuildingAsync(Guid id, CreateBuildingRequest request, CancellationToken token)
    {
        var access=Access(write:true); Dictionary<string,ApiErrorMessage[]> errors=[]; Text(errors,"name",request.Name,"Bina/blok adı",100); if(request.FloorCount is <=0)errors["floorCount"]=[ErrorCatalog.UiTheNumberOfFloorsMustBePositive.Create()];
        PropertyDetails.Validate(errors,request.Features,CurrentYear); Check(errors);
        return store.CreateBuildingAsync(id,request,access,token);
    }
    public Task<BuildingResponse?> UpdateBuildingAsync(Guid id, Guid block, UpdateBuildingRequest request, CancellationToken token)
    {
        var access=Access(write:true); Dictionary<string,ApiErrorMessage[]> errors=[]; Text(errors,"name",request.Name,"Blok adı",100);
        if(request.FloorCount is <=0)errors["floorCount"]=[ErrorCatalog.UiTheNumberOfFloorsMustBePositive.Create()]; PropertyDetails.Validate(errors,request.Features,CurrentYear);
        Version(errors,request.Version,request.IsEnabled); Check(errors); return store.UpdateBuildingAsync(id,block,request,access,token);
    }
    public Task<bool> DeleteBuildingAsync(Guid id, Guid block, long version, CancellationToken token)
    {
        var access=Access(write:true); Dictionary<string,ApiErrorMessage[]> errors=[]; Version(errors,version); Check(errors);
        return store.DeleteBuildingAsync(id,block,version,access,token);
    }
    public Task<PagedRows<UnitResponse>?> ListUnitsAsync(Guid id, Guid? block, HierarchyQuery query, CancellationToken token)
    { var access=Access(); Query(query,true); return store.ListUnitsAsync(id,block,query,access,token); }
    public Task<UnitResponse?> GetUnitAsync(Guid id, Guid? block, Guid apartment, CancellationToken token) => store.GetUnitAsync(id,block,apartment,Access(),token);
    public Task<UnitResponse?> CreateUnitAsync(Guid id, Guid? block, CreateUnitRequest request, CancellationToken token)
    {
        var access=Access(write:true); Dictionary<string,ApiErrorMessage[]> errors=[]; Unit(errors,request.Number,request.Floor,request.GrossSquareMeters,request.Features); Check(errors);
        return store.CreateUnitAsync(id,block,request,access,token);
    }
    public Task<UnitResponse?> UpdateUnitAsync(Guid id, Guid? block, Guid apartment, UpdateUnitRequest request, CancellationToken token)
    {
        var access=Access(write:true); Dictionary<string,ApiErrorMessage[]> errors=[]; Unit(errors,request.Number,request.Floor,request.GrossSquareMeters,request.Features);
        Version(errors,request.Version,request.IsEnabled); Check(errors); return store.UpdateUnitAsync(id,block,apartment,request,access,token);
    }
    public Task<bool> DeleteUnitAsync(Guid id, Guid? block, Guid apartment, long version, CancellationToken token)
    {
        var access=Access(write:true); Dictionary<string,ApiErrorMessage[]> errors=[]; Version(errors,version); Check(errors);
        return store.DeleteUnitAsync(id,block,apartment,version,access,token);
    }
}
