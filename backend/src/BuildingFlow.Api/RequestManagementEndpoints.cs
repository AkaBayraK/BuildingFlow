using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class RequestManagementEndpoints {
 public static void MapRequestManagementEndpoints(this WebApplication app) {
  var routes=app.MapGroup("/api/v1/properties/{propertyId:guid}/requests").RequireAuthorization("CanManagePeople").WithTags("Requests");
  routes.MapGet("",async(Guid propertyId,IRequestManagementStore store,CancellationToken token,int page=1,int pageSize=10,string? search=null,string? status=null,string? category=null,string? priority=null,string sort="newest")=>Results.Ok(await store.ListAsync(propertyId,page,pageSize,search,status,category,priority,sort,token)));
  routes.MapPost("",async(Guid propertyId,CreateRequest command,IRequestManagementStore store,CancellationToken token)=>{var result=await store.CreateAsync(propertyId,command,token);return Results.Created($"/api/v1/properties/{propertyId}/requests/{result.Record.Id}",result);});
  routes.MapGet("/{id:guid}",async(Guid propertyId,Guid id,IRequestManagementStore store,CancellationToken token)=>await store.DetailAsync(propertyId,id,token) is {} result?Results.Ok(result):Results.NotFound());
  routes.MapPost("/{id:guid}/management",async(Guid propertyId,Guid id,ManageRequest command,IRequestManagementStore store,CancellationToken token)=>await store.ManageAsync(propertyId,id,command,token) is {} result?Results.Ok(result):Results.NotFound());
 }
}
