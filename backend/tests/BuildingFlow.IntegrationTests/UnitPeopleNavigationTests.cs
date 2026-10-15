using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class UnitPeopleNavigationTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Root(Guid id) => $"/api/v1/properties/{id}";
    private async Task<(string Actor, Property Property, Unit First, Unit Second)> Seed(bool apartment=true)
    {
        var actor=Guid.NewGuid().ToString();
        var property=new Property("Navigation fixture", "City", "District", "Address", apartment?PropertyType.APARTMENT:PropertyType.SITE);
        var first=apartment?property.AddUnit("1",0,100):property.AddBuilding("A").AddUnit("1",0,100);
        var second=apartment?property.AddUnit("10",1,100):property.AddBuilding("B").AddUnit("1",1,100);
        await factory.WithDb(async db=>{db.Add(property);db.Add(new PropertyMembership(property.Id,actor));await db.SaveChangesAsync();});
        return (actor,property,first,second);
    }
    [PostgresTheory][InlineData(true)][InlineData(false)]
    public async Task CurrentPartiesIncludeInclusiveSharedOwnersAndTenantsButExcludePastFutureAndResidents(bool apartment)
    {
        var x=await Seed(apartment);var owner=new Person(x.Property.Id,"Current","Owner");var shared=new Person(x.Property.Id,"Shared","Owner");var tenant=new Person(x.Property.Id,"Current","Tenant");
        await factory.WithDb(async db=>{
            var past=new Person(x.Property.Id,"Past","Tenant");var future=new Person(x.Property.Id,"Future","Tenant");var resident=new Person(x.Property.Id,"Current","Resident");
            db.AddRange(owner,shared,tenant,past,future,resident);
            db.AddRange(new UnitOccupancy(x.Property.Id,x.First.Id,owner.Id,OccupancyType.OWNER,Today.AddDays(-10),Today),
                new UnitOccupancy(x.Property.Id,x.First.Id,shared.Id,OccupancyType.OWNER,Today),
                new UnitOccupancy(x.Property.Id,x.First.Id,tenant.Id,OccupancyType.TENANT,Today),
                new UnitOccupancy(x.Property.Id,x.First.Id,past.Id,OccupancyType.TENANT,Today.AddDays(-10),Today.AddDays(-1)),
                new UnitOccupancy(x.Property.Id,x.First.Id,future.Id,OccupancyType.TENANT,Today.AddDays(1)),
                new UnitOccupancy(x.Property.Id,x.First.Id,resident.Id,OccupancyType.RESIDENT,Today));await db.SaveChangesAsync();
        });
        using var client=TestToken.Client(factory,x.Actor,"MANAGER");
        var response=await client.GetAsync(Root(x.Property.Id)+$"/unit-parties?unitIds={x.First.Id}&unitIds={x.Second.Id}");Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var result=(await response.Content.ReadFromJsonAsync<UnitCurrentPartiesResponse>())!;Assert.Equal(Today,result.AsOfDate);Assert.Equal(x.Property.Id,result.PropertyId);
        var first=result.Items[0];Assert.Equal(x.First.Id,first.UnitId);Assert.Equal(x.First.BuildingId,first.BuildingId);
        Assert.Equal(new[]{owner.Id,shared.Id}.Order(),first.Owners.Select(p=>p.PersonId).Order());Assert.Equal(tenant.Id,Assert.Single(first.Tenants).PersonId);
        Assert.Empty(result.Items[1].Owners);Assert.Empty(result.Items[1].Tenants);
        var json=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("email",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("phone",json,StringComparison.OrdinalIgnoreCase);
    }
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task OnlyManagementRolesReadBatchedParties(string role)
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,role);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(Root(x.Property.Id)+$"/unit-parties?unitIds={x.First.Id}")).StatusCode);
    }
    [PostgresFact]
    public async Task BatchValidationAndPropertyMembershipFailClosed()
    {
        var x=await Seed();var other=await Seed();using var client=TestToken.Client(factory,x.Actor,"MANAGER");var root=Root(x.Property.Id)+"/unit-parties";
        foreach(var query in new[]{"", "?unitIds=bad", "?unitIds="+Guid.Empty,$"?unitIds={x.First.Id}&unitIds={x.First.Id}","?"+string.Join("&",Enumerable.Range(0,101).Select(_=>"unitIds="+Guid.NewGuid()))})Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(root+query)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(root+$"?unitIds={x.First.Id}&unitIds={other.First.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(Root(other.Property.Id)+$"/unit-parties?unitIds={other.First.Id}")).StatusCode);
        await factory.WithDb(async db=>{db.Remove(await db.PropertyMemberships.SingleAsync(p=>p.PropertyId==x.Property.Id&&p.UserId==x.Actor));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(root+$"?unitIds={x.First.Id}")).StatusCode);
    }
    [PostgresTheory][InlineData("person")][InlineData("occupancy")][InlineData("unit")][InlineData("property")][InlineData("building")]
    public async Task SoftDeletedParentsAndPersonalRecordsCannotLeakCurrentParties(string target)
    {
        var x=await Seed(false);var person=new Person(x.Property.Id,"Hidden","Person");var link=new UnitOccupancy(x.Property.Id,x.First.Id,person.Id,OccupancyType.OWNER,Today);
        await factory.WithDb(async db=>{db.AddRange(person,link);await db.SaveChangesAsync();});
        using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var uri=Root(x.Property.Id)+$"/unit-parties?unitIds={x.First.Id}";
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(uri)).StatusCode);
        await factory.WithDb(async db=>{switch(target){case "person":db.Remove(await db.People.SingleAsync(p=>p.Id==person.Id));break;case "occupancy":db.Remove(await db.UnitOccupancies.SingleAsync(p=>p.Id==link.Id));break;case "unit":db.Remove(await db.Units.SingleAsync(p=>p.Id==x.First.Id));break;case "property":db.Remove(await db.Properties.SingleAsync(p=>p.Id==x.Property.Id));break;case "building":db.Remove(await db.Buildings.SingleAsync(p=>p.Id==x.First.BuildingId));break;}await db.SaveChangesAsync();});
        var response=await client.GetAsync(uri);if(target is "person" or "occupancy"){Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Empty(Assert.Single((await response.Content.ReadFromJsonAsync<UnitCurrentPartiesResponse>())!.Items).Owners);}else Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
    }
    [PostgresTheory][InlineData(true)][InlineData(false)]
    public async Task UnitIdFilterIsExactAndRetainsBuildingPropertyScope(bool apartment)
    {
        var x=await Seed(apartment);var other=await Seed(apartment);using var client=TestToken.Client(factory,x.Actor,"MANAGER");
        var uri=Root(x.Property.Id)+(apartment?"":$"/buildings/{x.First.BuildingId}")+"/units";
        var filtered=(await client.GetFromJsonAsync<PagedRows<UnitResponse>>(uri+$"?unitId={x.First.Id}"))!;Assert.Equal(1,filtered.TotalCount);Assert.Equal(x.First.Id,Assert.Single(filtered.Items).Id);
        Assert.Equal(0,(await client.GetFromJsonAsync<PagedRows<UnitResponse>>(uri+$"?unitId={other.First.Id}"))!.TotalCount);
        if(!apartment)Assert.Equal(0,(await client.GetFromJsonAsync<PagedRows<UnitResponse>>(uri+$"?unitId={x.Second.Id}"))!.TotalCount);
        foreach(var value in new[]{Guid.Empty.ToString(),"bad"})Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(uri+"?unitId="+value)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(Root(other.Property.Id)+(apartment?"":$"/buildings/{other.First.BuildingId}")+"/units?unitId="+other.First.Id)).StatusCode);
        using var anonymous=TestToken.Client(factory,null,"");Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(uri+$"?unitId={x.First.Id}")).StatusCode);
    }
}
