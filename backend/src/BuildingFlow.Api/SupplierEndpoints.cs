using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v2/supplier-workspaces", async (ISupplierStore store, CancellationToken token, int page = 1, int pageSize = 20) => Results.Ok(await store.Workspaces(page, pageSize, token))).RequireAuthorization("CanManagePeople").WithTags("Suppliers");
        var catalog = app.MapGroup("/api/v2/tenants/{tenantId:guid}/supplier-categories").RequireAuthorization("CanManagePeople").WithTags("Suppliers");
        catalog.MapGet("", async (Guid tenantId, ISupplierRelationsStore store, CancellationToken token, int page=1, int pageSize=20) => Found(await store.Categories(tenantId,page,pageSize,token)));
        catalog.MapGet("/manage", async (Guid tenantId, ISupplierRelationsStore store, CancellationToken token, int page=1, int pageSize=20) => Found(await store.ManageCategories(tenantId,page,pageSize,token)));
        catalog.MapPut("/{id:guid}", async (Guid tenantId, Guid id, SupplierCategoryUpdate request, ISupplierRelationsStore store, CancellationToken token) => Found(await store.SaveCategory(tenantId,id,request,token)));
        catalog.MapPost("", async (Guid tenantId, SupplierCategoryRequest request, ISupplierRelationsStore store, CancellationToken token) => Found(await store.CreateCategory(tenantId,request,token)));
        app.MapGet("/api/v2/tenants/{tenantId:guid}/supplier-property-choices", async (Guid tenantId, ISupplierRelationsStore store, CancellationToken token, int page=1, int pageSize=20, Guid? supplierId=null) => Found(await store.Properties(tenantId,page,pageSize,token,supplierId))).RequireAuthorization("CanManagePeople").WithTags("Suppliers");
        var root = app.MapGroup("/api/v2/tenants/{tenantId:guid}/suppliers").RequireAuthorization("CanManagePeople").WithTags("Suppliers");
        root.MapGet("", async (Guid tenantId, SupplierService service, CancellationToken token, int page = 1, int pageSize = 10, string? search = null, string? status = null, string sortBy = "name", string sortDirection = "asc") => Found(await service.List(tenantId, new(page, pageSize, search, status, sortBy, sortDirection), token)));
        root.MapGet("/{supplierId:guid}/areas", async (Guid tenantId, Guid supplierId, ISupplierRelationsStore store, CancellationToken token, int page=1, int pageSize=10) => Found(await store.Areas(tenantId,supplierId,page,pageSize,token)));
        root.MapPut("/{supplierId:guid}/areas", async (Guid tenantId, Guid supplierId, SupplierAreaRequest request, ISupplierRelationsStore store, CancellationToken token) => Found(await store.Link(tenantId,supplierId,request,token)));
        root.MapGet("/{supplierId:guid}", async (Guid tenantId, Guid supplierId, SupplierService service, CancellationToken token) => Found(await service.Get(tenantId, supplierId, token)));
        root.MapPost("", async (Guid tenantId, SupplierRequest request, SupplierService service, CancellationToken token) => Found(await service.Save(tenantId, null, request, token)));
        root.MapPut("/{supplierId:guid}", async (Guid tenantId, Guid supplierId, SupplierRequest request, SupplierService service, CancellationToken token) => Found(await service.Save(tenantId, supplierId, request, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
