using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BuildingFlow.Application;
namespace BuildingFlow.Worker;
// Keeps verified domains inside the public 24-hour freshness window; host names and tokens are never logged.
public sealed class WebsiteDomainWorker(IServiceScopeFactory scopes,ILogger<WebsiteDomainWorker> logger):BackgroundService{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromHours(1));do{try{await using var scope=scopes.CreateAsyncScope();var renewed=await scope.ServiceProvider.GetRequiredService<IWebsiteDomainStore>().Recheck(100,stoppingToken);if(renewed>0)logger.LogInformation("Website domain re-check renewed {Count} domains.",renewed);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception){logger.LogWarning("Website domain re-check failed; retrying on the next interval.");}}while(await timer.WaitForNextTickAsync(stoppingToken));}
}
