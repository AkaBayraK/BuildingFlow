using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BuildingFlow.Application;
namespace BuildingFlow.Worker;
// Off by default: the sandbox keeps its state inside the API process, so only a real provider adapter can be polled from here.
public sealed class OnlinePaymentWorker(IServiceScopeFactory scopes,ILogger<OnlinePaymentWorker> logger):BackgroundService{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromMinutes(5));do{try{await using var scope=scopes.CreateAsyncScope();var changed=await scope.ServiceProvider.GetRequiredService<IOnlinePaymentScheduler>().SyncPendingAsync(stoppingToken);if(changed>0)logger.LogInformation("BuildingFlow.Payments verified {Count} pending online payments.",changed);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception){logger.LogWarning("BuildingFlow.Payments pending payment verification failed; retrying on the next interval.");}}while(await timer.WaitForNextTickAsync(stoppingToken));}
}
