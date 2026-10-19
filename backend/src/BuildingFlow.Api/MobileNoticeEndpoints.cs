using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MobileNoticeEndpoints{
 public static void MapMobileNoticeEndpoints(this WebApplication app){
  var root=app.MapGroup("/api/v1/mobile/notices").RequireAuthorization().WithTags("Mobile notices");
  root.MapGet("",async(Guid propertyId,string role,string kind,int page,IMobileNoticeStore store,CancellationToken token,Guid? unitId=null,Guid? itemId=null)=>Results.Ok(await store.ReadAsync(new(propertyId,unitId,role,kind,page,itemId),token)));
  root.MapPost("/read",async(MobileNoticeReadCommand command,IMobileNoticeStore store,CancellationToken token)=>Results.Ok(await store.MarkReadAsync(command,token)));
  root.MapPost("/preference",async(MobileNoticePreferenceCommand command,IMobileNoticeStore store,CancellationToken token)=>Results.Ok(await store.PreferenceAsync(command,token)));
 }
}
