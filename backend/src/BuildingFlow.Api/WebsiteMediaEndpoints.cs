using BuildingFlow.Application;
using Microsoft.AspNetCore.Http.Features;
namespace BuildingFlow.Api;
internal static class WebsiteMediaEndpoints{
 public static void MapWebsiteMediaEndpoints(this WebApplication app){
  var root=app.MapGroup("/api/v1/properties/{propertyId:guid}/website/media").RequireAuthorization("CanManageWebsite").WithTags("Website media");
  root.MapGet("",async(Guid propertyId,WebsiteMediaService service,CancellationToken t)=>Found(await service.List(propertyId,t)));
  root.MapPost("",Upload).DisableAntiforgery();
  root.MapPut("/{id:guid}",async(Guid propertyId,Guid id,WebsiteMediaEdit request,WebsiteMediaService service,CancellationToken t)=>Found(await service.Edit(propertyId,id,request,t)));
  root.MapDelete("/{id:guid}",async(Guid propertyId,Guid id,long version,WebsiteMediaService service,CancellationToken t)=>await service.Delete(propertyId,id,version,t)?Results.NoContent():Results.NotFound());
  root.MapGet("/{id:guid}/content",async(Guid propertyId,Guid id,HttpResponse response,WebsiteMediaService service,CancellationToken t)=>{var image=await service.Content(propertyId,id,t);if(image is null)return Results.NotFound();response.Headers.CacheControl="no-store";response.Headers.XContentTypeOptions="nosniff";return Results.File(image.Bytes,image.ContentType);});

 }
 private static async Task<IResult> Upload(Guid propertyId,HttpRequest request,WebsiteMediaService service,CancellationToken t){
  var size=request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();if(size is{IsReadOnly:false})size.MaxRequestBodySize=WebsiteImage.MaxBytes+65536;
  if(!request.HasFormContentType)throw new RequestValidationException(new(){["media"]=[ErrorCatalog.WebsiteMediaInvalid.Create()]});
  var form=await request.ReadFormAsync(t);var file=form.Files.GetFile("file");if(file is null||file.Length is <1 or >WebsiteImage.MaxBytes||!Guid.TryParse(form["requestId"],out var id)||!bool.TryParse(form["containsPeople"],out var people)||!bool.TryParse(form["peopleConsentConfirmed"],out var consent))throw new RequestValidationException(new(){["media"]=[ErrorCatalog.WebsiteMediaInvalid.Create()]});
  using var memory=new MemoryStream();using var source=file.OpenReadStream();var buffer=new byte[81920];int read;while((read=await source.ReadAsync(buffer,t))>0){if(memory.Length+read>WebsiteImage.MaxBytes)throw new RequestValidationException(new(){["media"]=[ErrorCatalog.WebsiteMediaInvalid.Create()]});await memory.WriteAsync(buffer.AsMemory(0,read),t);}
  var result=await service.Upload(propertyId,new(id,memory.ToArray(),file.ContentType,file.FileName,new(form["altText"].ToString(),form["rightsBasis"].ToString(),form["rightsNote"].ToString(),people,consent,0)),t);return Found(result);
 }
 private static IResult Found<T>(T? value)where T:class=>value is null?Results.NotFound():Results.Ok(value);
}
