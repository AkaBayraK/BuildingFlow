using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MobileConsentEndpoints
{
    public static void MapMobileConsentEndpoints(this WebApplication app)
    {
        // The account owner's own consent: current state and the text to approve only, never the history (centre-only).
        var root = app.MapGroup("/api/v1/mobile/consents").RequireAuthorization().WithTags("Mobile consents");
        root.MapGet("", async (IMobileConsentStore store, CancellationToken token) => Results.Ok(await store.ListAsync(token)));
        root.MapPost("/commands", async (HttpRequest request, IMobileConsentStore store, CancellationToken token) =>
        {
            if (request.ContentLength > 16 * 1024) return Results.StatusCode(413);
            if (!request.HasJsonContentType()) return Results.StatusCode(415);
            MobileConsentCommand? command;
            try { command = await request.ReadFromJsonAsync<MobileConsentCommand>(token); } catch (System.Text.Json.JsonException) { return Results.BadRequest(); }
            return command is null ? Results.BadRequest() : Results.Ok(await store.CommandAsync(command, token));
        }).Accepts<MobileConsentCommand>("application/json");
    }
}
