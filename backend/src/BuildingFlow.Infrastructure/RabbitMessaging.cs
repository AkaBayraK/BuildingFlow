using System.Text.RegularExpressions;
using BuildingFlow.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BuildingFlow.Infrastructure;

public sealed class RabbitMessagingOptions
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string VirtualHost { get; init; } = "/";
    public string Prefix { get; init; } = "buildingflow.messaging.v1";
    public bool UseTls { get; init; }
    public IReadOnlyList<TimeSpan> RetryDelays { get; init; } =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10)];
    public int MaxAttempts { get; init; } = 5;
    public static RabbitMessagingOptions From(IConfiguration config) => new() {
        Host = config["RabbitMQ:Host"] ?? "127.0.0.1", Username = config["RabbitMQ:Username"] ?? "",
        Password = config["RabbitMQ:Password"] ?? "", VirtualHost = config["RabbitMQ:VirtualHost"] ?? "/",
        Prefix = config["RabbitMQ:Prefix"] ?? "buildingflow.messaging.v1",
        Port = int.TryParse(config["RabbitMQ:Port"], out var port) ? port : 5672,
        UseTls = string.Equals(config["RabbitMQ:UseTls"], "true", StringComparison.OrdinalIgnoreCase)
    };
    public ConnectionFactory Factory(string clientName = "buildingflow-publisher")
    {
        if (string.IsNullOrWhiteSpace(Host) || Port is < 1 or > 65535 || string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrEmpty(Password) || string.IsNullOrWhiteSpace(VirtualHost) || Prefix.Length > 100 ||
            !Regex.IsMatch(Prefix, @"^[A-Za-z0-9][A-Za-z0-9.-]+$", RegexOptions.CultureInvariant) ||
            MaxAttempts is < 1 or > 20 || RetryDelays.Count is < 1 or > 6 ||
            RetryDelays.Any(delay => delay < TimeSpan.FromMilliseconds(100) || delay > TimeSpan.FromHours(1)))
            throw new InvalidOperationException("RabbitMQ configuration is invalid.");
        return new ConnectionFactory {
            HostName = Host, Port = Port, UserName = Username, Password = Password, VirtualHost = VirtualHost,
            AutomaticRecoveryEnabled = false, RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
            ContinuationTimeout = TimeSpan.FromSeconds(10), ClientProvidedName = clientName,
            Ssl = new SslOption { Enabled = UseTls, ServerName = Host }
        };
    }
}

public static class RabbitMessageTopology
{
    public const int DeliveryLimit = 10;
    public static string Exchange(string prefix) => prefix + ".commands";
    public static string DeadExchange(string prefix) => prefix + ".dead";
    public static string Queue(string prefix, string type) => prefix + "." + type;
    public static string DeadQueue(string prefix, string type) => Queue(prefix, type) + ".failed";
    public static string Retry(string prefix, TimeSpan delay) => prefix + ".retry." + (long)delay.TotalMilliseconds;

