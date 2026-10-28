using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BuildingFlow.Api;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Test Merkezi: yalnız SUPER_ADMIN, kapalıyken route yok; UUID replay, tek aktif koşum, iptal, zaman aşımı ve yeniden deneme.
public sealed class QaCenterApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Root = "/api/v1/qa-center";
    private static readonly SemaphoreSlim Gate = new(0);
    private WebApplicationFactory<Program>? enabled;
    private WebApplicationFactory<Program> Enabled => enabled ??= factory.WithWebHostBuilder(builder =>
    {
        builder.UseSetting("QaCenter:Enabled", "true");
        builder.UseSetting("QaCenter:PollMilliseconds", "100");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IQaScenario>(new FakeScenario("test.fail", 30, async token => { await Task.Yield(); return QaStepStatus.FAILED; }));
            services.AddSingleton<IQaScenario>(new FakeScenario("test.slow", 1, async token => { await Task.Delay(Timeout.Infinite, token); return QaStepStatus.PASSED; }));
            services.AddSingleton<IQaScenario>(new FakeScenario("test.gate", 60, async token => { await Gate.WaitAsync(token); return QaStepStatus.PASSED; }));
        });
    });

    [PostgresFact] public async Task OnlySuperAdminReachesTheCentre()
    {
        using var anonymous = Enabled.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root + "/overview")).StatusCode);
        foreach (var role in new[] { "SITE_ADMIN", "MANAGER", "ACCOUNTANT" })
        {
            using var other = TestToken.Client(Enabled, Guid.NewGuid().ToString(), role);
            Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync(Root + "/overview")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "db.migrations", requestId = Guid.NewGuid() })).StatusCode);
        }
        using var admin = Admin(); var overview = await Json(await admin.GetAsync(Root + "/overview"), HttpStatusCode.OK);
        Assert.True(overview.GetProperty("endpointCount").GetInt32() > 0);
        Assert.Contains("db.migrations", (await Json(await admin.GetAsync(Root + "/scenarios"), HttpStatusCode.OK)).EnumerateArray().Select(x => x.GetProperty("key").GetString()));
    }

    [PostgresFact] public async Task DisabledCentreHasNoRoutes()
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Root + "/overview")).StatusCode);
    }

    [PostgresFact] public async Task ReadOnlyScenarioPassesWithStepsAndReplaysByRequestId()
    {
        using var admin = Admin(); var requestId = Guid.NewGuid();
        var created = await Json(await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "db.migrations", requestId }), HttpStatusCode.Created);
        var id = created.GetProperty("id").GetGuid(); Assert.Matches("^qa-[0-9]{8}T[0-9]{6}Z-[0-9a-f]{6}$", created.GetProperty("code").GetString());
        var replay = await Json(await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "db.migrations", requestId }), HttpStatusCode.OK);
        Assert.Equal(id, replay.GetProperty("id").GetGuid());
        await Code(await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "api.authorization", requestId }), HttpStatusCode.Conflict, "BF_QA_REQUEST_REUSED");
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "rm -rf", requestId = Guid.NewGuid() })).StatusCode);
        var detail = await WaitFor(admin, id, "PASSED");
        var steps = detail.GetProperty("steps").EnumerateArray().ToArray();
        Assert.Equal(["database.connect", "database.pendingMigrations"], steps.Select(x => x.GetProperty("stepKey").GetString()));
        Assert.All(steps, step => Assert.Equal("PASSED", step.GetProperty("status").GetString()));
        Assert.Equal("0", steps[1].GetProperty("actual").GetString());
        var page = await Json(await admin.GetAsync(Root + "/runs?status=PASSED&pageSize=5"), HttpStatusCode.OK);
        Assert.Contains(id, page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync(Root + "/runs?status=DONE")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync(Root + "/runs?pageSize=101")).StatusCode);
    }

    [PostgresFact] public async Task AuthorizationInventoryScenarioPasses()
    {
        using var admin = Admin();
        var created = await Json(await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "api.authorization", requestId = Guid.NewGuid() }), HttpStatusCode.Created);
        var steps = (await WaitFor(admin, created.GetProperty("id").GetGuid(), "PASSED")).GetProperty("steps").EnumerateArray().ToArray();
        Assert.Equal("0", steps.Single(x => x.GetProperty("stepKey").GetString() == "endpoints.authorization").GetProperty("actual").GetString());
    }

    [PostgresFact] public async Task FailingStepFailsTheRun()
    {
        using var admin = Admin();
        var created = await Json(await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "test.fail", requestId = Guid.NewGuid() }), HttpStatusCode.Created);
        var run = (await WaitFor(admin, created.GetProperty("id").GetGuid(), "FAILED")).GetProperty("run");
        Assert.Equal("STEP_FAILED", run.GetProperty("resultCode").GetString()); Assert.Equal(1, run.GetProperty("failedSteps").GetInt32());
    }

    [PostgresFact] public async Task TimedOutRunCanBeRetriedOnceWithItsVersion()
    {
        using var admin = Admin();
        var created = await Json(await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "test.slow", requestId = Guid.NewGuid() }), HttpStatusCode.Created);
        var id = created.GetProperty("id").GetGuid();
        var run = (await WaitFor(admin, id, "TIMED_OUT")).GetProperty("run");
        Assert.Equal("TIMEOUT", run.GetProperty("resultCode").GetString());
        var version = run.GetProperty("version").GetInt64(); var requestId = Guid.NewGuid();
        await Code(await admin.PostAsJsonAsync($"{Root}/runs/{id}/retry", new { requestId, version = version - 1 }), HttpStatusCode.Conflict, "BF_QA_RUN_STATE");
        var retry = await Json(await admin.PostAsJsonAsync($"{Root}/runs/{id}/retry", new { requestId, version }), HttpStatusCode.Created);
        Assert.Equal(2, retry.GetProperty("attempt").GetInt32()); Assert.Equal(id, retry.GetProperty("retryOfId").GetGuid());
        Assert.Equal(retry.GetProperty("id").GetGuid(), (await Json(await admin.PostAsJsonAsync($"{Root}/runs/{id}/retry", new { requestId, version }), HttpStatusCode.OK)).GetProperty("id").GetGuid());
        await WaitFor(admin, retry.GetProperty("id").GetGuid(), "TIMED_OUT");
    }

    [PostgresFact] public async Task RunningRunBlocksDuplicatesAndStopsAfterCancel()
    {
        using var admin = Admin();
        var created = await Json(await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "test.gate", requestId = Guid.NewGuid() }), HttpStatusCode.Created);
        var id = created.GetProperty("id").GetGuid();
        var running = (await WaitFor(admin, id, "RUNNING")).GetProperty("run");
        await Code(await admin.PostAsJsonAsync(Root + "/runs", new { scenarioKey = "test.gate", requestId = Guid.NewGuid() }), HttpStatusCode.Conflict, "BF_QA_RUN_ACTIVE");
        var version = running.GetProperty("version").GetInt64();
        await Code(await admin.PutAsJsonAsync($"{Root}/runs/{id}/cancel", new { version = version + 7 }), HttpStatusCode.Conflict, "BF_QA_RUN_STATE");
        var cancelling = await Json(await admin.PutAsJsonAsync($"{Root}/runs/{id}/cancel", new { version }), HttpStatusCode.OK);
        Assert.True(cancelling.GetProperty("cancelRequested").GetBoolean());
        Gate.Release();
        var detail = await WaitFor(admin, id, "CANCELLED");
        Assert.Equal(1, detail.GetProperty("steps").GetArrayLength());
        var finished = detail.GetProperty("run").GetProperty("version").GetInt64();
        await Code(await admin.PutAsJsonAsync($"{Root}/runs/{id}/cancel", new { version = finished }), HttpStatusCode.Conflict, "BF_QA_RUN_STATE");
    }

    private HttpClient Admin() => TestToken.Client(Enabled, Guid.NewGuid().ToString(), "SUPER_ADMIN");

    private static async Task<JsonElement> WaitFor(HttpClient client, Guid id, string status)
    {
        for (var attempt = 0; attempt < 150; attempt++)
        {
            var detail = await Json(await client.GetAsync($"{Root}/runs/{id}"), HttpStatusCode.OK);
            if (detail.GetProperty("run").GetProperty("status").GetString() == status) return detail;
            await Task.Delay(100);
        }
        throw new TimeoutException($"Run {id} did not reach {status}.");
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static async Task Code(HttpResponseMessage response, HttpStatusCode expected, string code) =>
        Assert.Equal(code, (await Json(response, expected)).GetProperty("code").GetString());

    private sealed class FakeScenario(string key, int timeoutSeconds, Func<CancellationToken, Task<QaStepStatus>> step) : IQaScenario
    {
        public string Key => key;
        public int TimeoutSeconds => timeoutSeconds;
        public async IAsyncEnumerable<QaStepResult> RunAsync(IServiceProvider services, [EnumeratorCancellation] CancellationToken token)
        {
            var status = await step(token);
            yield return new QaStepResult("fake.step", status, "PASSED", status.ToString(), 1);
        }
    }
}
