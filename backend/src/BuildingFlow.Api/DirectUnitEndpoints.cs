using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class DirectUnitEndpoints
{
    public static void MapDirectUnitEndpoints(this WebApplication app)
    {
        var units=app.MapGroup("/api/v1/properties/{propertyId:guid}/units").WithTags("Units").RequireAuthorization("CanReadProperties");
        units.MapGet("",async (Guid propertyId,HierarchyService service,CancellationToken token,int page=1,int pageSize=10,string? search=null,string sortBy="number",string sortDirection="asc",Guid? unitId=null)=>Found(await service.ListUnitsAsync(propertyId,null,new(page,pageSize,search,sortBy,sortDirection,unitId),token)));
        units.MapGet("/{unitId:guid}",async (Guid propertyId,Guid unitId,HierarchyService service,CancellationToken token)=>Found(await service.GetUnitAsync(propertyId,null,unitId,token)));
        units.MapPost("",async(Guid propertyId,CreateUnitRequest request,HierarchyService service,CancellationToken token)=>{var unit=await service.CreateUnitAsync(propertyId,null,request,token);return unit is null?Results.NotFound():Results.Created($"/api/v1/properties/{propertyId}/units/{unit.Id}",unit);}).RequireAuthorization("CanManageHierarchy");
        units.MapPut("/{unitId:guid}",async(Guid propertyId,Guid unitId,UpdateUnitRequest request,HierarchyService service,CancellationToken token)=>Found(await service.UpdateUnitAsync(propertyId,null,unitId,request,token))).RequireAuthorization("CanManageHierarchy");
        units.MapDelete("/{unitId:guid}",async(Guid propertyId,Guid unitId,long version,HierarchyService service,CancellationToken token)=>await service.DeleteUnitAsync(propertyId,null,unitId,version,token)?Results.NoContent():Results.NotFound()).RequireAuthorization("CanManageHierarchy");
    }
    private static IResult Found<T>(T? value)where T:class=>value is null?Results.NotFound():Results.Ok(value);
}
