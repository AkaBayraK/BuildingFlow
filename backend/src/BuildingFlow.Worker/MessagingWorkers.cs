using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingFlow.Worker;

internal sealed class SystemUser : ICurrentUserService
{
    public string? UserId => null;
    public string? Username => null;
    public bool IsAuthenticated => false;
    public bool IsInRole(string role) => false;
}

internal sealed class OutboxRelayWorker(IServiceScopeFactory scopes, ILogger<OutboxRelayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BuildingFlowTelemetry.WorkerFailures.Add(0,new KeyValuePair<string,object?>("operation","relay"));
        while (!stoppingToken.IsCancellationRequested) {
            var published = 0;
            try {
                await using var scope = scopes.CreateAsyncScope();
                using var activity = BuildingFlowTelemetry.Activities.StartActivity("outbox.relay");
                published = await scope.ServiceProvider.GetRequiredService<OutboxRelay>().PublishPendingAsync(stoppingToken);
                BuildingFlowTelemetry.WorkerMessages.Add(published);
                if (published > 0) logger.LogInformation("Outbox relay published {Count} messages.", published);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception exception) {
                BuildingFlowTelemetry.WorkerFailures.Add(1,new KeyValuePair<string,object?>("operation","relay"));
                logger.LogWarning("Outbox relay cycle failed: {ErrorType}.", exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }
            if (published < OutboxRelay.BatchSize) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}

internal sealed class MessageConsumerWorker(RabbitMessagingOptions options, IServiceScopeFactory scopes,
    IEnumerable<IMessageHandler> handlers, ILogger<MessageConsumerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach(var outcome in new[]{"processed","duplicate","retry","dead_letter"}) BuildingFlowTelemetry.Deliveries.Add(0,new KeyValuePair<string,object?>("outcome",outcome));
        foreach(var operation in new[]{"consumer","delivery"}) BuildingFlowTelemetry.WorkerFailures.Add(0,new KeyValuePair<string,object?>("operation",operation));
        var types = handlers.Select(handler => handler.Type).Distinct().ToArray();
        if (types.Length == 0) {
            logger.LogInformation("No message delivery channel is enabled; queues stay durable until a channel is configured.");
            return;
        }
        while (!stoppingToken.IsCancellationRequested) {
            await using var consumer = new RabbitMessageConsumer(options, scopes);
            try {
                await consumer.StartAsync(types, stoppingToken);
                logger.LogInformation("Message consumers started for {Types}.", string.Join(",", types));
                while (consumer.IsOpen) await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                logger.LogWarning("Message consumer connection closed; reconnecting.");
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception exception) {
                BuildingFlowTelemetry.WorkerFailures.Add(1,new KeyValuePair<string,object?>("operation","consumer"));
                logger.LogWarning("Message consumer start failed: {ErrorType}.", exception.GetType().Name);
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
