using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class PersonCenterEndpoints
{
    public static void MapPersonCenterEndpoints(this WebApplication app)
    {
        var root=app.MapGroup("/api/v2").RequireAuthorization("CanManagePeople");
        root.AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled")?await next(context):Results.NotFound());
        root.MapGet("/person-workspaces",async(IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=20)=>Results.Ok(await s.WorkspacesAsync(page,pageSize,ct)));
        root.MapGet("/tenants/{tenantId:guid}/person-workspace",async(Guid tenantId,IPersonCenterStore s,CancellationToken ct)=>Found(await s.WorkspaceAsync(tenantId,ct)));
        root.MapGet("/tenants/{tenantId:guid}/person-properties",async(Guid tenantId,IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=20)=>Found(await s.PropertiesAsync(tenantId,page,pageSize,ct)));
        root.MapGet("/tenants/{tenantId:guid}/properties/{propertyId:guid}/person-unit-choices",async(Guid tenantId,Guid propertyId,IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=20)=>Found(await s.UnitsAsync(tenantId,propertyId,page,pageSize,ct)));
        var person=root.MapGroup("/tenants/{tenantId:guid}/people/{personId:guid}");
        person.MapGet("/center",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct)=>Found(await s.DetailAsync(tenantId,personId,ct)));
        person.MapGet("/account-status",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct)=>Found(await s.AccountAsync(tenantId,personId,ct)));
        person.MapGet("/unit-relations",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=10)=>Found(await s.RelationsAsync(tenantId,personId,page,pageSize,ct)));
        person.MapGet("/unit-relations/{kind}/{id:guid}",async(Guid tenantId,Guid personId,string kind,Guid id,IPersonCenterStore s,CancellationToken ct)=>Found(await s.RelationAsync(tenantId,personId,kind,id,ct)));
        person.MapPost("/unit-relations",async(Guid tenantId,Guid personId,CreatePersonUnitRelationRequest request,IPersonCenterStore s,CancellationToken ct)=>Found(await s.CreateRelationAsync(tenantId,personId,request,ct)));
        person.MapPost("/unit-relations/{kind}/{id:guid}/end",async(Guid tenantId,Guid personId,string kind,Guid id,EndPersonUnitRelationRequest request,IPersonCenterStore s,CancellationToken ct)=>Found(await s.EndRelationAsync(tenantId,personId,kind,id,request,ct)));
        person.MapGet("/management-duties",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=10)=>Found(await s.DutiesAsync(tenantId,personId,page,pageSize,ct)));
    }
    private static IResult Found<T>(T? value) where T:class=>value==null?Results.NotFound():Results.Ok(value);
}
