using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class PublicWebsiteMediaEndpoints{
 public static void MapPublicWebsiteMediaEndpoints(this WebApplication app){
  app.MapGet("/api/public/v1/sites/{siteKey}/media/{id:guid}/{sha}.{ext}",async(string siteKey,Guid id,string sha,string ext,HttpResponse response,IPublicWebsiteMediaStore service,CancellationToken t)=>{
   if(!System.Text.RegularExpressions.Regex.IsMatch(siteKey,"^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$")||!System.Text.RegularExpressions.Regex.IsMatch(sha,"^[0-9a-f]{64}$")||ext is not("jpg" or "png"))return Results.NotFound();
   var image=await service.Public(siteKey,id,sha,t);if(image is null||ext!=(image.ContentType=="image/jpeg"?"jpg":"png"))return Results.NotFound();
   // Remains no-store until WEBSITE FAZ 07 cache policy; suspension/revocation is checked on every read.
   response.Headers.CacheControl="no-store";response.Headers.XContentTypeOptions="nosniff";response.Headers.ContentDisposition="inline";return Results.File(image.Bytes,image.ContentType);
  }).AllowAnonymous();
 }
}
