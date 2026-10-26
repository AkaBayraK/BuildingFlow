using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SmsConsentEndpoints
{
    public static void MapSmsConsentEndpoints(this WebApplication app)
    {
        var property = app.MapGroup("/api/v1/properties/{propertyId:guid}/sms").RequireAuthorization("CanManagePeople").WithTags("SMS");
        property.MapGet("/legal-sender", async (Guid propertyId, SmsConsentService service, CancellationToken token) => Found(await service.LegalSenderAsync(propertyId, token)));
        property.MapPut("/legal-sender", async (Guid propertyId, SaveLegalSenderRequest request, SmsConsentService service, CancellationToken token) =>
            Found(await service.SaveLegalSenderAsync(propertyId, request, token)));
        property.MapPost("/preview", async (Guid propertyId, SmsPreviewRequest request, SmsConsentService service, CancellationToken token) => Found(await service.PreviewAsync(propertyId, request, token)));
        property.MapGet("/commercial-consents", async (Guid propertyId, SmsConsentService service, CancellationToken token, int page = 1, int pageSize = 10) =>
            Found(await service.ConsentsAsync(propertyId, page, pageSize, token)));
        property.MapPost("/opt-outs", async (Guid propertyId, AdminOptOutRequest request, SmsConsentService service, CancellationToken token) => Found(await service.OptOutAsync(propertyId, request, token)));

        var platform = app.MapGroup("/api/v1/sms/platform-sender").RequireAuthorization("CanManageProperties").WithTags("SMS");
        platform.MapGet("", async (SmsConsentService service, CancellationToken token) => Found(await service.LegalSenderAsync(null, token)));
        platform.MapPut("", async (SaveLegalSenderRequest request, SmsConsentService service, CancellationToken token) => Found(await service.SaveLegalSenderAsync(null, request, token)));

        var account = app.MapGroup("/api/v1/account/communication-preferences").RequireAuthorization().WithTags("Account");
        account.MapGet("", async (SmsConsentService service, CancellationToken token) => Results.Ok(await service.PreferencesAsync(token)));
        account.MapPut("/operational", async (OperationalPreferenceCommand command, SmsConsentService service, CancellationToken token) => Results.Ok(await service.SetOperationalAsync(command, token)));
        account.MapPut("/{senderId:guid}", async (Guid senderId, CommunicationPreferenceCommand command, SmsConsentService service, CancellationToken token) =>
            Found(await service.SetPreferenceAsync(senderId, command, token)));
        account.MapGet("/{senderId:guid}/history", async (Guid senderId, SmsConsentService service, CancellationToken token, int page = 1, int pageSize = 10) =>
            Found(await service.PreferenceHistoryAsync(senderId, page, pageSize, token)));

        // The mobile client only issues GET/POST through its whitelisted transport; decisions made there are recorded as MOBILE.
        var mobile = app.MapGroup("/api/v1/mobile/communication-preferences").RequireAuthorization().WithTags("Mobile");
        mobile.MapGet("", async (SmsConsentService service, CancellationToken token) => Results.Ok(await service.PreferencesAsync(token)));
        mobile.MapPost("/operational", async (OperationalPreferenceCommand command, SmsConsentService service, CancellationToken token) => Results.Ok(await service.SetOperationalAsync(command, token)));
        mobile.MapPost("/{senderId:guid}", async (Guid senderId, CommunicationPreferenceCommand command, SmsConsentService service, CancellationToken token) =>
            Found(await service.SetPreferenceAsync(senderId, command with { Channel = "MOBILE" }, token)));
    }
    public const string SignatureHeader = "X-BuildingFlow-Signature";
    public const int MaxCallbackBytes = 4096;

    public static void MapSmsOptOutCallback(this WebApplication app)
    {
        // Provider forwards an inbound "RET ..." reply. Off unless a shared HMAC key is configured; the phone number is never logged.
        app.MapPost("/api/v1/sms/opt-out-callbacks", async (HttpRequest request, IConfiguration config, SmsConsentService service, ILoggerFactory logs, CancellationToken token) =>
        {
            var key = config["Sms:OptOutCallbackKey"];
            if (string.IsNullOrEmpty(key) || key.Length < 32) return Results.NotFound();
            if (request.ContentLength is > MaxCallbackBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer, token);
            if (buffer.Length > MaxCallbackBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            var body = buffer.ToArray();
            var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), body);
            var supplied = request.Headers[SignatureHeader].ToString();
            byte[] given;
            try { given = Convert.FromHexString(supplied); } catch (FormatException) { given = []; }
            var log = logs.CreateLogger("BuildingFlow.Sms.OptOutCallback");
            if (!CryptographicOperations.FixedTimeEquals(expected, given)) { log.LogWarning("Rejected SMS opt-out callback with an invalid signature."); return Results.Unauthorized(); }
            SmsOptOutCallback? callback;
            try { callback = JsonSerializer.Deserialize<SmsOptOutCallback>(body, JsonSerializerOptions.Web); } catch (JsonException) { callback = null; }
            if (callback is null) return Results.BadRequest();
            var result = await service.InboundOptOutAsync(callback, token);
            log.LogInformation("SMS opt-out callback recorded for {Senders} sender(s).", result.MatchedSenders);
            return Results.Ok(result);
        }).AllowAnonymous().WithTags("SMS");
    }

    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
