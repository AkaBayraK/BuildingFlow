using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;
namespace BuildingFlow.IntegrationTests;
// QA FAZ 12/13 performans kapısı: sentetik site üzerinde eşzamanlı kişi/ilişki/finans okumalarında hata yok ve p95 sınırda.
public sealed class QaPerformanceTests(SiteApiTests.SiteApiFactory factory, ITestOutputHelper output) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const int Requests = 100;
    private const int Concurrency = 10;
    // Generous for a shared developer machine; the measured value is written to the test output as evidence.
    private static readonly TimeSpan P95Limit = TimeSpan.FromSeconds(2);

    [PostgresFact] public async Task ConcurrentManagementAndFinanceReadsStayWithinP95()
    {
        var app = factory.WithWebHostBuilder(builder => builder.UseSetting("QaCenter:Enabled", "true"));
        using var admin = TestToken.Client(app, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var batch = await admin.PostAsJsonAsync("/api/v1/qa-center/data/batches", new { kind = "TEST", requestId = Guid.NewGuid(), blocks = 5, unitsPerBlock = 20 });
        Assert.Equal(HttpStatusCode.Created, batch.StatusCode);
        var property = JsonDocument.Parse(await batch.Content.ReadAsStringAsync()).RootElement.GetProperty("propertyId").GetGuid();
        string[] paths = [$"people?page=1&pageSize=20", "occupancies?page=1&pageSize=20", "dashboard", $"finance-summary?period={DateTime.UtcNow:yyyy-MM}", "payment-ledger?page=1&pageSize=20"];
        foreach (var path in paths) Assert.True((await admin.GetAsync($"/api/v1/properties/{property}/{path}")).StatusCode == HttpStatusCode.OK, path);

        var durations = new List<double>(); var failures = 0; using var gate = new SemaphoreSlim(Concurrency);
        await Task.WhenAll(Enumerable.Range(0, Requests).Select(async index =>
        {
            await gate.WaitAsync();
            try
            {
                var watch = Stopwatch.StartNew();
                using var response = await admin.GetAsync($"/api/v1/properties/{property}/{paths[index % paths.Length]}");
                watch.Stop();
                lock (durations) { durations.Add(watch.Elapsed.TotalMilliseconds); if (!response.IsSuccessStatusCode) failures++; }
            }
            finally { gate.Release(); }
        }));
        durations.Sort();
        var p95 = durations[(int)Math.Ceiling(durations.Count * 0.95) - 1];
        output.WriteLine($"QA performans: {Requests} istek, eşzamanlılık {Concurrency}, p50 {durations[durations.Count / 2]:F0} ms, p95 {p95:F0} ms, max {durations[^1]:F0} ms, hata {failures}");
        Assert.Equal(0, failures);
        Assert.True(p95 < P95Limit.TotalMilliseconds, $"p95 {p95:F0} ms");
    }
}
