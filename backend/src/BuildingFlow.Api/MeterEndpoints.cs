using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class MeterEndpoints
{
    public static void MapMeterEndpoints(this WebApplication app)
    {
        var root=app.MapGroup("/api/v1/properties/{propertyId:guid}/meters").RequireAuthorization("CanManageDues").WithTags("Meters");
        root.MapGet("",async(Guid propertyId,MeterService service,CancellationToken token,int page=1,int pageSize=10)=>Found(await service.List(propertyId,page,pageSize,token)));
        root.MapPost("",async(Guid propertyId,CreateMeterRequest request,MeterService service,CancellationToken token)=>Found(await service.Create(propertyId,request,token))).RequireAuthorization("CanManageHierarchy");
        root.MapGet("/{meterId:guid}/readings",async(Guid propertyId,Guid meterId,MeterService service,CancellationToken token,int page=1,int pageSize=10)=>Found(await service.Readings(propertyId,meterId,page,pageSize,token)));
        root.MapPost("/{meterId:guid}/readings",async(Guid propertyId,Guid meterId,ReadingRequest request,MeterService service,CancellationToken token)=>Found(await service.Save(propertyId,meterId,null,request,token))).RequireAuthorization("CanManageHierarchy");
        root.MapPut("/{meterId:guid}/readings/{readingId:guid}",async(Guid propertyId,Guid meterId,Guid readingId,ReadingRequest request,MeterService service,CancellationToken token)=>Found(await service.Save(propertyId,meterId,readingId,request,token))).RequireAuthorization("CanManageHierarchy");
    }
    private static IResult Found<T>(T? value) where T:class=>value is null?Results.NotFound():Results.Ok(value);
}
