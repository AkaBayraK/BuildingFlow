using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SupplierQuoteEndpoints
{
    public static void MapSupplierQuoteEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v2/tenants/{tenantId:guid}/suppliers/{supplierId:guid}").RequireAuthorization("CanManagePeople").WithTags("Supplier quotes");
        root.MapGet("/quote-candidates", async (Guid tenantId, Guid supplierId, Guid propertyId, Guid categoryId, ISupplierQuoteStore store, CancellationToken token, int page = 1, int pageSize = 20) => Found(await store.QuoteCandidates(tenantId, supplierId, propertyId, categoryId, page, pageSize, token)));
        root.MapGet("/inquiries", async (Guid tenantId, Guid supplierId, ISupplierQuoteStore store, CancellationToken token, int page = 1, int pageSize = 10) => Found(await store.Inquiries(tenantId, supplierId, page, pageSize, token)));
        root.MapPost("/inquiries", async (Guid tenantId, Guid supplierId, SupplierInquiryRequest request, ISupplierQuoteStore store, CancellationToken token) => Found(await store.CreateInquiry(tenantId, supplierId, request, token)));
        root.MapGet("/inquiries/{inquiryId:guid}", async (Guid tenantId, Guid supplierId, Guid inquiryId, ISupplierQuoteStore store, CancellationToken token) => Found(await store.Inquiry(tenantId, supplierId, inquiryId, token)));
        root.MapPut("/inquiries/{inquiryId:guid}/quotes", async (Guid tenantId, Guid supplierId, Guid inquiryId, SupplierQuoteRequest request, ISupplierQuoteStore store, CancellationToken token) => Found(await store.EnterQuote(tenantId, supplierId, inquiryId, request, token)));
        root.MapPut("/inquiries/{inquiryId:guid}/quotes/{quoteId:guid}/approve", async (Guid tenantId, Guid supplierId, Guid inquiryId, Guid quoteId, SupplierInquiryAction request, ISupplierQuoteStore store, CancellationToken token) => Found(await store.ActOnInquiry(tenantId, supplierId, inquiryId, quoteId, "APPROVE", request, token)));
        root.MapPut("/inquiries/{inquiryId:guid}/quotes/{quoteId:guid}/reject", async (Guid tenantId, Guid supplierId, Guid inquiryId, Guid quoteId, SupplierInquiryAction request, ISupplierQuoteStore store, CancellationToken token) => Found(await store.ActOnInquiry(tenantId, supplierId, inquiryId, quoteId, "REJECT", request, token)));
        root.MapPut("/inquiries/{inquiryId:guid}/cancel", async (Guid tenantId, Guid supplierId, Guid inquiryId, SupplierInquiryAction request, ISupplierQuoteStore store, CancellationToken token) => Found(await store.ActOnInquiry(tenantId, supplierId, inquiryId, null, "CANCEL", request, token)));
        root.MapPut("/inquiries/{inquiryId:guid}/convert", async (Guid tenantId, Guid supplierId, Guid inquiryId, SupplierInquiryAction request, ISupplierQuoteStore store, CancellationToken token) => Found(await store.ActOnInquiry(tenantId, supplierId, inquiryId, null, "CONVERT", request, token)));
        root.MapGet("/inquiries/{inquiryId:guid}/history", async (Guid tenantId, Guid supplierId, Guid inquiryId, ISupplierQuoteStore store, CancellationToken token, int page = 1, int pageSize = 10) => Found(await store.InquiryHistory(tenantId, supplierId, inquiryId, page, pageSize, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
