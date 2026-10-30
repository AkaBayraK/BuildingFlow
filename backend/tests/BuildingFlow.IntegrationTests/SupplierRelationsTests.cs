using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierRelationsTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private async Task<(Guid Tenant, string User, Guid First, Guid Second, Guid Hidden, Guid Foreign)> Seed()
    {
        var t = new PersonTenant("Relations"); var foreignTenant = new PersonTenant("Foreign"); var user = Guid.NewGuid().ToString(); var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user);
        var first = new Property("Visible first", "City", "District", "Address"); var second = new Property("Visible second", "City", "District", "Address"); var hidden = new Property("Never disclose this area", "City", "District", "Address"); var foreign = new Property("Foreign area", "City", "District", "Address");
        await factory.WithDb(async db => { db.AddRange(t, foreignTenant, account, new PersonTenantMembership(t.Id, account.Id), first, second, hidden, foreign, new PersonPropertyScope(t.Id, first.Id), new PersonPropertyScope(t.Id, second.Id), new PersonPropertyScope(t.Id, hidden.Id), new PersonPropertyScope(foreignTenant.Id, foreign.Id), new PropertyMembership(first.Id, user), new PropertyMembership(second.Id, user)); await db.SaveChangesAsync(); });
        return (t.Id, user, first.Id, second.Id, hidden.Id, foreign.Id);
    }
    private static async Task<T> Value<T>(HttpResponseMessage r) { Assert.Equal(HttpStatusCode.OK, r.StatusCode); return (await r.Content.ReadFromJsonAsync<T>())!; }
    [PostgresFact] public async Task CategoriesAreRenamedAndDisabledWithoutLosingServices()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); var root = $"/api/v2/tenants/{x.Tenant}";
        var supplier = (await Value<SupplierMutation>(await c.PostAsJsonAsync($"{root}/suppliers", new SupplierRequest(Guid.NewGuid(), "Lift Co", "COMPANY", Status:"ACTIVE")))).Record;
        var lift = await Value<SupplierWorkspace>(await c.PostAsJsonAsync($"{root}/supplier-categories", new SupplierCategoryRequest(Guid.NewGuid(), "Lift")));
        var garden = await Value<SupplierWorkspace>(await c.PostAsJsonAsync($"{root}/supplier-categories", new SupplierCategoryRequest(Guid.NewGuid(), "Garden")));
        await Value<SupplierMutation>(await c.PutAsJsonAsync($"{root}/suppliers/{supplier.Id}/areas", new SupplierAreaRequest(Guid.NewGuid(), supplier.Version, x.First, lift.Id, "Inspection")));
        var managed = (await c.GetFromJsonAsync<PagedRows<SupplierCategoryRow>>($"{root}/supplier-categories/manage"))!; var row = managed.Items.Single(r => r.Id == lift.Id); Assert.Equal(1, row.ServiceCount); Assert.True(row.IsEnabled);
        var renamed = await Value<SupplierCategoryRow>(await c.PutAsJsonAsync($"{root}/supplier-categories/{lift.Id}", new SupplierCategoryUpdate(" Elevators ", false, row.Version)));
        Assert.Equal("Elevators", renamed.Name); Assert.False(renamed.IsEnabled); Assert.Equal(1, renamed.ServiceCount);
        Assert.Equal(renamed, await Value<SupplierCategoryRow>(await c.PutAsJsonAsync($"{root}/supplier-categories/{lift.Id}", new SupplierCategoryUpdate("Elevators", false, row.Version))));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"{root}/supplier-categories/{lift.Id}", new SupplierCategoryUpdate("Other", true, row.Version))).StatusCode);
        var duplicate = await c.PutAsJsonAsync($"{root}/supplier-categories/{garden.Id}", new SupplierCategoryUpdate("elevators", true, 1)); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode); Assert.Contains("BF_SUPPLIER_DUPLICATE", await duplicate.Content.ReadAsStringAsync());
        Assert.DoesNotContain((await c.GetFromJsonAsync<PagedRows<SupplierWorkspace>>($"{root}/supplier-categories"))!.Items, r => r.Id == lift.Id);
        var areas = (await c.GetFromJsonAsync<PagedRows<SupplierAreaRow>>($"{root}/suppliers/{supplier.Id}/areas"))!; Assert.Equal("Elevators", Assert.Single(areas.Items).CategoryName);
        var current = (await c.GetFromJsonAsync<SupplierResponse>($"{root}/suppliers/{supplier.Id}"))!;
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"{root}/suppliers/{supplier.Id}/areas", new SupplierAreaRequest(Guid.NewGuid(), current.Version, x.Second, lift.Id, "Repair"))).StatusCode);
        foreach (var body in new[] { new SupplierCategoryUpdate("", true, 1), new SupplierCategoryUpdate("X", null, 1), new SupplierCategoryUpdate("X", true, 0) }) Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"{root}/supplier-categories/{garden.Id}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"{root}/supplier-categories/{Guid.NewGuid()}", new SupplierCategoryUpdate("X", true, 1))).StatusCode);
        using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT"); Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync($"{root}/supplier-categories/manage")).StatusCode);
        using var outsider = TestToken.Client(factory, Guid.NewGuid().ToString(), "MANAGER"); Assert.Equal(HttpStatusCode.NotFound, (await outsider.PutAsJsonAsync($"{root}/supplier-categories/{garden.Id}", new SupplierCategoryUpdate("X", true, 1))).StatusCode);
    }
    [PostgresFact] public async Task MultipleAreasAndServicesReplayWithoutDuplicatesAndForeignLinksAreRejected()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER");
        var supplier = (await Value<SupplierMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers", new SupplierRequest(Guid.NewGuid(), "Acme", "COMPANY", Status:"ACTIVE")))).Record;
        var categoryRequest = new SupplierCategoryRequest(Guid.NewGuid(), "Maintenance"); var category = await Value<SupplierWorkspace>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-categories", categoryRequest));
        Assert.Equal(category, await Value<SupplierWorkspace>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-categories", categoryRequest)));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-categories", categoryRequest with { Name = "Changed" })).StatusCode);
        var request = new SupplierAreaRequest(Guid.NewGuid(), supplier.Version, x.First, category.Id, " Elevator service ");
        var one = await Value<SupplierMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request)); Assert.Equal(2, one.Record.Version);
        var two = await Value<SupplierMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request with { OperationId = Guid.NewGuid(), Version = 2, PropertyId = x.Second }));
        var replay = await Value<SupplierMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request)); Assert.True(replay.Replayed); Assert.Equal(two.Record.Version, replay.Record.Version);
        var page = (await c.GetFromJsonAsync<PagedRows<SupplierAreaRow>>($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas"))!; Assert.Equal(2, page.TotalCount); Assert.All(page.Items, r => Assert.Equal("Elevator service", r.ServiceName));
        var properties = (await c.GetFromJsonAsync<PagedRows<SupplierWorkspace>>($"/api/v2/tenants/{x.Tenant}/supplier-property-choices"))!; Assert.Equal(2, properties.TotalCount); Assert.DoesNotContain(properties.Items, r => r.Id == x.Hidden || r.Id == x.Foreign);
        foreach (var property in new[] { x.Hidden, x.Foreign }) Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request with { OperationId = Guid.NewGuid(), Version = two.Record.Version, PropertyId = property })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request with { OperationId = Guid.NewGuid(), Version = two.Record.Version, CategoryId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request with { OperationId = Guid.NewGuid() })).StatusCode);
        var foreignCategory = Guid.NewGuid();
        await factory.WithDb(async db => { var scope = await db.Set<PersonPropertyScope>().SingleAsync(p => p.PropertyId == x.Foreign); db.Add(new SupplierCategory(scope.TenantId,"Foreign category",foreignCategory)); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request with { OperationId = Guid.NewGuid(), Version = two.Record.Version, CategoryId = foreignCategory })).StatusCode);
        var races = await Task.WhenAll(c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request with { OperationId = Guid.NewGuid(), Version = two.Record.Version }), c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request with { OperationId = Guid.NewGuid(), Version = two.Record.Version, PropertyId = x.Second }));
        Assert.Single(races,r=>r.StatusCode==HttpStatusCode.OK); Assert.Single(races,r=>r.StatusCode==HttpStatusCode.Conflict);
        await factory.WithDb(async db => Assert.Equal(1, await db.Set<BuildingFlow.Domain.SupplierService>().CountAsync(s => s.TenantId == x.Tenant && s.SupplierId == supplier.Id)));
        await factory.WithDb(async db => { var membership = await db.PropertyMemberships.SingleAsync(m => m.PropertyId == x.First && m.UserId == x.User); membership.SetEnabled(false); await db.SaveChangesAsync(); });
        var remaining = (await c.GetFromJsonAsync<PagedRows<SupplierAreaRow>>($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas"))!; Assert.Single(remaining.Items); Assert.Equal(x.Second, remaining.Items[0].PropertyId); Assert.DoesNotContain(x.First.ToString(), await c.GetStringAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas"));
    }
    [PostgresFact] public async Task RoleValidationInactiveSupplierAndPropertyAreProtected()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); var root = $"/api/v2/tenants/{x.Tenant}";
        var supplier = (await Value<SupplierMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers", new SupplierRequest(Guid.NewGuid(), "Draft", "COMPANY")))).Record;
        var category = await Value<SupplierWorkspace>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-categories", new SupplierCategoryRequest(Guid.NewGuid(), "Test")));
        var request = new SupplierAreaRequest(Guid.NewGuid(), 1, x.First, category.Id, "Inspection");
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request)).StatusCode);
        using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT"); using var anonymous = factory.CreateClient();
        foreach (var path in new[] { "/supplier-categories", "/supplier-property-choices", $"/suppliers/{supplier.Id}/areas" }) { Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync(root + path)).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(root + path)).StatusCode); }
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-categories", new SupplierCategoryRequest(Guid.Empty, ""))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request with { ServiceName = " " })).StatusCode);
        await factory.WithDb(async db => { (await db.Properties.SingleAsync(p => p.Id == x.First)).SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{supplier.Id}/areas", request)).StatusCode);
    }
}
