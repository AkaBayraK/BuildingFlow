using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using BuildingFlow.Application;
namespace BuildingFlow.Api;

internal static class ContactVerificationEndpoints
{
    public static void MapContactVerificationEndpoints(this WebApplication app)
    {
        foreach (var path in new[] { "/api/v1/account/contact-verification", "/api/v1/mobile/contact-verification" })
        {
            var group = app.MapGroup(path).RequireAuthorization().WithTags("Account contact verification");
            group.MapGet("", (IContactVerificationStore store, CancellationToken token) => store.ReadAsync(token));
            group.MapPost("/phone", async (HttpRequest request, IContactVerificationStore store, CancellationToken token) => {
                if (!request.HasJsonContentType()) return Results.StatusCode(415);
                if (request.ContentLength > 4096) return Results.StatusCode(413);
                var size = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (size is { IsReadOnly: false }) size.MaxRequestBodySize = 4096;
                StagePhoneRequest? command;
                try { command = await request.ReadFromJsonAsync<StagePhoneRequest>(token); }
                catch (JsonException) { throw new RequestValidationException(new() { ["phone"] = [ErrorCatalog.UserInvalid.Create()] }); }
                if (command is null) throw new RequestValidationException(new() { ["phone"] = [ErrorCatalog.UserInvalid.Create()] });
                return Results.Ok(await store.StagePhoneAsync(command, token));
            });
        }
    }
}
