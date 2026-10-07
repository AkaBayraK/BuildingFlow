using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingFlow.Worker;

internal sealed class FoundationWorker(ILogger<FoundationWorker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(configuration["RabbitMQ:Host"]))
        {
            throw new InvalidOperationException("RabbitMQ:Host is required.");
        }

        logger.LogInformation("BuildingFlow worker started. Messaging consumers are deferred to the messaging lesson.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
