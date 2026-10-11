using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using BuildingFlow.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class TelemetryTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    [Fact] public async Task MetricsUseOnlyRouteTemplatesMethodAndStatusNeverSensitiveUrls()
    {
        var rows=new ConcurrentBag<KeyValuePair<string,object?>[]>();using var listener=new MeterListener();listener.InstrumentPublished=(i,l)=>{if(i.Meter.Name==BuildingFlowTelemetry.Source&&i.Name=="buildingflow.http.requests")l.EnableMeasurementEvents(i);};listener.SetMeasurementEventCallback<long>((i,v,t,s)=>rows.Add(t.ToArray()));listener.Start();using var c=factory.CreateClient();await c.GetAsync("/private-person-identifier?password=private-secret");await c.GetAsync("/api/v1/status?email=secret@example.test");Assert.Contains(rows,r=>r.Any(t=>t.Key=="route"&&Equals(t.Value,"unmatched")));Assert.Contains(rows,r=>r.Any(t=>t.Key=="status"&&Equals(t.Value,401)));foreach(var tags in rows){Assert.All(tags,t=>Assert.Contains(t.Key,new[]{"route","method","status"}));Assert.DoesNotContain(tags,t=>Convert.ToString(t.Value)!.Contains("private")||Convert.ToString(t.Value)!.Contains("secret"));}
    }
    [Theory][InlineData("http://user:pass@localhost:4318")][InlineData("http://localhost:4318?token=secret")][InlineData("ftp://localhost:4318")][InlineData("http://localhost:4318/v1/logs")]
    public void ExporterConfigurationCannotCarryCredentialsOrQueries(string endpoint){var c=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Telemetry:Enabled"]="true",["Telemetry:Endpoint"]=endpoint}).Build();var services=new ServiceCollection();services.AddLogging(log=>Assert.Throws<InvalidOperationException>(()=>services.AddBuildingFlowTelemetry(log,c,"buildingflow-test")));}
    [Fact] public void DisabledTelemetryNeedsNoEndpointAndNoOutboundProvider(){var services=new ServiceCollection();var c=new ConfigurationBuilder().Build();services.AddLogging(log=>services.AddBuildingFlowTelemetry(log,c,"buildingflow-test"));Assert.DoesNotContain(services,d=>d.ServiceType.FullName?.Contains("OpenTelemetry")??false);}
}
