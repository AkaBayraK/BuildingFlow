using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class PublicListingEndpoints{public static void MapPublicListingEndpoints(this WebApplication app){
 var root=app.MapGroup("/api/public/v1/sites/{siteKey}/listings");
 root.MapGet("",async(string siteKey,string? kind,int? page,HttpResponse response,IPublicListingStore store,CancellationToken t)=>{response.Headers.CacheControl="no-store";var rows=await store.List(siteKey,kind,page??1,t);return rows is null?Results.NotFound():Results.Ok(rows);});
 root.MapGet("/{id:guid}",async(string siteKey,Guid id,HttpResponse response,IPublicListingStore store,CancellationToken t)=>{response.Headers.CacheControl="no-store";var row=await store.Get(siteKey,id,t);return row is null?Results.NotFound():Results.Ok(row);});
}}
