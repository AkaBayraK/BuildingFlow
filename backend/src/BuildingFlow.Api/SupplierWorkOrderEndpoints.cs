using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SupplierWorkOrderEndpoints
{
    public static void MapSupplierWorkOrderEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v2/tenants/{tenantId:guid}/suppliers/{supplierId:guid}").RequireAuthorization("CanManagePeople").WithTags("Supplier work orders");
        root.MapGet("/work-order-sources", async (Guid tenantId, Guid supplierId, Guid propertyId, string source, ISupplierWorkOrderStore store, CancellationToken token, int page = 1, int pageSize = 10) => Found(await store.WorkOrderSources(tenantId, supplierId, propertyId, source, page, pageSize, token)));
        root.MapGet("/work-orders", async (Guid tenantId, Guid supplierId, ISupplierWorkOrderStore store, CancellationToken token, string? status = null, int page = 1, int pageSize = 10) => Found(await store.WorkOrders(tenantId, supplierId, status, page, pageSize, token)));
        root.MapPost("/work-orders", async (Guid tenantId, Guid supplierId, SupplierWorkOrderCreate request, ISupplierWorkOrderStore store, CancellationToken token) => Found(await store.CreateWorkOrder(tenantId, supplierId, request, token)));
        root.MapGet("/work-orders/{orderId:guid}", async (Guid tenantId, Guid supplierId, Guid orderId, ISupplierWorkOrderStore store, CancellationToken token) => Found(await store.WorkOrder(tenantId, supplierId, orderId, token)));
        root.MapPut("/work-orders/{orderId:guid}", async (Guid tenantId, Guid supplierId, Guid orderId, SupplierWorkOrderEdit request, ISupplierWorkOrderStore store, CancellationToken token) => Found(await store.EditWorkOrder(tenantId, supplierId, orderId, request, token)));
        root.MapPut("/work-orders/{orderId:guid}/transition", async (Guid tenantId, Guid supplierId, Guid orderId, SupplierWorkOrderTransition request, ISupplierWorkOrderStore store, CancellationToken token) => Found(await store.TransitionWorkOrder(tenantId, supplierId, orderId, request, token)));
        root.MapGet("/work-orders/{orderId:guid}/history", async (Guid tenantId, Guid supplierId, Guid orderId, ISupplierWorkOrderStore store, CancellationToken token, int page = 1, int pageSize = 10) => Found(await store.WorkOrderHistory(tenantId, supplierId, orderId, page, pageSize, token)));
        root.MapGet("/work-orders/{orderId:guid}/photos", async (Guid tenantId, Guid supplierId, Guid orderId, ISupplierWorkOrderStore store, CancellationToken token, int page = 1, int pageSize = 10) => Found(await store.WorkOrderPhotos(tenantId, supplierId, orderId, page, pageSize, token)));
        root.MapPost("/work-orders/{orderId:guid}/photos", Upload);
    }
    private static async Task<IResult> Upload(Guid tenantId, Guid supplierId, Guid orderId, HttpRequest request, ISupplierWorkOrderStore store, CancellationToken token)
    {
        var upload = await CommunicationEndpoints.ReadUpload(request, token); var form = upload.Form;
        if (!Guid.TryParse(form["operationId"], out var operation) || !long.TryParse(form["version"], out var version)) { CommunicationService.Invalid("photo", ErrorCatalog.SupplierInvalid); return Results.BadRequest(); }
        return Found(await store.UploadWorkOrderPhoto(tenantId, supplierId, orderId, new(operation, version, upload.File.FileName, upload.File.ContentType, upload.Bytes, form["caption"].ToString()), token));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