    // Retry queues have no consumer: TTL expiry dead-letters the message back to the commands exchange
    // with its original routing key, so one queue per delay serves every message type.
    public static async Task Declare(IChannel channel, RabbitMessagingOptions options, CancellationToken token)
    {
        var prefix = options.Prefix;
        await channel.ExchangeDeclareAsync(Exchange(prefix), ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: token);
        await channel.ExchangeDeclareAsync(DeadExchange(prefix), ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: token);
        foreach (var delay in options.RetryDelays.Distinct()) {
            await channel.ExchangeDeclareAsync(Retry(prefix, delay), ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: token);
            await channel.QueueDeclareAsync(Retry(prefix, delay), durable: true, exclusive: false, autoDelete: false,
                arguments: SafeDeadLettering(Exchange(prefix), null, new() { ["x-message-ttl"] = (long)delay.TotalMilliseconds }), cancellationToken: token);
            await channel.QueueBindAsync(Retry(prefix, delay), Retry(prefix, delay), "", cancellationToken: token);
        }
        foreach (var type in MessageContracts.Types) {
            await channel.QueueDeclareAsync(DeadQueue(prefix, type), durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" }, cancellationToken: token);
            await channel.QueueBindAsync(DeadQueue(prefix, type), DeadExchange(prefix), type, cancellationToken: token);
            await channel.QueueDeclareAsync(Queue(prefix, type), durable: true, exclusive: false, autoDelete: false,
                arguments: SafeDeadLettering(DeadExchange(prefix), type, new() { ["x-delivery-limit"] = DeliveryLimit }), cancellationToken: token);
            await channel.QueueBindAsync(Queue(prefix, type), Exchange(prefix), type, cancellationToken: token);
        }
    }

    private static Dictionary<string, object?> SafeDeadLettering(string exchange, string? routingKey, Dictionary<string, object?> arguments)
    {
        arguments["x-queue-type"] = "quorum";
        arguments["x-dead-letter-exchange"] = exchange;
        arguments["x-dead-letter-strategy"] = "at-least-once";
        arguments["x-overflow"] = "reject-publish";
        if (routingKey is not null) arguments["x-dead-letter-routing-key"] = routingKey;
        return arguments;
    }
}

public static class RabbitRetryPolicy
{
    public const string AttemptHeader = "x-buildingflow-attempt";
    public const string FailureHeader = "x-buildingflow-failure";
    public const string Rejected = "rejected";
    public const string Exhausted = "retry-exhausted";

    public static int Attempts(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(AttemptHeader, out var value)) return 0;
        var attempts = value switch {
            int number => number, long number => number, byte number => number, short number => number,
            byte[] text when long.TryParse(System.Text.Encoding.ASCII.GetString(text), out var parsed) => parsed,
            _ => 0
        };
        return (int)Math.Clamp(attempts, 0, 100);
    }

    public static TimeSpan? NextDelay(int failedAttempts, RabbitMessagingOptions options) =>
        failedAttempts >= options.MaxAttempts ? null : options.RetryDelays[Math.Clamp(failedAttempts - 1, 0, options.RetryDelays.Count - 1)];
}

// One long-lived connection/channel, serialized use and confirms. Business transactions must enqueue via the later Outbox.
public sealed class RabbitMessagePublisher(RabbitMessagingOptions options) : IMessagePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IConnection? connection;
    private IChannel? channel;
    private bool disposed;
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class
    {
        if (message is not IMessageCommand command) throw new InvalidMessageException();
        var body = MessageContracts.Encode(command);
        cancellationToken.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var entered = false;
        try {
            await gate.WaitAsync(timeout.Token); entered = true;
            ObjectDisposedException.ThrowIf(disposed, this);
            if (channel is not { IsOpen: true } || connection is not { IsOpen: true }) {
                await Reset();
                connection = await options.Factory().CreateConnectionAsync(timeout.Token);
                channel = await connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), timeout.Token);
                await RabbitMessageTopology.Declare(channel, options, timeout.Token);
            }
            var properties = new BasicProperties {
                ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent,
                MessageId = command.MessageId.ToString("D"), CorrelationId = command.CorrelationId.ToString("D"),
                Type = MessageContracts.TypeOf(command), AppId = "buildingflow", Timestamp = new AmqpTimestamp(command.OccurredAt.ToUnixTimeSeconds())
            };
            await channel.BasicPublishAsync(RabbitMessageTopology.Exchange(options.Prefix), properties.Type, mandatory: true,
                basicProperties: properties, body: body, cancellationToken: timeout.Token);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            if (entered) await Reset(); throw;
        } catch (ObjectDisposedException) { throw; }
        catch {
            if (entered) await Reset();
            // No credentials, recipient, body or SDK exception escapes into API/worker logs.
            throw new MessagePublicationException();
        } finally { if (entered) gate.Release(); }
    }
    private async Task Reset()
    {
        if (channel is not null) { try { await channel.DisposeAsync(); } catch { } channel = null; }
        if (connection is not null) { try { await connection.DisposeAsync(); } catch { } connection = null; }
    }
    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try { if (disposed) return; disposed = true; await Reset(); }
        finally { gate.Release(); }
    }
}

