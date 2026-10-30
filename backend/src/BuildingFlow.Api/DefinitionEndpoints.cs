using BuildingFlow.Application;

namespace BuildingFlow.Api;

public static class DefinitionEndpoints
{
    public static void MapDefinitionEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/definitions").WithTags("Definitions");
        root.MapGet("", async (IDefinitionStore store, CancellationToken token) => Results.Ok(await store.ListAsync(token)))
            .RequireAuthorization("CanManageHierarchy");
        root.MapPut("/{listKey}/values/{code}", async (string listKey, string code, SaveDefinitionValueRequest request, IDefinitionStore store, CancellationToken token) =>
                await store.SaveAsync(listKey, code, request, token) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequireAuthorization("CanManageProperties");
    }
}
