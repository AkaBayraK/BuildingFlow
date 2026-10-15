using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MobileFinanceEndpoints {
 public static void MapMobileFinanceEndpoints(this WebApplication app){
  app.MapGet("/api/v1/mobile/finance",async(Guid propertyId,string role,string view,int page,IMobileFinanceStore store,CancellationToken token,Guid? unitId=null,Guid? chargeId=null)=>{
   if(page<1 || page>10000 || view is not ("charges" or "payments"))return Results.BadRequest();
   return Results.Ok(await store.ReadAsync(propertyId,unitId,role,view,page,chargeId,token));
  }).RequireAuthorization().WithTags("Mobile finance");
 }
}
