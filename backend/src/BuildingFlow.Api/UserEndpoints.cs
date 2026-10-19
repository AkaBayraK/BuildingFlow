using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/users").RequireAuthorization("CanManageUsers").WithTags("Users");
        root.MapGet("", async (UserService service, CancellationToken token, int page = 1, int pageSize = 20, string? search = null) => Results.Ok(await service.ListAsync(page, pageSize, search, token)));
        root.MapGet("/{userId}", async (string userId, UserService service, CancellationToken token) => Found(await service.GetAsync(userId, token)));
        root.MapPost("", async (CreateUserRequest request, UserService service, CancellationToken token) => { var created = await service.CreateAsync(request, token); return Results.Created($"/api/v1/users/{created.Id}", created); });
        root.MapPut("/{userId}", async (string userId, UpdateUserRequest request, UserService service, CancellationToken token) => Found(await service.UpdateAsync(userId, request, token)));
        root.MapGet("/{userId}/photo", async (string userId, UserService service, CancellationToken token) => await service.PhotoAsync(userId, token) is { } bytes ? AccountEndpoints.Photo(bytes) : Results.NotFound());
        root.MapPost("/{userId}/password-reset", async (string userId, UserService service, CancellationToken token) => await service.SendPasswordResetAsync(userId, token) ? Results.NoContent() : Results.NotFound());
        root.MapPost("/{userId}/people", async (string userId, LinkPersonRequest request, UserService service, CancellationToken token) => Found(await service.LinkAsync(userId, request, token)));
        root.MapDelete("/{userId}/people/{personId:guid}", async (string userId, Guid personId, Guid propertyId, long version, UserService service, CancellationToken token) => await service.UnlinkAsync(userId, propertyId, personId, version, token) ? Results.NoContent() : Results.NotFound());
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
