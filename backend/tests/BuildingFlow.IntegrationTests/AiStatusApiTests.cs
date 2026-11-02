using System.Net;
using System.Text;
using System.Text.Json;
using BuildingFlow.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// AI Durumu: yalnız yönetim rolleri okur; model çağrısı yapılmaz, token/secret/tam adres yanıta sızmaz, AkaAI hatası endpoint'i düşürmez.
public sealed class AiStatusApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Path = "/api/v1/system/ai-status";
    private const string Secret = "fixture-secret-not-real";

    private sealed class FakeAkaAi : HttpMessageHandler
    {
        public bool Down { get; init; }
        public HttpStatusCode Models { get; init; } = HttpStatusCode.OK;
        public List<string> Paths { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (Down) throw new HttpRequestException("connection refused");
            var path = request.RequestUri!.AbsolutePath; Paths.Add(path);
            if (path.EndsWith("/token", StringComparison.Ordinal)) return Json(new { access_token = "fake-token", expires_in = 300 });
            if (path == "/readyz") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            Assert.Equal("/api/v1/integrations/models", path);
            Assert.Equal("Bearer fake-token", request.Headers.Authorization!.ToString());
            if (Models != HttpStatusCode.OK) return Task.FromResult(new HttpResponseMessage(Models) { Content = new StringContent("{\"error\":\"SECRET_UPSTREAM_BODY\"}") });
            return Json(new { data = new object[] { new { id = "other", x_akaai_default = false }, new { id = "llama3.2", x_akaai_default = true, x_akaai_locality = "EXTERNAL", owned_by = "local-ollama" } } });
        }
        private static Task<HttpResponseMessage> Json(object value) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") });
    }

    private WebApplicationFactory<Program> App(FakeAkaAi? akaai) => factory.WithWebHostBuilder(builder =>
    {
        if (akaai is null) return;
        builder.UseSetting("Assistant:AkaAi:BaseUrl", "http://akaai.test:8090/");
        builder.UseSetting("Assistant:AkaAi:TokenUrl", "http://idp.test/realms/akaai/protocol/openid-connect/token");
        builder.UseSetting("Assistant:AkaAi:ClientId", "buildingflow-assistant");
        builder.UseSetting("Assistant:AkaAi:ClientSecret", Secret);
        builder.UseSetting("Assistant:AkaAi:ConsoleUrl", "http://console.akaai.test/");
        builder.ConfigureServices(services => services.AddHttpClient(AkaAiHealthProbe.ClientName).ConfigurePrimaryHttpMessageHandler(() => akaai));
    });

    private static async Task<(JsonElement Root, Dictionary<string, JsonElement> Components, string Text)> Read(HttpClient client)
    {
        var response = await client.GetAsync(Path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var text = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(text).RootElement;
        var components = root.GetProperty("components").EnumerateArray().ToDictionary(c => c.GetProperty("id").GetString()!);
        Assert.Equal(components.Count, root.GetProperty("totalComponents").GetInt32());
        return (root, components, text);
    }

    [PostgresFact] public async Task AnonymousAndAccountantAreRejected()
    {
        using var anonymous = factory.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Path)).StatusCode);
        using var accountant = TestToken.Client(factory, Guid.NewGuid().ToString(), "ACCOUNTANT"); Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync(Path)).StatusCode);
    }

    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task WithoutAkaAiTheAssistantIsReportedInRulesMode(string role)
    {
        using var client = TestToken.Client(factory, Guid.NewGuid().ToString(), role);
        var (root, components, _) = await Read(client);
        Assert.Equal("DISABLED", root.GetProperty("overallStatus").GetString());
        Assert.Equal("RULES", root.GetProperty("mode").GetString());
        Assert.Equal("-", root.GetProperty("endpoint").GetString());
        Assert.Equal("NOT_CHECKED", components["akaai-model"].GetProperty("detail").GetString());
        Assert.Equal("RULES_MODE", components["assistant-configuration"].GetProperty("detail").GetString());
        Assert.Equal("DISABLED", components["resident-assistant"].GetProperty("status").GetString());
        Assert.Equal(root.GetProperty("activePromptCount").GetInt32(), components["prompt-templates"].TryGetProperty("count", out var count) && count.ValueKind == JsonValueKind.Number ? count.GetInt32() : 0);
    }

    [PostgresFact] public async Task HealthyAkaAiShowsDefaultModelWithoutCallingTheModelOrLeakingSecrets()
    {
        var akaai = new FakeAkaAi();
        using var client = TestToken.Client(App(akaai), Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var (root, components, text) = await Read(client);
        Assert.Equal("AKAAI", root.GetProperty("mode").GetString());
        Assert.Equal("UP", components["akaai-model"].GetProperty("status").GetString());
        Assert.Equal("DEFAULT_MODEL_READY", components["akaai-model"].GetProperty("detail").GetString());
        Assert.Equal("llama3.2", root.GetProperty("model").GetString());
        Assert.Equal("local-ollama", root.GetProperty("modelProvider").GetString());
        Assert.Equal("EXTERNAL", root.GetProperty("locality").GetString());
        Assert.Equal("akaai.test:8090", root.GetProperty("endpoint").GetString());
        Assert.Equal("http://console.akaai.test/models", root.GetProperty("consoleUrl").GetString());
        Assert.DoesNotContain(Secret, text); Assert.DoesNotContain("fake-token", text); Assert.DoesNotContain("idp.test", text);
        Assert.DoesNotContain(akaai.Paths, p => p.Contains("chat/completions", StringComparison.Ordinal));
        var calls = akaai.Paths.Count;
        await Read(client);
        Assert.Equal(calls, akaai.Paths.Count);
    }

    [PostgresFact] public async Task UnreachableAkaAiIsDownAndOverallDownWithoutFailingTheEndpoint()
    {
        using var client = TestToken.Client(App(new FakeAkaAi { Down = true }), Guid.NewGuid().ToString(), "MANAGER");
        var (root, components, _) = await Read(client);
        Assert.Equal("DOWN", root.GetProperty("overallStatus").GetString());
        Assert.Equal("UNREACHABLE", components["akaai-model"].GetProperty("detail").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("locality").ValueKind);
    }

    [PostgresFact] public async Task RejectedServiceAccountIsDownAndUpstreamBodyIsNotEchoed()
    {
        using var client = TestToken.Client(App(new FakeAkaAi { Models = HttpStatusCode.Forbidden }), Guid.NewGuid().ToString(), "SITE_ADMIN");
        var (_, components, text) = await Read(client);
        Assert.Equal("REJECTED", components["akaai-model"].GetProperty("detail").GetString());
        Assert.DoesNotContain("SECRET_UPSTREAM_BODY", text);
    }
}
