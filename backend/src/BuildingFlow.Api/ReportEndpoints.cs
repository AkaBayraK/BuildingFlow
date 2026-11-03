using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/properties/{propertyId:guid}/reports",async(Guid propertyId,string period,ReportService service,CancellationToken token)=>{var value=await service.Get(propertyId,period,token);return value is null?Results.NotFound():Results.Ok(value);}).RequireAuthorization("CanManageDues").WithTags("Reports");
        foreach (var format in new[] { "xlsx", "pdf" })
        {
            var selected = format;
            app.MapGet("/api/v1/properties/{propertyId:guid}/reports." + selected,
                async (Guid propertyId, string period, string? locale, ReportService service, HttpResponse response, CancellationToken token) =>
                {
                    if (locale is not (null or "tr" or "en")) throw new RequestValidationException(new() { ["locale"] = [ErrorCatalog.InvalidRequest.Create()] });
                    var report = await service.Get(propertyId, period, token);
                    if (report is null) return Results.NotFound();
                    token.ThrowIfCancellationRequested();
                    var file = FinanceReportExport.Create(report, selected, locale ?? "tr");
                    response.Headers.CacheControl = "no-store";
                    response.Headers.XContentTypeOptions = "nosniff";
                    return Results.File(file.Content, file.ContentType, file.FileName);
                }).RequireAuthorization("CanManageDues").WithTags("Reports");
        }
    }
}
