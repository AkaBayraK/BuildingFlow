using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class ManagementAssignmentEndpoints
{
    public static void MapManagementAssignmentEndpoints(this WebApplication app)
    {
        var root=app.MapGroup("/api/v2/tenants/{tenantId:guid}/properties/{propertyId:guid}").RequireAuthorization();
        root.AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled") && app.Configuration.GetValue<bool>("PersonRefactor:AssignmentsEnabled")?await next(context):Results.NotFound());
        root.MapGet("/management-access",async(Guid tenantId,Guid propertyId,IManagementAccess access,CancellationToken token,string role="MANAGER")=>Results.Ok(await access.EvaluateAsync(tenantId,propertyId,role,token)));
        var assignments=root.MapGroup("/management-assignments").RequireAuthorization("CanManagePeople");
        assignments.MapGet("",async(Guid tenantId,Guid propertyId,IManagementAssignmentStore store,CancellationToken token,int page=1,int pageSize=10)=>Found(await store.ListAsync(tenantId,propertyId,page,pageSize,token)));
        assignments.MapGet("/{id:guid}",async(Guid tenantId,Guid propertyId,Guid id,IManagementAssignmentStore store,CancellationToken token)=>Found(await store.GetAsync(tenantId,propertyId,id,token)));
        assignments.MapGet("/{id:guid}/history",async(Guid tenantId,Guid propertyId,Guid id,IManagementAssignmentStore store,CancellationToken token,int page=1,int pageSize=10)=>Found(await store.HistoryAsync(tenantId,propertyId,id,page,pageSize,token)));
        assignments.MapPost("",async(Guid tenantId,Guid propertyId,CreateManagementAssignmentRequest request,IManagementAssignmentStore store,CancellationToken token)=>Found(await store.CreateAsync(tenantId,propertyId,request,token))).RequireAuthorization("CanManageUsers");
        assignments.MapPost("/{id:guid}/end",async(Guid tenantId,Guid propertyId,Guid id,EndManagementAssignmentRequest request,IManagementAssignmentStore store,CancellationToken token)=>Found(await store.EndAsync(tenantId,propertyId,id,request,token))).RequireAuthorization("CanManageUsers");
        assignments.MapPost("/{id:guid}/cancel",async(Guid tenantId,Guid propertyId,Guid id,CancelManagementAssignmentRequest request,IManagementAssignmentStore store,CancellationToken token)=>Found(await store.CancelAsync(tenantId,propertyId,id,request,token))).RequireAuthorization("CanManageUsers");
    }
    private static IResult Found<T>(T? value) where T:class=>value==null?Results.NotFound():Results.Ok(value);
}
