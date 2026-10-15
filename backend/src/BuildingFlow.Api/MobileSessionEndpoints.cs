using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MobileSessionEndpoints
{
    public static void MapMobileSessionEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/mobile/session", async (ICurrentUserService actor, IMobileSessionStore store, CancellationToken token) =>
        {
            if (!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId)) return Results.Unauthorized();
            string[] allowed = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT"];
            return Results.Ok(await store.ReadAsync(actor.UserId, actor.Username, allowed.Where(actor.IsInRole).ToArray(), token));
        }).RequireAuthorization().WithTags("Mobile session");
    }
}
