using System.Net;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
namespace BuildingFlow.Api;

internal static class OnlinePaymentEndpoints
{
    private const int WebhookLimit = 4096;
    public static void MapOnlinePaymentEndpoints(this WebApplication app)
    {
        var sandbox = app.Services.GetRequiredService<SandboxPaymentOptions>();
        if (sandbox.Enabled && app.Environment.IsProduction()) throw new InvalidOperationException("The payment sandbox cannot run in Production.");
        var today = () => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(app.Services.GetRequiredService<TimeProvider>().GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);

        var providers = app.MapGroup("/api/v1/payment-providers").WithTags("Payment providers").RequireAuthorization("CanManageDues");
        providers.MapGet("", async (OnlinePaymentService service, CancellationToken token) => Results.Ok(await service.ProvidersAsync(token)));
        providers.MapPut("/{code}", async (string code, UpdatePaymentProviderRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.UpdateProviderAsync(code, request, token)));

        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}").WithTags("Online payments").RequireAuthorization("CanManageDues");
        root.MapGet("/payment-settings", async (Guid propertyId, OnlinePaymentService service, CancellationToken token) => Found(await service.SettingsAsync(propertyId, token)));
        root.MapPut("/payment-settings", async (Guid propertyId, PaymentSettingsRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.UpdateSettingsAsync(propertyId, request, token)));
        root.MapGet("/bank-accounts", async (Guid propertyId, OnlinePaymentService service, CancellationToken token, int page = 1, int pageSize = 20, string? status = null) =>
            Found(await service.BankAccountsAsync(propertyId, page, pageSize, status, token)));
        root.MapPost("/bank-accounts", async (Guid propertyId, BankAccountRequest request, OnlinePaymentService service, CancellationToken token) =>
            Created(await service.CreateBankAccountAsync(propertyId, request, token), value => value.Replayed, value => $"/api/v1/properties/{propertyId}/bank-accounts/{value.Id}"));
        root.MapPut("/bank-accounts/{accountId:guid}/decision", async (Guid propertyId, Guid accountId, BankAccountDecisionRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.DecideBankAccountAsync(propertyId, accountId, request, token)));
        root.MapPut("/bank-accounts/{accountId:guid}/close", async (Guid propertyId, Guid accountId, BankAccountCloseRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.CloseBankAccountAsync(propertyId, accountId, request, token)));
        root.MapPut("/bank-accounts/{accountId:guid}/default", async (Guid propertyId, Guid accountId, VersionRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.DefaultBankAccountAsync(propertyId, accountId, request, token)));
        root.MapGet("/merchant-accounts", async (Guid propertyId, OnlinePaymentService service, CancellationToken token, int page = 1, int pageSize = 20) =>
            Found(await service.MerchantsAsync(propertyId, page, pageSize, token)));
        root.MapPost("/merchant-accounts", async (Guid propertyId, MerchantAccountRequest request, OnlinePaymentService service, CancellationToken token) =>
            Created(await service.CreateMerchantAsync(propertyId, request, token), value => value.Replayed, value => $"/api/v1/properties/{propertyId}/merchant-accounts/{value.Id}"));
        root.MapPut("/merchant-accounts/{merchantId:guid}/default", async (Guid propertyId, Guid merchantId, VersionRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.DefaultMerchantAsync(propertyId, merchantId, request, token)));
        root.MapPut("/merchant-accounts/{merchantId:guid}/deactivate", async (Guid propertyId, Guid merchantId, VersionRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.DeactivateMerchantAsync(propertyId, merchantId, request, token)));
        root.MapPost("/merchant-accounts/{merchantId:guid}/test", async (Guid propertyId, Guid merchantId, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.TestMerchantAsync(propertyId, merchantId, token)));

