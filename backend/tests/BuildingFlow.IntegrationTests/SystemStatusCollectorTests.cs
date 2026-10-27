using System.Net;
using BuildingFlow.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Collector durumu health_check eklentisinden okunur; OTLP kökünün 404'ü yalnız sağlık adresi ayarlanmamışsa yeterlidir.
public sealed class SystemStatusCollectorTests
{
    private const string Otlp="http://collector.test:4318";
    private const string Health="http://collector.test:13133";
    [Theory]
    [InlineData(HttpStatusCode.OK,"UP")]
    [InlineData(HttpStatusCode.ServiceUnavailable,"DEGRADED")]
    [InlineData(HttpStatusCode.NotFound,"DOWN")]
    public async Task HealthExtensionDecidesCollectorStatus(HttpStatusCode health,string expected)
    {
        var requested=new List<string>();
        Assert.Equal(expected,await CollectorStatus(Health,uri=>{requested.Add(uri.AbsoluteUri);return uri.Port==13133?health:HttpStatusCode.NotFound;}));
        Assert.Equal(new[]{Health+"/"},requested);
    }
    [Fact] public async Task UnreachableHealthExtensionIsDown()=>
        Assert.Equal("DOWN",await CollectorStatus(Health,_=>throw new HttpRequestException("refused")));
    [Fact] public async Task WithoutHealthUrlAnyOtlpResponseIsReachable()=>
        Assert.Equal("UP",await CollectorStatus(null,uri=>uri.Port==4318?HttpStatusCode.NotFound:HttpStatusCode.OK));
    private static async Task<string> CollectorStatus(string? healthUrl,Func<Uri,HttpStatusCode> respond)
    {
        var configuration=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["Telemetry:Enabled"]="true",["Telemetry:Endpoint"]=Otlp,["SystemStatus:CollectorHealthUrl"]=healthUrl,["SystemStatus:TimeoutMs"]="500"}).Build();
        var scopes=new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var service=new SystemStatusService(scopes,configuration,new HostingEnvironment{EnvironmentName="Test"},new Clients(respond),TimeProvider.System);
        var status=await service.CheckAsync(CancellationToken.None);
        return status.Components.Single(c=>c.Id=="otel-collector").Status;
    }
    private sealed class Clients(Func<Uri,HttpStatusCode> respond):IHttpClientFactory
    {
        public HttpClient CreateClient(string name)=>new(new Handler(respond));
    }
    private sealed class Handler(Func<Uri,HttpStatusCode> respond):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>
            Task.FromResult(new HttpResponseMessage(respond(request.RequestUri!)));
    }
}
