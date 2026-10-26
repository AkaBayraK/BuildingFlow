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
        account.MapPut("/{senderId:guid}", async (Guid senderId, CommunicationPreferenceCommand command, SmsConsentService service, CancellationToken token) =>
            Found(await service.SetPreferenceAsync(senderId, command, token)));
        account.MapGet("/{senderId:guid}/history", async (Guid senderId, SmsConsentService service, CancellationToken token, int page = 1, int pageSize = 10) =>
            Found(await service.PreferenceHistoryAsync(senderId, page, pageSize, token)));

        // The mobile client only issues GET/POST through its whitelisted transport; decisions made there are recorded as MOBILE.
        var mobile = app.MapGroup("/api/v1/mobile/communication-preferences").RequireAuthorization().WithTags("Mobile");
        mobile.MapGet("", async (SmsConsentService service, CancellationToken token) => Results.Ok(await service.PreferencesAsync(token)));
        mobile.MapPost("/{senderId:guid}", async (Guid senderId, CommunicationPreferenceCommand command, SmsConsentService service, CancellationToken token) =>
            Found(await service.SetPreferenceAsync(senderId, command with { Channel = "MOBILE" }, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