        root.MapGet("/online-payments", async (Guid propertyId, OnlinePaymentService service, CancellationToken token, int page = 1, int pageSize = 10,
            string? status = null, Guid? unitId = null, DateOnly? from = null, DateOnly? to = null) =>
            Found(await service.ListAsync(propertyId, new(page, pageSize, status, unitId, from, to), token)));
        root.MapGet("/online-payments/{intentId:guid}", async (Guid propertyId, Guid intentId, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.GetAsync(propertyId, intentId, token)));
        root.MapPut("/online-payments/{intentId:guid}/settle", async (Guid propertyId, Guid intentId, VersionRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.SettleReviewAsync(propertyId, intentId, request, token)));
        root.MapPost("/online-payments/{intentId:guid}/refunds", async (Guid propertyId, Guid intentId, RefundRequest request, OnlinePaymentService service, CancellationToken token) =>
            Created(await service.RequestRefundAsync(propertyId, intentId, request, token), value => value.Replayed, value => $"/api/v1/properties/{propertyId}/online-payments/{intentId}"));
        root.MapPut("/payment-refunds/{refundId:guid}/decision", async (Guid propertyId, Guid refundId, RefundDecisionRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.DecideRefundAsync(propertyId, refundId, request, token)));
        root.MapGet("/payment-reconciliation/items", async (Guid propertyId, OnlinePaymentService service, CancellationToken token, string? status = null, int page = 1, int pageSize = 20) =>
            Found(await service.IssuesAsync(propertyId, status, page, pageSize, token)));
        root.MapPut("/payment-reconciliation/items/{itemId:guid}/resolve", async (Guid propertyId, Guid itemId, ResolveItemRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.ResolveIssueAsync(propertyId, itemId, request, token)));
        root.MapPost("/payment-reconciliation/run", async (Guid propertyId, ReconciliationRunRequest request, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.ReconcileAsync(propertyId, request, today(), token)));
        root.MapGet("/payment-reconciliation/settlement", async (Guid propertyId, OnlinePaymentService service, CancellationToken token, DateOnly? from = null, DateOnly? to = null) =>
            Found(await service.SettlementAsync(propertyId, from, to, today(), token)));
        root.MapGet("/payment-ledger", async (Guid propertyId, OnlinePaymentService service, CancellationToken token, DateOnly? from = null, DateOnly? to = null, int page = 1, int pageSize = 20) =>
            Found(await service.LedgerAsync(propertyId, from, to, page, pageSize, token)));
        root.MapGet("/payments/{paymentId:guid}/receipt", async (Guid propertyId, Guid paymentId, OnlinePaymentService service, CancellationToken token) =>
            Found(await service.ReceiptAsync(propertyId, paymentId, token)));

        var mobile = app.MapGroup("/api/v1/mobile/payments").WithTags("Mobile payments").RequireAuthorization();
        mobile.MapGet("/options", async (Guid propertyId, Guid unitId, string role, ResidentPaymentService service, CancellationToken token) =>
            Results.Ok(await service.OptionsAsync(propertyId, unitId, role, token)));
        mobile.MapPost("/intents", async (CreateIntentRequest request, ResidentPaymentService service, CancellationToken token) =>
        {
            var result = await service.CreateIntentAsync(request, token);
            return result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/mobile/payments/intents/{result.Id}", result);
        });
        mobile.MapGet("/intents", async (Guid propertyId, Guid unitId, string role, ResidentPaymentService service, CancellationToken token, int page = 1) =>
            Results.Ok(await service.IntentsAsync(propertyId, unitId, role, page, token)));
        mobile.MapGet("/intents/{intentId:guid}", async (Guid intentId, Guid propertyId, Guid unitId, string role, ResidentPaymentService service, CancellationToken token) =>
            Found(await service.IntentAsync(propertyId, unitId, role, intentId, false, token)));
        mobile.MapPut("/intents/{intentId:guid}/cancel", async (Guid intentId, Guid propertyId, Guid unitId, string role, ResidentPaymentService service, CancellationToken token) =>
            Found(await service.IntentAsync(propertyId, unitId, role, intentId, true, token)));
        mobile.MapGet("/receipts/{paymentId:guid}", async (Guid paymentId, Guid propertyId, Guid unitId, string role, ResidentPaymentService service, CancellationToken token) =>
            Found(await service.ReceiptAsync(propertyId, unitId, role, paymentId, token)));

