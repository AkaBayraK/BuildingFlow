using System.Diagnostics;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Routing;
namespace BuildingFlow.Api;
internal static class RequestTelemetry
{
    public static void UseBuildingFlowRequestTelemetry(this WebApplication app)
    {
        app.Use(async(context,next)=>{
            using var activity=BuildingFlowTelemetry.Activities.StartActivity("http.request",ActivityKind.Server);
            var start=Stopwatch.GetTimestamp();
            using var scope=app.Logger.BeginScope(new Dictionary<string,object?>{["TraceId"]=Activity.Current?.TraceId.ToHexString()});
            try{await next(context);}
            finally{
                var route=(context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText??"unmatched";
                // Only code-owned endpoint templates, even for 404s and requests carrying sensitive queries.
                var method=context.Request.Method is "GET" or "POST" or "PUT" or "DELETE" or "OPTIONS" or "HEAD"?context.Request.Method:"OTHER";
                var status=context.Response.StatusCode;var elapsed=Stopwatch.GetElapsedTime(start).TotalSeconds;
                var tags=new TagList{{"route",route},{"method",method},{"status",status}};
                BuildingFlowTelemetry.Requests.Add(1,tags);BuildingFlowTelemetry.Duration.Record(elapsed,tags);
                activity?.SetTag("http.route",route);activity?.SetTag("http.request.method",method);activity?.SetTag("http.response.status_code",status);
                if(status>=500)activity?.SetStatus(ActivityStatusCode.Error);
                app.Logger.LogInformation("HTTP {Method} {Route} completed {Status} in {DurationSeconds} seconds",method,route,status,elapsed);
            }
        });
    }
}
