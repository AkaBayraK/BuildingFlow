using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class SmsSecurityTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Key = "test-opt-out-callback-key-0123456789abcdef";
    private const string Phone = "+905551234567";
    private static readonly SmsTypeDefinition Marketing = SmsTypes.Marketing.Single();

    [PostgresFact] public async Task QueuedCommercialSmsIsStoppedWhenRecipientOptsOutBeforeDispatch()
    {
        var (property, sender) = await Seed("4500");
        Guid message = default;
        await factory.WithDb(async db =>
        {
            await new CommercialConsentStore(db, TimeProvider.System).RecordAsync(new(sender, Phone, true, CommercialConsentStatus.APPROVED, CommercialConsentSource.WEB, Guid.NewGuid(), "u", null, 1, new string('d', 64)), default);
            var queued = await Queue(db).EnqueueAsync(property, Marketing, Phone, new Dictionary<string, string> { ["message"] = "Kampanya" }, null, null, default);
            Assert.Equal("QUEUED", queued.Status); message = queued.MessageId!.Value; await db.SaveChangesAsync();
            await new CommercialConsentStore(db, TimeProvider.System).RecordAsync(new(sender, Phone, false, CommercialConsentStatus.REJECTED, CommercialConsentSource.SMS_REPLY, Guid.NewGuid()), default);
        });
        using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<SmsDispatcher>().DispatchPendingAsync(default);
        await factory.WithDb(async db =>
        {
            var row = await db.SmsMessages.AsNoTracking().SingleAsync(x => x.Id == message);
            Assert.Equal(SmsMessageStatus.FAILED, row.Status); Assert.Equal(SmsPolicyErrors.OptedOut, row.ErrorCode); Assert.Null(row.ProtectedPayload);
            Assert.False(await db.SmsCreditTransactions.AnyAsync(x => x.MessageId == message));
        });
    }

    [PostgresFact] public async Task SignedRetReplyOptsOutOnceAndUnsignedOrUnconfiguredCallbacksAreRejected()
    {
        var (_, sender) = await Seed("4501");
        using var off = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await Post(off, Body("RET GUL", "4501", "m-1"), null)).StatusCode);
        using var on = factory.WithWebHostBuilder(b => b.UseSetting("Sms:OptOutCallbackKey", Key)).CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(on, Body("RET GUL", "4501", "m-1"), "00")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(on, Body("RET GUL", "4501", "m-1"), Sign(Body("RET GUL", "4501", "m-2")))).StatusCode);
        var body = Body("  ret   gul ", "4501", "m-1");
        var first = await Post(on, body, Sign(body));
        Assert.Equal(1, (await first.Content.ReadFromJsonAsync<SmsOptOutCallbackResult>())!.MatchedSenders);
        await Post(on, body, Sign(body));
        var other = Body("RET BASKA", "4501", "m-3");
        Assert.Equal(0, (await (await Post(on, other, Sign(other))).Content.ReadFromJsonAsync<SmsOptOutCallbackResult>())!.MatchedSenders);
        var chat = Body("Merhaba", "4501", "m-4");
        Assert.Equal(0, (await (await Post(on, chat, Sign(chat))).Content.ReadFromJsonAsync<SmsOptOutCallbackResult>())!.MatchedSenders);
        var big = "{\"text\":\"" + new string('x', 4096) + "\"}";
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await Post(on, big, Sign(big))).StatusCode);
        await factory.WithDb(async db =>
        {
            var rows = await db.CommercialConsentRecords.AsNoTracking().Where(x => x.SenderId == sender).ToListAsync();
            var row = Assert.Single(rows);
            Assert.Equal(CommercialConsentStatus.REJECTED, row.Status); Assert.Equal(CommercialConsentSource.SMS_REPLY, row.Source); Assert.False(row.RecipientVerified);
        });
    }

    [PostgresFact] public async Task BareRetOnSharedShortCodeStopsEverySenderOnIt()
    {
        var (_, first) = await Seed("4502"); var (_, second) = await Seed("4502", "RET IKI");
        using var on = factory.WithWebHostBuilder(b => b.UseSetting("Sms:OptOutCallbackKey", Key)).CreateClient();
        var body = Body("RET", "4502", "m-bare");
        Assert.Equal(2, (await (await Post(on, body, Sign(body))).Content.ReadFromJsonAsync<SmsOptOutCallbackResult>())!.MatchedSenders);
        await factory.WithDb(async db =>
        {
            var store = new CommercialConsentStore(db, TimeProvider.System);
            Assert.Equal("REJECTED", (await store.CurrentAsync(first, Phone, default))!.Status); Assert.Equal("REJECTED", (await store.CurrentAsync(second, Phone, default))!.Status);
        });
    }

    private static string Body(string text, string code, string id) => $"{{\"phoneNumber\":\"{Phone}\",\"text\":\"{text}\",\"shortCode\":\"{code}\",\"providerMessageId\":\"{id}\"}}";
    private static string Sign(string body) => Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(body)));
    private static Task<HttpResponseMessage> Post(HttpClient client, string body, string? signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sms/opt-out-callbacks") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (signature is not null) request.Headers.Add("X-BuildingFlow-Signature", signature);
        return client.SendAsync(request);
    }
    private static SmsQueue Queue(BuildingFlowDbContext db) => new(db, new SmsPayloadProtector(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()), TimeProvider.System);
    private async Task<(Guid Property, Guid Sender)> Seed(string code, string keyword = "RET GUL")
    {
        var property = new Property("Security " + Guid.NewGuid(), "City", "District", "Address", PropertyType.APARTMENT);
        var sender = new SmsLegalSender(property.Id, "Gül", "0123456789012345", "c", keyword, code, true, false, null);
        var account = new SmsAccount(property.Id, "SMS", SmsProvider.MOCK, "BUILDING", 1);
        await factory.WithDb(async db =>
        {
            db.Add(property); db.Add(sender); db.Add(account); await db.SaveChangesAsync();
            db.Add(new SmsCreditTransaction(account, SmsCreditTransactionType.LOAD, 10, null, null, null, null, null)); await db.SaveChangesAsync();
        });
        return (property.Id, sender.Id);
    }
}
