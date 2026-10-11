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
            !Regex.IsMatch(Prefix, @"^[A-Za-z0-9][A-Za-z0-9.-]+$", RegexOptions.CultureInvariant))
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
    public static string Exchange(string prefix) => prefix + ".commands";
    public static string DeadExchange(string prefix) => prefix + ".dead";
    public static string Queue(string prefix, string type) => prefix + "." + type;
    public static string DeadQueue(string prefix, string type) => Queue(prefix, type) + ".failed";
    public static async Task Declare(IChannel channel, string prefix, CancellationToken token)
    {
        await channel.ExchangeDeclareAsync(Exchange(prefix), ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: token);
        await channel.ExchangeDeclareAsync(DeadExchange(prefix), ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: token);
        foreach (var type in MessageContracts.Types) {
            await channel.QueueDeclareAsync(DeadQueue(prefix, type), durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" }, cancellationToken: token);
            await channel.QueueBindAsync(DeadQueue(prefix, type), DeadExchange(prefix), type, cancellationToken: token);
            await channel.QueueDeclareAsync(Queue(prefix, type), durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> {
                    ["x-queue-type"] = "quorum", ["x-dead-letter-exchange"] = DeadExchange(prefix), ["x-dead-letter-routing-key"] = type
                }, cancellationToken: token);
            await channel.QueueBindAsync(Queue(prefix, type), Exchange(prefix), type, cancellationToken: token);
        }
    }
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
                await RabbitMessageTopology.Declare(channel, options.Prefix, timeout.Token);
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

// Acknowledges only after the inbox row and handler effect are committed; rejected messages are dead-lettered.
public sealed class RabbitMessageConsumer(RabbitMessagingOptions options, IServiceScopeFactory scopes) : IAsyncDisposable
{
    public const ushort Prefetch = 10;
    private IConnection? connection;
    private IChannel? channel;
    public bool IsOpen => connection is { IsOpen: true } && channel is { IsOpen: true };

    public async Task StartAsync(IReadOnlyCollection<string> types, CancellationToken cancellationToken)
    {
        if (types.Count == 0 || types.Any(type => !MessageContracts.Types.Contains(type))) throw new InvalidMessageException();
        connection = await options.Factory("buildingflow-consumer").CreateConnectionAsync(cancellationToken);
        channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await RabbitMessageTopology.Declare(channel, options.Prefix, cancellationToken);
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
        MessageProcessingResult result;
        try {
            await using var scope = scopes.CreateAsyncScope();
            result = await scope.ServiceProvider.GetRequiredService<MessageProcessor>().ProcessAsync(type, delivery.Body, delivery.CancellationToken);
        } catch {
            await active.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);
            return;
        }
        if (result == MessageProcessingResult.Rejected) await active.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
        else await active.BasicAckAsync(delivery.DeliveryTag, multiple: false);
    }

    public async ValueTask DisposeAsync()
    {
        if (channel is not null) { try { await channel.DisposeAsync(); } catch { } channel = null; }
        if (connection is not null) { try { await connection.DisposeAsync(); } catch { } connection = null; }
    }
}