// Acknowledges only after the inbox row and handler effect are committed, or after a retry/dead-letter copy is confirmed.
public sealed class RabbitMessageConsumer(RabbitMessagingOptions options, IServiceScopeFactory scopes) : IAsyncDisposable
{
    public const ushort Prefetch = 10;
    private readonly SemaphoreSlim publishGate = new(1, 1);
    private IConnection? connection;
    private IChannel? channel;
    private IChannel? publishChannel;
    public bool IsOpen => connection is { IsOpen: true } && channel is { IsOpen: true } && publishChannel is { IsOpen: true };

    public async Task StartAsync(IReadOnlyCollection<string> types, CancellationToken cancellationToken)
    {
        if (types.Count == 0 || types.Any(type => !MessageContracts.Types.Contains(type))) throw new InvalidMessageException();
        connection = await options.Factory("buildingflow-consumer").CreateConnectionAsync(cancellationToken);
        publishChannel = await connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), cancellationToken);
        channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await RabbitMessageTopology.Declare(channel, options, cancellationToken);
        await channel.BasicQosAsync(0, Prefetch, false, cancellationToken);
        foreach (var type in types) {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, delivery) => Handle(type, delivery);
            await channel.BasicConsumeAsync(RabbitMessageTopology.Queue(options.Prefix, type), autoAck: false, consumer, cancellationToken);
        }
    }

    private async Task Handle(string type, BasicDeliverEventArgs delivery)
    {
        var active = channel!;
        string? failure = null;
        try {
            await using var scope = scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<MessageProcessor>().ProcessAsync(type, delivery.Body, delivery.CancellationToken);
            if (result != MessageProcessingResult.Rejected) {
                await active.BasicAckAsync(delivery.DeliveryTag, multiple: false);
                return;
            }
            failure = RabbitRetryPolicy.Rejected;
        } catch (OperationCanceledException) when (delivery.CancellationToken.IsCancellationRequested) {
            await active.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);
            return;
        } catch (Exception) {
            failure = null;
        }
        var attempts = RabbitRetryPolicy.Attempts(delivery.BasicProperties.Headers) + 1;
        var delay = failure is null ? RabbitRetryPolicy.NextDelay(attempts, options) : null;
        try {
            if (delay is { } retryDelay) await Republish(RabbitMessageTopology.Retry(options.Prefix, retryDelay), type, delivery, attempts, null);
            else await Republish(RabbitMessageTopology.DeadExchange(options.Prefix), type, delivery, attempts, failure ?? RabbitRetryPolicy.Exhausted);
        } catch (Exception) {
            await active.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);
            return;
        }
        await active.BasicAckAsync(delivery.DeliveryTag, multiple: false);
    }

    private async Task Republish(string exchange, string type, BasicDeliverEventArgs delivery, int attempts, string? failure)
    {
        var source = delivery.BasicProperties;
        var headers = new Dictionary<string, object?> { [RabbitRetryPolicy.AttemptHeader] = attempts };
        if (failure is not null) headers[RabbitRetryPolicy.FailureHeader] = failure;
        var properties = new BasicProperties {
            ContentType = source.ContentType, DeliveryMode = DeliveryModes.Persistent, MessageId = source.MessageId,
            CorrelationId = source.CorrelationId, Type = source.Type, AppId = source.AppId, Timestamp = source.Timestamp, Headers = headers
        };
        var body = delivery.Body.ToArray();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await publishGate.WaitAsync(timeout.Token);
        try {
            await publishChannel!.BasicPublishAsync(exchange, type, mandatory: true, basicProperties: properties, body: body, cancellationToken: timeout.Token);
        } finally { publishGate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (channel is not null) { try { await channel.DisposeAsync(); } catch { } channel = null; }
        if (publishChannel is not null) { try { await publishChannel.DisposeAsync(); } catch { } publishChannel = null; }
        if (connection is not null) { try { await connection.DisposeAsync(); } catch { } connection = null; }
    }
}
