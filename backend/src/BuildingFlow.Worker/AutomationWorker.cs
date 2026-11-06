using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Worker;

// PostgreSQL holds the schedule; every Worker instance may tick, the Property lock and dedup keys keep a reminder single.
public sealed class AutomationWorker(IServiceScopeFactory scopes, ILogger<AutomationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BuildingFlowTelemetry.WorkerFailures.Add(0, new KeyValuePair<string, object?>("operation", "automation"));
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var dispatched = await scope.ServiceProvider.GetRequiredService<IAutomationScheduler>().RunDueAsync(stoppingToken);
                if (dispatched > 0) logger.LogInformation("BuildingFlow.Automation dispatched {Count} notifications.", dispatched);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                BuildingFlowTelemetry.WorkerFailures.Add(1, new KeyValuePair<string, object?>("operation", "automation"));
                logger.LogWarning("BuildingFlow.Automation cycle failed: {ErrorType}.", exception.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
