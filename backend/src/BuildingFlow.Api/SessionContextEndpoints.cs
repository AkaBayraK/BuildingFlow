using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal sealed record WorkspacePropertyPermissions(bool CanManagePeople,bool CanManageAssignments);
internal static class SessionContextEndpoints
{
    public static void MapSessionContextEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v2/mobile/authorization",async(IMobileAuthorizationStore store,CancellationToken token)=>Results.Ok(await store.ReadAsync(token)))
            .RequireAuthorization().AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled") && app.Configuration.GetValue<bool>("PersonRefactor:SessionContextsEnabled") && app.Configuration.GetValue<bool>("PersonRefactor:MobileAuthorizationEnabled")?await next(context):Results.NotFound());
        app.MapPost("/api/v2/me/person-links",async(LinkMyPersonRequest request,PersonAccountLinkService service,HttpResponse response,CancellationToken token)=>{response.Headers.CacheControl="no-store";return Results.Ok(await service.LinkAsync(request,token));})
            .RequireAuthorization().AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled")?await next(context):Results.NotFound());
        var me=app.MapGroup("/api/v2/me").RequireAuthorization();
        me.AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled") && app.Configuration.GetValue<bool>("PersonRefactor:SessionContextsEnabled")?await next(context):Results.NotFound());
        me.MapGet("/portfolio",async(IMyPortfolioStore store,HttpResponse response,CancellationToken token)=>{response.Headers.CacheControl="no-store";return Results.Ok(await store.PortfolioAsync(token));});
        // The selected context is only a hint: it resolves from current database facts or returns 404.
        me.MapGet("/active-context",async(Guid propertyId,string capacity,Guid relationId,IMyPortfolioStore store,HttpResponse response,CancellationToken token,Guid? unitId=null)=>{response.Headers.CacheControl="no-store";return await store.ResolveAsync(new(propertyId,unitId,capacity,relationId),token) is {} value?Results.Ok(value):Results.NotFound();});
        var unit=me.MapGroup("/units/{unitId:guid}");unit.AddEndpointFilter(async(context,next)=>{context.HttpContext.Response.Headers.CacheControl="no-store";return await next(context);});
        unit.MapGet("/dues",async(Guid unitId,Guid propertyId,string capacity,Guid relationId,IMyUnitStore store,CancellationToken token,int page=1,int pageSize=20)=>Found(await store.DuesAsync(new(propertyId,unitId,capacity,relationId,page,pageSize),token)));
        unit.MapGet("/payments",async(Guid unitId,Guid propertyId,string capacity,Guid relationId,IMyUnitStore store,CancellationToken token,int page=1,int pageSize=20)=>Found(await store.PaymentsAsync(new(propertyId,unitId,capacity,relationId,page,pageSize),token)));
        unit.MapGet("/announcements",async(Guid unitId,Guid propertyId,string capacity,Guid relationId,IMyUnitStore store,CancellationToken token,int page=1,int pageSize=20)=>Found(await store.AnnouncementsAsync(new(propertyId,unitId,capacity,relationId,page,pageSize),token)));
        unit.MapGet("/documents",async(Guid unitId,Guid propertyId,string capacity,Guid relationId,IMyUnitStore store,CancellationToken token,int page=1,int pageSize=20)=>Found(await store.DocumentsAsync(new(propertyId,unitId,capacity,relationId,page,pageSize),token)));
        unit.MapGet("/documents/{documentId:guid}/download",async(Guid unitId,Guid documentId,Guid propertyId,string capacity,Guid relationId,IMyUnitStore store,HttpResponse response,CancellationToken token)=>{var file=await store.DownloadAsync(new(propertyId,unitId,capacity,relationId),documentId,token);if(file==null)return Results.NotFound();response.Headers["X-Content-Type-Options"]="nosniff";return Results.File(file.Content,file.ContentType,file.FileName,enableRangeProcessing:false);});
        me.MapGet("/finance-summary",async(IMyUnitStore store,HttpResponse response,CancellationToken token)=>{response.Headers.CacheControl="no-store";return Results.Ok(await store.FinanceSummaryAsync(token));});
        unit.MapGet("/leases",async(Guid unitId,Guid propertyId,string capacity,Guid relationId,IMyUnitStore store,CancellationToken token,int page=1,int pageSize=20)=>Found(await store.LeasesAsync(new(propertyId,unitId,capacity,relationId,page,pageSize),token)));
        var root=app.MapGroup("/api/v2/session/contexts").RequireAuthorization();
        root.AddEndpointFilter(async(context,next)=>app.Configuration.GetValue<bool>("PersonRefactor:Enabled") && app.Configuration.GetValue<bool>("PersonRefactor:SessionContextsEnabled")?await next(context):Results.NotFound());
        root.MapGet("",async(ISessionContextStore store,CancellationToken token,int page=1,int pageSize=20)=>Results.Ok(await store.ListAsync(page,pageSize,token)));
        root.MapGet("/website/{siteKey}/{audience}",async(string siteKey,string audience,ISessionContextStore store,HttpResponse response,CancellationToken token,int page=1,int pageSize=20)=>{response.Headers.CacheControl="no-store";return await store.WebsiteAsync(siteKey,audience,page,pageSize,token) is {} value?Results.Ok(value):Results.NotFound();});
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
    private static IResult Found<T>(T? value) where T:class=>value==null?Results.NotFound():Results.Ok(value);
}
