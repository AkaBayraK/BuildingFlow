using BuildingFlow.Application;
namespace BuildingFlow.Api;
public sealed class ProfilePhotoMaintenance(IServiceScopeFactory scopes,ILogger<ProfilePhotoMaintenance> logger):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
  using var timer=new PeriodicTimer(TimeSpan.FromMinutes(2));
  while(await timer.WaitForNextTickAsync(stoppingToken)) {
   try {using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<IMobileProfileStore>().MaintainAsync(stoppingToken);await scope.ServiceProvider.GetRequiredService<IMobileSyncStore>().MaintainPhotosAsync(stoppingToken);}
   catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){return;}
   catch{logger.LogWarning("Mobile photo maintenance deferred.");}
  }
 }
}
