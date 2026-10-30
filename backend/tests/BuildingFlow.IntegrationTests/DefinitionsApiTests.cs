using System.Net;
using System.Text.Json;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// TNM 01: definitions hub is read-only and limited to management roles; ACCOUNTANT/SECURITY and anonymous are rejected.
public sealed class DefinitionsApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Path="/api/v1/definitions";
    [PostgresTheory][InlineData("ACCOUNTANT")][InlineData("SECURITY")]
    public async Task AnonymousAndNonManagementRolesAreRejected(string role)
    {
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Path)).StatusCode);
        using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),role);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Path)).StatusCode);
    }
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task ManagementRolesReadStringCodedRegistry(string role)
    {
        using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),role);var response=await c.GetAsync(Path);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var root=json.RootElement;
        var request=root.GetProperty("lists").EnumerateArray().Single(x=>x.GetProperty("key").GetString()=="REQUEST_CATEGORY");
        Assert.Equal("REQUEST",request.GetProperty("group").GetString());Assert.Equal("PROPERTY",request.GetProperty("level").GetString());
        Assert.Contains("ELEVATOR",request.GetProperty("values").EnumerateArray().Select(x=>x.GetProperty("code").GetString()));Assert.True(request.GetProperty("editable").GetBoolean());
        Assert.Contains(root.GetProperty("catalogs").EnumerateArray(),x=>x.GetProperty("key").GetString()=="CONSENT_TYPES"&&x.GetProperty("route").GetString()=="/consents");
        Assert.Equal(JsonValueKind.Null,root.GetProperty("catalogs").EnumerateArray().Single(x=>x.GetProperty("key").GetString()=="DUES_CANCELLATION_REASONS").GetProperty("route").ValueKind);
    }
}
