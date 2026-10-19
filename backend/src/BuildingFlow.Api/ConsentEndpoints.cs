using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class ConsentEndpoints
{
    public static void MapConsentEndpoints(this WebApplication app)
    {
        // Consent history is personal data: people managers read it within their scope, only SUPER_ADMIN writes texts and types.
        var root = app.MapGroup("/api/v1/consents").RequireAuthorization("CanManagePeople").WithTags("Consents");
        root.MapGet("/types", async (ConsentService service, CancellationToken token) => Results.Ok(await service.TypesAsync(token)));
        root.MapPost("/types", async (CreateConsentTypeRequest request, ConsentService service, CancellationToken token) => Results.Ok(await service.CreateTypeAsync(request, token))).RequireAuthorization("CanManageProperties");
        root.MapPut("/types/{typeId:guid}", async (Guid typeId, UpdateConsentTypeRequest request, ConsentService service, CancellationToken token) => Found(await service.UpdateTypeAsync(typeId, request, token))).RequireAuthorization("CanManageProperties");
        root.MapDelete("/types/{typeId:guid}", async (Guid typeId, long version, ConsentService service, CancellationToken token) => await service.DeleteTypeAsync(typeId, version, token) ? Results.NoContent() : Results.NotFound()).RequireAuthorization("CanManageProperties");
        root.MapGet("/types/{typeId:guid}/texts", async (Guid typeId, ConsentService service, CancellationToken token, int page = 1, int pageSize = 20) => Found(await service.TextsAsync(typeId, page, pageSize, token)));
        root.MapPost("/types/{typeId:guid}/texts", async (Guid typeId, PublishConsentTextRequest request, ConsentService service, CancellationToken token) => Found(await service.PublishAsync(typeId, request, token))).RequireAuthorization("CanManageProperties");
        root.MapGet("/texts/{textId:guid}", async (Guid textId, ConsentService service, CancellationToken token) => Found(await service.TextAsync(textId, token)));
        root.MapGet("/records", async (ConsentService service, CancellationToken token, int page = 1, int pageSize = 20, Guid? typeId = null, string? state = null, Guid? propertyId = null, string? search = null, string? userId = null) =>
            Found(await service.RecordsAsync(new ConsentRecordQuery(page, pageSize, typeId, state, propertyId, search, userId), token)));
        root.MapGet("/records/{recordId:guid}", async (Guid recordId, ConsentService service, CancellationToken token) => Found(await service.DocumentAsync(recordId, token)));
        root.MapGet("/users/{userId}", async (string userId, ConsentService service, CancellationToken token) => Found(await service.HistoryAsync(userId, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
