using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MaintenanceEndpoints
{
    public static void MapMaintenanceEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/maintenance-plans").RequireAuthorization("CanManageDues").WithTags("Maintenance plans");
        root.MapGet("", async (Guid propertyId, MaintenanceService service, CancellationToken token, int page = 1, int pageSize = 10, string? search = null, string? type = null, string? status = null, DateOnly? from = null, DateOnly? to = null) => Found(await service.List(propertyId, new(page, pageSize, search, type, status, from, to), token)));
        root.MapGet("/upcoming", async (Guid propertyId, MaintenanceService service, CancellationToken token, int limit = MaintenanceService.UpcomingLimit) => Found(await service.Upcoming(propertyId, limit, token)));
        root.MapGet("/{planId:guid}", async (Guid propertyId, Guid planId, MaintenanceService service, CancellationToken token) => Found(await service.Get(propertyId, planId, token)));
        root.MapPost("", async (Guid propertyId, MaintenancePlanRequest request, MaintenanceService service, CancellationToken token) => {
            var result = await service.Save(propertyId, null, request, token);
            return result is null ? Results.NotFound() : Results.Created($"/api/v1/properties/{propertyId}/maintenance-plans/{result.Id}", result);
        }).RequireAuthorization("CanManageHierarchy");
        root.MapPut("/{planId:guid}", async (Guid propertyId, Guid planId, MaintenancePlanRequest request, MaintenanceService service, CancellationToken token) => Found(await service.Save(propertyId, planId, request, token))).RequireAuthorization("CanManageHierarchy");
        root.MapDelete("/{planId:guid}", async (Guid propertyId, Guid planId, long version, MaintenanceService service, CancellationToken token) => await service.Delete(propertyId, planId, version, token) ? Results.NoContent() : Results.NotFound()).RequireAuthorization("CanManageHierarchy");
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
