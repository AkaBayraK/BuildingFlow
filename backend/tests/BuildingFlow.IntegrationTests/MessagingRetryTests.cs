using System.Diagnostics;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Xunit;
namespace BuildingFlow.IntegrationTests;

[Collection(RabbitCollection.Name)]
public sealed class MessagingRetryTests(MessagingOutboxTests.Fixture fixture) : IClassFixture<MessagingOutboxTests.Fixture>
{
    private async Task<EmailRequested> Publish()
    {
        await using var db = fixture.Db();
        var property = new BuildingFlow.Domain.Property("Retry " + Guid.NewGuid().ToString("N")[..8], "City", "District", "Address", BuildingFlow.Domain.PropertyType.APARTMENT);
        db.Add(property);
        var email = new EmailRequested(Guid.NewGuid(), property.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, "tr", "recipient@example.test", "Subject", "Body");
        new EfMessageOutbox(db).Enqueue(email); await db.SaveChangesAsync();
        Assert.Equal(1, await new OutboxRelay(db, fixture.Publisher!, TimeProvider.System).PublishPendingAsync(default));
        return email;
    }

    private async Task<RabbitMessageConsumer> Start(MessagingOutboxTests.TestHandler handler, ServiceProvider provider)
    {
        var consumer = new RabbitMessageConsumer(fixture.Options!, provider.GetRequiredService<IServiceScopeFactory>());
        await consumer.StartAsync([MessageContracts.Email], default);
        return consumer;
    }

    private async Task<BasicGetResult> Dead()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true) {
            var result = await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.DeadQueue(fixture.Prefix, MessageContracts.Email), autoAck: true, timeout.Token);
            if (result is not null) return result;
            await Task.Delay(50, timeout.Token);
        }
    }

    private async Task AssertQueuesEmpty()
    {
        await Task.Delay(300);
        Assert.Null(await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.Queue(fixture.Prefix, MessageContracts.Email), autoAck: true));
        Assert.Null(await fixture.Channel.BasicGetAsync(RabbitMessageTopology.DeadQueue(fixture.Prefix, MessageContracts.Email), autoAck: true));
        foreach (var delay in fixture.Options!.RetryDelays) Assert.Null(await fixture.Channel.BasicGetAsync(RabbitMessageTopology.Retry(fixture.Prefix, delay), autoAck: true));
    }

    private static string Header(BasicGetResult result, string name) => result.BasicProperties.Headers![name] switch {
        byte[] bytes => Encoding.UTF8.GetString(bytes), var value => Convert.ToString(value)!
    };

    [PostgresRabbitFact] public async Task TransientFailuresAreRetriedAfterDelaysAndProcessedOnce()
    {
        var handler = new MessagingOutboxTests.TestHandler { FailuresLeft = 2 };
        await using var provider = fixture.Services(handler);
        await using var consumer = await Start(handler, provider);
        var watch = Stopwatch.StartNew();
        var email = await Publish();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var db = fixture.Db();
        while (!await db.ProcessedMessages.AnyAsync(m => m.MessageId == email.MessageId, timeout.Token)) await Task.Delay(50, timeout.Token);
        Assert.Equal(3, handler.Calls);
        Assert.True(watch.Elapsed >= TimeSpan.FromMilliseconds(850), $"Retries returned too early: {watch.Elapsed}.");
        await AssertQueuesEmpty();
    }

    [PostgresRabbitFact] public async Task ExhaustedRetriesAreDeadLetteredWithReasonAndNoInboxRow()
    {
        var handler = new MessagingOutboxTests.TestHandler { FailuresLeft = 100 };
        await using var provider = fixture.Services(handler);
        await using var consumer = await Start(handler, provider);
        var email = await Publish();
        var dead = await Dead();
        Assert.Equal(email, MessageContracts.Decode(dead.Body.Span));
        Assert.Equal(RabbitRetryPolicy.Exhausted, Header(dead, RabbitRetryPolicy.FailureHeader));
        Assert.Equal("3", Header(dead, RabbitRetryPolicy.AttemptHeader));
        Assert.Equal(email.MessageId.ToString("D"), dead.BasicProperties.MessageId);
        Assert.Equal(DeliveryModes.Persistent, dead.BasicProperties.DeliveryMode);
        Assert.Equal(3, handler.Calls);
        await using var db = fixture.Db();
        Assert.False(await db.ProcessedMessages.AnyAsync(m => m.MessageId == email.MessageId));
        await AssertQueuesEmpty();
    }

    [PostgresRabbitFact] public async Task ConflictingPayloadIsDeadLetteredImmediatelyWithoutRetry()
    {
        var handler = new MessagingOutboxTests.TestHandler();
        await using var provider = fixture.Services(handler);
        await using var consumer = await Start(handler, provider);
        var email = await Publish();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (handler.Calls == 0) await Task.Delay(50, timeout.Token);
        await fixture.Publisher!.PublishAsync(email with { Subject = "Changed" }, default);
        var dead = await Dead();
        Assert.Equal(RabbitRetryPolicy.Rejected, Header(dead, RabbitRetryPolicy.FailureHeader));
        Assert.Equal("1", Header(dead, RabbitRetryPolicy.AttemptHeader));
        Assert.Equal("Changed", ((EmailRequested)MessageContracts.Decode(dead.Body.Span)).Subject);
        Assert.Equal(1, handler.Calls);
        await AssertQueuesEmpty();
    }

    [PostgresRabbitFact] public async Task RedeliveredSuccessIsAcknowledgedWithoutSecondDelivery()
    {
        var handler = new MessagingOutboxTests.TestHandler();
        await using var provider = fixture.Services(handler);
        await using var consumer = await Start(handler, provider);
        var email = await Publish();
        await fixture.Publisher!.PublishAsync(email, default);
        await fixture.Publisher.PublishAsync(email, default);
        await Task.Delay(1000);
        Assert.Equal(1, handler.Calls);
        await using var db = fixture.Db();
        Assert.Equal(1, await db.ProcessedMessages.CountAsync(m => m.MessageId == email.MessageId));
        await AssertQueuesEmpty();
    }

    [PostgresRabbitFact] public async Task ForgedAttemptHeaderCannotExtendRetries()
    {
        var handler = new MessagingOutboxTests.TestHandler { FailuresLeft = 100 };
        await using var provider = fixture.Services(handler);
        await using var consumer = await Start(handler, provider);
        var email = await Publish();
        await Dead();
        var forged = new BasicProperties { DeliveryMode = DeliveryModes.Persistent, ContentType = "application/json",
            Headers = new Dictionary<string, object?> { [RabbitRetryPolicy.AttemptHeader] = "-50" } };
        var before = handler.Calls;
        await fixture.Channel!.BasicPublishAsync(RabbitMessageTopology.Exchange(fixture.Prefix), MessageContracts.Email, false, forged, MessageContracts.Encode(email with { MessageId = Guid.NewGuid() }));
        var dead = await Dead();
        Assert.Equal(RabbitRetryPolicy.Exhausted, Header(dead, RabbitRetryPolicy.FailureHeader));
        Assert.Equal(3, handler.Calls - before);
        await AssertQueuesEmpty();
    }
}

