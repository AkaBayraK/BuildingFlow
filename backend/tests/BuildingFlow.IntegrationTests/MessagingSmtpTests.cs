using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class MailpitFactAttribute : FactAttribute
{
    public MailpitFactAttribute(bool needsBroker = false)
    {
        var missing = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_SMTP_PORT")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_MAILPIT_URL"));
        if (needsBroker) missing |= string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_HOST"));
        if (missing) Skip = "A dedicated local Mailpit test instance is required.";
    }
}

public static class TestMailpit
{
    public static SmtpOptions Options => new() {
        Enabled = true, Host = "127.0.0.1", Port = int.Parse(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_SMTP_PORT")!),
        UseStartTls = false, AllowInsecureLocal = true, From = "no-reply@buildingflow.local", FromName = "BuildingFlow Test", TimeoutSeconds = 10
    };

    private static readonly HttpClient Http = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_MAILPIT_URL") ?? "http://127.0.0.1:1") };

    public static async Task<JsonElement[]> BySubject(string subject)
    {
        var result = await Http.GetFromJsonAsync<JsonElement>("/api/v1/search?query=" + Uri.EscapeDataString($"subject:\"{subject}\""));
        return result.GetProperty("messages").EnumerateArray().ToArray();
    }

    public static async Task<JsonElement> WaitOne(string subject)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true) {
            var found = await BySubject(subject);
            if (found.Length > 0) return Assert.Single(found);
            await Task.Delay(100, timeout.Token);
        }
    }

    public static Task<JsonElement> Message(string id) => Http.GetFromJsonAsync<JsonElement>($"/api/v1/message/{id}");
    public static Task<JsonElement> Headers(string id) => Http.GetFromJsonAsync<JsonElement>($"/api/v1/message/{id}/headers");
}

public sealed class SmtpEmailHandlerTests
{
    private static EmailRequested Email(string recipient = "resident@example.test") => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        DateTimeOffset.UtcNow, "tr", recipient, "Aidat hatırlatması ğüşıöç " + Guid.NewGuid().ToString("N"), "Merhaba,\nEkim aidatı: 1.250,00 TL\n");

    [MailpitFact] public async Task DeliversUtf8PlainTextWithTraceHeaders()
    {
        var email = Email();
        Assert.Equal(MessageHandlingOutcome.Delivered, await new SmtpEmailHandler(TestMailpit.Options).HandleAsync(email, default));
        var summary = await TestMailpit.WaitOne(email.Subject);
        Assert.Equal(email.Subject, summary.GetProperty("Subject").GetString());
        Assert.Equal("resident@example.test", summary.GetProperty("To")[0].GetProperty("Address").GetString());
        Assert.Equal("no-reply@buildingflow.local", summary.GetProperty("From").GetProperty("Address").GetString());
        var id = summary.GetProperty("ID").GetString()!;
        var message = await TestMailpit.Message(id);
        Assert.Equal(email.Body.ReplaceLineEndings("\r\n").TrimEnd(), message.GetProperty("Text").GetString()!.ReplaceLineEndings("\r\n").TrimEnd());
        Assert.Equal($"{email.MessageId:D}@buildingflow", message.GetProperty("MessageID").GetString());
        var headers = await TestMailpit.Headers(id);
        Assert.Equal(email.MessageId.ToString("D"), headers.GetProperty("X-Buildingflow-Message-Id")[0].GetString());
        Assert.Equal(email.CorrelationId.ToString("D"), headers.GetProperty("X-Buildingflow-Correlation-Id")[0].GetString());
    }

    [MailpitFact] public async Task RejectedRecipientIsPermanentAndNothingIsStored()
    {
        var email = Email("someone@blocked.invalid");
        await Assert.ThrowsAsync<PermanentMessageFailureException>(() => new SmtpEmailHandler(TestMailpit.Options).HandleAsync(email, default));
        Assert.Empty(await TestMailpit.BySubject(email.Subject));
    }

    [Fact] public async Task UnreachableServerIsTransientWithoutLeakingDetails()
    {
        var options = new SmtpOptions { Host = "127.0.0.1", Port = 1, UseStartTls = false, AllowInsecureLocal = true, From = "no-reply@buildingflow.local", TimeoutSeconds = 5 };
        var error = await Assert.ThrowsAsync<TimeoutException>(() => new SmtpEmailHandler(options).HandleAsync(Email(), default));
        Assert.DoesNotContain("example.test", error.ToString()); Assert.Null(error.InnerException);
    }

    [Fact] public void ConfigurationRefusesPlainTextOutsideLocalCaptureAndLeaksNoSecrets()
    {
        SmtpOptions Valid(string host = "smtp.provider.example", bool tls = true, bool insecure = false, string user = "user", string password = "s3cret-value",
            string from = "no-reply@buildingflow.example", string name = "BuildingFlow", int port = 587) =>
            new() { Host = host, UseStartTls = tls, AllowInsecureLocal = insecure, Username = user, Password = password, From = from, FromName = name, Port = port };
        Valid().Validate();
        new SmtpOptions { Host = "mailpit", Port = 1025, UseStartTls = false, AllowInsecureLocal = true, From = "no-reply@buildingflow.local" }.Validate();
        foreach (var options in new[] {
            Valid(tls: false, insecure: true), Valid(host: "mailpit", tls: false, insecure: false, user: "", password: ""),
            Valid(host: "mailpit", tls: false, insecure: true), Valid(password: ""), Valid(user: ""), Valid(from: "Name <a@b.example>"),
            Valid(from: "broken"), Valid(name: "Evil\r\nBcc: x@y.example"), Valid(port: 0), Valid(host: "") }) {
            var error = Assert.Throws<InvalidOperationException>(options.Validate);
            Assert.DoesNotContain("s3cret", error.Message);
        }
        Assert.False(SmtpOptions.FromConfiguration(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()).Enabled);
    }
}

