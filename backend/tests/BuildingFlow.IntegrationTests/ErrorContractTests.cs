using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
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

// HTTP contract tests replace only the store; no production or test database is touched.
public sealed class ErrorContractTests(ErrorContractTests.ApiFactory factory) : IClassFixture<ErrorContractTests.ApiFactory>
{
    [Theory]
    [InlineData(401, "BF_SIGN_IN_REQUIRED", "/api/v1/status", false)]
    [InlineData(404, "BF_RESOURCE_NOT_FOUND", "/api/v1/missing", true)]
    [InlineData(404, "BF_RESOURCE_NOT_FOUND", "/api/v1/properties/00000000-0000-0000-0000-000000000000", true)]
    [InlineData(409, "BF_RECORD_CHANGED", "/api/v1/properties/00000000-0000-0000-0000-000000000001", true)]
    [InlineData(500, "BF_UNEXPECTED_FAILURE", "/api/v1/properties/00000000-0000-0000-0000-000000000002", true)]
    [InlineData(400, "BF_INVALID_REQUEST", "/api/v1/properties?page=invalid", true)]
    public async Task EveryFailurePreservesHttpStatusAndUsesSafeContract(int status, string code, string path, bool authenticated)
    {
        using var client = Client(authenticated);
        using var response = await client.GetAsync(path);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-test-secret", raw);
        using var body = JsonDocument.Parse(raw);
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
        Assert.Equal(status, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(status == 409 ? "warning" : "error", body.RootElement.GetProperty("severity").GetString());
        Assert.Equal(body.RootElement.GetProperty("title").GetString(), body.RootElement.GetProperty("message").GetString());
        Assert.Empty(body.RootElement.GetProperty("parameters").EnumerateObject());
        if (status == 401) Assert.Contains(response.Headers.WwwAuthenticate, value => value.Scheme == "Bearer");
    }

    [Fact]
    public async Task ForbiddenHasCodeWithoutOpeningRoleAccess()
    {
        using var client = Client(true, false);
        using var response = await client.PostAsJsonAsync("/api/v1/properties", new { name = "Ignored" });
        Assert.Equal(403, (int)response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("BF_ACCESS_DENIED", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ValidationIncludesCodedFieldParametersAndLegacyErrors()
    {
        using var client = Client(true);
        using var response = await client.PostAsJsonAsync("/api/v1/properties", new { name = new string('x', 201), city = "City", district = "District", address = "Address", type = "SITE" });
        Assert.Equal(400, (int)response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("BF_VALIDATION_FAILED", body.RootElement.GetProperty("code").GetString());
        var issue = body.RootElement.GetProperty("fieldIssues").GetProperty("name")[0];
        Assert.Equal("BF_FIELD_TOO_LONG", issue.GetProperty("code").GetString());
        Assert.Equal("propertyName", issue.GetProperty("parameters").GetProperty("field").GetString());
        Assert.Equal(200, issue.GetProperty("parameters").GetProperty("max").GetInt32());
        Assert.Equal(issue.GetProperty("message").GetString(), body.RootElement.GetProperty("errors").GetProperty("name")[0].GetString());
    }

    [Fact]
    public void CatalogCodesAreUniqueAndParameterContractRejectsSensitiveValues()
    {
        Assert.Equal(ErrorCatalog.All.Count, ErrorCatalog.All.Select(value => value.Code).Distinct().Count());
        foreach (var value in ErrorCatalog.All) Assert.Matches("^BF_[A-Z0-9_]+$", value.Code);
        Assert.Throws<ArgumentException>(() => ErrorCatalog.FieldTooLong.Create(("field", "private-test-secret"), ("max", 200)));
        Assert.Throws<ArgumentException>(() => ErrorCatalog.FieldTooLong.Create(("field", "propertyName"), ("max", -1)));
        Assert.Throws<ArgumentException>(() => ErrorCatalog.FieldTooLong.Create(("field", "propertyName")));
        Assert.Throws<ArgumentException>(() => ErrorCatalog.RecordChanged.Create(("token", "private-test-secret")));
        Assert.Equal("Yönetim alanı adı en fazla 200 karakter olabilir.", ErrorCatalog.FieldTooLong.Create(("field", "propertyName"), ("max", 200)).Message);
    }

    private HttpClient Client(bool authenticated, bool admin = true)
    {
        var client = factory.CreateClient();
        if (!authenticated) return client;
        var claims = new[] { new Claim("sub", "error-contract-test"), new Claim("realm_access", JsonSerializer.Serialize(new { roles = admin ? new[] { "SUPER_ADMIN" } : Array.Empty<string>() }), JsonClaimValueTypes.Json) };
        var jwt = new JwtSecurityToken(ApiFactory.Issuer, "buildingflow-api", claims, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5), new SigningCredentials(ApiFactory.Key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return client;
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        internal const string Issuer = "http://localhost:8080/realms/buildingflow";
        internal static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes(new string('e', 64))) { KeyId = "errors-test-only" };
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = Issuer,
                ["Authentication:Audience"] = "buildingflow-api",
                ["Authentication:MetadataAddress"] = "",
                ["ConnectionStrings:Database"] = "Host=localhost;Database=unused_error_contract_tests"
            }));
            return base.CreateHost(builder);
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IPropertyStore, ContractStore>();
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var metadata = new OpenIdConnectConfiguration { Issuer = Issuer }; metadata.SigningKeys.Add(Key);
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
                });
            });
        }
    }
    private sealed class ContractStore : IPropertyStore
    {
        public Task<PropertyResponse?> GetAsync(Guid id, string userId, bool readAll, CancellationToken token)
        {
            if (id == Guid.Parse("00000000-0000-0000-0000-000000000001")) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            if (id == Guid.Parse("00000000-0000-0000-0000-000000000002")) throw new InvalidOperationException("private-test-secret");
            return Task.FromResult<PropertyResponse?>(null);
        }
        public Task SaveNewAsync(Property property, string userId, CancellationToken token) => throw new InvalidOperationException("No writes in error contract tests");
        public Task<PagedProperties> ListAsync(string userId, bool readAll, PropertyListQuery query, CancellationToken token) => Task.FromResult(new PagedProperties([], query.Page, query.PageSize, 0));
    }
}
