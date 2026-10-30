using BuildingFlow.Domain;
using System.Text.Json.Serialization;
namespace BuildingFlow.Application;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierRequest(Guid OperationId, string? Name, string? Kind, string? Description = null, long Version = 0, string Status = "DRAFT", string? StatusReason = null);
public sealed record SupplierResponse(Guid Id, Guid TenantId, string Name, string Kind, string? Description, string Status, string? StatusReason, bool IsEnabled, long Version, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);
public sealed record SupplierMutation(SupplierResponse Record, bool Replayed, long AppliedVersion);
public sealed record SupplierWorkspace(Guid Id, string Name);
public sealed record SupplierQuery(int Page = 1, int PageSize = 10, string? Search = null, string? Status = null, string SortBy = "name", string SortDirection = "asc");
public interface ISupplierStore
{
    Task<PagedRows<SupplierWorkspace>> Workspaces(int page, int pageSize, CancellationToken token);
    Task<PagedRows<SupplierResponse>?> List(Guid tenant, SupplierQuery query, CancellationToken token);
    Task<SupplierResponse?> Get(Guid tenant, Guid id, CancellationToken token);
    Task<SupplierMutation?> Save(Guid tenant, Guid? id, SupplierRequest command, CancellationToken token);
}
public sealed class SupplierService(ISupplierStore store)
{
    private static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.SupplierInvalid.Create()] });
    public Task<PagedRows<SupplierResponse>?> List(Guid tenant, SupplierQuery query, CancellationToken token)
    {
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100) Invalid("page");
        if (query.Search?.Length > 200) Invalid("search");
        if (query.SortBy is not ("name" or "createdAt") || query.SortDirection is not ("asc" or "desc")) Invalid("sortBy");
        if (query.Status is not null && !Enum.GetNames<SupplierStatus>().Contains(query.Status)) Invalid("status");
        return store.List(tenant, query, token);
    }
    public Task<SupplierResponse?> Get(Guid tenant, Guid id, CancellationToken token) => store.Get(tenant, id, token);
    public Task<SupplierMutation?> Save(Guid tenant, Guid? id, SupplierRequest c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || (id.HasValue ? c.Version < 1 : c.Version != 0)) Invalid("version");
        if (string.IsNullOrWhiteSpace(c.Name) || c.Name.Trim().Length > 200 || c.Name.Any(char.IsControl)) Invalid("name");
        if (c.Kind is null || !Enum.GetNames<SupplierKind>().Contains(c.Kind)) Invalid("kind");
        if (!Enum.GetNames<SupplierStatus>().Contains(c.Status)) Invalid("status");
        if (c.Description?.Trim().Length > 2000 || c.Description?.Any(char.IsControl) == true) Invalid("description");
        if (c.StatusReason?.Trim().Length > 1000 || c.StatusReason?.Any(char.IsControl) == true || c.Status is "INACTIVE" or "BLOCKED" && string.IsNullOrWhiteSpace(c.StatusReason)) Invalid("statusReason");
        return store.Save(tenant, id, c with { Name = c.Name!.Trim(), Description = Clean(c.Description), StatusReason = Clean(c.StatusReason) }, token);
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierCategoryRequest(Guid OperationId, string Name);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierAreaRequest(Guid OperationId, long Version, Guid PropertyId, Guid CategoryId, string ServiceName);
public sealed record SupplierAreaRow(Guid Id, Guid TenantId, Guid SupplierId, Guid PropertyId, string PropertyName, Guid CategoryId, string CategoryName, string ServiceName);
public interface ISupplierRelationsStore
{
    Task<PagedRows<SupplierWorkspace>?> Categories(Guid tenant, int page, int pageSize, CancellationToken token);
    Task<SupplierWorkspace?> CreateCategory(Guid tenant, SupplierCategoryRequest request, CancellationToken token);
    Task<PagedRows<SupplierWorkspace>?> Properties(Guid tenant, int page, int pageSize, CancellationToken token, Guid? supplier = null);
    Task<PagedRows<SupplierAreaRow>?> Areas(Guid tenant, Guid supplier, int page, int pageSize, CancellationToken token);
    Task<SupplierMutation?> Link(Guid tenant, Guid supplier, SupplierAreaRequest request, CancellationToken token);
}
