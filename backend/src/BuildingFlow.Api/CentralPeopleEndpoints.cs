using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class CentralPeopleEndpoints {
 public static void MapCentralPeopleEndpoints(this WebApplication app){
  var root=app.MapGroup("/api/v2/tenants/{tenantId:guid}").RequireAuthorization().WithTags("CentralPeople");
  root.AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled")?await next(context):Results.NotFound());
  root.MapGet("/people",async(Guid tenantId,CentralPeopleService service,CancellationToken token,int page=1,int pageSize=10,string? search=null,string sortBy="name",string sortDirection="asc")=>Found(await service.ListAsync(tenantId,new(page,pageSize,search,sortBy,sortDirection),token))).RequireAuthorization("CanManagePeople");
  root.MapGet("/people/{personId:guid}",async(Guid tenantId,Guid personId,CentralPeopleService service,CancellationToken token)=>Found(await service.GetAsync(tenantId,personId,token))).RequireAuthorization("CanManagePeople");
  root.MapPost("/people",async(Guid tenantId,CreateCentralPersonRequest request,CentralPeopleService service,CancellationToken token)=>Found(await service.CreateAsync(tenantId,request,token))).RequireAuthorization("CanManagePeople");
  root.MapPut("/people/{personId:guid}",async(Guid tenantId,Guid personId,UpdateCentralPersonRequest request,CentralPeopleService service,CancellationToken token)=>Found(await service.UpdateAsync(tenantId,personId,request,token))).RequireAuthorization("CanManagePeople");
  root.MapGet("/me/person",async(Guid tenantId,HttpContext context,CentralPeopleService service,CancellationToken token)=>{if(context.Request.Query.Count>0)throw new ArgumentException("Self scope cannot be selected.");return Found(await service.SelfAsync(tenantId,token));});
 }
 private static IResult Found<T>(T? value) where T:class=>value==null?Results.NotFound():Results.Ok(value);
}
