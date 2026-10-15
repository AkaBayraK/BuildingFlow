using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MobileSessionTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    [PostgresFact]
    public async Task AnonymousRejectedAndForeignAccountHasNoContexts()
    {
        using var anonymous = TestToken.Client(factory, null, "");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/mobile/session")).StatusCode);
        using var other = TestToken.Client(factory, Guid.NewGuid().ToString(), "RESIDENT");
        var value = await other.GetFromJsonAsync<MobileSession>("/api/v1/mobile/session");
        Assert.Empty(value!.Contexts);
    }
    [PostgresTheory]
    [InlineData("MANAGER",true)][InlineData("SITE_ADMIN",true)][InlineData("ACCOUNTANT",true)]
    [InlineData("RESIDENT",false)][InlineData("spoofed",false)][InlineData("client-role",false)]
    public async Task SignedManagementRolesRequireActiveMembership(string role, bool allowed)
    {
        var actor=Guid.NewGuid().ToString();var property=new Property("Mobile fixture","City","District","Address");
        var member=new PropertyMembership(property.Id,actor);
        await factory.WithDb(async db=>{db.Add(property);db.Add(member);await db.SaveChangesAsync();});
        using var client=TestToken.Client(factory,actor,role);
        var value=await client.GetFromJsonAsync<MobileSession>("/api/v1/mobile/session");
        Assert.Equal(allowed,value!.Contexts.Any(c=>c.PropertyId==property.Id));
        await factory.WithDb(async db=>{var m=await db.PropertyMemberships.FindAsync(member.Id);m!.SetEnabled(false);await db.SaveChangesAsync();});
        value=await client.GetFromJsonAsync<MobileSession>("/api/v1/mobile/session");
        Assert.DoesNotContain(value!.Contexts,c=>c.PropertyId==property.Id);
    }
    [PostgresFact]
    public async Task OccupancyRolesAreAccountScopedAndEndedRelationshipsDisappear()
    {
        var actor=Guid.NewGuid().ToString();var property=new Property("Mobile occupant","City","District","Address");
        var building=property.AddBuilding("A");var unit=building.AddUnit("1",1,90m);
        var person=new Person(property.Id,"Fixture","User","5550000000","fixture@example.test");
        person.SetProfile("NATURAL","FOREIGN","US","TEST1234",null,null,null,actor);
        var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
        var owner=new UnitOccupancy(property.Id,unit.Id,person.Id,OccupancyType.OWNER,today.AddDays(-10));
        var tenant=new UnitOccupancy(property.Id,unit.Id,person.Id,OccupancyType.TENANT,today.AddDays(-10),today.AddDays(-1));
        await factory.WithDb(async db=>{db.Add(property);db.Add(person);db.Add(owner);db.Add(tenant);db.Add(new PropertyMembership(property.Id,actor));await db.SaveChangesAsync();});
        using var client=TestToken.Client(factory,actor,"RESIDENT");
        var value=await client.GetFromJsonAsync<MobileSession>("/api/v1/mobile/session");
        var c=Assert.Single(value!.Contexts);Assert.Equal("OWNER",c.Role);Assert.Equal(unit.Id,c.UnitId);
        using var foreign=TestToken.Client(factory,Guid.NewGuid().ToString(),"RESIDENT");
        Assert.Empty((await foreign.GetFromJsonAsync<MobileSession>("/api/v1/mobile/session"))!.Contexts);
        await factory.WithDb(async db=>{var u=await db.Units.FindAsync(unit.Id);u!.SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Empty((await client.GetFromJsonAsync<MobileSession>("/api/v1/mobile/session"))!.Contexts);
    }
}
