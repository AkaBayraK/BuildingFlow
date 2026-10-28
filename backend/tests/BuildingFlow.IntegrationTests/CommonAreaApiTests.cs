using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommonAreaApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static string Root(Guid p) => $"/api/v1/properties/{p}/common-areas";
    private static CommonAreaRequest Request(string name = " Havuz ", Guid? request = null) => new(request ?? Guid.NewGuid(), name, "POOL", null, "PROPERTY", "RESERVATION", 20, " Açıklama ", " Kurallar ", "08:00", "22:00", "100.00");
    private async Task<(Property P, string User)> Seed(PropertyType type = PropertyType.APARTMENT)
    {
        var user = Guid.NewGuid().ToString(); var p = new Property("Alan " + user, "City", "District", "Address", type);
        await factory.WithDb(async db => { db.AddRange(p, new PropertyMembership(p.Id, user)); await db.SaveChangesAsync(); }); return (p, user);
    }
    private static async Task<CommonAreaResponse> Read(HttpResponseMessage r, HttpStatusCode status = HttpStatusCode.OK)
    { Assert.True(r.StatusCode == status, $"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}"); return (await r.Content.ReadFromJsonAsync<CommonAreaResponse>())!; }
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task CrudReplayAuditConcurrencyAndDeletedReplay(string role)
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, role); var request = Request();
        var area = await Read(await c.PostAsJsonAsync(Root(x.P.Id), request), HttpStatusCode.Created);
        Assert.Equal("Havuz", area.Name); Assert.Equal("Kurallar", area.Rules); Assert.Equal(1, area.Version);
        var replay = await Read(await c.PostAsJsonAsync(Root(x.P.Id), request with { Name = "Havuz", FeeAmount = "100" }));
        Assert.True(replay.Replayed); Assert.Equal(area.Id, replay.Id); Assert.Equal(area.Version, replay.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(Root(x.P.Id), request with { Name = "Başka" })).StatusCode);
        var updated = await Read(await c.PutAsJsonAsync(Root(x.P.Id) + "/" + area.Id, request with { Name = "Yeni", Version = area.Version, IsEnabled = false }));
        Assert.Equal(2, updated.Version); Assert.False(updated.IsEnabled); Assert.Equal(area.CreatedAt, updated.CreatedAt);
        var currentReplay = await Read(await c.PostAsJsonAsync(Root(x.P.Id), request)); Assert.Equal("Yeni", currentReplay.Name); Assert.Equal(2, currentReplay.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(Root(x.P.Id) + "/" + area.Id, request with { Version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync(Root(x.P.Id) + $"/{area.Id}?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync(Root(x.P.Id) + $"/{area.Id}?version=2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Root(x.P.Id) + "/" + area.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(Root(x.P.Id), request)).StatusCode);
        await factory.WithDb(async db => { var row = await db.Set<CommonArea>().IgnoreQueryFilters().SingleAsync(a => a.Id == area.Id); Assert.True(row.IsDeleted); Assert.Equal(3, row.Version); Assert.Equal(x.User, row.CreatedBy); Assert.Equal(area.CreatedAt, row.CreatedAt); });
    }
    [PostgresFact] public async Task RejectsRolesForeignMembershipAndForeignResources()
    {
        var x = await Seed(); var foreign = await Seed(); using var owner = TestToken.Client(factory, x.User, "MANAGER");
        var area = await Read(await owner.PostAsJsonAsync(Root(x.P.Id), Request()), HttpStatusCode.Created);
        foreach (var role in new[] { "RESIDENT", "OWNER", "TENANT", "ACCOUNTANT", "spoofed", "client-role" }) {
            using var c = TestToken.Client(factory, x.User, role);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync(Root(x.P.Id))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync(Root(x.P.Id), Request())).StatusCode);
        }
        using var anonymous = factory.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root(x.P.Id))).StatusCode);
        using var other = TestToken.Client(factory, foreign.User, "MANAGER");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Root(x.P.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync(Root(x.P.Id), Request())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync(Root(foreign.P.Id) + "/" + area.Id, Request() with { Version = 1 })).StatusCode);
        await factory.WithDb(async db => { var member = await db.PropertyMemberships.SingleAsync(m => m.PropertyId == x.P.Id && m.UserId == x.User); member.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(Root(x.P.Id))).StatusCode);
    }
    [PostgresFact] public async Task ValidatesFieldsScopeAndFiltersWithoutPartialWrites()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); var r = Request();
        CommonAreaRequest[] invalid = [r with { Name = " " }, r with { Type = "pool" }, r with { Visibility = "BUILDING" }, r with { RequestId = Guid.Empty }, r with { Capacity = 0 }, r with { Currency = "EUR" }, r with { FeeAmount = "1.001" }, r with { ClosesAt = "07:00" }, r with { OpensAt = "8:00" }, r with { Rules = new string('x', 8001) }, r with { Version = 1 }, r with { Usage = "1" }];
        foreach (var bad in invalid) { var response = await c.PostAsJsonAsync(Root(x.P.Id), bad); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("BF_COMMON_AREA_INVALID", await response.Content.ReadAsStringAsync()); }
        await factory.WithDb(async db => Assert.False(await db.Set<CommonArea>().AnyAsync(a => a.PropertyId == x.P.Id)));
        await Read(await c.PostAsJsonAsync(Root(x.P.Id), Request("%_ Havuz")), HttpStatusCode.Created);
        await Read(await c.PostAsJsonAsync(Root(x.P.Id), Request("Salon") with { Type = "MEETING_ROOM", IsEnabled = false }), HttpStatusCode.Created);
        var rows = (await c.GetFromJsonAsync<PagedRows<CommonAreaResponse>>(Root(x.P.Id) + "?search=%25_"))!; Assert.Equal(1, rows.TotalCount);
        Assert.Single((await c.GetFromJsonAsync<PagedRows<CommonAreaResponse>>(Root(x.P.Id) + "?isEnabled=false&type=MEETING_ROOM"))!.Items);
        foreach (var q in new[] { "?page=0", "?pageSize=101", "?type=OTHER_BAD" }) Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(Root(x.P.Id) + q)).StatusCode);
        await factory.WithDb(async db => { var p = await db.Properties.SingleAsync(p => p.Id == x.P.Id); p.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(Root(x.P.Id), Request())).StatusCode);
    }
    [PostgresFact] public async Task BuildingScopeAndParentHistoryAreProtected()
    {
        var x = await Seed(PropertyType.SITE); var foreign = await Seed(PropertyType.SITE);
        var block = x.P.AddBuilding("A"); var wrong = foreign.P.AddBuilding("B");
        await factory.WithDb(async db => { db.AddRange(block, wrong); await db.SaveChangesAsync(); }); using var c = TestToken.Client(factory, x.User, "MANAGER");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(Root(x.P.Id), Request() with { BuildingId = wrong.Id })).StatusCode);
        var area = await Read(await c.PostAsJsonAsync(Root(x.P.Id), Request() with { BuildingId = block.Id, Visibility = "BUILDING" }), HttpStatusCode.Created);
        long version = 0; await factory.WithDb(async db => version = (await db.Buildings.AsNoTracking().SingleAsync(b => b.Id == block.Id)).Version);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/v1/properties/{x.P.Id}/buildings/{block.Id}?version={version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync(Root(x.P.Id) + $"/{area.Id}?version={area.Version}")).StatusCode);
        using var admin = TestToken.Client(factory, x.User, "SUPER_ADMIN");
        await factory.WithDb(async db => version = (await db.Properties.AsNoTracking().SingleAsync(p => p.Id == x.P.Id)).Version);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/properties/{x.P.Id}?version={version}")).StatusCode);
    }
    [PostgresFact] public async Task ConcurrentCreateSameUuidReturnsOneRecord()
    {
        var x = await Seed(); var r = Request(); using var a = TestToken.Client(factory, x.User, "MANAGER"); using var b = TestToken.Client(factory, x.User, "MANAGER");
        var responses = await Task.WhenAll(a.PostAsJsonAsync(Root(x.P.Id), r), b.PostAsJsonAsync(Root(x.P.Id), r));
        Assert.Single(responses, v => v.StatusCode == HttpStatusCode.Created); Assert.Single(responses, v => v.StatusCode == HttpStatusCode.OK);
        var values = await Task.WhenAll(responses.Select(v => v.Content.ReadFromJsonAsync<CommonAreaResponse>())); Assert.Equal(values[0]!.Id, values[1]!.Id);
        await factory.WithDb(async db => Assert.Equal(1, await db.Set<CommonArea>().CountAsync(v => v.PropertyId == x.P.Id)));
    }
}
