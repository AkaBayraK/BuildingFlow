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
        var reasons = root.MapGroup("/dues-cancellation-reasons");
        reasons.MapGet("", async (IDefinitionStore store, CancellationToken token) => Results.Ok(await store.CancellationReasonsAsync(token))).RequireAuthorization("CanManageHierarchy");
        reasons.MapPost("", async (SaveCancellationReasonRequest request, IDefinitionStore store, CancellationToken token) => Results.Ok(await store.CreateCancellationReasonAsync(request, token))).RequireAuthorization("CanManageProperties");
        reasons.MapPut("/{id:guid}", async (Guid id, SaveCancellationReasonRequest request, IDefinitionStore store, CancellationToken token) =>
                await store.SaveCancellationReasonAsync(id, request, token) is { } value ? Results.Ok(value) : Results.NotFound()).RequireAuthorization("CanManageProperties");
        var area = app.MapGroup("/api/v1/properties/{propertyId:guid}/definitions").WithTags("Definitions");
        // Any active member reads the area's choices for forms; managers change them for their own areas only.
        area.MapGet("", async (Guid propertyId, IDefinitionStore store, ICurrentUserService user, CancellationToken token) =>
                await store.PropertyListAsync(propertyId, Scope(user), token) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequireAuthorization("CanReadProperties");
        area.MapPut("/{listKey}/values/{code}", async (Guid propertyId, string listKey, string code, SavePropertyDefinitionRequest request, IDefinitionStore store, ICurrentUserService user, CancellationToken token) =>
                await store.SavePropertyAsync(propertyId, listKey, code, request, Scope(user), token) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequireAuthorization("CanManageHierarchy");
    }

    private static AccessScope Scope(ICurrentUserService user) => new(user.UserId!, user.IsInRole("SUPER_ADMIN"));
}
