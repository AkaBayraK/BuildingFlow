using BuildingFlow.Application;
using Microsoft.AspNetCore.Http.Features;
namespace BuildingFlow.Api;
internal static class MobileSyncEndpoints {
    public static void MapMobileSyncEndpoints(this WebApplication app) {
        var root=app.MapGroup("/api/v1/mobile/sync").RequireAuthorization().WithTags("Mobile sync");
        root.MapPost("/commands",async(HttpRequest request,IMobileSyncStore store,CancellationToken token)=>{
            if(request.ContentLength>32768)return Results.StatusCode(413);
            var limit=request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();if(limit is {IsReadOnly:false})limit.MaxRequestBodySize=32768;
            if(!request.HasJsonContentType())return Results.StatusCode(415);
            MobileSyncCommand? command;
            try {command=await request.ReadFromJsonAsync<MobileSyncCommand>(token);}catch(System.Text.Json.JsonException){return Results.BadRequest();}
            return command==null?Results.BadRequest():Results.Ok(await store.PushAsync(command,token));
        }).Accepts<MobileSyncCommand>("application/json");
        root.MapGet("/requests",async(Guid propertyId,string role,IMobileSyncStore store,CancellationToken token,Guid? unitId=null)=>Results.Ok(await store.PullAsync(propertyId,unitId,role,token)));
    }
}
