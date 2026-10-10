using BuildingFlow.Application;

namespace BuildingFlow.Api;

internal static class PropertyEndpoints
{
    public static void MapPropertyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/properties").WithTags("Properties")
            .RequireAuthorization("CanReadProperties");
        group.MapPost("", async (CreatePropertyRequest request, IPropertyService service, CancellationToken token) =>
        {
            var site = await service.CreateAsync(request, token);
            return Results.Created($"/api/v1/properties/{site.Id}", site);
        }).RequireAuthorization("CanManageProperties");
        group.MapGet("", async (IPropertyService service, CancellationToken token, int page = 1, int pageSize = 12, string? search = null, string sortBy = "name", string sortDirection = "asc") =>
            Results.Ok(await service.ListAsync(new(page, pageSize, search, sortBy, sortDirection), token)));
        group.MapGet("/{id:guid}", async (Guid id, IPropertyService service, CancellationToken token) =>
        {
            var site = await service.GetAsync(id, token);
            return site is null ? Results.NotFound() : Results.Ok(site);
        });
    }
}
