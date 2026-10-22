using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class SmsApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly CreatePersonRequest Ada = new("Ada", "Kaya", "0555 123 45 67", "ada@example.test", "NATURAL", "DOMESTIC", "TR", "12345678901");
    private static SaveSmsAccountRequest Account(long version = 0, string? secret = null, bool enabled = true) => new("Site SMS", "MOCK", "BUILDING", "api-user", secret, null, 5, enabled, version);

    [PostgresTheory] [InlineData("SITE_ADMIN")] [InlineData("MANAGER")]
    public async Task OnlySuperAdminConfiguresAccountAndCredits(string role)
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(Root(x.Property) + "/account", Account())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Root(x.Property) + "/account/credits", new SmsCreditRequest("LOAD", 10))).StatusCode);
        var raw = await client.GetStringAsync(Root(x.Property));
        Assert.Contains("\"purpose\":\"TRANSACTIONAL\"", raw); Assert.Contains("\"purpose\":\"OPERATIONAL\"", raw);
        var overview = await client.GetFromJsonAsync<SmsOverview>(Root(x.Property));
        Assert.Null(overview!.Account); Assert.Contains(overview.Types, t => t.Code == SmsTypes.PersonRegistered);
    }

    [PostgresTheory] [InlineData("RESIDENT")] [InlineData("ACCOUNTANT")] [InlineData("")]
    public async Task NonManagersCannotReadSmsData(string role)
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, role);
        foreach (var path in new[] { "", "/templates", "/messages", "/account/credits" }) Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Root(x.Property) + path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Root(x.Property) + "/messages", new SendSmsRequest(Guid.NewGuid(), null, "+905551234567", "Hi"))).StatusCode);
    }

    [PostgresFact] public async Task AccountSecretIsWriteOnlyAndCreditsAreLedgered()
    {
        var x = await Seed(); using var admin = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var root = Root(x.Property);
        var created = (await (await admin.PutAsJsonAsync(root + "/account", Account(secret: "provider-secret-value"))).Content.ReadFromJsonAsync<SmsAccountResponse>())!;
        Assert.True(created.HasSecret); Assert.Equal(0, created.CreditBalance); Assert.True(created.IsLowCredit);
        Assert.DoesNotContain("provider-secret-value", await admin.GetStringAsync(root));
        await factory.WithDb(async db => Assert.DoesNotContain("provider-secret-value", (await db.SmsAccounts.SingleAsync(a => a.Id == created.Id)).ProtectedSecret!));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(root + "/account", Account())).StatusCode);
        var updated = (await (await admin.PutAsJsonAsync(root + "/account", Account(created.Version))).Content.ReadFromJsonAsync<SmsAccountResponse>())!;
        Assert.True(updated.HasSecret);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(root + "/account", Account(updated.Version) with { Provider = "NETGSM" })).StatusCode);
        var load = (await (await admin.PostAsJsonAsync(root + "/account/credits", new SmsCreditRequest("LOAD", 100, 0.2m, "TRY", "INV-1"))).Content.ReadFromJsonAsync<SmsCreditTransactionResponse>())!;
        Assert.Equal(100, load.BalanceAfter);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(root + "/account/credits", new SmsCreditRequest("ADJUST", -101))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(root + "/account/credits", new SmsCreditRequest("CONSUME", 1))).StatusCode);
        var adjust = (await (await admin.PostAsJsonAsync(root + "/account/credits", new SmsCreditRequest("ADJUST", -10, Description: "Düzeltme"))).Content.ReadFromJsonAsync<SmsCreditTransactionResponse>())!;
        Assert.Equal(90, adjust.BalanceAfter);
        var page = (await admin.GetFromJsonAsync<PagedRows<SmsCreditTransactionResponse>>(root + "/account/credits?pageSize=1"))!;
        Assert.Equal(2, page.TotalCount); Assert.Equal("ADJUST", Assert.Single(page.Items).Type);
    }

    [PostgresFact] public async Task TemplatesOverrideDefaultsWithAllowedParametersOnly()
    {
        var x = await Seed(); using var manager = TestToken.Client(factory, x.Actor, "MANAGER"); var root = Root(x.Property);
        var defaults = (await manager.GetFromJsonAsync<SmsTemplateResponse[]>(root + "/templates"))!;
        Assert.All(defaults, t => Assert.True(t.IsSystemDefault));
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync(root + "/templates", new SmsTemplateRequest(SmsTypes.PersonRegistered, "Kayıt", "Kodunuz {{code}} {{password}}"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync(root + "/templates", new SmsTemplateRequest("UNKNOWN", "X", "Y"))).StatusCode);
        var custom = (await (await manager.PostAsJsonAsync(root + "/templates", new SmsTemplateRequest(SmsTypes.PersonRegistered, "Kayıt", "Kodunuz: {{code}}"))).Content.ReadFromJsonAsync<SmsTemplateResponse>())!;
        Assert.False(custom.IsSystemDefault); Assert.Equal(1, custom.EstimatedSegments);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync(root + "/templates", new SmsTemplateRequest(SmsTypes.PersonRegistered, "Again", "{{code}}"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PutAsJsonAsync($"{root}/templates/{custom.Id}", new SmsTemplateRequest(SmsTypes.PersonRegistered, "Kayıt", "{{code}}", true, custom.Version + 1))).StatusCode);
        var disabled = (await (await manager.PutAsJsonAsync($"{root}/templates/{custom.Id}", new SmsTemplateRequest(SmsTypes.PersonRegistered, "Kayıt", "{{code}}", false, custom.Version))).Content.ReadFromJsonAsync<SmsTemplateResponse>())!;
        Assert.False(disabled.IsEnabled);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"{root}/templates/{custom.Id}?version={disabled.Version}")).StatusCode);
        Assert.True((await manager.GetFromJsonAsync<SmsTemplateResponse[]>(root + "/templates"))!.Single(t => t.TypeCode == SmsTypes.PersonRegistered).IsSystemDefault);
    }

    [PostgresFact] public async Task RegistrationSmsIsMaskedEncryptedAndDeliveredByMock()
    {
        var x = await Seed(); using var admin = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var root = Root(x.Property);
        var withoutAccount = await CreatePerson(admin, x.Property);
        Assert.Equal("FAILED", withoutAccount.RegistrationSms!.Status); Assert.Equal(SmsQueue.NoAccount, withoutAccount.RegistrationSms.ErrorCode);
        var account = (await (await admin.PutAsJsonAsync(root + "/account", Account())).Content.ReadFromJsonAsync<SmsAccountResponse>())!;
        await admin.PostAsJsonAsync(root + "/account/credits", new SmsCreditRequest("LOAD", 3));
        // The same identity, phone and e-mail may exist only once per Property, so the first fixture person is removed.
        await factory.WithDb(async db => { var first = await db.People.SingleAsync(p => p.Id == withoutAccount.Id); db.Entry(first).Property(p => p.IsDeleted).CurrentValue = true; await db.SaveChangesAsync(); });
        var person = await CreatePerson(admin, x.Property);
        Assert.Equal("QUEUED", person.RegistrationSms!.Status);
        var messageId = person.RegistrationSms.MessageId!.Value;
        await factory.WithDb(async db => {
            var message = await db.SmsMessages.SingleAsync(m => m.Id == messageId);
            Assert.DoesNotContain(person.IssuedPersonCode!, message.MaskedBody); Assert.Contains("Ada KAYA", message.MaskedBody);
            Assert.DoesNotContain(person.IssuedPersonCode!, message.ProtectedPayload!); Assert.DoesNotContain("5551234567", message.ProtectedPayload!);
            Assert.Equal("+90********67", message.PhoneMasked); Assert.Equal(person.Id, message.PersonId);
        });
        Assert.True(await Dispatch() >= 1);
        await factory.WithDb(async db => {
            var message = await db.SmsMessages.SingleAsync(m => m.Id == messageId);
            Assert.Equal(SmsMessageStatus.SENT, message.Status); Assert.Equal(SmsProvider.MOCK, message.Provider); Assert.StartsWith("MOCK-", message.ProviderMessageId); Assert.Null(message.ProtectedPayload);
            Assert.Equal(3 - message.CreditCost, (await db.SmsAccounts.SingleAsync(a => a.Id == account.Id)).CreditBalance);
            Assert.Equal(-message.CreditCost, (await db.SmsCreditTransactions.SingleAsync(t => t.MessageId == messageId)).Quantity);
        });
        var history = (await admin.GetFromJsonAsync<PagedRows<SmsMessageResponse>>(root + "/messages?status=SENT"))!;
        Assert.Contains(history.Items, m => m.Id == messageId && !m.Message.Contains(person.IssuedPersonCode!));
        Assert.DoesNotContain(person.IssuedPersonCode!, await admin.GetStringAsync(root + "/messages"));
    }

    [PostgresFact] public async Task DisabledTemplateSkipsAndRegenerateCanResend()
    {
        var x = await Seed(); using var admin = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var root = Root(x.Property);
        await admin.PutAsJsonAsync(root + "/account", Account()); await admin.PostAsJsonAsync(root + "/account/credits", new SmsCreditRequest("LOAD", 10));
        var template = (await (await admin.PostAsJsonAsync(root + "/templates", new SmsTemplateRequest(SmsTypes.PersonRegistered, "Kapalı", "{{code}}", false))).Content.ReadFromJsonAsync<SmsTemplateResponse>())!;
        var person = await CreatePerson(admin, x.Property);
        Assert.Equal("SKIPPED", person.RegistrationSms!.Status); Assert.Null(person.RegistrationSms.MessageId);
        await admin.DeleteAsync($"{root}/templates/{template.Id}?version={template.Version}");
        var renewed = (await (await admin.PostAsJsonAsync($"/api/v1/properties/{x.Property}/people/{person.Id}/code", new RegeneratePersonCodeRequest(person.Version, true))).Content.ReadFromJsonAsync<PersonResponse>())!;
        Assert.Equal("QUEUED", renewed.RegistrationSms!.Status);
        var silent = (await (await admin.PostAsJsonAsync($"/api/v1/properties/{x.Property}/people/{person.Id}/code", new RegeneratePersonCodeRequest(renewed.Version))).Content.ReadFromJsonAsync<PersonResponse>())!;
        Assert.Null(silent.RegistrationSms);
    }

    [PostgresFact] public async Task ManualSendIsIdempotentAndScoped()
    {
        var x = await Seed(); var other = await Seed(); using var admin = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); using var manager = TestToken.Client(factory, x.Actor, "MANAGER");
        var root = Root(x.Property); var request = new SendSmsRequest(Guid.NewGuid(), null, "+90 555 111 22 33", "Su kesintisi 14:00");
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync(root + "/messages", request)).StatusCode);
        await admin.PutAsJsonAsync(root + "/account", Account());
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync(root + "/messages", request)).StatusCode);
        await admin.PostAsJsonAsync(root + "/account/credits", new SmsCreditRequest("LOAD", 5));
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync(root + "/messages", request with { RequestId = Guid.NewGuid(), PhoneNumber = "123" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync(root + "/messages", request with { RequestId = Guid.NewGuid(), PersonId = Guid.NewGuid() })).StatusCode);
        var first = (await (await manager.PostAsJsonAsync(root + "/messages", request)).Content.ReadFromJsonAsync<SmsMessageResponse>())!;
        var replay = (await (await manager.PostAsJsonAsync(root + "/messages", request)).Content.ReadFromJsonAsync<SmsMessageResponse>())!;
        Assert.Equal(first.Id, replay.Id); Assert.Equal("QUEUED", first.Status); Assert.Equal(SmsTypes.Manual, first.TypeCode); Assert.Equal("Su kesintisi 14:00", first.Message);
        var person = await CreatePerson(admin, x.Property);
        var toPerson = (await (await manager.PostAsJsonAsync(root + "/messages", new SendSmsRequest(Guid.NewGuid(), person.Id, null, "Merhaba"))).Content.ReadFromJsonAsync<SmsMessageResponse>())!;
        Assert.Equal(person.Id, toPerson.PersonId); Assert.Equal("+90********67", toPerson.PhoneMasked);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.PostAsJsonAsync(Root(other.Property) + "/messages", request with { RequestId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync(Root(other.Property) + "/messages")).StatusCode);
    }

    [PostgresFact] public async Task ProviderFailureRetriesWithoutConsumingCredit()
    {
        var x = await Seed(); using var admin = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var root = Root(x.Property);
        await admin.PutAsJsonAsync(root + "/account", Account()); await admin.PostAsJsonAsync(root + "/account/credits", new SmsCreditRequest("LOAD", 5));
        var queued = (await (await admin.PostAsJsonAsync(root + "/messages", new SendSmsRequest(Guid.NewGuid(), null, "+905551112233", "Deneme"))).Content.ReadFromJsonAsync<SmsMessageResponse>())!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>();
            var dispatcher = new SmsDispatcher(db, scope.ServiceProvider.GetRequiredService<ISmsProtector>(), [new FailingProvider(permanent: false)], TimeProvider.System);
            await dispatcher.DispatchPendingAsync(default);
        }
        await factory.WithDb(async db => {
            var message = await db.SmsMessages.SingleAsync(m => m.Id == queued.Id);
            Assert.Equal(SmsMessageStatus.QUEUED, message.Status); Assert.Equal(1, message.Attempts); Assert.Equal("PROVIDER_DOWN", message.ErrorCode); Assert.NotNull(message.ProtectedPayload);
            Assert.True(message.NextAttemptAt > DateTimeOffset.UtcNow.AddSeconds(5));
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE sms_messages SET next_attempt_at = now() - interval '1 second' WHERE id = {queued.Id}");
        });
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>();
            await new SmsDispatcher(db, scope.ServiceProvider.GetRequiredService<ISmsProtector>(), [new FailingProvider(permanent: true)], TimeProvider.System).DispatchPendingAsync(default);
        }
        await factory.WithDb(async db => {
            var message = await db.SmsMessages.SingleAsync(m => m.Id == queued.Id);
            Assert.Equal(SmsMessageStatus.FAILED, message.Status); Assert.Null(message.ProtectedPayload);
            Assert.Equal(5, (await db.SmsAccounts.SingleAsync(a => a.PropertyId == x.Property)).CreditBalance);
            Assert.False(await db.SmsCreditTransactions.AnyAsync(t => t.MessageId == queued.Id));
        });
    }

    private sealed class FailingProvider(bool permanent) : ISmsProvider
    {
        public SmsProvider Kind => SmsProvider.MOCK;
        public Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken token) => Task.FromResult(new SmsSendResult(false, null, "PROVIDER_DOWN", permanent));
    }
    private async Task<int> Dispatch()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SmsDispatcher>().DispatchPendingAsync(default);
    }
    private static async Task<PersonResponse> CreatePerson(HttpClient client, Guid property)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/properties/{property}/people", Ada);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); return (await response.Content.ReadFromJsonAsync<PersonResponse>())!;
    }
    private static string Root(Guid property) => $"/api/v1/properties/{property}/sms";
    private async Task<(string Actor, Guid Property)> Seed()
    {
        var actor = Guid.NewGuid().ToString(); var property = new Property("SMS " + actor[..8], "City", "District", "Address", PropertyType.APARTMENT);
        await factory.WithDb(async db => { db.Add(property); db.Add(new PropertyMembership(property.Id, actor)); await db.SaveChangesAsync(); });
        return (actor, property.Id);
    }
}
