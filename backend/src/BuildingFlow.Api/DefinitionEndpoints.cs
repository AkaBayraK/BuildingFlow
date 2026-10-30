using BuildingFlow.Application;

namespace BuildingFlow.Api;

public static class DefinitionEndpoints
{
    public static void MapDefinitionEndpoints(this WebApplication app) =>
        app.MapGet("/api/v1/definitions", () => Results.Ok(DefinitionRegistry.Current))
            .RequireAuthorization("CanManageHierarchy").WithTags("Definitions");
}
