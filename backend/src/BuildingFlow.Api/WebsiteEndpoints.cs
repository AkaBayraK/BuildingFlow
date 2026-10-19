using BuildingFlow.Application;
namespace BuildingFlow.Api;
// Management API for the public website. Editors: SUPER_ADMIN/SITE_ADMIN/MANAGER; publishing: SUPER_ADMIN/SITE_ADMIN.
// Property membership is re-checked in the store; out-of-scope records answer 404.
internal static class WebsiteEndpoints
{
    public static void MapWebsiteEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/website").WithTags("Website").RequireAuthorization("CanManageWebsite");
        root.MapGet("", async (Guid propertyId, WebsiteService service, CancellationToken token) => Found(await service.Get(propertyId, token)));
        root.MapPost("", async (Guid propertyId, WebsiteCreateRequest request, WebsiteService service, CancellationToken token) => {
            var result = await service.Create(propertyId, request, token);
            return result is null ? Results.NotFound() : result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/properties/{propertyId}/website", result);
        }).RequireAuthorization("CanPublishWebsite");
        root.MapPut("", async (Guid propertyId, WebsiteUpdateRequest request, WebsiteService service, CancellationToken token) => Found(await service.Update(propertyId, request, token)));
        root.MapPut("/theme", async (Guid propertyId, WebsiteThemeRequest request, WebsiteService service, CancellationToken token) => Found(await service.Theme(propertyId, request, token)));
        root.MapPut("/status", async (Guid propertyId, WebsiteStatusRequest request, WebsiteService service, CancellationToken token) => Found(await service.Status(propertyId, request, token))).RequireAuthorization("CanManageProperties");
        root.MapPost("/pages", async (Guid propertyId, WebsitePageRequest request, WebsiteService service, CancellationToken token) => {
            var result = await service.SavePage(propertyId, null, request, token);
            return result is null ? Results.NotFound() : Results.Created($"/api/v1/properties/{propertyId}/website/pages/{result.Id}", result);
        });
        root.MapPut("/pages/order", async (Guid propertyId, WebsiteOrderRequest request, WebsiteService service, CancellationToken token) => Found(await service.OrderPages(propertyId, request, token)));
        root.MapPut("/pages/{pageId:guid}", async (Guid propertyId, Guid pageId, WebsitePageRequest request, WebsiteService service, CancellationToken token) => Found(await service.SavePage(propertyId, pageId, request, token)));
        root.MapDelete("/pages/{pageId:guid}", async (Guid propertyId, Guid pageId, long version, WebsiteService service, CancellationToken token) => await service.DeletePage(propertyId, pageId, version, token) ? Results.NoContent() : Results.NotFound());
        root.MapPost("/pages/{pageId:guid}/sections", async (Guid propertyId, Guid pageId, WebsiteSectionRequest request, WebsiteService service, CancellationToken token) => {
            var result = await service.SaveSection(propertyId, pageId, null, request, token);
            return result is null ? Results.NotFound() : Results.Created($"/api/v1/properties/{propertyId}/website/pages/{pageId}/sections/{result.Id}", result);
        });
        root.MapPut("/pages/{pageId:guid}/sections/order", async (Guid propertyId, Guid pageId, WebsiteOrderRequest request, WebsiteService service, CancellationToken token) => Found(await service.OrderSections(propertyId, pageId, request, token)));
        root.MapPut("/pages/{pageId:guid}/sections/{sectionId:guid}", async (Guid propertyId, Guid pageId, Guid sectionId, WebsiteSectionRequest request, WebsiteService service, CancellationToken token) => Found(await service.SaveSection(propertyId, pageId, sectionId, request, token)));
        root.MapDelete("/pages/{pageId:guid}/sections/{sectionId:guid}", async (Guid propertyId, Guid pageId, Guid sectionId, long version, WebsiteService service, CancellationToken token) => await service.DeleteSection(propertyId, pageId, sectionId, version, token) ? Results.NoContent() : Results.NotFound());
        root.MapGet("/preview", async (Guid propertyId, WebsiteService service, CancellationToken token) => Found(await service.Preview(propertyId, token)));
        root.MapGet("/publications", async (Guid propertyId, WebsiteService service, CancellationToken token) => Found(await service.Publications(propertyId, token))).RequireAuthorization("CanPublishWebsite");
        root.MapPost("/publications", async (Guid propertyId, WebsitePublishRequest request, WebsiteService service, CancellationToken token) => Found(await service.Publish(propertyId, request, token))).RequireAuthorization("CanPublishWebsite");
        root.MapPost("/publications/{publicationId:guid}/restore", async (Guid propertyId, Guid publicationId, WebsitePublishRequest request, WebsiteService service, CancellationToken token) => Found(await service.Restore(propertyId, publicationId, request, token))).RequireAuthorization("CanPublishWebsite");
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
