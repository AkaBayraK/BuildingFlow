using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SmsEndpoints
{
    public static void MapSmsEndpoints(this WebApplication app)
    {
        // Phone numbers and message history are personal data: the people-management policy applies to every SMS route.
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/sms").RequireAuthorization("CanManagePeople").WithTags("SMS");
        root.MapGet("", async (Guid propertyId, SmsService service, CancellationToken token) => Found(await service.OverviewAsync(propertyId, token)));
        root.MapPut("/account", async (Guid propertyId, SaveSmsAccountRequest request, SmsService service, CancellationToken token) => Found(await service.SaveAccountAsync(propertyId, request, token))).RequireAuthorization("CanManageProperties");
        root.MapPost("/account/credits", async (Guid propertyId, SmsCreditRequest request, SmsService service, CancellationToken token) => Found(await service.AddCreditAsync(propertyId, request, token))).RequireAuthorization("CanManageProperties");
        root.MapGet("/account/credits", async (Guid propertyId, SmsService service, CancellationToken token, int page = 1, int pageSize = 10) => Found(await service.CreditsAsync(propertyId, page, pageSize, token)));
        root.MapGet("/templates", async (Guid propertyId, SmsService service, CancellationToken token) => Found(await service.TemplatesAsync(propertyId, token)));
        root.MapPost("/templates", async (Guid propertyId, SmsTemplateRequest request, SmsService service, CancellationToken token) => Found(await service.SaveTemplateAsync(propertyId, null, request, token)));
        root.MapPut("/templates/{templateId:guid}", async (Guid propertyId, Guid templateId, SmsTemplateRequest request, SmsService service, CancellationToken token) => Found(await service.SaveTemplateAsync(propertyId, templateId, request, token)));
        root.MapDelete("/templates/{templateId:guid}", async (Guid propertyId, Guid templateId, long version, SmsService service, CancellationToken token) => await service.DeleteTemplateAsync(propertyId, templateId, version, token) ? Results.NoContent() : Results.NotFound());
        root.MapGet("/messages", async (Guid propertyId, SmsService service, CancellationToken token, int page = 1, int pageSize = 10, string? status = null) => Found(await service.MessagesAsync(propertyId, page, pageSize, status, token)));
        root.MapPost("/messages", async (Guid propertyId, SendSmsRequest request, SmsService service, CancellationToken token) => Found(await service.SendAsync(propertyId, request, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
