using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class AutomationEndpoints
{
    public static void MapAutomationEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/automations").WithTags("Automations").RequireAuthorization("CanManagePeople");
        root.MapGet("", async (Guid propertyId, AutomationService service, CancellationToken token) => Found(await service.OverviewAsync(propertyId, token)));
        root.MapPost("", async (Guid propertyId, AutomationRequest request, AutomationService service, CancellationToken token) =>
            await service.CreateAsync(propertyId, request, token) is { } value ? Results.Created($"/api/v1/properties/{propertyId}/automations/{value.Id}", value) : Results.NotFound());
        root.MapPut("/{automationId:guid}", async (Guid propertyId, Guid automationId, AutomationRequest request, AutomationService service, CancellationToken token) =>
            Found(await service.UpdateAsync(propertyId, automationId, request, token)));
        root.MapPost("/{automationId:guid}/activate", async (Guid propertyId, Guid automationId, AutomationStatusRequest request, AutomationService service, CancellationToken token) =>
            Found(await service.SetActiveAsync(propertyId, automationId, true, request, token)));
        root.MapPost("/{automationId:guid}/pause", async (Guid propertyId, Guid automationId, AutomationStatusRequest request, AutomationService service, CancellationToken token) =>
            Found(await service.SetActiveAsync(propertyId, automationId, false, request, token)));
        root.MapPost("/{automationId:guid}/run", async (Guid propertyId, Guid automationId, AutomationService service, CancellationToken token) =>
            Found(await service.RunAsync(propertyId, automationId, token)));
        root.MapGet("/{automationId:guid}/preview", async (Guid propertyId, Guid automationId, AutomationService service, CancellationToken token, DateOnly? date = null) =>
            Found(await service.PreviewAsync(propertyId, automationId, date, token)));
        root.MapGet("/{automationId:guid}/revisions", async (Guid propertyId, Guid automationId, AutomationService service, CancellationToken token) =>
            Found(await service.RevisionsAsync(propertyId, automationId, token)));
        root.MapGet("/executions", async (Guid propertyId, AutomationService service, CancellationToken token, int page = 1, int pageSize = 20, Guid? automationId = null) =>
            Found(await service.ExecutionsAsync(propertyId, page, pageSize, automationId, token)));
        root.MapGet("/dispatches", async (Guid propertyId, AutomationService service, CancellationToken token, int page = 1, int pageSize = 20, Guid? automationId = null,
            string? channel = null, string? status = null) =>
            Found(await service.DispatchesAsync(propertyId, new(page, pageSize, automationId, channel, status), token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
