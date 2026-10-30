using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Issuer = "http://localhost:8080/realms/buildingflow";
    private static string Root(Guid t) => $"/api/v2/tenants/{t}/suppliers";
    private async Task<(Guid Tenant, string User)> Seed()
    {
        var t = new PersonTenant("Supplier fixture"); var u = Guid.NewGuid().ToString(); var a = new PersonUserAccount(Issuer, u);
        await factory.WithDb(async db => { db.AddRange(t, a, new PersonTenantMembership(t.Id, a.Id)); await db.SaveChangesAsync(); }); return (t.Id, u);
    }
    private static SupplierRequest Request(string name = "Acme") => new(Guid.NewGuid(), name, "COMPANY");
    private static async Task<SupplierMutation> Value(HttpResponseMessage r)
    { Assert.Equal(HttpStatusCode.OK, r.StatusCode); return (await r.Content.ReadFromJsonAsync<SupplierMutation>())!; }
    [PostgresTheory] [InlineData("MANAGER")] [InlineData("SITE_ADMIN")] [InlineData("SUPER_ADMIN")]
    public async Task ManagersWriteVersionReplayDuplicateAndInactiveHistory(string role)
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, role); var request = Request(" Acme ");
        var one = await Value(await c.PostAsJsonAsync(Root(x.Tenant), request)); Assert.Equal("Acme", one.Record.Name); Assert.Equal(1, one.Record.Version);
        var edit = request with { OperationId = Guid.NewGuid(), Name = "Acme revised", Version = 1, Status = "INACTIVE", StatusReason = "No longer contracted" };
        var two = await Value(await c.PutAsJsonAsync(Root(x.Tenant) + "/" + one.Record.Id, edit)); Assert.Equal(2, two.Record.Version); Assert.False(two.Record.IsEnabled);
        var replay = await Value(await c.PostAsJsonAsync(Root(x.Tenant), request with { Name = "Acme" })); Assert.True(replay.Replayed); Assert.Equal(two.Record, replay.Record); Assert.Equal(1, replay.AppliedVersion);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(Root(x.Tenant), request with { Name = "Different" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(Root(x.Tenant) + "/" + one.Record.Id, edit with { OperationId = Guid.NewGuid() })).StatusCode);
        var duplicate = await c.PostAsJsonAsync(Root(x.Tenant), Request("ACME REVISED")); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode); Assert.Contains("BF_SUPPLIER_DUPLICATE", await duplicate.Content.ReadAsStringAsync());
        var page = (await c.GetFromJsonAsync<PagedRows<SupplierResponse>>(Root(x.Tenant) + "?status=INACTIVE"))!; Assert.Equal(one.Record.Id, Assert.Single(page.Items).Id);
        await factory.WithDb(async db => { var s = await db.Set<Supplier>().SingleAsync(s => s.Id == one.Record.Id); Assert.Equal(x.User, s.CreatedBy); Assert.Equal(one.Record.CreatedAt, s.CreatedAt); });
    }
    [PostgresFact] public async Task AnonymousRolesForeignTenantAndRevokedMembershipAreDenied()
    {
        var x = await Seed(); var other = await Seed(); using var manager = TestToken.Client(factory, x.User, "MANAGER");
        var one = await Value(await manager.PostAsJsonAsync(Root(x.Tenant), Request()));
        using var anon = factory.CreateClient(); using var resident = TestToken.Client(factory, x.User, "RESIDENT"); using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT"); using var outsider = TestToken.Client(factory, other.User, "MANAGER");
        foreach (var suffix in new[] { "", "/" + one.Record.Id })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(Root(x.Tenant) + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await resident.GetAsync(Root(x.Tenant) + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync(Root(x.Tenant) + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(Root(x.Tenant) + suffix)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync(Root(x.Tenant), Request())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync(Root(other.Tenant) + "/" + one.Record.Id)).StatusCode);
        await factory.WithDb(async db => { var m = await db.Set<PersonTenantMembership>().SingleAsync(m => m.TenantId == x.Tenant); m.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await manager.PutAsJsonAsync(Root(x.Tenant) + "/" + one.Record.Id, Request() with { Version = 1 })).StatusCode);
    }
    [PostgresFact] public async Task PaginationLiteralSearchAndInvalidRequests()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER");
        await Value(await c.PostAsJsonAsync(Root(x.Tenant), Request("100%_special"))); await Value(await c.PostAsJsonAsync(Root(x.Tenant), Request("Another")));
        var page = (await c.GetFromJsonAsync<PagedRows<SupplierResponse>>(Root(x.Tenant) + "?search=%25_"))!; Assert.Equal("100%_special", Assert.Single(page.Items).Name);
        var second = (await c.GetFromJsonAsync<PagedRows<SupplierResponse>>(Root(x.Tenant) + "?pageSize=1&page=2"))!; Assert.Single(second.Items); Assert.Equal(2, second.TotalCount);
        foreach (var query in new[] { "page=0", "pageSize=101", "status=unknown", "sortBy=identity", "sortDirection=sideways" }) Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(Root(x.Tenant) + "?" + query)).StatusCode);
        foreach (var request in new[] { Request() with { Name = " " }, Request() with { Kind = "unknown" }, Request() with { Status = "BLOCKED" }, Request() with { OperationId = Guid.Empty }, Request() with { Version = 1 } })
        { var response = await c.PostAsJsonAsync(Root(x.Tenant), request); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("BF_SUPPLIER_INVALID", await response.Content.ReadAsStringAsync()); }
    }
    [PostgresFact] public async Task ConcurrentSameUuidAndVersionHaveOneWinner()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); var request = Request();
        var created = await Task.WhenAll(c.PostAsJsonAsync(Root(x.Tenant), request), c.PostAsJsonAsync(Root(x.Tenant), request)); var values = await Task.WhenAll(created.Select(Value)); Assert.Single(values, x => x.Replayed); Assert.Equal(values[0].Record.Id, values[1].Record.Id);
        var id = values[0].Record.Id; var edit = Request("Changed") with { Version = 1 };
        var updates = await Task.WhenAll(c.PutAsJsonAsync(Root(x.Tenant) + "/" + id, edit), c.PutAsJsonAsync(Root(x.Tenant) + "/" + id, edit with { OperationId = Guid.NewGuid() }));
        Assert.Single(updates, x => x.StatusCode == HttpStatusCode.OK); Assert.Single(updates, x => x.StatusCode == HttpStatusCode.Conflict);
    }
    [PostgresFact] public async Task EnforcedModeRequiresCurrentManagementAssignmentAndIssuerMatches()
    {
        var x = await Seed(); using var signed = TestToken.Client(factory, x.User, "MANAGER");
        using var host = factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { [$"PersonRefactor:ManagementModes:{x.Tenant}"] = "ENFORCED" })));
        using var restricted = host.CreateClient(); restricted.DefaultRequestHeaders.Authorization = signed.DefaultRequestHeaders.Authorization;
        Assert.Equal(HttpStatusCode.NotFound, (await restricted.GetAsync(Root(x.Tenant))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await restricted.PostAsJsonAsync(Root(x.Tenant), Request())).StatusCode);
        await factory.WithDb(async db => { var account = await db.Set<PersonUserAccount>().SingleAsync(a => a.Subject == x.User); db.Entry(account).Property(a => a.Issuer).CurrentValue = "https://other.example.test/realm"; await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await signed.GetAsync(Root(x.Tenant))).StatusCode);
    }

    [PostgresFact] public async Task WorkspaceChoicesRespectMembershipRolesAndPagination()
    {
        var x = await Seed(); var other = await Seed(); using var manager = TestToken.Client(factory, x.User, "MANAGER");
        var page = (await manager.GetFromJsonAsync<PagedRows<SupplierWorkspace>>("/api/v2/supplier-workspaces"))!;
        Assert.Contains(page.Items, t => t.Id == x.Tenant); Assert.DoesNotContain(page.Items, t => t.Id == other.Tenant);
        using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT");
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync("/api/v2/supplier-workspaces")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.GetAsync("/api/v2/supplier-workspaces?pageSize=101")).StatusCode);
    }

}