[Collection(RabbitCollection.Name)]
public sealed class SmtpDeliveryPipelineTests(MessagingOutboxTests.Fixture fixture) : IClassFixture<MessagingOutboxTests.Fixture>
{
    [MailpitFact(needsBroker: true)] public async Task OutboxToMailpitDeliversOnceAndDeadLettersRejectedRecipients()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped(_ => fixture.Db());
        services.AddSingleton<IMessageHandler>(new SmtpEmailHandler(TestMailpit.Options));
        services.AddScoped<MessageProcessor>();
        await using var provider = services.BuildServiceProvider();
        await using var consumer = new RabbitMessageConsumer(fixture.Options!, provider.GetRequiredService<IServiceScopeFactory>());
        await consumer.StartAsync([MessageContracts.Email], default);

        await using var db = fixture.Db();
        var property = new BuildingFlow.Domain.Property("SMTP " + Guid.NewGuid().ToString("N")[..8], "City", "District", "Address", BuildingFlow.Domain.PropertyType.APARTMENT);
        db.Add(property);
        var delivered = new EmailRequested(Guid.NewGuid(), property.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, "en", "owner@example.test", "Pipeline " + Guid.NewGuid().ToString("N"), "Body");
        var rejected = delivered with { MessageId = Guid.NewGuid(), Recipient = "owner@blocked.invalid", Subject = "Rejected " + Guid.NewGuid().ToString("N") };
        new EfMessageOutbox(db).Enqueue(delivered); new EfMessageOutbox(db).Enqueue(rejected); await db.SaveChangesAsync();
        Assert.Equal(2, await new OutboxRelay(db, fixture.Publisher!, TimeProvider.System).PublishPendingAsync(default));

        await TestMailpit.WaitOne(delivered.Subject);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        RabbitMQ.Client.BasicGetResult? dead = null;
        while (dead is null) {
            dead = await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.DeadQueue(fixture.Prefix, MessageContracts.Email), autoAck: true, timeout.Token);
            if (dead is null) await Task.Delay(50, timeout.Token);
        }
        Assert.Equal(rejected.MessageId, MessageContracts.Decode(dead.Body.Span).MessageId);
        Assert.Equal(RabbitRetryPolicy.Permanent, Encoding.UTF8.GetString((byte[])dead.BasicProperties.Headers![RabbitRetryPolicy.FailureHeader]!));

        await fixture.Publisher!.PublishAsync(delivered, default);
        await Task.Delay(1000);
        Assert.Single(await TestMailpit.BySubject(delivered.Subject));
        Assert.Empty(await TestMailpit.BySubject(rejected.Subject));
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.ProcessedMessages.CountAsync(m => m.MessageId == delivered.MessageId && m.Consumer == SmtpEmailHandler.Name));
        Assert.False(await db.ProcessedMessages.AnyAsync(m => m.MessageId == rejected.MessageId));
    }
}
