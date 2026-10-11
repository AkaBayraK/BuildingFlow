using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class AuthenticationTests : IClassFixture<AuthenticationTests.ApiFactory>
{
    private const string Issuer = "http://localhost:8080/realms/buildingflow";
    private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes(new string('a', 64))) { KeyId = "test-only" };
    private readonly HttpClient client;
    public AuthenticationTests(ApiFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task AnonymousRequestIsUnauthorized() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/status")).StatusCode);

    [Fact]
    public async Task ValidSignedTokenCanAccessProtectedApi() =>
        Assert.Equal(HttpStatusCode.OK, (await Request(Token())).StatusCode);

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("signature")]
    [InlineData("malformed")]
    public async Task InvalidTokensAreRejected(string fault) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Request(fault == "malformed" ? "invalid-token" : Token(fault))).StatusCode);

    [Theory]
    [InlineData("http://localhost:3003", true)]
    [InlineData("http://untrusted.example", false)]
    public async Task CorsOnlyAllowsConfiguredFrontend(string origin, bool allowed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/status");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        using var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed) Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    private Task<HttpResponseMessage> Request(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }

    private static string Token(string? fault = null)
    {
        var key = fault == "signature" ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('b', 64))) { KeyId = "test-only" } : SigningKey;
        var token = new JwtSecurityToken(fault == "issuer" ? "http://wrong-issuer" : Issuer,
            fault == "audience" ? "wrong-api" : "buildingflow-api", [new Claim("sub", "test-user")],
            DateTime.UtcNow.AddMinutes(-20), fault == "expired" ? DateTime.UtcNow.AddMinutes(-10) : DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = Issuer,
                ["Authentication:Audience"] = "buildingflow-api",
                ["Authentication:MetadataAddress"] = "",
                ["ConnectionStrings:Database"] = "Host=localhost;Database=unused_auth_tests",
                ["Cors:Origins:0"] = "http://localhost:3003",
                ["Database:AutoMigrate"] = "false"
            }));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services => services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
                metadata.SigningKeys.Add(SigningKey);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            }));
        }
    }
}
