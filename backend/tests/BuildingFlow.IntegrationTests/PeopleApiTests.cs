using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class PeopleApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    [PostgresFact] public async Task HistoricalProjectionAndUnitChoicesAreExecutableRelationalQueries()
    {
        var x=await Seed();
        await factory.WithDb(async db=>{
            var person=new BuildingFlow.Domain.Person(x.Property.Id,"Ada","Kaya");db.Add(person);
            var link=new UnitOccupancy(x.Property.Id,x.Unit.Id,person.Id,OccupancyType.OWNER,Today);db.Add(link);await db.SaveChangesAsync();
            var store=new PeopleStore(db,TimeProvider.System);
            Assert.NotNull(await store.GetOccupancyAsync(x.Property.Id,link.Id,new(x.Actor,false),default));
            Assert.NotNull(await store.UnitChoicesAsync(x.Property.Id,new(),new(x.Actor,false),default));
        });
    }
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Root(Guid property) => $"/api/v1/properties/{property}";
    private async Task<(string Actor, Property Property, Unit Unit)> Seed(bool apartment = true)
    {
        var actor = Guid.NewGuid().ToString(); var property = new Property("People " + actor, "City", "District", "Address", apartment ? PropertyType.APARTMENT : PropertyType.SITE);
        var unit = apartment ? property.AddUnit("1", 0, 100) : property.AddBuilding("A").AddUnit("1", 0, 100);
        await factory.WithDb(async db => { db.Add(property); db.Add(new PropertyMembership(property.Id, actor)); await db.SaveChangesAsync(); });
        return (actor, property, unit);
    }
    private static async Task<PersonResponse> Person(HttpClient client, Guid property, string first = "Ada")
    {
        var response = await client.PostAsJsonAsync(Root(property) + "/people", new CreatePersonRequest(first, "Kaya", "+90 555 123", "ada@example.test"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); return (await response.Content.ReadFromJsonAsync<PersonResponse>())!;
    }
    private static CreateOccupancyRequest Link(PersonResponse person, Unit unit, string role = "OWNER", bool primary = false, DateOnly? end = null) => new(person.Id, unit.Id, role, Today.AddDays(-10), end, primary);
    private static UpdatePersonRequest Edit(PersonResponse person, bool enabled = true) => new("Edited", person.LastName, person.PhoneNumber, person.Email, enabled, person.Version);

    [PostgresFact] public async Task AnonymousIsRejectedBeforePersonalDataAccess()
    {
        using var client = TestToken.Client(factory, null, ""); var root = Root(Guid.NewGuid());
        foreach (var path in new[] { "/people", "/people/" + Guid.NewGuid(), "/occupancies", "/occupancies/" + Guid.NewGuid(), "/unit-choices", "/unit-parties?unitIds=" + Guid.NewGuid() }) Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(root + path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(root + "/people", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(root + "/occupancies", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync(root + "/occupancies/" + Guid.NewGuid() + "/end", new { })).StatusCode);
    }
    [PostgresTheory] [InlineData("RESIDENT")] [InlineData("ACCOUNTANT")] [InlineData("")] [InlineData("spoofed")] [InlineData("client-role")]
    public async Task ReadOnlyAndForgedRolesCannotReadOrWritePersonalData(string role)
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, role); var root = Root(x.Property.Id);
        foreach (var path in new[] { "/people", "/occupancies", "/unit-choices", "/unit-parties?unitIds=" + Guid.NewGuid() }) Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(root + path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(root + "/people", new CreatePersonRequest("A", "B"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(root + "/occupancies", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync(root + "/people/" + Guid.NewGuid() + "?version=1")).StatusCode);
    }
    [PostgresTheory] [InlineData("SUPER_ADMIN")] [InlineData("SITE_ADMIN")] [InlineData("MANAGER")]
    public async Task ManagersCanCrudPeopleWithTrustedAuditAndVersion(string role)
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, role); var root = Root(x.Property.Id);
        var response = await client.PostAsJsonAsync(root + "/people", new { firstName = " Ada ", lastName = " Kaya ", createdBy = "forged", propertyId = Guid.NewGuid(), isDeleted = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); var person = (await response.Content.ReadFromJsonAsync<PersonResponse>())!;
        Assert.Equal("Ada", person.FirstName); Assert.Equal(x.Property.Id, person.PropertyId); Assert.Equal(1, person.Version);
        Assert.Equal(root + "/people/" + person.Id, response.Headers.Location!.ToString());
        var edit = await client.PutAsJsonAsync(response.Headers.Location, Edit(person)); Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var updated = (await edit.Content.ReadFromJsonAsync<PersonResponse>())!; Assert.Equal(2, updated.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(response.Headers.Location, Edit(person))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(response.Headers.Location + "?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(response.Headers.Location + "?version=" + updated.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(response.Headers.Location)).StatusCode);
        await factory.WithDb(async db => { var saved = await db.People.IgnoreQueryFilters().SingleAsync(value => value.Id == person.Id); Assert.True(saved.IsDeleted); Assert.Equal(x.Actor, saved.CreatedBy); Assert.Equal(x.Actor, saved.ModifiedBy); Assert.Equal(person.CreatedAt, saved.CreatedAt); });
    }
    [PostgresFact] public async Task MembershipScopeAndWrongForeignIdsAreRejected()
    {
        var x = await Seed(); var other = await Seed(); using var own = TestToken.Client(factory, x.Actor, "MANAGER"); using var global = TestToken.Client(factory, x.Actor, "SUPER_ADMIN");
        var p = await Person(own, x.Property.Id); var foreign = await Person(global, other.Property.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await own.GetAsync(Root(other.Property.Id) + "/people")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await own.GetAsync(Root(other.Property.Id) + "/occupancies")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await own.GetAsync(Root(x.Property.Id) + "/people/" + foreign.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await own.PostAsJsonAsync(Root(x.Property.Id) + "/occupancies", Link(foreign, x.Unit))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await own.PostAsJsonAsync(Root(x.Property.Id) + "/occupancies", Link(p, other.Unit))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await own.GetAsync(Root(x.Property.Id) + "/occupancies?personId=" + foreign.Id)).StatusCode);
        var link = await own.PostAsJsonAsync(Root(x.Property.Id) + "/occupancies", Link(p, x.Unit)); Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        // Historical joins must not disable the membership soft-delete filter.
        await factory.WithDb(async db => { db.Remove(await db.PropertyMemberships.SingleAsync(value => value.PropertyId == x.Property.Id && value.UserId == x.Actor)); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await own.GetAsync(Root(x.Property.Id) + "/occupancies")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await own.GetAsync(link.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await own.GetAsync(Root(x.Property.Id) + "/people")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await global.GetAsync(link.Headers.Location)).StatusCode);
        await factory.WithDb(async db => { db.Remove(await db.Properties.SingleAsync(value => value.Id == x.Property.Id)); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await global.GetAsync(link.Headers.Location)).StatusCode);
    }
    [PostgresTheory] [InlineData(true)] [InlineData(false)]
    public async Task OccupancySupportsStandaloneAndBuildingUnitsAndPreservesHistory(bool apartment)
    {
        var x = await Seed(apartment); using var client = TestToken.Client(factory, x.Actor, "MANAGER"); var root = Root(x.Property.Id); var p = await Person(client, x.Property.Id);
        var response = await client.PostAsJsonAsync(root + "/occupancies", Link(p, x.Unit, "TENANT", true)); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = (await response.Content.ReadFromJsonAsync<OccupancyResponse>())!; Assert.Equal("ACTIVE", link.Status); Assert.Equal(x.Unit.BuildingId, link.BuildingId);
        Assert.Equal(1, (await client.GetFromJsonAsync<PersonResponse>(root + "/people/" + p.Id))!.ActiveOccupancyCount);
        var ended = await client.PutAsJsonAsync(response.Headers.Location + "/end", new EndOccupancyRequest(Today.AddDays(-1), link.Version)); Assert.Equal(HttpStatusCode.OK, ended.StatusCode);
        var history = (await ended.Content.ReadFromJsonAsync<OccupancyResponse>())!; Assert.Equal("ENDED", history.Status); Assert.Equal(link.StartDate, history.StartDate); Assert.Equal(2, history.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(response.Headers.Location + "/end", new EndOccupancyRequest(Today, link.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(response.Headers.Location + "/end", new EndOccupancyRequest(Today, history.Version))).StatusCode);
        p = (await client.GetFromJsonAsync<PersonResponse>(root + "/people/" + p.Id))!;
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(root + "/people/" + p.Id + "?version=" + p.Version)).StatusCode);
        var historyRows = (await client.GetFromJsonAsync<PagedRows<OccupancyResponse>>(root + "/occupancies?personId=" + p.Id))!;
        Assert.Equal("Ada Kaya", Assert.Single(historyRows.Items).PersonName);
    }
    [PostgresFact] public async Task InclusiveOverlapPrimaryContactAndSharedOwnershipRules()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var root = Root(x.Property.Id); var p = await Person(client, x.Property.Id); var p2 = await Person(client, x.Property.Id, "Deniz");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(root + "/occupancies", Link(p, x.Unit, "OWNER", true, Today.AddDays(-1)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root + "/occupancies", Link(p, x.Unit))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(root + "/occupancies", Link(p2, x.Unit))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(root + "/occupancies", Link(p, x.Unit, "RESIDENT"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root + "/occupancies", Link(p2, x.Unit, "TENANT", true))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root + "/occupancies", new CreateOccupancyRequest(p.Id, x.Unit.Id, "OWNER", Today.AddDays(-1)))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(root + "/occupancies", new CreateOccupancyRequest(p.Id, x.Unit.Id, "OWNER", Today, null, true))).StatusCode);
    }
    [PostgresFact] public async Task ActiveOrScheduledRelationsBlockPersonAndUnitDeletionOrDisable()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var root = Root(x.Property.Id); var p = await Person(client, x.Property.Id);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(root + "/occupancies", new CreateOccupancyRequest(p.Id, x.Unit.Id, "RESIDENT", Today.AddDays(20)))).StatusCode);
        p = (await client.GetFromJsonAsync<PersonResponse>(root + "/people/" + p.Id))!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(root + "/people/" + p.Id + "?version=" + p.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(root + "/people/" + p.Id, Edit(p, false))).StatusCode);
        var unit = (await client.GetFromJsonAsync<UnitResponse>(root + "/units/" + x.Unit.Id))!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(root + "/units/" + unit.Id + "?version=" + unit.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(root + "/units/" + unit.Id, new UpdateUnitRequest(unit.Number, unit.Floor, unit.GrossSquareMeters, false, unit.Version))).StatusCode);
    }
    [PostgresFact] public async Task ConcurrentBoundedRelationsDoNotCommitOverlaps()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var p = await Person(client, x.Property.Id); var root = Root(x.Property.Id);
        var request = Link(p, x.Unit, end: Today.AddDays(10));
        var results = await Task.WhenAll(client.PostAsJsonAsync(root + "/occupancies", request), client.PostAsJsonAsync(root + "/occupancies", request));
        Assert.Single(results, value => value.StatusCode == HttpStatusCode.Created); Assert.Single(results, value => value.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, (await client.GetFromJsonAsync<PagedRows<OccupancyResponse>>(root + "/occupancies"))!.TotalCount);
    }
    [PostgresFact] public async Task ValidationPaginationLiteralSearchAndParentStatus()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var root = Root(x.Property.Id);
        foreach (var request in new[] { new CreatePersonRequest("", "A"), new CreatePersonRequest("A", "", Email: "bad"), new CreatePersonRequest(new string('a',101),"B"), new CreatePersonRequest("A","B",new string('x',31)), new CreatePersonRequest("A","B",Email:"Name <a@example.test>") }) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(root + "/people", request)).StatusCode);
        var p = await Person(client, x.Property.Id, "Ada_%"); await Person(client, x.Property.Id, "Zeynep");
        Assert.Equal(1, (await client.GetFromJsonAsync<PagedRows<PersonResponse>>(root + "/people?search=" + Uri.EscapeDataString("_%")))!.TotalCount);
        var page = (await client.GetFromJsonAsync<PagedRows<PersonResponse>>(root + "/people?pageSize=1"))!; Assert.Equal(2, page.TotalCount); Assert.Single(page.Items);
        foreach (var query in new[] { "page=0", "pageSize=101", "sortBy=email", "sortDirection=no", "search=" + new string('a',201) }) Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root + "/people?" + query)).StatusCode);
        foreach (var request in new[] { new CreateOccupancyRequest(Guid.Empty,x.Unit.Id,"OWNER",Today),new CreateOccupancyRequest(p.Id,x.Unit.Id,"invalid",Today),new CreateOccupancyRequest(p.Id,x.Unit.Id,"OWNER",null),new CreateOccupancyRequest(p.Id,x.Unit.Id,"OWNER",Today,Today.AddDays(-1)) }) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(root + "/occupancies",request)).StatusCode);
        var choices = (await client.GetFromJsonAsync<PagedRows<UnitChoice>>(root + "/unit-choices"))!; Assert.Equal(x.Unit.Id, Assert.Single(choices.Items).Id);
        p = (await client.GetFromJsonAsync<PersonResponse>(root + "/people/" + p.Id))!;
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(root + "/people/" + p.Id, Edit(p,false))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root + "/occupancies",Link(p,x.Unit))).StatusCode);
        await factory.WithDb(async db => { (await db.Properties.SingleAsync(value => value.Id==x.Property.Id)).SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(root+"/people",new CreatePersonRequest("A","B"))).StatusCode);
    }
    [PostgresFact] public async Task DatabaseRejectsCrossPropertyForeignKeysAndInvalidDates()
    {
        var x = await Seed(); var other = await Seed(); using var client = TestToken.Client(factory,x.Actor,"SUPER_ADMIN"); var p=await Person(client,x.Property.Id);
        await factory.WithDb(async db => {
            db.Add(new UnitOccupancy(x.Property.Id,other.Unit.Id,p.Id,OccupancyType.OWNER,Today));
            await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
        });
        var response=await client.PostAsJsonAsync(Root(x.Property.Id)+"/occupancies",Link(p,x.Unit)); var link=(await response.Content.ReadFromJsonAsync<OccupancyResponse>())!;
        await factory.WithDb(async db => { await Assert.ThrowsAsync<Npgsql.PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_occupancies SET end_date={Today.AddDays(-20)} WHERE id={link.Id}")); });
    }
}