        // Anonymous by design: authenticity comes from the provider signature, timestamp window and event-id replay guard.
        app.MapPost("/api/v1/payment-webhooks/{provider}", async (string provider, HttpContext context, IOnlinePaymentStore store, CancellationToken token) =>
        {
            var body = await ReadLimited(context.Request, token);
            var headers = context.Request.Headers.ToDictionary(value => value.Key.ToLowerInvariant(), value => value.Value.ToString());
            if (body is not null && provider.Length <= 30 && await store.HandleWebhookAsync(provider, headers, body, token)) return Results.Ok(new { received = true });
            context.Response.StatusCode = 400;
            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Instance = context.Request.Path };
            ApiProblems.Apply(problem, ErrorCatalog.PaymentWebhookInvalid.Create());
            await ApiProblems.Write(context, problem, token);
            return Results.Empty;
        }).WithTags("Payment webhooks").AllowAnonymous();

        if (!sandbox.Enabled) return;
        var checkout = app.MapGroup("/api/v1/payment-sandbox/checkout").WithTags("Payment sandbox").AllowAnonymous();
        checkout.MapGet("/{reference}", (string reference, SandboxPaymentProvider provider, HttpContext context) =>
            provider.Describe(reference) is { } session ? Page(context, reference, session.Description, session.Amount, session.Open, null) : Results.NotFound());
        checkout.MapPost("/{reference}", async (string reference, HttpRequest request, SandboxPaymentProvider provider, IOnlinePaymentStore store, HttpContext context, CancellationToken token) =>
        {
            var form = await request.ReadFormAsync(token);
            var notice = provider.Complete(reference, form["result"] == "success");
            if (notice is null) return Results.NotFound();
            await store.HandleWebhookAsync(SandboxPaymentProvider.ProviderCode, notice.Value.Headers, notice.Value.Body, token);
            var session = provider.Describe(reference)!.Value;
            return Page(context, reference, session.Description, session.Amount, false, form["result"] == "success");
        });
    }
    private static async Task<byte[]?> ReadLimited(HttpRequest request, CancellationToken token)
    {
        if (request.ContentLength > WebhookLimit) return null;
        using var buffer = new MemoryStream();
        var chunk = new byte[1024]; int read;
        while ((read = await request.Body.ReadAsync(chunk, token)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > WebhookLimit) return null;
        }
        return buffer.ToArray();
    }
    // Sandbox page only: no card fields, no scripts; it stands in for the provider's hosted checkout.
    private static IResult Page(HttpContext context, string reference, string description, decimal amount, bool open, bool? success)
    {
        context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; base-uri 'none'; frame-ancestors 'none'";
        var text = WebUtility.HtmlEncode(description); var money = amount.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        var body = new StringBuilder();
        body.Append("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
            .Append("<title>BuildingFlow Sandbox Ödeme</title><style>body{font-family:system-ui,sans-serif;max-width:420px;margin:40px auto;padding:0 16px;color:#082b49}")
            .Append(".card{border:1px solid #d6e0ea;border-radius:16px;padding:24px}button{width:100%;min-height:48px;border-radius:12px;border:0;font-size:16px;margin-top:12px;cursor:pointer}")
            .Append(".ok{background:#087a68;color:#fff}.no{background:#eaf0f7;color:#082b49}.warn{background:#fff3db;padding:12px;border-radius:12px}</style></head><body><div class=\"card\">")
            .Append("<p class=\"warn\">TEST ORTAMI — gerçek para hareketi yoktur, kart bilgisi istenmez.</p>")
            .Append("<h1>").Append(money).Append(" TL</h1><p>").Append(text).Append("</p>");
        if (success is not null) body.Append("<p><strong>").Append(success.Value ? "Sandbox ödemesi onaylandı." : "Sandbox ödemesi reddedildi.").Append("</strong> Uygulamaya dönebilirsiniz.</p>");
        else if (open) body.Append("<form method=\"post\" action=\"").Append(WebUtility.HtmlEncode(reference)).Append("\"><button class=\"ok\" name=\"result\" value=\"success\">Başarılı ödeme (sandbox)</button>")
            .Append("<button class=\"no\" name=\"result\" value=\"fail\">Reddedilen ödeme (sandbox)</button></form>");
        else body.Append("<p>Bu ödeme oturumu kapanmış.</p>");
        return Results.Content(body.Append("</div></body></html>").ToString(), "text/html; charset=utf-8");
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
    private static IResult Created<T>(T? value, Func<T, bool> replayed, Func<T, string> location) where T : class =>
        value is null ? Results.NotFound() : replayed(value) ? Results.Ok(value) : Results.Created(location(value), value);
}
