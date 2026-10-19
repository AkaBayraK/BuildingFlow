using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class ListingEndpoints{
 public static void MapListingEndpoints(this WebApplication app){
 var root=app.MapGroup("/api/v1/properties/{propertyId:guid}/listings").RequireAuthorization("CanManageWebsite").WithTags("Listings");
 root.MapGet("/unit-options",async(Guid propertyId,ListingService s,CancellationToken t)=>Found(await s.Units(propertyId,t)));
 root.MapGet("",async(Guid propertyId,ListingService s,CancellationToken t)=>Found(await s.List(propertyId,t)));
 root.MapGet("/{id:guid}",async(Guid propertyId,Guid id,ListingService s,CancellationToken t)=>Found(await s.Get(propertyId,id,t)));
 root.MapPost("",async(Guid propertyId,ListingEdit r,ListingService s,CancellationToken t)=>Found(await s.Save(propertyId,null,r,t)));
 root.MapPut("/{id:guid}",async(Guid propertyId,Guid id,ListingEdit r,ListingService s,CancellationToken t)=>Found(await s.Save(propertyId,id,r,t)));
 root.MapDelete("/{id:guid}",async(Guid propertyId,Guid id,long version,ListingService s,CancellationToken t)=>await s.Delete(propertyId,id,version,t)?Results.NoContent():Results.NotFound());
 root.MapPut("/{id:guid}/photos",async(Guid propertyId,Guid id,ListingPhotos r,ListingService s,CancellationToken t)=>Found(await s.Photos(propertyId,id,r,t)));
 root.MapGet("/{id:guid}/events",async(Guid propertyId,Guid id,ListingService s,CancellationToken t)=>Found(await s.Events(propertyId,id,t)));
 foreach(var action in new[]{"submit","approve","reject","publish","withdraw"}){var value=action.ToUpperInvariant();var endpoint=root.MapPost("/{id:guid}/"+action,async(Guid propertyId,Guid id,ListingAction r,ListingService s,CancellationToken t)=>Found(await s.Act(propertyId,id,value,r,t)));if(action!="submit")endpoint.RequireAuthorization("CanPublishWebsite");}
 root.MapPost("/{id:guid}/owner-consent",async(Guid propertyId,Guid id,HttpRequest request,ListingService s,CancellationToken t)=>{
 var size=request.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();if(size is{IsReadOnly:false})size.MaxRequestBodySize=10*1024*1024+65536;if(request.ContentLength>10*1024*1024+65536)throw new DocumentSizeException();if(!request.HasFormContentType)ListingValidation.Invalid();long v=0;var form=await request.ReadFormAsync(t);var file=form.Files.GetFile("file");if(file is null||file.Length>10*1024*1024||!long.TryParse(form["version"],out v))ListingValidation.Invalid();using var m=new MemoryStream();await file!.CopyToAsync(m,t);return Found(await s.Consent(propertyId,id,new(v,form["kind"].ToString(),form["agentLicenseNo"].ToString(),file!.FileName,file.ContentType,m.ToArray()),t));
 }).DisableAntiforgery().WithMetadata(new Microsoft.AspNetCore.Http.Features.FormOptions{MultipartBodyLengthLimit=10*1024*1024+65536},new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(10*1024*1024+65536));
 root.MapGet("/{id:guid}/owner-consent",async(Guid propertyId,Guid id,HttpResponse response,ListingService s,CancellationToken t)=>{var result=await s.ConsentContent(propertyId,id,t);response.Headers.CacheControl="no-store";return result is null?Results.NotFound():Results.File(result.Bytes,result.ContentType,"owner-consent"+(result.ContentType=="application/pdf"?".pdf":result.ContentType=="image/png"?".png":".jpg"));});
 }
 private static IResult Found<T>(T? v)where T:class=>v is null?Results.NotFound():Results.Ok(v);
}
