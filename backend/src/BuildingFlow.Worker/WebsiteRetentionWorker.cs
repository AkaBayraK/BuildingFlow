using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BuildingFlow.Application;
namespace BuildingFlow.Worker;
public sealed class WebsiteRetentionWorker(IServiceScopeFactory scopes,ILogger<WebsiteRetentionWorker> logger):BackgroundService{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromHours(1));do{try{await using var scope=scopes.CreateAsyncScope();await scope.ServiceProvider.GetRequiredService<IWebsiteInquiryStore>().Retain(stoppingToken);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception){logger.LogWarning("Website retention failed; retrying on the next interval.");}}while(await timer.WaitForNextTickAsync(stoppingToken));}
}
