using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)=>app.MapGet("/api/v1/properties/{propertyId:guid}/reports",async(Guid propertyId,string period,ReportService service,CancellationToken token)=>{var value=await service.Get(propertyId,period,token);return value is null?Results.NotFound():Results.Ok(value);}).RequireAuthorization("CanManageDues").WithTags("Reports");
}
