using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommonAreaReportTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Day=>DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).AddDays(9);
    private sealed record World(Guid Property,Guid Area,Guid Plan,string User);
    private async Task<World> Seed(bool hours=true)
    {
        var p=new Property("Rapor","City","District","Address");var user=Guid.NewGuid().ToString();
        var a=new CommonArea(p.Id,Guid.NewGuid(),new string('A',64),"Saha",CommonAreaType.SPORTS_COURT,null,CommonAreaVisibility.PROPERTY,CommonAreaUsage.APPROVAL,14,null,null,hours?new TimeOnly(8,0):null,hours?new TimeOnly(22,0):null,125m);
        var plan=new MaintenancePlan(p.Id,"Bakım","Private maintenance",MaintenanceType.MAINTENANCE,Day,null,null,MaintenanceStatus.PLANNED);
        var b=new CommonAreaReservation(p.Id,a.Id,Guid.NewGuid(),new string('B',64),null,Day,new(10,0),new(11,0),4,"Private booking");
        var other=new CommonAreaReservation(p.Id,a.Id,Guid.NewGuid(),new string('C',64),null,Day,new(10,0),new(11,0),6,null);
        var cancelled=new CommonAreaReservation(p.Id,a.Id,Guid.NewGuid(),new string('D',64),null,Day,new(13,0),new(14,0),10,null);cancelled.Cancel("Private reason",DateTimeOffset.UtcNow);
        await factory.WithDb(async db=>{db.AddRange(p,a,plan,b,other,cancelled,new PropertyMembership(p.Id,user));await db.SaveChangesAsync();});return new(p.Id,a.Id,plan.Id,user);
    }
    private static string Root(World w)=>$"/api/v1/properties/{w.Property}/common-areas/{w.Area}";
    private static string Query=>"?from="+Day.ToString("yyyy-MM-dd")+"&to="+Day.AddDays(1).ToString("yyyy-MM-dd");
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task ReportIsScopedConsistentPrivateAndIndependentOfListPage(string role)
    {
        var w=await Seed();using var c=TestToken.Client(factory,w.User,role);
        var closed=await c.PostAsJsonAsync(Root(w)+"/closures",new CommonAreaClosureRequest(Guid.NewGuid(),w.Plan,1,Day.ToString("yyyy-MM-dd"),"12:00","14:00","Bakım"));Assert.Equal(HttpStatusCode.OK,closed.StatusCode);var closure=(await closed.Content.ReadFromJsonAsync<CommonAreaClosureResponse>())!;
        var response=await c.GetAsync(Root(w)+"/report"+Query);Assert.Equal(HttpStatusCode.OK,response.StatusCode);var raw=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("Private",raw);Assert.DoesNotContain("unit",raw,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("feeAmount",raw);
        var report=(await response.Content.ReadFromJsonAsync<CommonAreaReportResponse>())!;Assert.Equal(w.Property,report.PropertyId);Assert.True(report.CurrentConfiguration);Assert.False(report.PaymentSupported);Assert.Equal(2,report.MeasuredDays);Assert.Equal(2,report.Totals.Confirmed);Assert.Equal(1,report.Totals.Cancelled);Assert.Equal(60,report.Totals.BookedMinutes);Assert.Equal(120,report.Totals.ClosedMinutes);Assert.Equal(1560,report.Totals.AvailableMinutes);Assert.Equal(600,report.Totals.PersonMinutes);Assert.Equal("3.85",report.Totals.OccupancyPercent);
        var page=(await c.GetFromJsonAsync<PagedRows<CommonAreaReservationResponse>>(Root(w)+"/reservations"+Query+"&pageSize=1&status=CONFIRMED"))!;Assert.Single(page.Items);Assert.Equal(2,page.TotalCount);
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync(Root(w)+$"/closures/{closure.Id}/release",new CommonAreaClosureReleaseRequest(Version:closure.Version,Reason:"Plan değişti"))).StatusCode);
        var released=(await c.GetFromJsonAsync<CommonAreaReportResponse>(Root(w)+"/report"+Query))!;Assert.Equal(0,released.Totals.ClosedMinutes);Assert.Equal(1680,released.Totals.AvailableMinutes);Assert.Equal(60,released.Totals.BookedMinutes);
    }
    [PostgresFact]public async Task MissingHoursNeverPretendToMeasureUtilization()
    {
        var w=await Seed(false);using var c=TestToken.Client(factory,w.User,"MANAGER");var r=(await c.GetFromJsonAsync<CommonAreaReportResponse>(Root(w)+"/report"+Query))!;Assert.Equal(0,r.MeasuredDays);Assert.Null(r.Totals.AvailableMinutes);Assert.Null(r.Totals.OccupancyPercent);Assert.Equal(2,r.Totals.Confirmed);
    }
    [PostgresTheory][InlineData("OWNER")][InlineData("TENANT")][InlineData("RESIDENT")][InlineData("ACCOUNTANT")]
    public async Task NonManagersCannotReadManagementReports(string role){var w=await Seed();using var c=TestToken.Client(factory,w.User,role);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Root(w)+"/report"+Query)).StatusCode);}
    [PostgresFact]public async Task ForeignMembershipAreaAndBadRangesAreRejected()
    {
        var w=await Seed();using var c=TestToken.Client(factory,w.User,"MANAGER");using var foreign=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");
        Assert.Equal(HttpStatusCode.NotFound,(await foreign.GetAsync(Root(w)+"/report"+Query)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(w).Replace(w.Area.ToString(),Guid.NewGuid().ToString())+"/report"+Query)).StatusCode);
        foreach(var q in new[]{"?from=2026-02-30&to=2026-03-01","?from=2026-01-01&to=2026-04-04","?from=2026-02-01&to=2026-01-01",""})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Root(w)+"/report"+q)).StatusCode);
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Root(w)+"/report"+Query)).StatusCode);
    }
}
