using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
namespace BuildingFlow.IntegrationTests;
internal static class TestToken
{
    public static HttpClient Client(SiteApiTests.SiteApiFactory factory,string? actor,string role)
    {
        var client=factory.CreateClient();if(actor is null)return client;
        var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('s',64))){KeyId="sites-test-only"};
        var claims = new List<Claim> { new("sub", actor) };
        if (role == "spoofed") claims.Add(new(ClaimTypes.Role, "SUPER_ADMIN"));
        else if (role == "client-role") claims.Add(new("resource_access", JsonSerializer.Serialize(new Dictionary<string, object> { ["buildingflow-web"] = new { roles = new[] { "SUPER_ADMIN" } } }), JsonClaimValueTypes.Json));
        else claims.Add(new("realm_access", JsonSerializer.Serialize(new { roles = new[] { role } }), JsonClaimValueTypes.Json));
        var jwt=new JwtSecurityToken("http://localhost:8080/realms/buildingflow","buildingflow-api",claims,DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5),new SigningCredentials(key,SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",new JwtSecurityTokenHandler().WriteToken(jwt));return client;
    }
}
