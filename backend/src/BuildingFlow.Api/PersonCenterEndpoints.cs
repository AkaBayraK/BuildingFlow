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
        root.MapGet("/people",async(IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=20,string? search=null,string sortBy="name",string sortDirection="asc",Guid? tenantId=null,Guid? propertyId=null,string? relation=null)=>Results.Ok(await s.DirectoryAsync(new(page,pageSize,search,sortBy,sortDirection,tenantId,propertyId,relation),ct)));
        root.MapGet("/tenants/{tenantId:guid}/properties/{propertyId:guid}/person-building-choices",async(Guid tenantId,Guid propertyId,IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=20)=>Found(await s.BuildingsAsync(tenantId,propertyId,page,pageSize,ct)));
        root.MapGet("/tenants/{tenantId:guid}/properties/{propertyId:guid}/person-unit-choices",async(Guid tenantId,Guid propertyId,IPersonCenterStore s,CancellationToken ct,Guid? buildingId=null,int page=1,int pageSize=20)=>Found(await s.UnitsAsync(tenantId,propertyId,buildingId,page,pageSize,ct)));
        var lease=root.MapGroup("/tenants/{tenantId:guid}/properties/{propertyId:guid}/units/{unitId:guid}/lease-contracts");
        lease.MapGet("",async(Guid tenantId,Guid propertyId,Guid unitId,ILeaseContractStore s,CancellationToken ct,int page=1,int pageSize=10)=>Found(await s.ListAsync(tenantId,propertyId,unitId,page,pageSize,ct)));
        lease.MapGet("/{contractId:guid}",async(Guid tenantId,Guid propertyId,Guid unitId,Guid contractId,ILeaseContractStore s,CancellationToken ct)=>Found(await s.GetAsync(tenantId,propertyId,unitId,contractId,ct)));
        lease.MapPost("",async(Guid tenantId,Guid propertyId,Guid unitId,CreateLeaseContractRequest request,ILeaseContractStore s,CancellationToken ct)=>Found(await s.CreateAsync(tenantId,propertyId,unitId,request,ct)));
        lease.MapPost("/{contractId:guid}/parties",async(Guid tenantId,Guid propertyId,Guid unitId,Guid contractId,AddLeasePartyRequest request,ILeaseContractStore s,CancellationToken ct)=>Found(await s.AddPartyAsync(tenantId,propertyId,unitId,contractId,request,ct)));
        lease.MapPost("/{contractId:guid}/attach",async(Guid tenantId,Guid propertyId,Guid unitId,Guid contractId,AttachLeasePartyRequest request,ILeaseContractStore s,CancellationToken ct)=>Found(await s.AttachAsync(tenantId,propertyId,unitId,contractId,request,ct)));
        lease.MapPost("/{contractId:guid}/end",async(Guid tenantId,Guid propertyId,Guid unitId,Guid contractId,EndLeaseContractRequest request,ILeaseContractStore s,CancellationToken ct)=>Found(await s.EndAsync(tenantId,propertyId,unitId,contractId,request,ct)));
        var person=root.MapGroup("/tenants/{tenantId:guid}/people/{personId:guid}");
        person.MapGet("/center",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct)=>Found(await s.DetailAsync(tenantId,personId,ct)));
        person.MapPost("/account-link-codes",async(Guid tenantId,Guid personId,PersonAccountLinkService s,HttpResponse response,CancellationToken ct)=>{response.Headers.CacheControl="no-store";return Found(await s.IssueAsync(tenantId,personId,ct));});
        person.MapPost("/account-link/end",async(Guid tenantId,Guid personId,PersonAccountLinkService s,CancellationToken ct)=>Found(await s.EndAsync(tenantId,personId,ct)));
        person.MapGet("/photo",async(Guid tenantId,Guid personId,IPersonCenterStore s,IMobileProfileStore photos,CancellationToken ct)=>
            await s.PhotoAccountAsync(tenantId,personId,ct) is { } user && await photos.AccountPhotoAsync(user,ct) is { } bytes?AccountEndpoints.Photo(bytes):Results.NotFound());
        person.MapGet("/portfolio",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct)=>Found(await s.PortfolioAsync(tenantId,personId,ct)));
        person.MapGet("/account-status",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct)=>Found(await s.AccountAsync(tenantId,personId,ct)));
        person.MapGet("/unit-relations",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=10)=>Found(await s.RelationsAsync(tenantId,personId,page,pageSize,ct)));
        person.MapGet("/unit-relations/{kind}/{id:guid}",async(Guid tenantId,Guid personId,string kind,Guid id,IPersonCenterStore s,CancellationToken ct)=>Found(await s.RelationAsync(tenantId,personId,kind,id,ct)));
        person.MapPost("/unit-relations",async(Guid tenantId,Guid personId,CreatePersonUnitRelationRequest request,IPersonCenterStore s,CancellationToken ct)=>Found(await s.CreateRelationAsync(tenantId,personId,request,ct)));
        person.MapPost("/unit-relations/{kind}/{id:guid}/end",async(Guid tenantId,Guid personId,string kind,Guid id,EndPersonUnitRelationRequest request,IPersonCenterStore s,CancellationToken ct)=>Found(await s.EndRelationAsync(tenantId,personId,kind,id,request,ct)));
        person.MapGet("/management-duties",async(Guid tenantId,Guid personId,IPersonCenterStore s,CancellationToken ct,int page=1,int pageSize=10)=>Found(await s.DutiesAsync(tenantId,personId,page,pageSize,ct)));
    }
    private static IResult Found<T>(T? value) where T:class=>value==null?Results.NotFound():Results.Ok(value);
}
