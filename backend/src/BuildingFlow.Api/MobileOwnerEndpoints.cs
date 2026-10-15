using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MobileOwnerEndpoints {
 public static void MapMobileOwnerEndpoints(this WebApplication app){
  app.MapGet("/api/v1/mobile/owner",async(Guid propertyId,Guid unitId,IMobileOwnerStore store,CancellationToken token)=>Results.Ok(await store.ReadAsync(propertyId,unitId,token))).RequireAuthorization().WithTags("Mobile owner");
  app.MapGet("/api/v1/mobile/tenant",async(Guid propertyId,Guid unitId,IMobileOwnerStore store,CancellationToken token)=>Results.Ok(await store.ReadTenantAsync(propertyId,unitId,token))).RequireAuthorization().WithTags("Mobile tenant");
 }
}
