using BuildingFlow.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Worker;
public sealed class ParkingOperationsWorker(IServiceScopeFactory scopes, ILogger<ParkingOperationsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                Guid? cursor = null;
                do
                {
                    await using var scope = scopes.CreateAsyncScope();
                    cursor = await scope.ServiceProvider.GetRequiredService<IParkingOperations>().RunBatch(cursor, stoppingToken);
                } while (cursor is not null && !stoppingToken.IsCancellationRequested);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("BuildingFlow.Parking scheduled operations failed; retrying next interval."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
