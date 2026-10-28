using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.IntegrationTests;
public sealed class CommonAreaMaintenanceTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Day=>DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).AddDays(5);
    private sealed record World(Guid P,Guid A,Guid Plan,string User,string Resident);
    private static string Root(World w)=>$"/api/v1/properties/{w.P}/common-areas/{w.A}";
    private async Task<World> Seed(bool restricted=false)
    {
        var p=new Property("Bakım","City","District","Address",restricted?PropertyType.SITE:PropertyType.APARTMENT);var building=restricted?p.AddBuilding("A"):null;string user=Guid.NewGuid().ToString(),resident=Guid.NewGuid().ToString();
        var a=new CommonArea(p.Id,Guid.NewGuid(),new string('A',64),"Halı saha",CommonAreaType.SPORTS_COURT,building?.Id,restricted?CommonAreaVisibility.BUILDING:CommonAreaVisibility.PROPERTY,CommonAreaUsage.RESERVATION,14,null,null,new(8,0),new(22,0),0);
        var plan=new MaintenancePlan(p.Id,"Saha bakımı","Yönetim içi açıklama",MaintenanceType.MAINTENANCE,Day,null,null,MaintenanceStatus.PLANNED);
        var person=new Person(p.Id,"Fixture","Resident","+905550000000","fixture@example.test");person.SetProfile("NATURAL","DOMESTIC","TR","12345678901",null,null,null,resident);
        await factory.WithDb(async db=>{db.AddRange(p,a,plan,person,new PropertyMembership(p.Id,user),new PropertyMembership(p.Id,resident));await db.SaveChangesAsync();});return new(p.Id,a.Id,plan.Id,user,resident);
    }
    private static CommonAreaClosureRequest Request(World w)=>new(Guid.NewGuid(),w.Plan,1,Day.ToString("yyyy-MM-dd"),"10:00","12:00","Saha zemin bakımı nedeniyle kapalıdır.");
    private static CommonAreaReservationRequest Booking(string start="10:00",string end="11:00")=>new(Guid.NewGuid(),null,Day.ToString("yyyy-MM-dd"),start,end,4,"Private booking note");
    private static async Task<T> Ok<T>(HttpResponseMessage r,HttpStatusCode status=HttpStatusCode.OK){Assert.True(r.StatusCode==status,$"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return(await r.Content.ReadFromJsonAsync<T>())!;}
    [PostgresFact]public async Task CloseReplayBlockReleasePublishAndNotifyAreAtomicAndDeduplicated()
    {
        var w=await Seed();using var c=TestToken.Client(factory,w.User,"MANAGER");var request=Request(w);var close=await Ok<CommonAreaClosureResponse>(await c.PostAsJsonAsync(Root(w)+"/closures",request));
        Assert.Equal(1,close.NotificationCount);var replay=await Ok<CommonAreaClosureResponse>(await c.PostAsJsonAsync(Root(w)+"/closures",request));Assert.True(replay.Replayed);Assert.Equal(close.Id,replay.Id);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(w)+"/closures",request with{Notice="Changed"})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(w)+"/reservations",Booking())).StatusCode);
        await Ok<CommonAreaReservationResponse>(await c.PostAsJsonAsync(Root(w)+"/reservations",Booking("12:00","13:00")),HttpStatusCode.Created);
        var schedule=await c.GetStringAsync(Root(w)+"/schedule?date="+Day.ToString("yyyy-MM-dd"));Assert.Contains("CLOSED",schedule);Assert.DoesNotContain("Private booking note",schedule);Assert.DoesNotContain("Yönetim içi",schedule);
        using var recipient=TestToken.Client(factory,w.Resident,"RESIDENT");var inbox=await recipient.GetFromJsonAsync<PagedRows<PersonMessageResponse>>("/api/v1/notifications");Assert.Single(inbox!.Items);
        var released=await Ok<CommonAreaClosureResponse>(await c.PostAsJsonAsync(Root(w)+$"/closures/{close.Id}/release",new CommonAreaClosureReleaseRequest(close.Version,"Bitti")));Assert.NotNull(released.ReleasedAt);Assert.Equal(2,released.NotificationCount);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(w)+$"/closures/{close.Id}/release",new CommonAreaClosureReleaseRequest(released.Version,"Bitti"))).StatusCode);
        Assert.DoesNotContain("CLOSED",await c.GetStringAsync(Root(w)+"/schedule?date="+Day.ToString("yyyy-MM-dd")));
        await Ok<CommonAreaReservationResponse>(await c.PostAsJsonAsync(Root(w)+"/reservations",Booking()),HttpStatusCode.Created);
        var latest=await Ok<CommonAreaClosureResponse>(await c.PostAsJsonAsync(Root(w)+"/closures",request));Assert.True(latest.Replayed);Assert.NotNull(latest.ReleasedAt);
        await factory.WithDb(async db=>{Assert.Equal(1,await db.Set<CommonAreaClosure>().CountAsync(x=>x.PropertyId==w.P));Assert.Equal(2,await db.PersonMessages.CountAsync(x=>x.PropertyId==w.P));Assert.Equal(AnnouncementStatus.ARCHIVED,(await db.Announcements.SingleAsync(x=>x.Id==close.AnnouncementId)).Status);});
    }
    [PostgresTheory][InlineData("COMPLETED")][InlineData("CANCELLED")][InlineData("DELETE")]
    public async Task MaintenanceCompletionCancellationOrDeletionReleasesOnlyItsClosures(string action)
    {
        var w=await Seed();using var c=TestToken.Client(factory,w.User,"MANAGER");await Ok<CommonAreaClosureResponse>(await c.PostAsJsonAsync(Root(w)+"/closures",Request(w)));
        var path=$"/api/v1/properties/{w.P}/maintenance-plans/{w.Plan}";var plan=await c.GetFromJsonAsync<MaintenancePlanResponse>(path);
        var r=action=="DELETE"?await c.DeleteAsync(path+"?version="+plan!.Version):await c.PutAsJsonAsync(path,new MaintenancePlanRequest(plan!.Title,plan.Description,plan.Type,Day,plan.Location,plan.Responsible,action,plan.Version));Assert.True(r.IsSuccessStatusCode);
        Assert.DoesNotContain("CLOSED",await c.GetStringAsync(Root(w)+"/schedule?date="+Day.ToString("yyyy-MM-dd")));
        await factory.WithDb(async db=>Assert.NotNull((await db.Set<CommonAreaClosure>().SingleAsync(x=>x.PropertyId==w.P)).ReleasedAt));
    }
    [PostgresFact]public async Task ExistingBookingAndPlanDateChangeAreNeverSilentlyCancelledOrMoved()
    {
        var w=await Seed();using var c=TestToken.Client(factory,w.User,"MANAGER");var booking=await Ok<CommonAreaReservationResponse>(await c.PostAsJsonAsync(Root(w)+"/reservations",Booking()),HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(w)+"/closures",Request(w))).StatusCode);
        await factory.WithDb(async db=>{Assert.False(await db.Set<CommonAreaClosure>().AnyAsync(x=>x.PropertyId==w.P));Assert.False(await db.Announcements.AnyAsync(x=>x.PropertyId==w.P));Assert.Equal(CommonAreaReservationStatus.CONFIRMED,(await db.Set<CommonAreaReservation>().SingleAsync(x=>x.Id==booking.Id)).Status);});
        var request=Request(w) with{StartsAt="13:00",EndsAt="14:00"};await Ok<CommonAreaClosureResponse>(await c.PostAsJsonAsync(Root(w)+"/closures",request));
        var path=$"/api/v1/properties/{w.P}/maintenance-plans/{w.Plan}";var plan=(await c.GetFromJsonAsync<MaintenancePlanResponse>(path))!;
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(path,new MaintenancePlanRequest(plan.Title,null,plan.Type,Day.AddDays(1),null,null,"PLANNED",plan.Version))).StatusCode);
    }
    [PostgresFact]public async Task ResidentContextSeesClosedTimesWithoutManagementDetails()
    {
        var w=await Seed();using var manager=TestToken.Client(factory,w.User,"MANAGER");await Ok<CommonAreaClosureResponse>(await manager.PostAsJsonAsync(Root(w)+"/closures",Request(w)));
        var tenant=new PersonTenant("Fixture");var account=new PersonUserAccount("http://localhost:8080/realms/buildingflow",w.Resident);
        var person=new CentralPerson(tenant.Id,"NATURAL","DOMESTIC","TR","12345678901","Fixture","Person",null,null,null,null);Guid unitId=Guid.Empty,relationId=Guid.Empty;
        await factory.WithDb(async db=>{
            var property=await db.Properties.SingleAsync(p=>p.Id==w.P);var unit=property.AddUnit("R1",0,80);unitId=unit.Id;
            db.AddRange(tenant,account,person,unit,new PersonTenantMembership(tenant.Id,account.Id),new PersonPropertyScope(tenant.Id,w.P));await db.SaveChangesAsync();
            var relation=new UnitResidency(tenant.Id,w.P,unit.Id,person.Id,DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).AddDays(-1));relation.MarkLegacy();relationId=relation.Id;
            db.AddRange(relation,new CentralPersonAccountLink(tenant.Id,account.Id,person.Id,DateTimeOffset.UtcNow.AddMinutes(-1)));await db.SaveChangesAsync();
        });
        using var child=factory.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"PersonRefactor:Enabled","true"},{"PersonRefactor:SessionContextsEnabled","true"}})));
        using var resident=TestToken.Client(child,w.Resident,"RESIDENT");var r=await resident.GetAsync($"/api/v2/me/units/{unitId}/common-areas/{w.A}/schedule?propertyId={w.P}&capacity=RESIDENT&relationId={relationId}&date={Day:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK,r.StatusCode);var body=await r.Content.ReadAsStringAsync();Assert.Contains("CLOSED",body);Assert.DoesNotContain(w.Plan.ToString(),body);Assert.DoesNotContain("Yönetim içi",body);Assert.DoesNotContain("Saha zemin",body);
    }
    [PostgresFact]public async Task BuildingRestrictedClosuresNeverBroadcastThroughPropertyWideChannels()
    {
        var w=await Seed(true);using var c=TestToken.Client(factory,w.User,"MANAGER");var closed=await Ok<CommonAreaClosureResponse>(await c.PostAsJsonAsync(Root(w)+"/closures",Request(w)));
        Assert.Equal(0,closed.NotificationCount);
        await factory.WithDb(async db=>{Assert.Equal(ContentAudience.MANAGEMENT,(await db.Announcements.SingleAsync(a=>a.Id==closed.AnnouncementId)).Audience);Assert.False(await db.PersonMessages.AnyAsync(m=>m.PropertyId==w.P));});
    }
    [PostgresFact]public async Task RolesForeignPlansValidationAndConcurrentBookingCannotBypassClosure()
    {
        var w=await Seed();var foreign=await Seed();using var c=TestToken.Client(factory,w.User,"MANAGER");var request=Request(w);
        foreach(var role in new[]{"ACCOUNTANT","RESIDENT","OWNER","TENANT","spoofed"}){using var user=TestToken.Client(factory,w.User,role);Assert.Equal(HttpStatusCode.Forbidden,(await user.PostAsJsonAsync(Root(w)+"/closures",request)).StatusCode);}
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync(Root(w)+"/closures",request with{MaintenancePlanId=foreign.Plan})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(w)+"/closures",request with{StartsAt="10:07"})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(w)+"/closures",request with{Notice=" "})).StatusCode);
        using var other=TestToken.Client(factory,w.User,"MANAGER");var race=await Task.WhenAll(c.PostAsJsonAsync(Root(w)+"/closures",request),other.PostAsJsonAsync(Root(w)+"/reservations",Booking()));
        Assert.Single(race,r=>r.IsSuccessStatusCode);Assert.Single(race,r=>r.StatusCode==HttpStatusCode.Conflict);
        await factory.WithDb(async db=>Assert.False(await db.Set<CommonAreaClosure>().AnyAsync(x=>x.PropertyId==w.P)&&await db.Set<CommonAreaReservation>().AnyAsync(x=>x.PropertyId==w.P)));
    }
}
