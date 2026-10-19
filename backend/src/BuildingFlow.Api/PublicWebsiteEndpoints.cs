using System.Text.RegularExpressions;
using BuildingFlow.Application;
namespace BuildingFlow.Api;
// Anonymous, read-only public website API. It returns only the active publication snapshot (an allow-listed DTO);
// every unavailable case (unknown key, draft, suspended, disabled Property) is the same plain 404.
internal static partial class PublicWebsiteEndpoints
{
    public static void MapPublicWebsiteEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/public/v1/sites").WithTags("Public website").AllowAnonymous();
        root.MapGet("/{siteKey}", async (string siteKey, HttpRequest request, HttpResponse response, IPublicWebsiteStore store, CancellationToken token) => {
            siteKey = siteKey.ToLowerInvariant();
            if (siteKey.Length > 63 || !SiteKey().IsMatch(siteKey)) return Results.NotFound();
            var site = await store.SiteAsync(siteKey, token); if (site is null) return Results.NotFound();
            var etag = $"\"{site.Sha256}\"";
            response.Headers.ETag = etag;
            if (request.Headers.IfNoneMatch.Contains(etag)) return Results.StatusCode(StatusCodes.Status304NotModified);
            return Results.Text(site.Json, "application/json; charset=utf-8");
        });
    }
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])$")] private static partial Regex SiteKey();
}
