using System.Text.Json;
using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class WebsiteDomainEndpoints{
 public static void MapWebsiteDomainEndpoints(this WebApplication app){
 var root=app.MapGroup("/api/v1/properties/{propertyId:guid}/website/domains").RequireAuthorization("CanPublishWebsite");
 root.MapGet("",async(Guid propertyId,WebsiteDomainService s,CancellationToken t)=>Found(await s.List(propertyId,t)));
 root.MapDelete("/{id:guid}",async(Guid propertyId,Guid id,long version,WebsiteDomainService s,CancellationToken t)=>Found(await s.Act(propertyId,id,"DELETE",version,t)));
 root.MapPost("",async(Guid propertyId,WebsiteDomainCreate r,WebsiteDomainService s,CancellationToken t)=>Found(await s.Create(propertyId,r,t)));
 foreach(var action in new[]{"verify","approve","primary","disable","rotate"}){var value=action.ToUpperInvariant();var endpoint=root.MapPost("/{id:guid}/"+action,async(Guid propertyId,Guid id,WebsiteDomainAction r,WebsiteDomainService s,CancellationToken t)=>Found(await s.Act(propertyId,id,value,r.Version,t)));if(action=="approve")endpoint.RequireAuthorization("CanManageProperties");}
 app.MapGet("/api/public/v1/domains/{host}",async(string host,IPublicWebsiteDomainStore s,HttpResponse response,CancellationToken t)=>{response.Headers.CacheControl="no-store";return Found(await s.Resolve(host,t));});
 app.MapGet("/api/public/v1/sites/{siteKey}/primary-domain",async(string siteKey,IPublicWebsiteDomainStore s,HttpResponse response,CancellationToken t)=>{response.Headers.CacheControl="no-store";var host=await s.Primary(siteKey,t);return host is null?Results.NotFound():Results.Ok(new{hostName=host});});
 app.MapPost("/api/v1/properties/{propertyId:guid}/website/preview-token",async(Guid propertyId,WebsiteVersionRequest r,IWebsitePreviewStore s,ICurrentUserService u,HttpResponse response,CancellationToken t)=>{response.Headers.CacheControl="no-store";if(r.Version<1)WebsiteDomainService.Invalid();return Found(await s.Issue(propertyId,r.Version,new(u.UserId!,u.IsInRole("SUPER_ADMIN")),t));}).RequireAuthorization("CanManageWebsite");
 app.MapPost("/api/public/v1/preview",async(HttpRequest request,HttpResponse response,IWebsitePreviewStore s,CancellationToken t)=>{response.Headers.CacheControl="no-store";response.Headers["X-Robots-Tag"]="noindex, nofollow";return Found(await s.Read(await Token(request,t),t));});
 app.MapPost("/api/public/v1/preview/media/{id:guid}/{sha}",async(Guid id,string sha,HttpRequest request,HttpResponse response,IWebsitePreviewStore s,CancellationToken t)=>{response.Headers.CacheControl="no-store";response.Headers["X-Robots-Tag"]="noindex, nofollow";var media=await s.Media(await Token(request,t),id,sha,t);return media is null?Results.NotFound():Results.File(media.Bytes,media.ContentType);});
 }
 private static IResult Found<T>(T? x)where T:class=>x is null?Results.NotFound():Results.Ok(x);
 private static async Task<string> Token(HttpRequest r,CancellationToken t){var size=r.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();if(size is{IsReadOnly:false})size.MaxRequestBodySize=2048;if(!r.HasJsonContentType()||r.ContentLength>2048)return "";using var m=new MemoryStream();var buffer=new byte[1024];int n;while((n=await r.Body.ReadAsync(buffer,t))>0){if(m.Length+n>2048)return "";m.Write(buffer,0,n);}try{return JsonSerializer.Deserialize<WebsitePreviewRequest>(m.ToArray(),new JsonSerializerOptions(JsonSerializerDefaults.Web))?.Token??"";}catch(JsonException){return "";}}
}
