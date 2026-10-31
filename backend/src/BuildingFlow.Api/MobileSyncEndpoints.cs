using BuildingFlow.Application;
using Microsoft.AspNetCore.Http.Features;
namespace BuildingFlow.Api;
internal static class MobileSyncEndpoints {
    public static void MapMobileSyncEndpoints(this WebApplication app) {
        var root=app.MapGroup("/api/v1/mobile/sync").RequireAuthorization().WithTags("Mobile sync");
        root.MapPost("/commands",async(HttpRequest request,IMobileSyncStore store,CancellationToken token)=>{
            if(request.ContentLength>400000)return Results.StatusCode(413);
            var limit=request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();if(limit is {IsReadOnly:false})limit.MaxRequestBodySize=400000;
            if(!request.HasJsonContentType())return Results.StatusCode(415);
            MobileSyncCommand? command;
            try {command=await request.ReadFromJsonAsync<MobileSyncCommand>(token);}catch(System.Text.Json.JsonException){return Results.BadRequest();}
            return command==null?Results.BadRequest():Results.Ok(await store.PushAsync(command,token));
        }).Accepts<MobileSyncCommand>("application/json");
        root.MapGet("/requests/{id:guid}/photo",async(Guid id,IMobileSyncStore store,CancellationToken token,string? contextKind=null,Guid? contextId=null)=>{var bytes=await store.PhotoAsync(id,token,contextKind,contextId);return bytes==null?Results.NotFound():Results.File(bytes,"image/jpeg");});
        root.MapGet("/definitions",async(Guid propertyId,string role,IMobileSyncStore store,CancellationToken token,Guid? unitId=null,string? contextKind=null,Guid? contextId=null)=>Results.Ok(await store.DefinitionsAsync(propertyId,unitId,role,token,contextKind,contextId)));
        root.MapGet("/requests",async(Guid propertyId,string role,IMobileSyncStore store,CancellationToken token,Guid? unitId=null,string? contextKind=null,Guid? contextId=null)=>Results.Ok(await store.PullAsync(propertyId,unitId,role,token,contextKind,contextId)));
    }
}
