using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MobileParkingTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private readonly List<WebApplicationFactory<Program>> children=[];
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private HttpClient Client(string? user,string role,bool canonical=false,bool enabled=true)
    {
        var child=factory.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"PersonRefactor:Enabled",enabled?"true":"false"},{"PersonRefactor:SessionContextsEnabled",canonical?"true":"false"},{"PersonRefactor:MobileAuthorizationEnabled",enabled?"true":"false"}})));
        children.Add(child);var client=child.CreateClient();using var signed=TestToken.Client(factory,user,role);client.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return client;
    }
    private sealed record Fixture(Guid Property,Guid Unit,Guid Person,string User,string Manager);
    private async Task<Fixture> Seed()
    {
        var user=Guid.NewGuid().ToString();var manager=Guid.NewGuid().ToString();var p=new Property("Parking mobile","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,100);
        var person=new Person(p.Id,"Own","Person","+905551234567","fixture@example.test");person.SetProfile("NATURAL","DOMESTIC","TR","12345678901",null,null,null,user);
        await factory.WithDb(async db=>{db.AddRange(p,person,new PropertyMembership(p.Id,user),new PropertyMembership(p.Id,manager));await db.SaveChangesAsync();db.AddRange(new UnitOccupancy(p.Id,u.Id,person.Id,OccupancyType.TENANT,Today.AddDays(-1)),new ParkingPolicy(p.Id,2,2,1,24));await db.SaveChangesAsync();});return new(p.Id,u.Id,person.Id,user,manager);
    }
    private static string Url(Fixture f,string role="TENANT",string suffix="",Guid? relation=null,int page=1)=>$"/api/v1/mobile/parking{suffix}?propertyId={f.Property}&role={role}"+(role is "TENANT" or "RESIDENT"?$"&unitId={f.Unit}":"")+(relation is null?"":$"&contextKind=RESIDENT&contextId={relation}")+$"&page={page}";
    private static ResidentVehicleRequest Vehicle(Fixture f,Guid? id=null)=>new(id??Guid.NewGuid(),f.Property,f.Unit,"34MOB123","CAR","Test",null,null);
    private static async Task<T> Ok<T>(HttpResponseMessage r){Assert.Equal(HttpStatusCode.OK,r.StatusCode);return(await r.Content.ReadFromJsonAsync<T>())!;}
    [PostgresFact]
    public async Task ResidentSnapshotRegistrationReplayAndRevocationAreScoped()
    {
        var f=await Seed();using var c=Client(f.User,"RESIDENT");var request=Vehicle(f);
        var first=await Ok<ResidentVehicleResponse>(await c.PostAsJsonAsync(Url(f,suffix:"/vehicles"),request));Assert.Equal("PENDING",first.Status);
        Assert.Equal(first.Id,(await Ok<ResidentVehicleResponse>(await c.PostAsJsonAsync(Url(f,suffix:"/vehicles"),request))).Id);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Url(f,suffix:"/vehicles"),request with {Brand="Different"})).StatusCode);
        var response=await c.GetAsync(Url(f));var s=await Ok<MobileParkingSnapshot>(response);Assert.Equal($"{f.Property}:{f.Unit}:TENANT",s.ContextKey);Assert.Single(s.Vehicles);Assert.Empty(s.Guests);Assert.Empty(s.Units);Assert.Empty(s.Spaces);
        Assert.DoesNotContain("Own Person",await response.Content.ReadAsStringAsync());
        var other=await Seed();Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Url(other))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Url(f,suffix:"/vehicles"),request with {UnitId=other.Unit})).StatusCode);
        await factory.WithDb(async db=>{(await db.UnitOccupancies.SingleAsync(o=>o.PersonId==f.Person)).End(Today.AddDays(-1));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Url(f))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Url(f,suffix:"/vehicles"),request)).StatusCode);
    }
    [PostgresFact]
    public async Task ManagerGuestOperationsReuseConflictsAndDoNotExposeNotes()
    {
        var f=await Seed();using var c=Client(f.Manager,"MANAGER");var facility=new ParkingFacility(f.Property,null,"Garage");var space=new ParkingSpace(f.Property,facility.Id,null,"V1",ParkingSpaceType.VISITOR);
        await factory.WithDb(async db=>{db.AddRange(facility,space);await db.SaveChangesAsync();});
        var start=DateTimeOffset.UtcNow.AddMinutes(-1);var r=new GuestVehicleRequest(Guid.NewGuid(),f.Unit,"34GUEST6","Private name",start,start.AddHours(1),space.Id,"Private note");
        var g=await Ok<GuestVehicleResponse>(await c.PostAsJsonAsync(Url(f,"MANAGER","/guests"),r));
        Assert.Equal(g.Id,(await Ok<GuestVehicleResponse>(await c.PostAsJsonAsync(Url(f,"MANAGER","/guests"),r))).Id);
        var s=await Ok<MobileParkingSnapshot>(await c.GetAsync(Url(f,"MANAGER")));Assert.Null(Assert.Single(s.Guests).GuestName);Assert.Null(s.Guests[0].Note);Assert.Single(s.Spaces);Assert.Single(s.Units);Assert.Empty(s.Vehicles);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Url(f,"MANAGER","/guests"),r with {RequestId=Guid.NewGuid(),Plate="34OTHER6"})).StatusCode);
        var arrived=await Ok<GuestVehicleResponse>(await c.PostAsJsonAsync(Url(f,"MANAGER",$"/guests/{g.Id}/arrive"),new ParkingVersionRequest(g.Version)));
        Assert.Equal("ARRIVED",arrived.Status);
        Assert.Equal("DEPARTED",(await Ok<GuestVehicleResponse>(await c.PostAsJsonAsync(Url(f,"MANAGER",$"/guests/{g.Id}/depart"),new ParkingVersionRequest(arrived.Version)))).Status);
        using var resident=Client(f.User,"RESIDENT");Assert.Equal(HttpStatusCode.Forbidden,(await resident.PostAsJsonAsync(Url(f,suffix:"/guests"),r)).StatusCode);
        using var accountant=Client(f.Manager,"ACCOUNTANT");Assert.Equal(HttpStatusCode.Forbidden,(await accountant.GetAsync(Url(f,"ACCOUNTANT"))).StatusCode);
        using var anon=Client(null,"MANAGER");Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Url(f,"MANAGER"))).StatusCode);
    }
    [PostgresFact]
    public async Task PaginationHasExactTotalsAndCannotSupplyCanonicalIdentityInLegacyMode()
    {
        var f=await Seed();using var c=Client(f.Manager,"MANAGER");await factory.WithDb(async db=>{var p=await db.Properties.SingleAsync(p=>p.Id==f.Property);for(var i=2;i<=23;i++)p.AddUnit(i.ToString(),0,100);await db.SaveChangesAsync();});
        var first=await Ok<MobileParkingSnapshot>(await c.GetAsync(Url(f,"MANAGER")));var second=await Ok<MobileParkingSnapshot>(await c.GetAsync(Url(f,"MANAGER",page:2)));Assert.Equal(23,first.UnitTotal);Assert.Equal(20,first.Units.Count);Assert.Equal(3,second.Units.Count);Assert.Empty(first.Units.Select(x=>x.Id).Intersect(second.Units.Select(x=>x.Id)));
        using var own=Client(f.User,"RESIDENT");Assert.Equal(HttpStatusCode.Forbidden,(await own.GetAsync(Url(f,relation:Guid.NewGuid()))).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Url(f,"MANAGER",page:0))).StatusCode);
    }
    [PostgresFact]
    public async Task CanonicalContextNeedsCurrentAliasAndRejectsRevokeWrongContextAndDisabledFeature()
    {
        var f=await Seed();var tenant=new PersonTenant("Parking canonical");var account=new PersonUserAccount("http://localhost:8080/realms/buildingflow",f.User);var central=new CentralPerson(tenant.Id,"NATURAL","DOMESTIC","TR","12345678901","Own","Person",null,null,null,null);
        var relation=TestRelations.Effective(new UnitResidency(tenant.Id,f.Property,f.Unit,central.Id,Today.AddDays(-1)));
        await factory.WithDb(async db=>{db.AddRange(tenant,account,central,new PersonTenantMembership(tenant.Id,account.Id),new CentralPersonAccountLink(tenant.Id,account.Id,central.Id,DateTimeOffset.UtcNow.AddDays(-1)),new PersonPropertyScope(tenant.Id,f.Property),relation);await db.SaveChangesAsync();});
        using var c=Client(f.User,"RESIDENT",canonical:true);var url=Url(f,"RESIDENT",relation:relation.Id);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(url)).StatusCode);
        await factory.WithDb(async db=>{var legacy=await db.People.SingleAsync(p=>p.Id==f.Person);db.Add(new LegacyPersonMap(tenant.Id,legacy,central.Id,"{}"));await db.SaveChangesAsync();});
        var s=await Ok<MobileParkingSnapshot>(await c.GetAsync(url));Assert.Equal($"{tenant.Id}:RESIDENT:{relation.Id}",s.ContextKey);
        var request=Vehicle(f);await Ok<ResidentVehicleResponse>(await c.PostAsJsonAsync(Url(f,"RESIDENT","/vehicles",relation.Id),request));
        await factory.WithDb(async db=>{var person=await db.People.SingleAsync(p=>p.Id==f.Person);db.Entry(person).Property(p=>p.IsEnabled).IsModified=true;await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(url)).StatusCode);
        await factory.WithDb(async db=>{var person=await db.People.SingleAsync(p=>p.Id==f.Person);var map=await db.Set<LegacyPersonMap>().SingleAsync(m=>m.LegacyPersonId==f.Person);db.Entry(map).Property(m=>m.SourceVersion).CurrentValue=person.Version;await db.SaveChangesAsync();});
        await Ok<MobileParkingSnapshot>(await c.GetAsync(url));
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Url(f,"RESIDENT",relation:Guid.NewGuid()))).StatusCode);
        using var disabled=Client(f.User,"RESIDENT",canonical:true,enabled:false);Assert.Equal(HttpStatusCode.Forbidden,(await disabled.GetAsync(url)).StatusCode);
        await factory.WithDb(async db=>{(await db.Set<UnitResidency>().SingleAsync(r=>r.Id==relation.Id)).End(Today.AddDays(-1));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(url)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Url(f,"RESIDENT","/vehicles",relation.Id),request)).StatusCode);
    }
    public void Dispose(){foreach(var c in children)c.Dispose();}
}
