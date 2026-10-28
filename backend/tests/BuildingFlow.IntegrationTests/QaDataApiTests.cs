using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Test verisi: etiketli parti, UUID replay, dry-run ön izleme, birebir onay, bağımlılık sırası, demo/finans/parti dışı kayıt koruması.
public sealed class QaDataApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Root = "/api/v1/qa-center/data/batches";
    private WebApplicationFactory<Program>? enabled;
    private WebApplicationFactory<Program> Enabled => enabled ??= factory.WithWebHostBuilder(builder => builder.UseSetting("QaCenter:Enabled", "true"));
    private HttpClient Admin() => TestToken.Client(Enabled, Guid.NewGuid().ToString(), "SUPER_ADMIN");

    [PostgresFact] public async Task TestBatchIsTaggedPreviewedAndCleanedInDependencyOrder()
    {
        using var admin = Admin(); var request = Guid.NewGuid();
        var batch = await Json(await admin.PostAsJsonAsync(Root, new { kind = "TEST", requestId = request, runCode = "qa-20261028T190000Z-abcdef" }), HttpStatusCode.Created);
        Assert.Equal(("TEST", "ACTIVE", 35, "Development"), (batch.GetProperty("kind").GetString(), batch.GetProperty("status").GetString(), batch.GetProperty("recordCount").GetInt32(), batch.GetProperty("environment").GetString()));
        var id = batch.GetProperty("id").GetGuid(); var property = batch.GetProperty("propertyId").GetGuid();
        Assert.Equal(id, (await Json(await admin.PostAsJsonAsync(Root, new { kind = "TEST", requestId = request }), HttpStatusCode.OK)).GetProperty("id").GetGuid());
        await Code(await admin.PostAsJsonAsync(Root, new { kind = "DEMO", requestId = request }), HttpStatusCode.Conflict, "BF_QA_REQUEST_REUSED");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Root, new { kind = "PROD", requestId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Root, new { kind = "TEST", requestId = Guid.NewGuid(), blocks = 6 })).StatusCode);
        await With(async db =>
        {
            Assert.Equal((2, 8, 12, 12), (await db.Buildings.CountAsync(x => x.PropertyId == property), await db.Units.CountAsync(x => x.PropertyId == property),
                await db.People.CountAsync(x => x.PropertyId == property), await db.UnitOccupancies.CountAsync(x => x.PropertyId == property)));
            Assert.All(await db.People.Where(x => x.PropertyId == property).Select(x => x.Email).ToListAsync(), email => Assert.EndsWith("@example.test", email));
        });

        var preview = await Json(await admin.GetAsync($"{Root}/{id}/cleanup-preview"), HttpStatusCode.OK);
        Assert.True(preview.GetProperty("canClean").GetBoolean());
        Assert.Equal(["UnitOccupancy:12", "Person:12", "Unit:8", "Building:2", "Property:1"], preview.GetProperty("items").EnumerateArray().Select(x => $"{x.GetProperty("entityType").GetString()}:{x.GetProperty("count").GetInt32()}"));
        var version = preview.GetProperty("version").GetInt64(); var confirmation = preview.GetProperty("confirmation").GetString()!;
        await Code(await admin.PostAsJsonAsync($"{Root}/{id}/cleanup", new { version, confirmation = "temizle" }), HttpStatusCode.Conflict, "BF_QA_DATA_CONFIRM");
        await Code(await admin.PostAsJsonAsync($"{Root}/{id}/cleanup", new { version = version + 1, confirmation }), HttpStatusCode.Conflict, "BF_QA_DATA_STATE");
        await With(async db => Assert.Equal(1, await db.Properties.CountAsync(x => x.Id == property)));

        var cleaned = await Json(await admin.PostAsJsonAsync($"{Root}/{id}/cleanup", new { version, confirmation }), HttpStatusCode.OK);
        Assert.Equal("CLEANED", cleaned.GetProperty("status").GetString());
        await With(async db =>
        {
            Assert.Equal(0, await db.Properties.CountAsync(x => x.Id == property));
            Assert.True((await db.Properties.IgnoreQueryFilters().SingleAsync(x => x.Id == property)).IsDeleted);
            Assert.Equal(0, await db.UnitOccupancies.CountAsync(x => x.PropertyId == property));
            Assert.Equal(35, await db.QaDataRecords.CountAsync(x => x.BatchId == id));
        });
        await Code(await admin.PostAsJsonAsync($"{Root}/{id}/cleanup", new { version = cleaned.GetProperty("version").GetInt64(), confirmation }), HttpStatusCode.Conflict, "BF_QA_DATA_STATE");
    }

    [PostgresFact] public async Task DemoFinanceAndForeignRowsBlockCleanupAndNothingChanges()
    {
        using var admin = Admin();
        var demo = await Json(await admin.PostAsJsonAsync(Root, new { kind = "DEMO", requestId = Guid.NewGuid(), blocks = 1, unitsPerBlock = 2 }), HttpStatusCode.Created);
        var demoPreview = await Json(await admin.GetAsync($"{Root}/{demo.GetProperty("id").GetGuid()}/cleanup-preview"), HttpStatusCode.OK);
        Assert.False(demoPreview.GetProperty("canClean").GetBoolean()); Assert.Contains("DEMO_PROTECTED", demoPreview.GetProperty("blockers").EnumerateArray().Select(x => x.GetString()));
        await Code(await admin.PostAsJsonAsync($"{Root}/{demo.GetProperty("id").GetGuid()}/cleanup", new { version = demoPreview.GetProperty("version").GetInt64(), confirmation = demoPreview.GetProperty("confirmation").GetString() }), HttpStatusCode.Conflict, "BF_QA_DATA_PROTECTED");

        foreach (var blocker in new[] { "FINANCE_HISTORY", "UNREGISTERED_ROWS" })
        {
            var batch = await Json(await admin.PostAsJsonAsync(Root, new { kind = "TEST", requestId = Guid.NewGuid(), blocks = 1, unitsPerBlock = 1 }), HttpStatusCode.Created);
            var id = batch.GetProperty("id").GetGuid(); var property = batch.GetProperty("propertyId").GetGuid();
            await With(async db =>
            {
                db.Add(blocker == "FINANCE_HISTORY"
                    ? new Expense(property, Guid.NewGuid(), new string('a', 64), ExpenseCategory.CLEANING, 100m, DateOnly.FromDateTime(DateTime.UtcNow), "Sentetik gider", null)
                    : new Person(property, "Elle", "Eklenen", null, "manual@example.test"));
                await db.SaveChangesAsync();
            });
            var preview = await Json(await admin.GetAsync($"{Root}/{id}/cleanup-preview"), HttpStatusCode.OK);
            Assert.Equal([blocker], preview.GetProperty("blockers").EnumerateArray().Select(x => x.GetString()));
            await Code(await admin.PostAsJsonAsync($"{Root}/{id}/cleanup", new { version = preview.GetProperty("version").GetInt64(), confirmation = preview.GetProperty("confirmation").GetString() }), HttpStatusCode.Conflict, "BF_QA_DATA_BLOCKED");
            await With(async db => { Assert.Equal(1, await db.Properties.CountAsync(x => x.Id == property)); Assert.Equal(1, await db.Units.CountAsync(x => x.PropertyId == property)); });
        }
        using var manager = TestToken.Client(Enabled, Guid.NewGuid().ToString(), "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync(Root)).StatusCode);
        Assert.Contains(demo.GetProperty("id").GetGuid(), (await Json(await admin.GetAsync(Root), HttpStatusCode.OK)).EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
    }

    private async Task With(Func<BuildingFlowDbContext, Task> action)
    {
        using var scope = Enabled.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>());
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static async Task Code(HttpResponseMessage response, HttpStatusCode expected, string code) =>
        Assert.Equal(code, (await Json(response, expected)).GetProperty("code").GetString());
}
