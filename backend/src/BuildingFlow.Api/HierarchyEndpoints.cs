using BuildingFlow.Application;
namespace BuildingFlow.Api;

internal static class HierarchyEndpoints
{
    public static void MapHierarchyEndpoints(this WebApplication app)
    {
        var sites=app.MapGroup("/api/v1/properties/{siteId:guid}").WithTags("Hierarchy").RequireAuthorization("CanReadProperties");
        sites.MapPut("",async (Guid siteId,UpdatePropertyRequest request,HierarchyService service,CancellationToken token)=> Found(await service.UpdatePropertyAsync(siteId,request,token))).RequireAuthorization("CanManageProperties");
        sites.MapDelete("",async (Guid siteId,long version,HierarchyService service,CancellationToken token)=> Deleted(await service.DeletePropertyAsync(siteId,version,token))).RequireAuthorization("CanManageProperties");
        var blocks=sites.MapGroup("/buildings");
        blocks.MapGet("",async (Guid siteId,HierarchyService service,CancellationToken token,int page=1,int pageSize=10,string? search=null,string sortBy="name",string sortDirection="asc")=> Found(await service.ListBuildingsAsync(siteId,new(page,pageSize,search,sortBy,sortDirection),token)));
        blocks.MapGet("/{blockId:guid}",async (Guid siteId,Guid blockId,HierarchyService service,CancellationToken token)=>Found(await service.GetBuildingAsync(siteId,blockId,token)));
        blocks.MapPost("",async (Guid siteId,CreateBuildingRequest request,HierarchyService service,CancellationToken token)=>{
            var result=await service.CreateBuildingAsync(siteId,request,token);
            return result is null?Results.NotFound():Results.Created($"/api/v1/properties/{siteId}/buildings/{result.Id}",result);
        }).RequireAuthorization("CanManageHierarchy");
        blocks.MapPut("/{blockId:guid}",async (Guid siteId,Guid blockId,UpdateBuildingRequest request,HierarchyService service,CancellationToken token)=>Found(await service.UpdateBuildingAsync(siteId,blockId,request,token))).RequireAuthorization("CanManageHierarchy");
        blocks.MapDelete("/{blockId:guid}",async (Guid siteId,Guid blockId,long version,HierarchyService service,CancellationToken token)=>Deleted(await service.DeleteBuildingAsync(siteId,blockId,version,token))).RequireAuthorization("CanManageHierarchy");
        var apartments=blocks.MapGroup("/{blockId:guid}/units");
        apartments.MapGet("",async (Guid siteId,Guid blockId,HierarchyService service,CancellationToken token,int page=1,int pageSize=10,string? search=null,string sortBy="number",string sortDirection="asc")=>Found(await service.ListUnitsAsync(siteId,blockId,new(page,pageSize,search,sortBy,sortDirection),token)));
        apartments.MapGet("/{apartmentId:guid}",async (Guid siteId,Guid blockId,Guid apartmentId,HierarchyService service,CancellationToken token)=>Found(await service.GetUnitAsync(siteId,blockId,apartmentId,token)));
        apartments.MapPost("",async (Guid siteId,Guid blockId,CreateUnitRequest request,HierarchyService service,CancellationToken token)=>{
            var result=await service.CreateUnitAsync(siteId,blockId,request,token);
            return result is null?Results.NotFound():Results.Created($"/api/v1/properties/{siteId}/buildings/{blockId}/units/{result.Id}",result);
        }).RequireAuthorization("CanManageHierarchy");
        apartments.MapPut("/{apartmentId:guid}",async (Guid siteId,Guid blockId,Guid apartmentId,UpdateUnitRequest request,HierarchyService service,CancellationToken token)=>Found(await service.UpdateUnitAsync(siteId,blockId,apartmentId,request,token))).RequireAuthorization("CanManageHierarchy");
        apartments.MapDelete("/{apartmentId:guid}",async (Guid siteId,Guid blockId,Guid apartmentId,long version,HierarchyService service,CancellationToken token)=>Deleted(await service.DeleteUnitAsync(siteId,blockId,apartmentId,version,token))).RequireAuthorization("CanManageHierarchy");
    }
    private static IResult Found<T>(T? value) where T:class => value is null?Results.NotFound():Results.Ok(value);
    private static IResult Deleted(bool value)=>value?Results.NoContent():Results.NotFound();
}
