using System.Net;
using System.Text.Json;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Sistem durumu: yalnız yönetim rolleri okur; kontrol hatası endpoint'i düşürmez ve yanıta adres/kimlik bilgisi sızmaz.
public sealed class SystemStatusApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Path="/api/v1/system/status";
    [PostgresFact] public async Task AnonymousAndAccountantAreRejected()
    {
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Path)).StatusCode);
        using var accountant=TestToken.Client(factory,Guid.NewGuid().ToString(),"ACCOUNTANT");Assert.Equal(HttpStatusCode.Forbidden,(await accountant.GetAsync(Path)).StatusCode);
    }
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task ManagementRolesSeeLiveComponentsWithSafeDescriptions(string role)
    {
        using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),role);var response=await c.GetAsync(Path);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var text=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("Password",text,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("Host=",text);
        using var json=JsonDocument.Parse(text);var root=json.RootElement;var components=root.GetProperty("components").EnumerateArray().ToArray();
        Assert.Equal(components.Length,root.GetProperty("totalComponents").GetInt32());
        string Status(string id)=>components.Single(x=>x.GetProperty("id").GetString()==id).GetProperty("status").GetString()!;
        Assert.Equal("UP",Status("buildingflow-api"));Assert.Equal("UP",Status("postgresql"));
        Assert.All(components,x=>Assert.Contains(x.GetProperty("status").GetString(),new[]{"UP","DEGRADED","DOWN","UNKNOWN","DISABLED"}));
        Assert.Contains(root.GetProperty("overallStatus").GetString(),new[]{"UP","DEGRADED","DOWN","UNKNOWN"});
    }
}
