using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class OnlinePaymentApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string WebhookSecret = "sandbox-webhook-secret-for-tests-only-0123456789";
    private const string MerchantSecret = "test-only-merchant-secret-value";
    private const string ValidIban = "TR400006200000000000000001";
    private const string SecondIban = "TR130006200000000000000002";
    private static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private WebApplicationFactory<Program> App() => factory.WithWebHostBuilder(builder => {
        builder.UseSetting("Payments:Sandbox:Enabled", "true");
        builder.UseSetting("Payments:Sandbox:WebhookSecret", WebhookSecret);
        builder.UseSetting("Payments:Sandbox:PublicBaseUrl", "http://localhost");
        builder.UseSetting("Payments:Secrets:sandbox-merchant", MerchantSecret);
    });
    private sealed record Fixture(string Admin, string SecondAdmin, string Manager, string Resident, Guid Property, Guid Unit, Guid OtherUnit, Guid Dues, Guid Extra);
    private static async Task WithDb(WebApplicationFactory<Program> app, Func<BuildingFlowDbContext, Task> action)
    {
        using var scope = app.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>());
    }
    // 3000 TL dues overdue 40 days (statutory 5%/month · days/30 = 200 TL penalty) and a 500 TL extra charge not yet due.
    private static async Task<Fixture> Seed(WebApplicationFactory<Program> app, bool online = true, bool partial = false, string merchantId = "SANDBOX-1")
    {
        string User() => Guid.NewGuid().ToString();
        var (admin, second, manager, resident) = (User(), User(), User(), User());
        var property = new Property("Online " + admin[..8], "City", "District", "Address", PropertyType.APARTMENT);
        var unit = property.AddUnit("1", 1, 100); var other = property.AddUnit("2", 2, 100);
        var person = new Person(property.Id, "Resident", "Owner", "5551112233", "resident@example.test");
        person.SetProfile("NATURAL", "FOREIGN", "US", "P123456", null, null, null, resident);
        var occupancy = new UnitOccupancy(property.Id, unit.Id, person.Id, OccupancyType.OWNER, Today.AddYears(-1));
        var due = Today.AddDays(-40);
        var dues = new DuesAssessment(property.Id, Guid.NewGuid(), new string('a', 64), new DateOnly(due.Year, due.Month, 1), 3000m, "TRY", due, "Aidat");
        var duesCharge = dues.AddCharge(unit.Id);
        var month = new DateOnly(Today.Year, Today.Month, 1);
        var extra = new DuesAssessment(property.Id, Guid.NewGuid(), new string('b', 64), month, 500m, "TRY", Today.AddDays(10), "Asansör onarımı", "UNIT", ChargeKind.EXTRA, ChargeCategory.REPAIR);
        var extraCharge = extra.AddCharge(unit.Id);
        await WithDb(app, async db => {
            db.AddRange(property, person, occupancy, dues, extra);
            foreach (var user in new[] { admin, second, manager, resident }) db.Add(new PropertyMembership(property.Id, user));
            db.Add(new LatePenaltySwitch(property.Id, Guid.NewGuid(), new(2000, 1, 1), true, "Yönetim planı"));
            if (online)
            {
                var settings = new PropertyPaymentSettings(property.Id); settings.Update(true, partial, true, true, "BF"); db.Add(settings);
                if (!await db.PaymentProviders.AnyAsync(value => value.Code == "SANDBOX")) db.Add(new PaymentProvider("SANDBOX", true));
                var merchant = new PaymentMerchantAccount(property.Id, Guid.NewGuid(), null, "SANDBOX", "Test", merchantId, "sandbox-merchant", PaymentEnvironment.SANDBOX);
                merchant.MarkDefault(true); db.Add(merchant);
            }
            await db.SaveChangesAsync();
            var provider = await db.PaymentProviders.SingleOrDefaultAsync(value => value.Code == "SANDBOX");
            if (online && provider is { IsEnabled: false }) { provider.SetEnabled(true); await db.SaveChangesAsync(); }
        });
        return new(admin, second, manager, resident, property.Id, unit.Id, other.Id, duesCharge.Id, extraCharge.Id);
    }
    private static string Context(Fixture x, Guid? unit = null) => $"propertyId={x.Property}&unitId={unit ?? x.Unit}&role=OWNER";
    private static async Task<string?> Code(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    private static Task<HttpResponseMessage> Intent(HttpClient client, Fixture x, Guid[] charges, string? amount = null, Guid? request = null) =>
        client.PostAsJsonAsync("/api/v1/mobile/payments/intents", new CreateIntentRequest(request ?? Guid.NewGuid(), x.Property, x.Unit, "OWNER", charges, amount));
    private static async Task<ResidentIntentResponse> Open(HttpClient client, Fixture x, Guid[] charges, string? amount = null)
    {
        var response = await Intent(client, x, charges, amount);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ResidentIntentResponse>(Json))!;
    }
    private static async Task<HttpResponseMessage> Checkout(WebApplicationFactory<Program> app, ResidentIntentResponse intent, string result)
    {
        using var anonymous = app.CreateClient();
        return await anonymous.PostAsync(new Uri(intent.CheckoutUrl!).PathAndQuery, new FormUrlEncodedContent([new("result", result)]));
    }
    private static async Task<ResidentIntentResponse> Status(HttpClient client, Fixture x, Guid intent) =>
        (await client.GetFromJsonAsync<ResidentIntentResponse>($"/api/v1/mobile/payments/intents/{intent}?{Context(x)}", Json))!;
    private static Task<DuesChargeResponse?> Charge(HttpClient admin, Fixture x, Guid charge) =>
        admin.GetFromJsonAsync<DuesChargeResponse>($"/api/v1/properties/{x.Property}/dues/{charge}", Json);
    private static async Task<string> Reference(WebApplicationFactory<Program> app, Guid intent)
    {
        string? reference = null;
        await WithDb(app, async db => reference = (await db.PaymentIntents.SingleAsync(value => value.Id == intent)).ProviderReference);
        return reference!;
    }

    [PostgresFact] public async Task ResidentPaysDuesWithPenaltyAndExtraChargeThroughVerifiedSandbox()
    {
        await using var app = App(); var x = await Seed(app);
        using var resident = TestToken.Client(app, x.Resident, "RESIDENT"); using var admin = TestToken.Client(app, x.Admin, "SITE_ADMIN");
        var options = (await resident.GetFromJsonAsync<ResidentPaymentOptions>("/api/v1/mobile/payments/options?" + Context(x), Json))!;
        Assert.True(options.OnlineEnabled); Assert.False(options.PartialAllowed);
        var dues = Assert.Single(options.Charges, value => value.ChargeId == x.Dues);
        Assert.Equal(("3000.00", "200.00", "3200.00", "DUES"), (dues.Principal, dues.Penalty, dues.Total, dues.ChargeKind));
        var extra = Assert.Single(options.Charges, value => value.ChargeId == x.Extra);
        Assert.Equal(("500.00", "0.00", "EXTRA", "REPAIR"), (extra.Principal, extra.Penalty, extra.ChargeKind, extra.ChargeCategory));

        var intent = await Open(resident, x, [x.Dues, x.Extra]);
        Assert.Equal(("PENDING", "3700.00"), (intent.Status, intent.Amount));
        Assert.Equal(["PENALTY:200.00", "PRINCIPAL:3000.00", "PRINCIPAL:500.00"], intent.Lines.Select(value => value.Kind + ":" + value.Amount));
        var busy = await Intent(resident, x, [x.Extra]);
        Assert.Equal(HttpStatusCode.Conflict, busy.StatusCode); Assert.Equal("BF_PAYMENT_INTENT_ACTIVE", await Code(busy));
        using (var anonymous = app.CreateClient())
        {
            var page = await anonymous.GetAsync(new Uri(intent.CheckoutUrl!).PathAndQuery);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync();
            Assert.Contains("TEST ORTAMI", html); Assert.DoesNotContain("<input", html); Assert.DoesNotContain("<script", html);
        }
        Assert.Equal(HttpStatusCode.OK, (await Checkout(app, intent, "success")).StatusCode);

        var done = await Status(resident, x, intent.Id);
        Assert.Equal("SUCCEEDED", done.Status); Assert.NotNull(done.PaymentId);
        Assert.Equal($"BF-{Today.Year}-000001", done.ReceiptNumber);
        Assert.Equal("0.00", (await Charge(admin, x, x.Dues))!.Balance); Assert.Equal("0.00", (await Charge(admin, x, x.Extra))!.Balance);
        Assert.Equal("0.00", (await admin.GetFromJsonAsync<LatePenaltyChargeDetail>($"/api/v1/properties/{x.Property}/late-penalties/charges/{x.Dues}", Json))!.PenaltyBalance);
        var payment = (await admin.GetFromJsonAsync<PaymentResponse>($"/api/v1/properties/{x.Property}/payments/{done.PaymentId}", Json))!;
        Assert.Equal(("ONLINE_CARD", "3700.00", done.ReceiptNumber), (payment.Method, payment.Amount, payment.ReceiptNumber));
        Assert.Contains(payment.Allocations, value => value.ChargeKind == "EXTRA" && value.ChargeCategory == "REPAIR" && value.Amount == "500.00");
        var receipt = (await resident.GetFromJsonAsync<PaymentReceiptResponse>($"/api/v1/mobile/payments/receipts/{done.PaymentId}?{Context(x)}", Json))!;
        Assert.Equal((done.ReceiptNumber, 3), (receipt.Number, receipt.Lines.Count));
        var ledger = (await admin.GetFromJsonAsync<LedgerPage>($"/api/v1/properties/{x.Property}/payment-ledger", Json))!;
        Assert.Equal(("COLLECTION", "3700.00"), (Assert.Single(ledger.Items).Type, ledger.Totals.Net));
        var online = (await admin.GetFromJsonAsync<OnlinePaymentPage>($"/api/v1/properties/{x.Property}/online-payments", Json))!;
        Assert.Equal(("3700.00", "73.63"), (online.Summary.SucceededAmount, online.Summary.FeeAmount));
        var run = (await (await admin.PostAsJsonAsync($"/api/v1/properties/{x.Property}/payment-reconciliation/run", new ReconciliationRunRequest(null))).Content.ReadFromJsonAsync<ReconciliationRunResult>(Json))!;
        Assert.Equal(0, run.IssuesOpened);
        var settlement = (await admin.GetFromJsonAsync<SettlementReport>($"/api/v1/properties/{x.Property}/payment-reconciliation/settlement", Json))!;
        var day = Assert.Single(settlement.Days); Assert.Equal(("3700.00", "73.63", "3626.37"), (day.Collected, day.Fees, day.Net));
        var cancel = await admin.PutAsJsonAsync($"/api/v1/properties/{x.Property}/payments/{done.PaymentId}/cancel", new CancelChargeRequest("Yanlış", payment.Version));
        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode); Assert.Equal("BF_PAYMENT_ONLINE_CANCEL", await Code(cancel));
    }

    [PostgresFact] public async Task WebhooksAreSignedTimeBoundAndIdempotent()
    {
        await using var app = App(); var x = await Seed(app);
        using var resident = TestToken.Client(app, x.Resident, "RESIDENT"); using var anonymous = app.CreateClient();
        var intent = await Open(resident, x, [x.Extra]);
        var reference = await Reference(app, intent.Id);
        var sandbox = app.Services.GetRequiredService<SandboxPaymentProvider>();
        async Task<HttpResponseMessage> Send((Dictionary<string, string> Headers, byte[] Body) notice, string? header = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payment-webhooks/sandbox") { Content = new ByteArrayContent(notice.Body) };
            request.Headers.TryAddWithoutValidation(SandboxPaymentProvider.SignatureHeader, header ?? notice.Headers[SandboxPaymentProvider.SignatureHeader]);
            return await anonymous.SendAsync(request);
        }
        // A forged "succeeded" notice cannot settle anything: the provider still reports PENDING.
        var forged = sandbox.Sign("payment.succeeded", reference, DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.OK, (await Send(forged)).StatusCode);
        Assert.Equal("PENDING", (await Status(resident, x, intent.Id)).Status);
        var bad = await Send(forged with { Body = forged.Body.Append((byte)' ').ToArray() });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode); Assert.Equal("BF_PAYMENT_WEBHOOK_INVALID", await Code(bad));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(forged, "t=1,v1=00")).StatusCode);
        var stale = sandbox.Sign("payment.succeeded", reference, DateTimeOffset.UtcNow, signedAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(stale)).StatusCode);

        var notice = sandbox.Complete(reference, true)!.Value;
        for (var attempt = 0; attempt < 3; attempt++) Assert.Equal(HttpStatusCode.OK, (await Send(notice)).StatusCode);
        Assert.Equal("SUCCEEDED", (await Status(resident, x, intent.Id)).Status);
        await WithDb(app, async db => {
            Assert.Single(await db.Payments.Where(value => value.PropertyId == x.Property).ToListAsync());
            Assert.Single(await db.PaymentLedgerEntries.Where(value => value.PropertyId == x.Property).ToListAsync());
            Assert.Equal(2, await db.PaymentProviderEvents.CountAsync(value => value.ProviderReference == reference));
        });
    }

    [PostgresFact] public async Task ChangedDebtGoesToReviewAndRefundNeedsSecondApprover()
    {
        await using var app = App(); var x = await Seed(app);
        using var resident = TestToken.Client(app, x.Resident, "RESIDENT"); using var admin = TestToken.Client(app, x.Admin, "SITE_ADMIN");
        using var second = TestToken.Client(app, x.SecondAdmin, "SITE_ADMIN"); using var manager = TestToken.Client(app, x.Manager, "MANAGER");
        var intent = await Open(resident, x, [x.Extra]);
        var charge = (await Charge(admin, x, x.Extra))!;
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/properties/{x.Property}/dues/{x.Extra}/cancel", new CancelChargeRequest("Yanlış tahakkuk", charge.Version))).StatusCode);
        await Checkout(app, intent, "success");
        Assert.Equal("REVIEW", (await Status(resident, x, intent.Id)).Status);
        var detail = (await admin.GetFromJsonAsync<OnlinePaymentResponse>($"/api/v1/properties/{x.Property}/online-payments/{intent.Id}", Json))!;
        Assert.Equal("DEBT_CHANGED", Assert.Single(detail.Issues!).Type);
        var settle = await admin.PutAsJsonAsync($"/api/v1/properties/{x.Property}/online-payments/{intent.Id}/settle", new VersionRequest(detail.Version));
        Assert.Equal("BF_PAYMENT_REVIEW_UNALLOCATABLE", await Code(settle));

        var request = new RefundRequest(Guid.NewGuid(), "Borç iptal edildi");
        var created = await manager.PostAsJsonAsync($"/api/v1/properties/{x.Property}/online-payments/{intent.Id}/refunds", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var refund = (await created.Content.ReadFromJsonAsync<RefundResponse>(Json))!;
        Assert.True((await (await manager.PostAsJsonAsync($"/api/v1/properties/{x.Property}/online-payments/{intent.Id}/refunds", request)).Content.ReadFromJsonAsync<RefundResponse>(Json))!.Replayed);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/v1/properties/{x.Property}/payment-refunds/{refund.Id}/decision", new RefundDecisionRequest("APPROVE", null, refund.Version))).StatusCode);
        var decided = await admin.PutAsJsonAsync($"/api/v1/properties/{x.Property}/payment-refunds/{refund.Id}/decision", new RefundDecisionRequest("APPROVE", null, refund.Version));
        Assert.Equal(HttpStatusCode.OK, decided.StatusCode); Assert.Equal("SUCCEEDED", (await decided.Content.ReadFromJsonAsync<RefundResponse>(Json))!.Status);
        Assert.Equal("REFUNDED", (await Status(resident, x, intent.Id)).Status);
        var ledger = (await admin.GetFromJsonAsync<LedgerPage>($"/api/v1/properties/{x.Property}/payment-ledger", Json))!;
        Assert.Equal(("500.00", "-500.00", "0.00"), (ledger.Totals.Credits, ledger.Totals.Debits, ledger.Totals.Net));
        Assert.Contains(ledger.Items, value => value.Type == "UNALLOCATED_RECEIPT"); Assert.Contains(ledger.Items, value => value.Type == "ONLINE_REFUND");

        var paid = await Open(resident, x, [x.Dues]);
        await Checkout(app, paid, "success");
        var own = (await (await admin.PostAsJsonAsync($"/api/v1/properties/{x.Property}/online-payments/{paid.Id}/refunds", new RefundRequest(Guid.NewGuid(), "Mükerrer"))).Content.ReadFromJsonAsync<RefundResponse>(Json))!;
        var self = await admin.PutAsJsonAsync($"/api/v1/properties/{x.Property}/payment-refunds/{own.Id}/decision", new RefundDecisionRequest("APPROVE", null, own.Version));
        Assert.Equal("BF_PAYMENT_SECOND_APPROVER", await Code(self));
        Assert.Equal(HttpStatusCode.OK, (await second.PutAsJsonAsync($"/api/v1/properties/{x.Property}/payment-refunds/{own.Id}/decision", new RefundDecisionRequest("APPROVE", null, own.Version))).StatusCode);
        var reopened = (await Charge(admin, x, x.Dues))!;
        Assert.Equal("3000.00", reopened.Balance);
        Assert.Equal("CANCELLED", (await admin.GetFromJsonAsync<PaymentResponse>($"/api/v1/properties/{x.Property}/payments/{(await Status(resident, x, paid.Id)).PaymentId}", Json))!.Status);
    }

    [PostgresFact] public async Task PartialPaymentsFailuresCancellationAndChargebackFollowTheStateMachine()
    {
        await using var app = App(); var x = await Seed(app, partial: true);
        using var resident = TestToken.Client(app, x.Resident, "RESIDENT"); using var admin = TestToken.Client(app, x.Admin, "SITE_ADMIN");
        var partial = await Open(resident, x, [x.Dues, x.Extra], "250.00");
        Assert.Equal(["PENALTY:200.00", "PRINCIPAL:50.00"], partial.Lines.Select(value => value.Kind + ":" + value.Amount));
        await Checkout(app, partial, "fail");
        Assert.Contains("BF_PAYMENT_PARTIAL_AMOUNT", await (await Intent(resident, x, [x.Extra], "500.01")).Content.ReadAsStringAsync());
        var failed = await Status(resident, x, partial.Id);
        Assert.Equal(("FAILED", "CARD_DECLINED"), (failed.Status, failed.FailureCode));

        var cancelled = await Open(resident, x, [x.Extra]);
        var cancel = await resident.PutAsync($"/api/v1/mobile/payments/intents/{cancelled.Id}/cancel?{Context(x)}", null);
        Assert.Equal("CANCELLED", (await cancel.Content.ReadFromJsonAsync<ResidentIntentResponse>(Json))!.Status);
        await Checkout(app, cancelled, "success");
        var issues = (await admin.GetFromJsonAsync<PagedRows<ReconciliationItemResponse>>($"/api/v1/properties/{x.Property}/payment-reconciliation/items?status=OPEN", Json))!;
        var mismatch = Assert.Single(issues.Items); Assert.Equal(("PROVIDER_MISMATCH", cancelled.Id), (mismatch.Type, mismatch.IntentId));
        Assert.Equal("500.00", (await Charge(admin, x, x.Extra))!.Balance);
        var resolved = await admin.PutAsJsonAsync($"/api/v1/properties/{x.Property}/payment-reconciliation/items/{mismatch.Id}/resolve", new ResolveItemRequest("Sağlayıcı panelinden iade edildi.", mismatch.Version));
        Assert.Equal("RESOLVED", (await resolved.Content.ReadFromJsonAsync<ReconciliationItemResponse>(Json))!.Status);

        var charged = await Open(resident, x, [x.Extra]);
        await Checkout(app, charged, "success");
        var sandbox = app.Services.GetRequiredService<SandboxPaymentProvider>();
        var notice = sandbox.ChargeBack(await Reference(app, charged.Id))!.Value;
        using var anonymous = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payment-webhooks/SANDBOX") { Content = new ByteArrayContent(notice.Body) };
        request.Headers.TryAddWithoutValidation(SandboxPaymentProvider.SignatureHeader, notice.Headers[SandboxPaymentProvider.SignatureHeader]);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.SendAsync(request)).StatusCode);
        Assert.Equal("CHARGED_BACK", (await Status(resident, x, charged.Id)).Status);
        Assert.Equal("500.00", (await Charge(admin, x, x.Extra))!.Balance);
    }

    [PostgresFact] public async Task OnlinePaymentStaysClosedUntilConfiguredAndProviderOutageIsRecorded()
    {
        await using var app = App();
        var closed = await Seed(app, online: false);
        using var resident = TestToken.Client(app, closed.Resident, "RESIDENT");
        Assert.False((await resident.GetFromJsonAsync<ResidentPaymentOptions>("/api/v1/mobile/payments/options?" + Context(closed), Json))!.OnlineEnabled);
        Assert.Equal("BF_PAYMENT_ONLINE_DISABLED", await Code(await Intent(resident, closed, [closed.Dues])));
        var full = await Seed(app, partial: false);
        using var payer = TestToken.Client(app, full.Resident, "RESIDENT");
        Assert.Equal("BF_PAYMENT_PARTIAL_DISABLED", await Code(await Intent(payer, full, [full.Dues], "10.00")));
        var outage = await Seed(app, merchantId: "SANDBOX-UNAVAILABLE");
        using var unlucky = TestToken.Client(app, outage.Resident, "RESIDENT");
        var request = Guid.NewGuid();
        Assert.Equal("BF_PAYMENT_PROVIDER_FAILURE", await Code(await Intent(unlucky, outage, [outage.Extra], request: request)));
        var replay = (await (await Intent(unlucky, outage, [outage.Extra], request: request)).Content.ReadFromJsonAsync<ResidentIntentResponse>(Json))!;
        Assert.Equal(("FAILED", true), (replay.Status, replay.Replayed));
        // A failed intent releases its reservation: the next attempt reaches the provider again instead of BF_PAYMENT_INTENT_ACTIVE.
        Assert.Equal("BF_PAYMENT_PROVIDER_FAILURE", await Code(await Intent(unlucky, outage, [outage.Extra])));
    }

    [PostgresFact] public async Task ResidentScopeAndManagementRolesAreEnforced()
    {
        await using var app = App(); var x = await Seed(app);
        using var anonymous = app.CreateClient(); using var resident = TestToken.Client(app, x.Resident, "RESIDENT");
        using var stranger = TestToken.Client(app, Guid.NewGuid().ToString(), "RESIDENT"); using var accountant = TestToken.Client(app, x.Manager, "ACCOUNTANT");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/mobile/payments/options?" + Context(x))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await resident.GetAsync("/api/v1/mobile/payments/options?" + Context(x, x.OtherUnit))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/api/v1/mobile/payments/options?" + Context(x))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Intent(resident, x, [x.Dues, Guid.NewGuid()])).StatusCode);
        var intent = await Open(resident, x, [x.Extra]);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/v1/mobile/payments/intents/{intent.Id}?{Context(x)}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await resident.GetAsync($"/api/v1/properties/{x.Property}/online-payments")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await accountant.GetAsync($"/api/v1/properties/{x.Property}/online-payments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.PostAsJsonAsync($"/api/v1/properties/{x.Property}/online-payments/{intent.Id}/refunds", new RefundRequest(Guid.NewGuid(), "x"))).StatusCode);
        using var outsider = TestToken.Client(app, Guid.NewGuid().ToString(), "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/v1/properties/{x.Property}/online-payments/{intent.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/v1/properties/{x.Property}/payment-ledger")).StatusCode);
        var json = await (await accountant.GetAsync($"/api/v1/properties/{x.Property}/online-payments/{intent.Id}")).Content.ReadAsStringAsync();
        foreach (var hidden in new[] { "Resident", "P123456", "5551112233", x.Resident }) Assert.DoesNotContain(hidden, json);
    }

    [PostgresFact] public async Task SettingsBankAccountsAndMerchantsUseRolesDualApprovalAndSecretReferences()
    {
        await using var app = App(); var x = await Seed(app, online: false);
        using var admin = TestToken.Client(app, x.Admin, "SITE_ADMIN"); using var second = TestToken.Client(app, x.SecondAdmin, "SITE_ADMIN");
        using var manager = TestToken.Client(app, x.Manager, "MANAGER"); using var system = TestToken.Client(app, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var root = $"/api/v1/properties/{x.Property}";
        var defaults = (await manager.GetFromJsonAsync<PaymentSettingsResponse>(root + "/payment-settings", Json))!;
        Assert.Equal((false, true, "BF", "ABSORBED", 0L, false), (defaults.OnlineEnabled, defaults.RequireDualApproval, defaults.ReceiptPrefix, defaults.CommissionPolicy, defaults.Version, defaults.CanEdit));
        var settings = new PaymentSettingsRequest(true, true, true, true, "SITE1", "ABSORBED", 0);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync(root + "/payment-settings", settings)).StatusCode);
        Assert.Contains("BF_PAYMENT_SETTINGS_INVALID", await (await admin.PutAsJsonAsync(root + "/payment-settings", settings with { CommissionPolicy = "PAYER" })).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(root + "/payment-settings", settings with { ReceiptPrefix = "x" })).StatusCode);
        Assert.Equal(1L, (await (await admin.PutAsJsonAsync(root + "/payment-settings", settings)).Content.ReadFromJsonAsync<PaymentSettingsResponse>(Json))!.Version);

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync("/api/v1/payment-providers/SANDBOX", new UpdatePaymentProviderRequest(false, 1))).StatusCode);
        var sandboxRow = (await admin.GetFromJsonAsync<PaymentProviderResponse[]>("/api/v1/payment-providers", Json))!.Single(value => value.Code == "SANDBOX");
        Assert.True((await (await system.PutAsJsonAsync("/api/v1/payment-providers/SANDBOX", new UpdatePaymentProviderRequest(true, sandboxRow.Version))).Content.ReadFromJsonAsync<PaymentProviderResponse>(Json))!.Enabled);
        Assert.Equal("BF_PAYMENT_PROVIDER_UNAVAILABLE", await Code(await system.PutAsJsonAsync("/api/v1/payment-providers/IYZICO", new UpdatePaymentProviderRequest(true, 0))));
        var catalog = (await admin.GetFromJsonAsync<PaymentProviderResponse[]>("/api/v1/payment-providers", Json))!;
        Assert.Contains(catalog, value => value.Code == "SANDBOX" && value.Integrated); Assert.Contains(catalog, value => value.Code == "PAYTR" && !value.Integrated && !value.Enabled);

        BankAccountRequest Account(string iban) => new(Guid.NewGuid(), null, "Test Bankası", "Site Yönetimi", iban, "ALL", Today, null);
        var invalid = await manager.PostAsJsonAsync(root + "/bank-accounts", Account("TR400006200000000000000002"));
        Assert.Contains("BF_PAYMENT_IBAN_INVALID", await invalid.Content.ReadAsStringAsync());
        var created = await manager.PostAsJsonAsync(root + "/bank-accounts", Account("tr40 0006 2000 0000 0000 0000 01"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var account = (await created.Content.ReadFromJsonAsync<BankAccountResponse>(Json))!;
        Assert.Equal(("PENDING_APPROVAL", ValidIban), (account.Status, account.Iban));
        Assert.Equal("BF_PAYMENT_IBAN_DUPLICATE", await Code(await manager.PostAsJsonAsync(root + "/bank-accounts", Account(ValidIban))));
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync(root + $"/bank-accounts/{account.Id}/decision", new BankAccountDecisionRequest("APPROVE", null, account.Version))).StatusCode);
        var approved = (await (await admin.PutAsJsonAsync(root + $"/bank-accounts/{account.Id}/decision", new BankAccountDecisionRequest("APPROVE", null, account.Version))).Content.ReadFromJsonAsync<BankAccountResponse>(Json))!;
        Assert.Equal("ACTIVE", approved.Status);
        var mine = (await (await admin.PostAsJsonAsync(root + "/bank-accounts", Account(SecondIban))).Content.ReadFromJsonAsync<BankAccountResponse>(Json))!;
        Assert.Equal("BF_PAYMENT_SECOND_APPROVER", await Code(await admin.PutAsJsonAsync(root + $"/bank-accounts/{mine.Id}/decision", new BankAccountDecisionRequest("APPROVE", null, mine.Version))));
        var other = (await (await second.PutAsJsonAsync(root + $"/bank-accounts/{mine.Id}/decision", new BankAccountDecisionRequest("APPROVE", null, mine.Version))).Content.ReadFromJsonAsync<BankAccountResponse>(Json))!;
        Assert.True((await (await admin.PutAsJsonAsync(root + $"/bank-accounts/{other.Id}/default", new VersionRequest(other.Version))).Content.ReadFromJsonAsync<BankAccountResponse>(Json))!.IsDefault);
        var closed = (await (await admin.PutAsJsonAsync(root + $"/bank-accounts/{approved.Id}/close", new BankAccountCloseRequest(Today, "Hesap kapatıldı", approved.Version))).Content.ReadFromJsonAsync<BankAccountResponse>(Json))!;
        Assert.Equal(("CLOSED", (DateOnly?)Today), (closed.Status, closed.ValidTo));

        MerchantAccountRequest Merchant(string provider, string environment) => new(Guid.NewGuid(), null, provider, "Sandbox POS", "SANDBOX-9", "sandbox-merchant", environment);
        Assert.Equal("BF_PAYMENT_LIVE_DISABLED", await Code(await manager.PostAsJsonAsync(root + "/merchant-accounts", Merchant("SANDBOX", "LIVE"))));
        Assert.Equal("BF_PAYMENT_PROVIDER_UNAVAILABLE", await Code(await manager.PostAsJsonAsync(root + "/merchant-accounts", Merchant("IYZICO", "SANDBOX"))));
        var merchantResponse = await manager.PostAsJsonAsync(root + "/merchant-accounts", Merchant("SANDBOX", "SANDBOX"));
        var body = await merchantResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, merchantResponse.StatusCode); Assert.DoesNotContain(MerchantSecret, body);
        var merchant = JsonSerializer.Deserialize<MerchantAccountResponse>(body, Json)!;
        Assert.True(merchant.SecretConfigured);
        Assert.True((await (await manager.PostAsync(root + $"/merchant-accounts/{merchant.Id}/test", null)).Content.ReadFromJsonAsync<MerchantAccountResponse>(Json))!.LastCheckSucceeded);
        var current = (await admin.GetFromJsonAsync<PagedRows<MerchantAccountResponse>>(root + "/merchant-accounts", Json))!.Items.Single();
        Assert.True((await (await admin.PutAsJsonAsync(root + $"/merchant-accounts/{merchant.Id}/default", new VersionRequest(current.Version))).Content.ReadFromJsonAsync<MerchantAccountResponse>(Json))!.IsDefault);
        using var resident = TestToken.Client(app, x.Resident, "RESIDENT");
        var options = (await resident.GetFromJsonAsync<ResidentPaymentOptions>("/api/v1/mobile/payments/options?" + Context(x), Json))!;
        Assert.True(options.OnlineEnabled); Assert.Equal(SecondIban, Assert.Single(options.BankAccounts).Iban);
        using var outsider = TestToken.Client(app, Guid.NewGuid().ToString(), "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(root + "/bank-accounts")).StatusCode);
    }

    [PostgresFact] public async Task ExtraChargesShareAMonthKeepDuesUniqueAndAcceptManualBankTransfers()
    {
        await using var app = App(); var x = await Seed(app, online: false);
        using var admin = TestToken.Client(app, x.Admin, "SITE_ADMIN"); using var second = TestToken.Client(app, x.SecondAdmin, "SITE_ADMIN");
        var root = $"/api/v1/properties/{x.Property}";
        var month = Today.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        CreateAssessmentRequest Assessment(string? kind, string? category) => new(Guid.NewGuid(), [x.Unit, x.OtherUnit], month, "120.50", "TRY", Today.AddDays(5), "Ek ödeme", "UNIT", kind, category);
        Assert.Contains("BF_DUES_KIND_INVALID", await (await admin.PostAsJsonAsync(root + "/dues/assessments", Assessment("EXTRA", null))).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(root + "/dues/assessments", Assessment(null, "FIXTURE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(root + "/dues/assessments", Assessment("EXTRA", "FIXTURE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(root + "/dues/assessments", Assessment("EXTRA", "HEATING"))).StatusCode);
        var extra = (await (await admin.PostAsJsonAsync(root + "/dues/assessments", Assessment("EXTRA", "OTHER"))).Content.ReadFromJsonAsync<AssessmentResponse>(Json))!;
        Assert.Equal(("EXTRA", "OTHER"), (extra.Kind, extra.Category));
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(root + "/dues/assessments", Assessment(null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(root + "/dues/assessments", Assessment("DUES", null))).StatusCode);
        var page = (await admin.GetFromJsonAsync<DuesPage>(root + $"/dues?kind=EXTRA&unitId={x.Unit}&pageSize=100", Json))!;
        Assert.Equal(4, page.TotalCount); Assert.All(page.Items, value => Assert.Equal("EXTRA", value.Kind));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync(root + "/dues?kind=FEE")).StatusCode);

        var account = (await (await admin.PostAsJsonAsync(root + "/bank-accounts", new BankAccountRequest(Guid.NewGuid(), null, "Banka", "Yönetim", ValidIban, "EXTRA", Today.AddDays(-30), null))).Content.ReadFromJsonAsync<BankAccountResponse>(Json))!;
        var chargeVersion = (await Charge(admin, x, x.Extra))!.Version;
        CreatePaymentRequest Transfer(Guid? bank) => new(Guid.NewGuid(), x.Unit, "500.00", "TRY", Today, "BANK_TRANSFER", "HVL-1", "Havale", [new(x.Extra, "500.00", chargeVersion)], bank);
        Assert.Equal("BF_PAYMENT_BANK_ACCOUNT_UNUSABLE", await Code(await admin.PostAsJsonAsync(root + "/payments", Transfer(account.Id))));
        await second.PutAsJsonAsync(root + $"/bank-accounts/{account.Id}/decision", new BankAccountDecisionRequest("APPROVE", null, account.Version));
        var created = await admin.PostAsJsonAsync(root + "/payments", Transfer(account.Id));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var payment = (await created.Content.ReadFromJsonAsync<PaymentResponse>(Json))!;
        Assert.Equal((account.Id, (string?)ValidIban, $"BF-{Today.Year}-000001"), (payment.BankAccountId!.Value, payment.BankAccountIban, payment.ReceiptNumber));
        var receipt = (await admin.GetFromJsonAsync<PaymentReceiptResponse>(root + $"/payments/{payment.Id}/receipt", Json))!;
        Assert.Equal(("EXTRA", "REPAIR", "Asansör onarımı"), (receipt.Lines[0].ChargeKind, receipt.Lines[0].ChargeCategory, receipt.Lines[0].Description));
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(root + $"/payments/{payment.Id}/cancel", new CancelChargeRequest("Hatalı", payment.Version))).StatusCode);
        var ledger = (await admin.GetFromJsonAsync<LedgerPage>(root + "/payment-ledger", Json))!;
        Assert.Equal(("500.00", "-500.00"), (ledger.Totals.Credits, ledger.Totals.Debits));
        var next = await admin.PostAsJsonAsync(root + "/payments", Transfer(null) with { Reference = "HVL-2", Allocations = [new(x.Extra, "500.00", (await Charge(admin, x, x.Extra))!.Version)] });
        Assert.Equal($"BF-{Today.Year}-000002", (await next.Content.ReadFromJsonAsync<PaymentResponse>(Json))!.ReceiptNumber);
    }

    // QA FAZ 06: a double tap or parallel retry opens one intent; the same UUID replays it instead of reserving twice.
    [PostgresFact] public async Task DuplicateTapsOpenExactlyOneIntent()
    {
        await using var app = App(); var x = await Seed(app);
        using var resident = TestToken.Client(app, x.Resident, "RESIDENT");
        var requests = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var responses = await Task.WhenAll(requests.Select(request => Intent(resident, x, [x.Extra], null, request)));
        var created = Assert.Single(responses, value => value.StatusCode == HttpStatusCode.Created);
        foreach (var busy in responses.Where(value => value != created))
        {
            Assert.Equal(HttpStatusCode.Conflict, busy.StatusCode); Assert.Equal("BF_PAYMENT_INTENT_ACTIVE", await Code(busy));
        }
        var first = (await created.Content.ReadFromJsonAsync<ResidentIntentResponse>(Json))!;
        var replay = await Intent(resident, x, [x.Extra], null, requests[Array.IndexOf(responses, created)]);
        Assert.True(replay.IsSuccessStatusCode, replay.StatusCode.ToString());
        Assert.Equal(first.Id, (await replay.Content.ReadFromJsonAsync<ResidentIntentResponse>(Json))!.Id);
        await WithDb(app, async db => Assert.Single(await db.PaymentIntents.Where(value => value.PropertyId == x.Property).ToListAsync()));
    }

    // QA FAZ 06: a late "failed" notice after success cannot reverse the payment or add ledger rows.
    [PostgresFact] public async Task OutOfOrderFailureNoticeCannotUndoASettledPayment()
    {
        await using var app = App(); var x = await Seed(app);
        using var resident = TestToken.Client(app, x.Resident, "RESIDENT"); using var anonymous = app.CreateClient();
        var intent = await Open(resident, x, [x.Extra]);
        var reference = await Reference(app, intent.Id);
        var sandbox = app.Services.GetRequiredService<SandboxPaymentProvider>();
        async Task<HttpStatusCode> Send((Dictionary<string, string> Headers, byte[] Body) notice)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payment-webhooks/sandbox") { Content = new ByteArrayContent(notice.Body) };
            request.Headers.TryAddWithoutValidation(SandboxPaymentProvider.SignatureHeader, notice.Headers[SandboxPaymentProvider.SignatureHeader]);
            return (await anonymous.SendAsync(request)).StatusCode;
        }
        Assert.Equal(HttpStatusCode.OK, await Send(sandbox.Complete(reference, true)!.Value));
        Assert.Equal("SUCCEEDED", (await Status(resident, x, intent.Id)).Status);
        Assert.Equal(HttpStatusCode.OK, await Send(sandbox.Sign("payment.failed", reference, DateTimeOffset.UtcNow)));
        Assert.Equal(HttpStatusCode.OK, await Send(sandbox.Sign("payment.succeeded", reference, DateTimeOffset.UtcNow)));
        Assert.Equal("SUCCEEDED", (await Status(resident, x, intent.Id)).Status);
        await WithDb(app, async db =>
        {
            Assert.Single(await db.Payments.Where(value => value.PropertyId == x.Property).ToListAsync());
            Assert.Single(await db.PaymentLedgerEntries.Where(value => value.PropertyId == x.Property).ToListAsync());
        });
    }
}
