using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)=>app.MapGet("/api/v1/properties/{propertyId:guid}/dashboard",async(Guid propertyId,DashboardService service,CancellationToken token)=>await service.Get(propertyId,token) is {} value?Results.Ok(value):Results.NotFound()).RequireAuthorization("CanManageDues").WithTags("Dashboard");
}
