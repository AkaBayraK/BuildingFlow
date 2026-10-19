using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class PersonTransitionEndpoints
{
    public static void MapPersonTransitionEndpoints(this WebApplication app)
    {
        var root=app.MapGroup("/api/v2/tenants/{tenantId:guid}/properties/{propertyId:guid}/person-transition").RequireAuthorization("CanManagePeople");
        root.AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled") && app.Configuration.GetValue<bool>("PersonRefactor:TransitionEnabled")?await next(context):Results.NotFound());
        root.MapGet("/preview",async(Guid tenantId,Guid propertyId,IPersonTransitionStore store,CancellationToken token)=>Found(await store.PreviewAsync(tenantId,propertyId,token)));
        root.MapPost("/apply",async(Guid tenantId,Guid propertyId,ApplyPersonTransitionRequest request,IPersonTransitionStore store,CancellationToken token)=>Found(await store.ApplyAsync(tenantId,propertyId,request.ExpectedSourceHash,token)));
        root.MapGet("/comparison",async(Guid tenantId,Guid propertyId,IPersonTransitionStore store,CancellationToken token)=>Found(await store.CompareAsync(tenantId,propertyId,token)));
        root.MapGet("/people/{legacyPersonId:guid}",async(Guid tenantId,Guid propertyId,Guid legacyPersonId,IPersonTransitionStore store,CancellationToken token)=>Found(await store.ReadAsync(tenantId,propertyId,legacyPersonId,token)));
    }
    private static IResult Found<T>(T? value) where T:class=>value==null?Results.NotFound():Results.Ok(value);
}
