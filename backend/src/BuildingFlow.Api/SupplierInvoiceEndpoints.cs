using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SupplierInvoiceEndpoints
{
    public static void MapSupplierInvoiceEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v2/tenants/{tenantId:guid}/suppliers/{supplierId:guid}/invoices").RequireAuthorization("CanManageDues").WithTags("Supplier invoices");
        root.MapGet("", async (Guid tenantId, Guid supplierId, ISupplierInvoiceStore store, CancellationToken token, string? status = null, int page = 1, int pageSize = 10) => Found(await store.Invoices(tenantId, supplierId, status, page, pageSize, token)));
        root.MapPost("", async (Guid tenantId, Guid supplierId, SupplierInvoiceCreate request, ISupplierInvoiceStore store, CancellationToken token) => Found(await store.CreateInvoice(tenantId, supplierId, request, token)));
        root.MapGet("/{invoiceId:guid}", async (Guid tenantId, Guid supplierId, Guid invoiceId, ISupplierInvoiceStore store, CancellationToken token) => Found(await store.Invoice(tenantId, supplierId, invoiceId, token)));
        root.MapGet("/{invoiceId:guid}/history", async (Guid tenantId, Guid supplierId, Guid invoiceId, ISupplierInvoiceStore store, CancellationToken token, int page = 1, int pageSize = 10) => Found(await store.InvoiceHistory(tenantId, supplierId, invoiceId, page, pageSize, token)));
        foreach (var action in new[] { "approve", "pay", "reconcile", "cancel" })
            root.MapPut("/{invoiceId:guid}/" + action, async (Guid tenantId, Guid supplierId, Guid invoiceId, SupplierInvoiceAction request, ISupplierInvoiceStore store, CancellationToken token) => Found(await store.TransitionInvoice(tenantId, supplierId, invoiceId, action, request, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
