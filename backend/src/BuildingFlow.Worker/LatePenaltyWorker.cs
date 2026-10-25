using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BuildingFlow.Application;
namespace BuildingFlow.Worker;
public sealed class LatePenaltyWorker(IServiceScopeFactory scopes,ILogger<LatePenaltyWorker> logger):BackgroundService{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromHours(6));do{try{await using var scope=scopes.CreateAsyncScope();var created=await scope.ServiceProvider.GetRequiredService<ILatePenaltyScheduler>().AccrueThroughPreviousMonthAsync(stoppingToken);if(created>0)logger.LogInformation("BuildingFlow.LatePenalty accrued {Count} month-end penalty records.",created);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception){logger.LogWarning("BuildingFlow.LatePenalty month-end accrual failed; retrying on the next interval.");}}while(await timer.WaitForNextTickAsync(stoppingToken));}
}
