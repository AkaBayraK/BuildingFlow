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
public sealed class CommonAreaContextTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>, IDisposable
{
    private readonly List<WebApplicationFactory<Program>> children = [];
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private HttpClient Client(string user, string role, bool flags = true)
    {
        var child = factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> {
            ["PersonRefactor:Enabled"] = flags ? "true" : "false", ["PersonRefactor:SessionContextsEnabled"] = flags ? "true" : "false"
        }))); children.Add(child); return TestToken.Client(child, user, role);
    }
    private sealed record World(Guid Tenant, Guid Property, Guid Unit, Guid OtherUnit, Guid Relation, Guid Person, string User, Guid PublicArea, Guid BlockArea, Guid ForeignBlockArea, Guid HiddenArea);
    private async Task<World> Seed()
    {
        var t = new PersonTenant("Common fixture"); var p = new Property("Site", "City", "District", "Address", PropertyType.SITE);
        var b = p.AddBuilding("A"); var other = p.AddBuilding("B"); var unit = b.AddUnit("1", 0, 80); var otherUnit = other.AddUnit("1", 0, 80);
        var user = Guid.NewGuid().ToString(); var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user);
        var person = new CentralPerson(t.Id, "NATURAL", "DOMESTIC", "TR", "12345678901", "Fixture", "Person", null, null, null, null);
        var relation = new UnitResidency(t.Id, p.Id, unit.Id, person.Id, Today.AddDays(-10)); relation.MarkLegacy();
        CommonArea Area(string name, Guid? building, CommonAreaVisibility visibility) => new(p.Id, Guid.NewGuid(), new string('A', 64), name, CommonAreaType.POOL, building, visibility, CommonAreaUsage.RESERVATION, 20, null, null, null, null, 0);
        var pub = Area("Property", other.Id, CommonAreaVisibility.PROPERTY); var own = Area("A", b.Id, CommonAreaVisibility.BUILDING);
        var hidden = Area("Hidden", b.Id, CommonAreaVisibility.BUILDING); hidden.SetEnabled(false); var foreign = Area("B", other.Id, CommonAreaVisibility.BUILDING);
        await factory.WithDb(async db => {
            db.AddRange(t, p, b, other, unit, otherUnit, account, person, new PersonTenantMembership(t.Id, account.Id), new PersonPropertyScope(t.Id, p.Id), new PropertyMembership(p.Id, user));
            await db.SaveChangesAsync(); db.AddRange(new CentralPersonAccountLink(t.Id, account.Id, person.Id, DateTimeOffset.UtcNow.AddMinutes(-1)), relation, pub, own, hidden, foreign); await db.SaveChangesAsync();
        }); return new(t.Id, p.Id, unit.Id, otherUnit.Id, relation.Id, person.Id, user, pub.Id, own.Id, foreign.Id, hidden.Id);
    }
    private static string Url(World w, Guid? area = null) => $"/api/v2/me/units/{w.Unit}/common-areas{(area.HasValue ? "/" + area : "")}?propertyId={w.Property}&capacity=RESIDENT&relationId={w.Relation}";
    [PostgresFact] public async Task UnitContextRestrictsBlocksAndDoesNotGrantManagementFromRealmRole()
    {
        var w = await Seed(); using var c = Client(w.User, "RESIDENT");
        var result = await c.GetAsync(Url(w)); Assert.Equal(HttpStatusCode.OK, result.StatusCode); Assert.Contains("no-store", result.Headers.CacheControl!.ToString());
        var rows = (await result.Content.ReadFromJsonAsync<PagedRows<CommonAreaResponse>>())!;
        Assert.Equal(2, rows.TotalCount); Assert.Contains(rows.Items, a => a.Id == w.PublicArea); Assert.Contains(rows.Items, a => a.Id == w.BlockArea);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(Url(w, w.BlockArea))).StatusCode);
        foreach (var id in new[] { w.ForeignBlockArea, w.HiddenArea, Guid.NewGuid() }) Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Url(w, id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Url(w with { Unit = w.OtherUnit }))).StatusCode);
        var foreign = await Seed(); Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Url(foreign))).StatusCode);
        using var forgedManager = Client(w.User, "MANAGER");
        Assert.Equal(HttpStatusCode.NotFound, (await forgedManager.GetAsync($"/api/v1/properties/{w.Property}/common-areas")).StatusCode); // Membership alone lacks active management assignment.
        using var disabled = Client(w.User, "RESIDENT", false); Assert.Equal(HttpStatusCode.NotFound, (await disabled.GetAsync(Url(w))).StatusCode);
        await factory.WithDb(async db => { var r = await db.Set<UnitResidency>().SingleAsync(r => r.Id == w.Relation); r.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Url(w))).StatusCode);
    }
    [PostgresFact] public async Task EndingAccountLinkOrDisablingParentRevokesRead()
    {
        var w = await Seed(); using var c = Client(w.User, "RESIDENT");
        await factory.WithDb(db => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE person_account_links SET is_enabled=false WHERE tenant_id={w.Tenant} AND person_id={w.Person}"));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Url(w))).StatusCode);
        var active = await Seed(); using var other = Client(active.User, "RESIDENT");
        await factory.WithDb(async db => { var p = await db.Properties.SingleAsync(p => p.Id == active.Property); p.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Url(active))).StatusCode);
    }
    [PostgresFact] public async Task CanonicalManagementRequiresCurrentAssignmentAndTenantMembership()
    {
        var w = await Seed(); using var c = Client(w.User, "MANAGER");
        var assignment = new ManagementAssignment(w.Tenant, w.Property, w.Person, "MANAGER", Today.AddDays(-1));
        await factory.WithDb(async db => { db.Add(assignment); await db.SaveChangesAsync(); });
        var request = new CommonAreaRequest(Guid.NewGuid(), "Salon", "MEETING_ROOM", null, "PROPERTY", "APPROVAL", 10);
        var root = $"/api/v1/properties/{w.Property}/common-areas";
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync(root, request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(root)).StatusCode);
        await factory.WithDb(async db => { var a = await db.Set<ManagementAssignment>().SingleAsync(a => a.Id == assignment.Id); a.Cancel(DateTimeOffset.UtcNow, "Fixture cancellation"); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync(root, request with { RequestId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(root)).StatusCode);
    }
    [PostgresTheory][InlineData("OWNER")][InlineData("TENANT")]
    public async Task OwnerAndTenantReadsUseTheirOwnRelationAndInclusiveEndDate(string capacity)
    {
        var w = await Seed(); var start = Today.AddDays(-2);
        var lease = new LeaseContract(w.Tenant, w.Property, w.Unit, start, null, null); lease.MarkLegacy();
        VerifiableRecord relation = capacity == "OWNER" ? new UnitOwnership(w.Tenant, w.Property, w.Unit, w.Person, start) : new UnitTenancy(lease, w.Person, start);
        relation.MarkLegacy();
        await factory.WithDb(async db => { if (capacity == "TENANT") db.Add(lease); db.Add(relation); await db.SaveChangesAsync(); });
        using var c = Client(w.User, capacity);
        var url = Url(w).Replace("capacity=RESIDENT", "capacity=" + capacity).Replace(w.Relation.ToString(), relation.Id.ToString());
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(url)).StatusCode);
        async Task End(DateOnly end) => await factory.WithDb(db => capacity == "OWNER"
            ? db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_ownerships SET end_date={end} WHERE id={relation.Id}")
            : db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_tenancies SET end_date={end} WHERE id={relation.Id}"));
        await End(Today);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(url)).StatusCode);
        // End is immutable: seed a separate expired relation rather than rewriting its history.
        var expired = await Seed();
        var oldLease = new LeaseContract(expired.Tenant, expired.Property, expired.Unit, start, null, null); oldLease.MarkLegacy();
        VerifiableRecord oldRelation = capacity == "OWNER"
            ? new UnitOwnership(expired.Tenant, expired.Property, expired.Unit, expired.Person, start, Today.AddDays(-1))
            : new UnitTenancy(oldLease, expired.Person, start, Today.AddDays(-1));
        oldRelation.MarkLegacy();
        await factory.WithDb(async db => { if (capacity == "TENANT") db.Add(oldLease); db.Add(oldRelation); await db.SaveChangesAsync(); });
        using var other = Client(expired.User, capacity);
        var expiredUrl = Url(expired).Replace("capacity=RESIDENT", "capacity=" + capacity).Replace(expired.Relation.ToString(), oldRelation.Id.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(expiredUrl)).StatusCode);
    }
    public void Dispose() { foreach (var child in children) child.Dispose(); }
}
