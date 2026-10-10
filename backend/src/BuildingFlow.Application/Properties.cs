using BuildingFlow.Domain;

namespace BuildingFlow.Application;

public sealed record CreatePropertyRequest(string? Name, string? City, string? District, string? Address, string? Type = null, int? FloorCount = null);
public sealed record PropertyResponse(Guid Id, string Name, string City, string District, string Address,
    long Version, DateTimeOffset CreatedAt, bool IsEnabled, string Type, int? FloorCount, int BuildingCount, int UnitCount, bool CanChangeType);
public sealed record PropertyListQuery(int Page = 1, int PageSize = 12, string? Search = null, string SortBy = "name", string SortDirection = "asc");
public sealed record PagedProperties(IReadOnlyList<PropertyResponse> Items, int Page, int PageSize, int TotalCount);
public sealed class RequestValidationException(Dictionary<string, ApiErrorMessage[]> issues) : Exception(ErrorCatalog.ValidationFailed.Message)
{
    public IReadOnlyDictionary<string, ApiErrorMessage[]> Issues { get; } = issues;
    public Dictionary<string, string[]> Errors { get; } = issues.ToDictionary(value => value.Key, value => value.Value.Select(issue => issue.Message).ToArray());
}

public interface IPropertyStore
{
    Task SaveNewAsync(Property site, string userId, CancellationToken cancellationToken);
    Task<PagedProperties> ListAsync(string userId, bool readAll, PropertyListQuery query, CancellationToken cancellationToken);
    Task<PropertyResponse?> GetAsync(Guid id, string userId, bool readAll, CancellationToken cancellationToken);
}
public interface IPropertyService
{
    Task<PropertyResponse> CreateAsync(CreatePropertyRequest request, CancellationToken cancellationToken);
    Task<PagedProperties> ListAsync(PropertyListQuery query, CancellationToken cancellationToken);
    Task<PropertyResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class PropertyService(IPropertyStore store, ICurrentUserService currentUser) : IPropertyService
{
    private string UserId => currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(currentUser.UserId)
        ? currentUser.UserId : throw new UnauthorizedAccessException();

    public async Task<PropertyResponse> CreateAsync(CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        var actor = UserId;
        if (!currentUser.IsInRole("SUPER_ADMIN")) throw new UnauthorizedAccessException();
        Dictionary<string, ApiErrorMessage[]> errors = [];
        Validate(errors, "name", request.Name, "Yönetim alanı adı", 200);
        Validate(errors, "city", request.City, "Şehir", 100);
        Validate(errors, "district", request.District, "İlçe", 100);
        Validate(errors, "address", request.Address, "Adres", 500);
        if (!Enum.TryParse<PropertyType>(request.Type, out var type) || !Enum.IsDefined(type) || request.Type != type.ToString()) errors["type"] = [ErrorCatalog.ErrorsValidPropertyType.Create()];
        if (request.FloorCount is <= 0 || (type != PropertyType.APARTMENT && request.FloorCount is not null)) errors["floorCount"] = [ErrorCatalog.ErrorsApartmentPositiveFloors.Create()];
        if (errors.Count > 0) throw new RequestValidationException(errors);
        var site = new Property(request.Name!, request.City!, request.District!, request.Address!, type, request.FloorCount);
        await store.SaveNewAsync(site, actor, cancellationToken);
        return new(site.Id, site.Name, site.City, site.District, site.Address, site.Version, site.CreatedAt, site.IsEnabled, site.Type.ToString(), site.FloorCount, 0, 0, true);
    }

    public Task<PagedProperties> ListAsync(PropertyListQuery query, CancellationToken cancellationToken)
    {
        var actor = UserId;
        Dictionary<string, ApiErrorMessage[]> errors = [];
        if (query.Page is < 1 or > 1000000) errors["page"] = [ErrorCatalog.ErrorsPageBounds.Create()];
        if (query.PageSize is < 1 or > 100) errors["pageSize"] = [ErrorCatalog.ErrorsPageSizeBounds.Create()];
        if (query.Search?.Trim().Length > 200) errors["search"] = [ErrorCatalog.ErrorsSearchBounds.Create()];
        if (query.SortBy is not ("name" or "city" or "createdAt")) errors["sortBy"] = [ErrorCatalog.InvalidSortField.Create()];
        if (query.SortDirection is not ("asc" or "desc")) errors["sortDirection"] = [ErrorCatalog.InvalidSortDirection.Create()];
        if (errors.Count > 0) throw new RequestValidationException(errors);
        return store.ListAsync(actor, currentUser.IsInRole("SUPER_ADMIN"), query, cancellationToken);
    }

    public Task<PropertyResponse?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        store.GetAsync(id, UserId, currentUser.IsInRole("SUPER_ADMIN"), cancellationToken);

    private static void Validate(Dictionary<string, ApiErrorMessage[]> errors, string field, string? value, string label, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[field] = [ErrorCatalog.FieldRequired.Create(("field", ErrorCatalog.FieldKey(label)))];
        else if (value.Trim().Length > max) errors[field] = [ErrorCatalog.FieldTooLong.Create(("field", ErrorCatalog.FieldKey(label)), ("max", max))];
    }
}
