using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommonAreaReleaseTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    [PostgresFact]public async Task ReportBudgetWithThirtyDaysAndConcurrentReaders()
    {
        var p=new Property("Release fixture","City","District","Address");var user=Guid.NewGuid().ToString();var day=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).AddDays(3);
        var area=new CommonArea(p.Id,Guid.NewGuid(),new string('A',64),"Report fixture",CommonAreaType.SPORTS_COURT,null,CommonAreaVisibility.PROPERTY,CommonAreaUsage.RESERVATION,14,null,null,new(8,0),new(22,0),0);
        var rows=new List<CommonAreaReservation>();for(var d=0;d<30;d++)for(var slot=0;slot<32;slot++){var start=new TimeOnly(8,0).AddMinutes(slot*15);rows.Add(new(p.Id,area.Id,Guid.NewGuid(),new string('B',64),null,day.AddDays(d),start,start.AddMinutes(15),2,null));}
        await factory.WithDb(async db=>{db.AddRange(p,area,new PropertyMembership(p.Id,user));db.AddRange(rows);await db.SaveChangesAsync();});
        using var client=TestToken.Client(factory,user,"MANAGER");var url=$"/api/v1/properties/{p.Id}/common-areas/{area.Id}/report?from={day:yyyy-MM-dd}&to={day.AddDays(29):yyyy-MM-dd}";
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(url)).StatusCode);var samples=new List<double>();var sizes=new List<int>();
        for(var round=0;round<3;round++){
            var result=await Task.WhenAll(Enumerable.Range(0,4).Select(async _=>{var timer=Stopwatch.StartNew();using var response=await client.GetAsync(url);var raw=await response.Content.ReadAsByteArrayAsync();timer.Stop();Assert.Equal(HttpStatusCode.OK,response.StatusCode);var report=JsonSerializer.Deserialize<CommonAreaReportResponse>(raw,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;Assert.Equal(960,report.Totals.Confirmed);Assert.Equal(14400,report.Totals.BookedMinutes);Assert.Equal(30,report.Days.Count);Assert.True(raw.Length<100000);return(timer.Elapsed.TotalMilliseconds,raw.Length);}));
            samples.AddRange(result.Select(x=>x.TotalMilliseconds));sizes.AddRange(result.Select(x=>x.Length));
        }
        samples.Sort();var p95=samples[(int)Math.Ceiling(samples.Count*.95)-1];Assert.True(p95<10000,$"Local report smoke exceeded10seconds: {p95:F0}ms");
        var path=Environment.GetEnvironmentVariable("BUILDINGFLOW_COMMON_AREA_PERF_REPORT");if(!string.IsNullOrEmpty(path))await File.WriteAllTextAsync(path,JsonSerializer.Serialize(new{records=960,days=30,samples=samples.Count,concurrency=4,p50Ms=Math.Round(samples[samples.Count/2],2),p95Ms=Math.Round(p95,2),maxBytes=sizes.Max(),scope="in-process HTTP + real isolated PostgreSQL; test JWT; not production load qualification"},new JsonSerializerOptions{WriteIndented=true}));
    }
}
