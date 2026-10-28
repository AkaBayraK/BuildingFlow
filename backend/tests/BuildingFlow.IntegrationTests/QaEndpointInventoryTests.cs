using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class QaEndpointInventoryTests : IClassFixture<QaEndpointInventoryTests.ApiFactory>
{
    public const string OutputSetting = "BUILDINGFLOW_QA_INVENTORY_OUT";
    // Public website routes are anonymous by design but carry no AllowAnonymous marker; the inventory lists them separately.
    private const string PublicPrefix = "/api/public/";
    private readonly ApiFactory factory;
    public QaEndpointInventoryTests(ApiFactory factory) => this.factory = factory;

    [Fact]
    public void EveryNonPublicApiEndpointDeclaresAnAuthorizationDecision()
    {
        var endpoints = Inventory();
        var output = Environment.GetEnvironmentVariable(OutputSetting);
        if (!string.IsNullOrWhiteSpace(output))
            File.WriteAllText(output, JsonSerializer.Serialize(endpoints, new JsonSerializerOptions { WriteIndented = true }));
        Assert.NotEmpty(endpoints);
        Assert.Empty(endpoints.Where(endpoint => endpoint.Access == "IMPLICIT").Select(endpoint => $"{endpoint.Method} {endpoint.Route}"));
    }

    private List<QaEndpoint> Inventory() => factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
        .OfType<RouteEndpoint>()
        .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)
        .SelectMany(endpoint => (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["ANY"])
            .Select(method => new QaEndpoint(method, endpoint.RoutePattern.RawText!, Access(endpoint), Policies(endpoint))))
        .OrderBy(endpoint => endpoint.Route, StringComparer.Ordinal).ThenBy(endpoint => endpoint.Method, StringComparer.Ordinal)
        .ToList();

    private static string Access(Endpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null ? "ANONYMOUS"
        : endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0 || endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>().Count > 0 ? "AUTHORIZED"
        : endpoint is RouteEndpoint { RoutePattern.RawText: { } route } && route.StartsWith(PublicPrefix, StringComparison.Ordinal) ? "PUBLIC_IMPLICIT"
        : "IMPLICIT";

    private static string[] Policies(Endpoint endpoint) => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
        .Select(data => data.Policy ?? data.Roles ?? "AUTHENTICATED").Distinct().Order(StringComparer.Ordinal).ToArray();

    public sealed record QaEndpoint(string Method, string Route, string Access, string[] Policies);

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "http://localhost:8080/realms/buildingflow",
                ["Authentication:Audience"] = "buildingflow-api",
                ["Authentication:MetadataAddress"] = "",
                ["ConnectionStrings:Database"] = "Host=localhost;Database=unused_qa_inventory",
                ["Cors:Origins:0"] = "http://localhost:3003",
                ["Database:AutoMigrate"] = "false"
            }));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
    }
}
