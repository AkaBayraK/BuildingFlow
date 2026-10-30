using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SupplierInsightEndpoints
{
    public static void MapSupplierInsightEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v2/tenants/{tenantId:guid}").RequireAuthorization("CanManagePeople").WithTags("Supplier insights");
        root.MapGet("/supplier-alerts", async (Guid tenantId, ISupplierInsightStore store, CancellationToken token, string? kind = null, int page = 1, int pageSize = 20) => Found(await store.Alerts(tenantId, kind, page, pageSize, token)));
        root.MapGet("/supplier-alert-preferences", async (Guid tenantId, ISupplierInsightStore store, CancellationToken token) => Found(await store.AlertPreference(tenantId, token)));
        root.MapPut("/supplier-alert-preferences", async (Guid tenantId, SupplierAlertPreferenceRequest request, ISupplierInsightStore store, CancellationToken token) => Found(await store.SaveAlertPreference(tenantId, request, token)));
        root.MapPost("/supplier-advice", async (Guid tenantId, SupplierAdviceRequest request, ISupplierAdviceStore store, HttpResponse response, CancellationToken token) =>
        {
            response.Headers.CacheControl = "no-store";
            try { return Found(await store.Advise(tenantId, request, token)); }
            catch (BuildingFlow.Infrastructure.RateLimitedException) { return Results.Problem(statusCode: 429, title: ErrorCatalog.RateLimited.Message, extensions: new Dictionary<string, object?> { ["code"] = ErrorCatalog.RateLimited.Code, ["severity"] = ErrorCatalog.RateLimited.Severity, ["parameters"] = ErrorCatalog.RateLimited.Create().Parameters }); }
        });
        root.MapGet("/supplier-performance", async (Guid tenantId, DateOnly from, DateOnly to, ISupplierInsightStore store, CancellationToken token, Guid? propertyId = null, int page = 1, int pageSize = 20) => Found(await store.Performance(tenantId, from, to, propertyId, page, pageSize, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
