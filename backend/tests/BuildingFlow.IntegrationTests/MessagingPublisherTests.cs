using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using RabbitMQ.Client;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class RabbitFactAttribute : FactAttribute
{
    public RabbitFactAttribute()
    { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_HOST"))) Skip="RabbitMQ test environment is required."; }
}

// Quorum queue creation is serialized across broker test classes; parallel Ra cluster formation timed out on the local broker.
[CollectionDefinition(RabbitCollection.Name, DisableParallelization = true)]
public sealed class RabbitCollection { public const string Name = "RabbitMQ"; }

public static class RabbitTestTopology
{
    // Cleanup uses a fresh connection because a broker exception closes the fixture channel.
    public static async Task DeleteAsync(RabbitMessagingOptions options)
    {
        Assert.StartsWith("buildingflow-test-", options.Prefix);
        await using var connection = await options.Factory("buildingflow-test-cleanup").CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        foreach (var type in MessageContracts.Types) {
            await channel.QueueDeleteAsync(RabbitMessageTopology.Queue(options.Prefix, type), false, false);
            await channel.QueueDeleteAsync(RabbitMessageTopology.DeadQueue(options.Prefix, type), false, false);
        }
        foreach (var delay in options.RetryDelays.Distinct()) {
            await channel.QueueDeleteAsync(RabbitMessageTopology.Retry(options.Prefix, delay), false, false);
            await channel.ExchangeDeleteAsync(RabbitMessageTopology.Retry(options.Prefix, delay));
        }
        await channel.ExchangeDeleteAsync(RabbitMessageTopology.Exchange(options.Prefix));
        await channel.ExchangeDeleteAsync(RabbitMessageTopology.DeadExchange(options.Prefix));
    }
}

[Collection(RabbitCollection.Name)]
public sealed class MessagingPublisherTests(MessagingPublisherTests.BrokerFixture fixture) : IClassFixture<MessagingPublisherTests.BrokerFixture>
{
    private static EmailRequested Email() => new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),DateTimeOffset.UtcNow,"tr","recipient@example.test","Test","Body");
    private async Task<BasicGetResult> Get(string type)
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while(true){var result=await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.Queue(fixture.Options!.Prefix,type),autoAck:true,timeout.Token);if(result is not null)return result;await Task.Delay(30,timeout.Token);}
    }
    [RabbitFact] public async Task ConfirmedPersistentMessagesReachOnlyTheirTypedQueues()
    {
        var e=Email();IMessageCommand[] commands=[e,new SmsRequested(Guid.NewGuid(),e.PropertyId,e.CorrelationId,e.OccurredAt,"en","+905551234567","SMS"),
            new NotificationRequested(Guid.NewGuid(),e.PropertyId,e.CorrelationId,e.OccurredAt,"tr","subject","Title","Body")];
        foreach(var command in commands){await fixture.Publisher!.PublishAsync(command,default);var received=await Get(MessageContracts.TypeOf(command));
            Assert.Equal(command,MessageContracts.Decode(received.Body.Span));Assert.Equal(command.MessageId.ToString("D"),received.BasicProperties.MessageId);
            Assert.Equal(command.CorrelationId.ToString("D"),received.BasicProperties.CorrelationId);Assert.Equal(DeliveryModes.Persistent,received.BasicProperties.DeliveryMode);
            Assert.Equal("application/json",received.BasicProperties.ContentType);Assert.Equal(MessageContracts.TypeOf(command),received.RoutingKey);}
    }
    [RabbitFact] public async Task ConcurrentPublishDoesNotMixPayloadsOrLoseConfirmations()
    {
        var commands=Enumerable.Range(0,20).Select(_=>Email()).ToArray();
        await Task.WhenAll(commands.Select(c=>fixture.Publisher!.PublishAsync(c,default)));
        var ids=new HashSet<Guid>();for(var i=0;i<commands.Length;i++)ids.Add(MessageContracts.Decode((await Get(MessageContracts.Email)).Body.Span).MessageId);
        Assert.Equal(commands.Length,ids.Count);Assert.All(commands,c=>Assert.Contains(c.MessageId,ids));
    }
    [RabbitFact] public async Task UnroutableMandatoryMessageFailsInsteadOfReportingSuccess()
    {
        await fixture.Publisher!.PublishAsync(Email(),default);await Get(MessageContracts.Email);
        var prefix=fixture.Options!.Prefix;await fixture.Channel!.QueueUnbindAsync(RabbitMessageTopology.Queue(prefix,MessageContracts.Email),RabbitMessageTopology.Exchange(prefix),MessageContracts.Email);
        try{await Assert.ThrowsAsync<MessagePublicationException>(()=>fixture.Publisher.PublishAsync(Email(),default));}
        finally{await RabbitMessageTopology.Declare(fixture.Channel,fixture.Options,default);}
        // A new confirmed channel reconnects after failure.
        await fixture.Publisher.PublishAsync(Email(),default);await Get(MessageContracts.Email);
    }
    [RabbitFact] public async Task InvalidAndCancelledCallsDoNotPublishAnything()
    {
        await fixture.Publisher!.PublishAsync(Email(),default);await Get(MessageContracts.Email);
        await Assert.ThrowsAsync<InvalidMessageException>(()=>fixture.Publisher.PublishAsync(new{Recipient="sensitive@example.test"},default));
        await Assert.ThrowsAsync<InvalidMessageException>(()=>fixture.Publisher.PublishAsync(Email() with{MessageId=Guid.Empty},default));
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>fixture.Publisher.PublishAsync(Email(),cancelled.Token));
        Assert.Null(await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.Queue(fixture.Options!.Prefix,MessageContracts.Email),autoAck:true));
    }
    [RabbitFact] public async Task DurableTopologySurvivesPublisherRecreationWithStableReplayIdentity()
    {
        var e=Email();await using(var other=new RabbitMessagePublisher(fixture.Options!))await other.PublishAsync(e,default);
        await fixture.Publisher!.PublishAsync(e,default);
        var first=await Get(MessageContracts.Email);var second=await Get(MessageContracts.Email);
        Assert.Equal(e.MessageId.ToString("D"),first.BasicProperties.MessageId);Assert.Equal(first.Body.ToArray(),second.Body.ToArray());
        // Duplicate delivery is intentional; step02/03 consumer inbox must deduplicate it.
    }
    [Fact] public async Task UnavailableBrokerThrowsSafeFailureWithoutCredentials()
    {
        await using var p=new RabbitMessagePublisher(new RabbitMessagingOptions{Host="127.0.0.1",Port=1,Username="test-secret-user",Password="test-secret-password"});
        var error=await Assert.ThrowsAsync<MessagePublicationException>(()=>p.PublishAsync(Email(),default));
        Assert.DoesNotContain("secret",error.ToString());Assert.Null(error.InnerException);
    }

    public sealed class BrokerFixture : IAsyncLifetime
    {
        public RabbitMessagingOptions? Options{get;private set;} public RabbitMessagePublisher? Publisher{get;private set;}
        public IConnection? Connection{get;private set;} public IChannel? Channel{get;private set;}
        public async Task InitializeAsync()
        {
            var host=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_HOST");if(string.IsNullOrWhiteSpace(host))return;
            Options=new(){Host=host,Port=int.TryParse(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_PORT"),out var port)?port:5672,
                Username=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_USERNAME")??"",Password=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_PASSWORD")??"",Prefix="buildingflow-test-"+Guid.NewGuid().ToString("N"),RetryDelays=[TimeSpan.FromSeconds(1)]};
            Connection=await Options.Factory().CreateConnectionAsync();Channel=await Connection.CreateChannelAsync();
            await RabbitMessageTopology.Declare(Channel,Options,default);Publisher=new(Options);
        }
        public async Task DisposeAsync()
        {
            if(Publisher is not null)await Publisher.DisposeAsync();
            if(Channel is not null)try{await Channel.DisposeAsync();}catch(Exception){}
            if(Connection is not null)try{await Connection.DisposeAsync();}catch(Exception){}
            if(Options is not null)await RabbitTestTopology.DeleteAsync(Options);
        }
    }
}