public sealed class RabbitRetryPolicyTests
{
    private static readonly RabbitMessagingOptions Options = new() { RetryDelays = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)], MaxAttempts = 4 };

    [Theory][InlineData(null, 0)][InlineData(2, 2)][InlineData(3L, 3)][InlineData("7", 7)][InlineData("-9", 0)][InlineData("x", 0)][InlineData(5000, 100)]
    public void AttemptHeaderIsParsedDefensively(object? value, int expected)
    {
        var headers = new Dictionary<string, object?>();
        if (value is not null) headers[RabbitRetryPolicy.AttemptHeader] = value is string text ? Encoding.ASCII.GetBytes(text) : value;
        Assert.Equal(expected, RabbitRetryPolicy.Attempts(headers));
        Assert.Equal(0, RabbitRetryPolicy.Attempts(null));
    }

    [Fact] public void DelaysEscalateThenStopAtTheAttemptLimit()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), RabbitRetryPolicy.NextDelay(1, Options));
        Assert.Equal(TimeSpan.FromSeconds(30), RabbitRetryPolicy.NextDelay(2, Options));
        Assert.Equal(TimeSpan.FromSeconds(30), RabbitRetryPolicy.NextDelay(3, Options));
        Assert.Null(RabbitRetryPolicy.NextDelay(4, Options));
    }

    [Fact] public void UnsafeRetryConfigurationIsRejected()
    {
        foreach (var options in new[] {
            new RabbitMessagingOptions { Username = "u", Password = "p", MaxAttempts = 0 },
            new RabbitMessagingOptions { Username = "u", Password = "p", RetryDelays = [] },
            new RabbitMessagingOptions { Username = "u", Password = "p", RetryDelays = [TimeSpan.FromMilliseconds(10)] } })
            Assert.Throws<InvalidOperationException>(() => options.Factory());
    }
}
