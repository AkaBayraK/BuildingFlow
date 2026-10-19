using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BuildingFlow.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class ReleaseSecurityTests
{
    const string Issuer="https://auth.example.test/realms/buildingflow";
    static readonly SymmetricSecurityKey Key=new(Encoding.UTF8.GetBytes(new string('r',64))){KeyId="release-test-only"};
    static Dictionary<string,string?> Valid() => new() { ["Authentication:Authority"]=Issuer,["Authentication:Audience"]="buildingflow-api",["Authentication:MetadataAddress"]="",["Cors:Origins:0"]="https://web.example.test",["AllowedHosts"]="api.example.test",["ConnectionStrings:Database"]="Host=localhost;Database=unused_security_tests",["Security:RateLimit:Anonymous"]="2",["PersonCode:HmacKey"]=new string('a',64) };
    [Theory][InlineData("Authentication:Authority","http://auth.example.test")][InlineData("Authentication:Authority","https://user:secret@auth.example.test")][InlineData("Authentication:MetadataAddress","http://auth.example.test/metadata")][InlineData("Cors:Origins:0","https://web.example.test/path")][InlineData("Cors:Origins:0","http://web.example.test")][InlineData("Cors:Origins:0","*")][InlineData("AllowedHosts","*")][InlineData("AllowedHosts","")][InlineData("ASPNETCORE_FORWARDEDHEADERS_ENABLED","true")]
    [InlineData("PersonCode:HmacKey","")][InlineData("PersonCode:HmacKey","00112233445566778899aabbccddeeff")][InlineData("PersonCode:HmacKey","zz112233445566778899aabbccddeeff00112233445566778899aabbccddeeff")]
    public void UnsafeProductionConfigurationFailsClosed(string key,string value){var settings=Valid();settings[key]=value;Assert.Throws<InvalidOperationException>(()=>ReleaseSecurity.ValidateProduction(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));}
    [Fact]public void HttpsProductionConfigurationIsAccepted()=>ReleaseSecurity.ValidateProduction(new ConfigurationBuilder().AddInMemoryCollection(Valid()).Build());
    [Theory][InlineData("Security:RateLimit:Enabled","false")][InlineData("Security:RateLimit:Reads","0")][InlineData("Security:RateLimit:Writes","10001")][InlineData("Security:KnownProxies:0","0.0.0.0")][InlineData("Security:KnownProxies:0","10.0.0.0/8")][InlineData("Security:KnownProxies:0","proxy.example.test")]
    public void ProductionRejectsDisabledLimitsAndBroadProxyTrust(string key,string value){using var f=new Factory(new(){[key]=value});Assert.Throws<InvalidOperationException>(()=>f.CreateClient());}
    [Fact]public void ForwardedHeadersStayDisabledWithoutExplicitProxy(){using var f=new Factory();f.CreateClient().Dispose();var o=f.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;Assert.Equal(ForwardedHeaders.None,o.ForwardedHeaders);Assert.Empty(o.KnownProxies);Assert.Empty(o.KnownIPNetworks);}
    [Fact]public async Task HttpAndUntrustedForwardedProtoCannotReachProtectedApi(){using var f=new Factory();using var c=f.CreateClient(new(){BaseAddress=new("http://api.example.test"),AllowAutoRedirect=false});c.DefaultRequestHeaders.Add("X-Forwarded-Proto","https");using var r=await c.GetAsync("/api/v1/status");Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Equal("BF_INVALID_REQUEST",(await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());}
    [Fact]public async Task ForwardedHeadersFromUnlistedPeerAreIgnored(){using var f=new Factory(new(){["Security:KnownProxies:0"]="10.0.0.5"},"10.0.0.6");using var c=f.CreateClient(new(){BaseAddress=new("http://api.example.test"),AllowAutoRedirect=false});c.DefaultRequestHeaders.Add("X-Forwarded-Proto","https");Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/status")).StatusCode);}
    [Fact]public async Task TrustedProxyForwardsSchemeAndClientAddress(){using var f=new Factory(new(){["Security:KnownProxies:0"]="10.0.0.5"},"10.0.0.5");using var c=f.CreateClient(new(){BaseAddress=new("http://api.example.test"),AllowAutoRedirect=false});
        async Task<HttpStatusCode> From(string client){using var m=new HttpRequestMessage(HttpMethod.Get,"/api/v1/status");m.Headers.Add("X-Forwarded-Proto","https");m.Headers.Add("X-Forwarded-For",client);using var r=await c.SendAsync(m);return r.StatusCode;}
        Assert.Equal(HttpStatusCode.Unauthorized,await From("198.51.100.1"));Assert.Equal(HttpStatusCode.Unauthorized,await From("198.51.100.1"));Assert.Equal(HttpStatusCode.TooManyRequests,await From("198.51.100.1"));Assert.Equal(HttpStatusCode.Unauthorized,await From("198.51.100.2"));}
    [Fact]public async Task SpoofedForwardedForCannotEscapeAnonymousLimit(){using var f=new Factory();using var c=f.CreateClient(new(){BaseAddress=new("https://api.example.test")});var codes=new List<HttpStatusCode>();
        foreach(var spoof in new[]{"203.0.113.1","203.0.113.2","203.0.113.3"}){using var m=new HttpRequestMessage(HttpMethod.Get,"/api/v1/status");m.Headers.Add("X-Forwarded-For",spoof);using var r=await c.SendAsync(m);codes.Add(r.StatusCode);}
        Assert.Equal([HttpStatusCode.Unauthorized,HttpStatusCode.Unauthorized,HttpStatusCode.TooManyRequests],codes);}
    [Fact]public async Task ProductionHttpsHasHstsNoStoreAndCoded429(){using var f=new Factory();using var c=f.CreateClient(new(){BaseAddress=new("https://api.example.test")});using var a=await c.GetAsync("/api/v1/status");Assert.Equal(HttpStatusCode.Unauthorized,a.StatusCode);Assert.True(a.Headers.Contains("Strict-Transport-Security"));Assert.Equal("DENY",a.Headers.GetValues("X-Frame-Options").Single());Assert.Equal("nosniff",a.Headers.GetValues("X-Content-Type-Options").Single());Assert.Equal("no-referrer",a.Headers.GetValues("Referrer-Policy").Single());Assert.Equal("no-store",a.Headers.CacheControl!.ToString());await c.GetAsync("/api/v1/status");using var limited=await c.GetAsync("/api/v1/status");Assert.Equal(HttpStatusCode.TooManyRequests,limited.StatusCode);Assert.NotNull(limited.Headers.RetryAfter);Assert.Equal("BF_RATE_LIMITED",(await limited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/health/live")).StatusCode);}
    [Fact]public async Task AuthenticatedLimitsArePerSubjectAndSeparateForWrites(){using var f=new Factory(new(){["Security:RateLimit:Reads"]="2",["Security:RateLimit:Writes"]="1"});using var c=f.CreateClient(new(){BaseAddress=new("https://api.example.test")});
        async Task<HttpStatusCode> Send(HttpMethod method,string token){using var m=new HttpRequestMessage(method,"/api/v1/status");m.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);using var r=await c.SendAsync(m);return r.StatusCode;}
        var alice=Token("alice");var bob=Token("bob");
        Assert.Equal(HttpStatusCode.OK,await Send(HttpMethod.Get,alice));Assert.Equal(HttpStatusCode.OK,await Send(HttpMethod.Get,alice));Assert.Equal(HttpStatusCode.TooManyRequests,await Send(HttpMethod.Get,alice));
        Assert.Equal(HttpStatusCode.OK,await Send(HttpMethod.Get,bob));
        Assert.NotEqual(HttpStatusCode.TooManyRequests,await Send(HttpMethod.Post,alice));Assert.Equal(HttpStatusCode.TooManyRequests,await Send(HttpMethod.Post,alice));}
    [Theory][InlineData("unsigned")][InlineData("foreign-key")][InlineData("http-issuer")][InlineData("expired")]
    public async Task ProductionRejectsForgedTokens(string fault){using var f=new Factory();using var c=f.CreateClient(new(){BaseAddress=new("https://api.example.test")});c.DefaultRequestHeaders.Authorization=new("Bearer",Token("mallory",fault));Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/status")).StatusCode);}
    [Fact]public async Task ProductionOpenApiIsNotPublished(){using var f=new Factory();using var c=f.CreateClient(new(){BaseAddress=new("https://api.example.test")});Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/openapi/v1.json")).StatusCode);}
    static string Token(string subject,string? fault=null){
        if(fault=="unsigned")return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(),new JwtPayload(Issuer,"buildingflow-api",[new("sub",subject)],DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5))));
        var key=fault=="foreign-key"?new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x',64))){KeyId="release-test-only"}:Key;
        var expires=fault=="expired"?DateTime.UtcNow.AddMinutes(-10):DateTime.UtcNow.AddMinutes(5);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(fault=="http-issuer"?"http://auth.example.test/realms/buildingflow":Issuer,"buildingflow-api",[new("sub",subject)],DateTime.UtcNow.AddMinutes(-20),expires,new SigningCredentials(key,SecurityAlgorithms.HmacSha256)));}
    sealed class Factory(Dictionary<string,string?>? overrides=null,string? peer=null):WebApplicationFactory<Program>{
        protected override IHost CreateHost(IHostBuilder builder){var settings=Valid();foreach(var(k,v)in overrides??[])settings[k]=v;builder.ConfigureHostConfiguration(c=>c.AddInMemoryCollection(settings));return base.CreateHost(builder);}
        protected override void ConfigureWebHost(IWebHostBuilder b){b.UseEnvironment("Production");b.ConfigureServices(s=>{
            if(peer is not null)s.AddSingleton<IStartupFilter>(new PeerAddress(IPAddress.Parse(peer)));
            s.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,o=>{var m=new OpenIdConnectConfiguration{Issuer=Issuer};m.SigningKeys.Add(Key);o.ConfigurationManager=new StaticConfigurationManager<OpenIdConnectConfiguration>(m);});});}
    }
    sealed class PeerAddress(IPAddress address):IStartupFilter{public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)=>app=>{app.Use((c,n)=>{c.Connection.RemoteIpAddress=address;return n(c);});next(app);};}
}
