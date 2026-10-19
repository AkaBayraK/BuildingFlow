using BuildingFlow.Application;
using System.Text.Json;
namespace BuildingFlow.Api;
internal static class WebsiteInquiryEndpoints{
 public static void MapWebsiteInquiryEndpoints(this WebApplication app){
 var admin=app.MapGroup("/api/v1/properties/{propertyId:guid}/website/inquiries").RequireAuthorization("CanManageWebsite");
 static AccessScope Scope(ICurrentUserService u)=>new(u.UserId!,u.IsInRole("SUPER_ADMIN"));
 admin.MapGet("",async(Guid propertyId,IWebsiteInquiryStore s,ICurrentUserService u,CancellationToken t)=>{var result=await s.List(propertyId,Scope(u),t);return result is null?Results.NotFound():Results.Ok(result);});
 admin.MapPut("/{id:guid}",async(Guid propertyId,Guid id,InquiryUpdate r,IWebsiteInquiryStore s,ICurrentUserService u,CancellationToken t)=>{var result=await s.Update(propertyId,id,r,Scope(u),t);return result is null?Results.NotFound():Results.Ok(result);});
 var root=app.MapGroup("/api/public/v1/sites/{siteKey}/inquiries");
 root.MapGet("/challenge",async(string siteKey,HttpContext c,IPublicWebsiteInquiryStore s,CancellationToken t)=>{c.Response.Headers.CacheControl="no-store";var r=await s.Challenge(siteKey,c.Connection.RemoteIpAddress?.ToString()??"unknown",t);return r is null?Results.NotFound():Results.Ok(r);});
 root.MapPost("",async(string siteKey,HttpContext c,IPublicWebsiteInquiryStore s,CancellationToken t)=>{c.Response.Headers.CacheControl="no-store";try{var r=await Read(c.Request,t);return await s.Submit(siteKey,c.Connection.RemoteIpAddress?.ToString()??"unknown",r,t)?Results.Accepted():Results.NotFound();}catch(InquiryRateLimitException){c.Response.Headers.RetryAfter="3600";return Results.Problem(statusCode:429,title:ErrorCatalog.RateLimited.Message,extensions:new Dictionary<string,object?>{{"code",ErrorCatalog.RateLimited.Code},{"severity",ErrorCatalog.RateLimited.Severity},{"parameters",ErrorCatalog.RateLimited.Create().Parameters}});}}).WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(16384));
 }
 private static async Task<InquiryRequest> Read(HttpRequest request,CancellationToken t){
 var size=request.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();if(size is{IsReadOnly:false})size.MaxRequestBodySize=16384;
 if(!request.HasJsonContentType()||request.ContentLength>16384)Invalid();using var memory=new MemoryStream();var buffer=new byte[4096];int n;while((n=await request.Body.ReadAsync(buffer,t))>0){if(memory.Length+n>16384)Invalid();memory.Write(buffer,0,n);}try{var r=JsonSerializer.Deserialize<InquiryRequest>(memory.ToArray(),new JsonSerializerOptions(JsonSerializerDefaults.Web));if(r is not null)return r;}catch(JsonException){}Invalid();return null!;
 }
 private static void Invalid()=>throw new RequestValidationException(new(){["inquiry"]=[ErrorCatalog.InquiryRejected.Create()]});
}
