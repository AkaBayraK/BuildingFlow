using System.Net;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
namespace BuildingFlow.Api;

public static class ReleaseSecurity
{
    public static IServiceCollection AddReleaseSecurity(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) ValidateProduction(config);
        var proxies = config.GetSection("Security:KnownProxies").Get<string[]>() ?? [];
        services.Configure<ForwardedHeadersOptions>(o => {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.ForwardLimit = 1; o.KnownProxies.Clear(); o.KnownIPNetworks.Clear();
            foreach (var address in proxies) {
                if (!IPAddress.TryParse(address, out var ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) throw new InvalidOperationException("Security:KnownProxies requires individual trusted IP addresses.");
                o.KnownProxies.Add(ip);
            }
            // With no configured proxy, forwarding stays disabled rather than trusting every sender.
            if (proxies.Length == 0) o.ForwardedHeaders = ForwardedHeaders.None;
        });
        services.AddHsts(o => { o.MaxAge = TimeSpan.FromDays(180); o.IncludeSubDomains = false; });
        var enabled = config.GetValue("Security:RateLimit:Enabled", !environment.IsDevelopment());
        if (!environment.IsDevelopment() && !enabled) throw new InvalidOperationException("Production API rate limiting cannot be disabled.");
        int Limit(string name,int fallback) {var v=config.GetValue("Security:RateLimit:"+name,fallback);if(v is <1 or >10000)throw new InvalidOperationException("Invalid API rate-limit configuration.");return v;}
        var anonymous = Limit("Anonymous",30); var reads = Limit("Reads",300); var writes=Limit("Writes",60);
        services.AddRateLimiter(o => {
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext,string>(c => {
                if(!enabled || !c.Request.Path.StartsWithSegments("/api"))return RateLimitPartition.GetNoLimiter("bypass");
                var subject = c.User.Identity?.IsAuthenticated == true ? c.User.FindFirst("sub")?.Value : null;
                var write = c.Request.Method is not ("GET" or "HEAD" or "OPTIONS");
                var key = subject is null ? "anonymous:"+(c.Connection.RemoteIpAddress?.ToString()??"unknown") : (write?"write:":"read:")+subject;
                return RateLimitPartition.GetFixedWindowLimiter(key,_=>new FixedWindowRateLimiterOptions {PermitLimit=subject is null?anonymous:write?writes:reads,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true});
            });
            o.OnRejected = async (c,token) => {
                c.HttpContext.Response.StatusCode=429;
                c.HttpContext.Response.Headers.RetryAfter = c.Lease.TryGetMetadata(MetadataName.RetryAfter,out var retry) ? Math.Max(1,(int)Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture) : "60";
                var problem=new ProblemDetails {Status=429};ApiProblems.Apply(problem,ApiProblems.ForStatus(429));await ApiProblems.Write(c.HttpContext,problem,token);
            };
        });
        return services;
    }
    public static void ValidateProduction(IConfiguration config)
    {
        static bool Https(string? value,bool origin=false) => Uri.TryCreate(value,UriKind.Absolute,out var u) && u.Scheme=="https" && string.IsNullOrEmpty(u.UserInfo) && string.IsNullOrEmpty(u.Query) && string.IsNullOrEmpty(u.Fragment) && (!origin || u.AbsolutePath=="/");
        if(!Https(config["Authentication:Authority"]))throw new InvalidOperationException("Production Authentication:Authority must use HTTPS.");
        if(config["Authentication:MetadataAddress"] is {Length:>0} metadata && !Https(metadata))throw new InvalidOperationException("Production discovery metadata must use HTTPS.");
        var origins=config.GetSection("Cors:Origins").Get<string[]>()??[];
        if(origins.Length==0 || origins.Any(o=>!Https(o,true)))throw new InvalidOperationException("Production Cors:Origins requires explicit HTTPS origins.");
        var hosts=(config["AllowedHosts"]??"").Split(';',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        if(hosts.Length==0 || hosts.Any(h=>h.Contains('*') || h.Contains('/') || Uri.CheckHostName(h)==UriHostNameType.Unknown))throw new InvalidOperationException("Production AllowedHosts requires explicit hostnames.");
        if(config.GetValue<bool>("ASPNETCORE_FORWARDEDHEADERS_ENABLED") || config.GetValue<bool>("DOTNET_FORWARDEDHEADERS_ENABLED") || config.GetValue<bool>("ForwardedHeaders_Enabled"))throw new InvalidOperationException("Automatic trust of forwarded headers is forbidden; configure Security:KnownProxies.");
    }
    public static IApplicationBuilder UseReleaseSecurity(this WebApplication app)
    {
        app.UseForwardedHeaders();
        if(!app.Environment.IsDevelopment()) app.UseHsts();
        return app.Use(async (c,next) => {
            c.Response.OnStarting(()=> {
                c.Response.Headers.XContentTypeOptions="nosniff";c.Response.Headers.XFrameOptions="DENY";c.Response.Headers["Referrer-Policy"]="no-referrer";
                if(c.Request.Path.StartsWithSegments("/api"))c.Response.Headers.CacheControl="no-store";
                return Task.CompletedTask;
            });
            if(!app.Environment.IsDevelopment() && c.Request.Path.StartsWithSegments("/api") && !c.Request.IsHttps) {
                c.Response.StatusCode=400;var p=new ProblemDetails{Status=400};ApiProblems.Apply(p,ApiProblems.ForStatus(400));await ApiProblems.Write(c,p,c.RequestAborted);return;
            }
            await next(c);
        });
    }
}
