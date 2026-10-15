using BuildingFlow.Application;
using Microsoft.AspNetCore.Http.Features;
namespace BuildingFlow.Api;
internal static class MobileProfileEndpoints {
 public static void MapMobileProfileEndpoints(this WebApplication app) {
  var group=app.MapGroup("/api/v1/mobile/profile").RequireAuthorization().WithTags("Mobile profile");
  group.MapGet("",async(IMobileProfileStore store,CancellationToken token)=>Results.Ok(await store.ReadAsync(token)));
  group.MapGet("/photo/{id:guid}",async(Guid id,IMobileProfileStore store,CancellationToken token)=> {
   var bytes=await store.PhotoAsync(id,token);return bytes==null?Results.NotFound():Results.Bytes(bytes,"image/jpeg");
  });
  group.MapPost("/commands",async(HttpContext context,IMobileProfileStore store,CancellationToken token)=> {
   const long limit=384*1024;
   if(context.Request.ContentLength>limit)return Results.StatusCode(413);
   var feature=context.Features.Get<IHttpMaxRequestBodySizeFeature>();if(feature is {IsReadOnly:false})feature.MaxRequestBodySize=limit;
   if(!context.Request.HasJsonContentType())return Results.StatusCode(415);
   MobileProfileCommand? command;
   try{command=await context.Request.ReadFromJsonAsync<MobileProfileCommand>(token);}catch(System.Text.Json.JsonException){return Results.BadRequest();}
   return command==null?Results.BadRequest():Results.Ok(await store.SaveAsync(command,token));
  });
 }
}
