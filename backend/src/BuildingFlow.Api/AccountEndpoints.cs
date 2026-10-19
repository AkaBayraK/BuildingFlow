using System.Security.Claims;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Http.Features;
namespace BuildingFlow.Api;
// İŞ19 "Hesap bilgilerim": the signed-in account's own data in the centre. Profile and photo are the same records the mobile
// app edits (one photo per account); consent decisions given here are recorded with channel WEB.
public sealed record AccountDto(string UserId, string? Username, string? Email, IReadOnlyList<string> Roles, IReadOnlyList<MobileContext> Contexts, bool HasMore, MobileProfileDto Profile);
internal static class AccountEndpoints
{
    private static readonly string[] Roles = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT", "RESIDENT"];
    internal static IResult Photo(byte[] bytes)
    {
        // Personal data: UseReleaseSecurity marks every /api response no-store, so browsers and proxies keep no copy.
        return Results.File(bytes, "image/jpeg");
    }
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/account").RequireAuthorization().WithTags("Account");
        root.MapGet("", async (ClaimsPrincipal principal, ICurrentUserService actor, IMobileSessionStore sessions, IMobileProfileStore profiles, CancellationToken token) =>
        {
            if (!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId)) return Results.Unauthorized();
            var roles = Roles.Where(actor.IsInRole).ToArray();
            var session = await sessions.ReadAsync(actor.UserId, actor.Username, roles.Where(r => r != "RESIDENT").ToArray(), token);
            var email = principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value;
            return Results.Ok(new AccountDto(actor.UserId, actor.Username, email, roles, session.Contexts, session.HasMore, await profiles.ReadAsync(token)));
        });
        root.MapGet("/profile/photo/{id:guid}", async (Guid id, IMobileProfileStore store, CancellationToken token) => await store.PhotoAsync(id, token) is { } bytes ? Photo(bytes) : Results.NotFound());
        root.MapPost("/profile/commands", async (HttpContext context, IMobileProfileStore store, CancellationToken token) =>
        {
            const long limit = 384 * 1024;
            if (context.Request.ContentLength > limit) return Results.StatusCode(413);
            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>(); if (feature is { IsReadOnly: false }) feature.MaxRequestBodySize = limit;
            if (!context.Request.HasJsonContentType()) return Results.StatusCode(415);
            MobileProfileCommand? command;
            try { command = await context.Request.ReadFromJsonAsync<MobileProfileCommand>(token); } catch (System.Text.Json.JsonException) { return Results.BadRequest(); }
            return command is null ? Results.BadRequest() : Results.Ok(await store.SaveAsync(command, token));
        });
        root.MapGet("/consents", async (IMobileConsentStore store, CancellationToken token) => Results.Ok(await store.ListAsync(token)));
        root.MapPost("/consents/commands", async (HttpRequest request, IMobileConsentStore store, CancellationToken token) =>
        {
            if (request.ContentLength > 16 * 1024) return Results.StatusCode(413);
            if (!request.HasJsonContentType()) return Results.StatusCode(415);
            MobileConsentCommand? command;
            try { command = await request.ReadFromJsonAsync<MobileConsentCommand>(token); } catch (System.Text.Json.JsonException) { return Results.BadRequest(); }
            return command is null ? Results.BadRequest() : Results.Ok(await store.CommandAsync(command, token, ConsentChannel.WEB));
        }).Accepts<MobileConsentCommand>("application/json");
    }
}
