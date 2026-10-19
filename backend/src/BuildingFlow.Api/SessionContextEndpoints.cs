using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal sealed record WorkspacePropertyPermissions(bool CanManagePeople,bool CanManageAssignments);
internal static class SessionContextEndpoints
{
    public static void MapSessionContextEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v2/mobile/authorization",async(IMobileAuthorizationStore store,CancellationToken token)=>Results.Ok(await store.ReadAsync(token)))
            .RequireAuthorization().AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled") && app.Configuration.GetValue<bool>("PersonRefactor:SessionContextsEnabled") && app.Configuration.GetValue<bool>("PersonRefactor:MobileAuthorizationEnabled")?await next(context):Results.NotFound());
        var root=app.MapGroup("/api/v2/session/contexts").RequireAuthorization();
        root.AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled") && app.Configuration.GetValue<bool>("PersonRefactor:SessionContextsEnabled")?await next(context):Results.NotFound());
        root.MapGet("",async(ISessionContextStore store,CancellationToken token,int page=1,int pageSize=20)=>Results.Ok(await store.ListAsync(page,pageSize,token)));
        root.MapGet("/{kind}/{id:guid}",async(string kind,Guid id,ISessionContextStore store,CancellationToken token)=>await store.GetAsync(kind,id,token) is {} value?Results.Ok(value):Results.NotFound());
    }
    public static void UseSessionContextAccess(this WebApplication app)
    {
        app.Use(async(context,next)=>
        {
            if(!app.Configuration.GetValue<bool>("PersonRefactor:SessionContextsEnabled") || context.User.Identity?.IsAuthenticated!=true){await next(context);return;}
            var path=context.Request.Path;
            // These global legacy administration APIs cannot represent a canonical tenant scope yet.
            // Keep them closed for tenant managers in the opt-in central flow; platform administration remains explicit.
            if(!context.User.IsInRole("SUPER_ADMIN") && (path.StartsWithSegments("/api/v1/users",StringComparison.OrdinalIgnoreCase) || path.StartsWithSegments("/api/v1/consents",StringComparison.OrdinalIgnoreCase))){context.Response.StatusCode=404;return;}
            if(path.StartsWithSegments("/api/v1/properties",StringComparison.OrdinalIgnoreCase,out var rest))
            {
                var segments=rest.Value?.Split('/',StringSplitOptions.RemoveEmptyEntries)??[];
                if(segments.Length>0 && Guid.TryParse(segments[0],out var property))
                {
                    var section=segments.Length>1?segments[1].ToLowerInvariant():"";var read=HttpMethods.IsGet(context.Request.Method);
                    var capability=section is "dues" or "payments" or "expenses" or "budgets" or "meters" or "reports"?"ACCOUNTANT":read && section is "" or "units" or "buildings" or "announcements" or "documents"?"ACCOUNTANT":"MANAGER";
                    var store=context.RequestServices.GetRequiredService<ISessionContextStore>();
                    if(!await store.PropertyAllowedAsync(property,capability,context.RequestAborted)){context.Response.StatusCode=404;return;}
                    var people=capability=="MANAGER" || await store.PropertyAllowedAsync(property,"MANAGER",context.RequestAborted);
                    var administrative=context.User.IsInRole("SUPER_ADMIN") || context.User.IsInRole("SITE_ADMIN") && await store.PropertyAllowedAsync(property,"SITE_ADMIN",context.RequestAborted);
                    context.Items[typeof(WorkspacePropertyPermissions)]=new WorkspacePropertyPermissions(people,administrative);
                }
            }
            await next(context);
        });
    }
}
