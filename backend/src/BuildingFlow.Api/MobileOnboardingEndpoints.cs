using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MobileOnboardingEndpoints
{
    public static void MapMobileOnboardingEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/mobile/registrations", async (MobileRegistrationRequest request, MobileOnboardingService service, HttpContext context, CancellationToken token) =>
        {
            var result = await service.RegisterAsync(request, ClientKey(context), token);
            context.Response.Headers.CacheControl = "no-store";
            return result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/mobile/registrations/{request.RequestId}", result);
        }).AllowAnonymous().WithTags("Mobile onboarding");
        app.MapPost("/api/v1/mobile/person-links", async (PersonLinkRequest request, MobileOnboardingService service, CancellationToken token) =>
            Results.Ok(await service.LinkAsync(request, token))).RequireAuthorization().WithTags("Mobile onboarding");
    }
    // Only a hash of the caller address is kept, for the per-client registration limit.
    private static string ClientKey(HttpContext context) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("buildingflow-registration:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"))));
}
