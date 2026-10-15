using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MobileManagerEndpoints {
 public static void MapMobileManagerEndpoints(this WebApplication app){
  app.MapGet("/api/v1/mobile/manager",async(Guid propertyId,string role,string section,int page,IMobileManagerStore store,CancellationToken token)=>{
   if(page<1 || page>10000 || section is not ("overview" or "buildings" or "units" or "residents" or "finance" or "announcements" or "requests"))return Results.BadRequest();
   return Results.Ok(await store.ReadAsync(propertyId,role,section,page,token));
  }).RequireAuthorization().WithTags("Mobile manager");
 }
}
