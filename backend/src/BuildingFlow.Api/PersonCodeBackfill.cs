using BuildingFlow.Application;
namespace BuildingFlow.Api;
public static class PersonCodeBackfill
{
    private const int BatchSize = 200;
    private const int MaxBatches = 10000;

    // Runs with schema migration so existing people receive codes once; failures leave codes missing but do not stop the API.
    public static async Task RunAsync(IServiceProvider services, ILogger logger, CancellationToken token = default)
    {
        var total = 0;
        try
        {
            for (var batch = 0; batch < MaxBatches; batch++)
            {
                await using var scope = services.CreateAsyncScope();
                var assigned = await scope.ServiceProvider.GetRequiredService<IPeopleStore>().AssignMissingCodesAsync(BatchSize, token);
                total += assigned;
                if (assigned < BatchSize) break;
            }
            if (total > 0) logger.LogInformation("BuildingFlow.Migrations: person codes assigned to {Count} existing people", total);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning("BuildingFlow.Migrations: person code backfill deferred after {Count} people ({ExceptionType})", total, exception.GetType().Name);
        }
    }
}
