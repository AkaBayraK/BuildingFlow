using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
namespace BuildingFlow.IntegrationTests;
// TEDARİKÇİ FAZ12 release gate: every supplier route, the full contract→work→invoice→report chain and read latency.
public sealed class SupplierReleaseTests(SiteApiTests.SiteApiFactory factory, ITestOutputHelper output) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private sealed record Fixture(Guid Tenant, Guid Property, Guid Supplier, Guid Category, Guid Area, Guid Request, string Manager, string Admin);
    private static readonly DateOnly Today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private static readonly Regex Parameter = new(@"\{([A-Za-z]+)(:[^}]*)?\}");
    private IReadOnlyList<(string Method, string Route)> SupplierRoutes() =>
        factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText is { } raw && (raw.StartsWith("/api/v2/tenants/{tenantId:guid}/supplier", StringComparison.Ordinal) || raw.StartsWith("/api/v2/supplier", StringComparison.Ordinal)))
            .SelectMany(e => (e.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()?.HttpMethods ?? []).Select(m => (m, e.RoutePattern.RawText!)))
            .Distinct().OrderBy(x => x.Item2).ThenBy(x => x.m).ToArray();
    private static string Fill(string route, IReadOnlyDictionary<string, string> values) => Parameter.Replace(route, m => values.TryGetValue(m.Groups[1].Value, out var v) ? v : Guid.NewGuid().ToString()).TrimEnd('/');
    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url) { Content = method is "GET" or "DELETE" ? null : new StringContent("{}", Encoding.UTF8, "application/json") };
    private async Task<Fixture> Seed(string suffix = "")
    {
        var t = new PersonTenant("Release fixture" + suffix); var p = new Property("Release property" + suffix, "City", "District", "Address"); var manager = Guid.NewGuid().ToString(); var admin = Guid.NewGuid().ToString();
        var supplier = new Supplier(t.Id, "Release supplier" + suffix, SupplierKind.COMPANY); supplier.ChangeStatus(SupplierStatus.ACTIVE, null); var category = new SupplierCategory(t.Id, "Elevator"); var service = new BuildingFlow.Domain.SupplierService(t.Id, supplier.Id, category.Id, "Repair"); var area = new SupplierAreaService(t.Id, supplier.Id, p.Id, service.Id);
        var request = new MobileRequest(Guid.NewGuid(), p.Id, null, "CENTER", manager, "Lift stopped");
        await factory.WithDb(async db =>
        {
            db.AddRange(t, p, supplier, category, service, area, request, new PersonPropertyScope(t.Id, p.Id), new SupplierManagementArea(t.Id, supplier.Id, p.Id));
            foreach (var user in new[] { manager, admin }) { var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user); db.AddRange(account, new PersonTenantMembership(t.Id, account.Id), new PropertyMembership(p.Id, user)); }
            await db.SaveChangesAsync();
        });
        return new(t.Id, p.Id, supplier.Id, category.Id, area.Id, request.Id, manager, admin);
    }
    private static async Task<T> Value<T>(HttpResponseMessage r) { Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.Method} {r.RequestMessage?.RequestUri?.AbsolutePath} → {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}"); return (await r.Content.ReadFromJsonAsync<T>())!; }

    [PostgresFact] public async Task EverySupplierRouteRejectsAnonymousAndNonManagementRoles()
    {
        var routes = SupplierRoutes(); Assert.True(routes.Count >= 60, $"Only {routes.Count} supplier routes discovered");
        using var anonymous = factory.CreateClient(); using var resident = TestToken.Client(factory, Guid.NewGuid().ToString(), "RESIDENT"); using var owner = TestToken.Client(factory, Guid.NewGuid().ToString(), "OWNER");
        var failures = new List<string>();
        foreach (var (method, route) in routes)
        {
            var url = Fill(route, new Dictionary<string, string>());
            using (var r = await anonymous.SendAsync(Request(method, url))) if (r.StatusCode != HttpStatusCode.Unauthorized) failures.Add($"anon {method} {route} {(int)r.StatusCode}");
            foreach (var client in new[] { resident, owner }) using (var r = await client.SendAsync(Request(method, url))) if (r.StatusCode != HttpStatusCode.Forbidden) failures.Add($"role {method} {route} {(int)r.StatusCode}");
        }
        output.WriteLine($"RBAC matrisi: {routes.Count} uç × 3 kimlik");
        Assert.Empty(failures);
    }

    [PostgresFact] public async Task AManagerOfAnotherTenantNeverReadsSupplierData()
    {
        var mine = await Seed(" A"); var other = await Seed(" B"); using var intruder = TestToken.Client(factory, mine.Manager, "SITE_ADMIN");
        var values = new Dictionary<string, string> { ["tenantId"] = other.Tenant.ToString(), ["supplierId"] = other.Supplier.ToString() };
        var query = $"propertyId={other.Property}&year={Today.Year}&from={Today.AddDays(-30):yyyy-MM-dd}&to={Today:yyyy-MM-dd}&source=REQUEST&page=1&pageSize=10";
        var leaks = new List<string>(); var checkedCount = 0;
        foreach (var (method, route) in SupplierRoutes().Where(r => r.Method == "GET" && r.Route.Contains("{tenantId", StringComparison.Ordinal)))
        {
            using var r = await intruder.GetAsync(Fill(route, values) + "?" + query); checkedCount++;
            if (r.IsSuccessStatusCode || (int)r.StatusCode >= 500) leaks.Add($"{route} {(int)r.StatusCode}");
        }
        using (var r = await intruder.PostAsJsonAsync($"/api/v2/tenants/{other.Tenant}/supplier-advice", new SupplierAdviceRequest("MATCH", other.Property, null, true, null))) if (r.StatusCode != HttpStatusCode.NotFound) leaks.Add("advice " + (int)r.StatusCode);
        output.WriteLine($"Tenant izolasyonu: {checkedCount} GET ucu yabancı tenant ile denendi");
        Assert.True(checkedCount >= 25); Assert.Empty(leaks);
    }

    [PostgresFact] public async Task ContractWorkOrderMaintenanceInvoiceAndReportsFlowEndToEnd()
    {
        var x = await Seed(); using var manager = TestToken.Client(factory, x.Manager, "MANAGER"); using var admin = TestToken.Client(factory, x.Admin, "SITE_ADMIN"); using var accountant = TestToken.Client(factory, x.Manager, "ACCOUNTANT");
        var root = $"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}";
        async Task<long> SupplierVersion() => (await Value<SupplierResponse>(await manager.GetAsync(root))).Version;
        var contract = await Value<SupplierContractMutation>(await manager.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts", new SupplierContractRequest(Guid.NewGuid(), await SupplierVersion(), 0, x.Property, x.Area, "Annual elevator", Today.AddDays(-10), Today.AddDays(20), "12000.00", 30, null, null)));
        contract = await Value<SupplierContractMutation>(await manager.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{contract.Record.Id}/activate", new SupplierContractTransition(Guid.NewGuid(), await SupplierVersion(), contract.Record.Version, null)));
        Assert.Equal("ACTIVE", contract.Record.Status);
        var order = await Value<SupplierWorkOrderMutation>(await manager.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", new SupplierWorkOrderCreate(Guid.NewGuid(), await SupplierVersion(), x.Property, x.Area, "REQUEST", x.Request, 1, "Lift repair", "Replace door sensor", DateTimeOffset.UtcNow.AddHours(6))));
        foreach (var status in new[] { "ASSIGNED", "ACCEPTED", "IN_PROGRESS", "COMPLETED" }) order = await Value<SupplierWorkOrderMutation>(await manager.PutAsJsonAsync($"{root}/work-orders/{order.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), order.Record.Version, status, "Field record " + status)));
        foreach (var status in new[] { "VERIFIED", "CLOSED" }) order = await Value<SupplierWorkOrderMutation>(await admin.PutAsJsonAsync($"{root}/work-orders/{order.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), order.Record.Version, status, "Manager approval")));
        var schedule = await Value<SupplierMaintenanceScheduleMutation>(await manager.PostAsJsonAsync($"{root}/maintenance-schedules", new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), await SupplierVersion(), new(x.Property, x.Area, contract.Record.Id, "Monthly inspection", null, "INSPECTION", "WEEKLY", Today.AddDays(-7), Today.AddDays(14), 3, "KEEP"))));
        var visit = (await Value<PagedRows<SupplierMaintenanceOccurrenceResponse>>(await manager.GetAsync($"{root}/maintenance-occurrences?scheduleId={schedule.Record.Id}"))).Items[0];
        await Value<SupplierMaintenanceOccurrenceMutation>(await manager.PutAsJsonAsync($"{root}/maintenance-occurrences/{visit.Id}/complete", new SupplierMaintenanceComplete(Guid.NewGuid(), visit.Version, Today, "Inspection signed", null)));
        var invoice = await Value<SupplierInvoiceMutation>(await accountant.PostAsJsonAsync($"{root}/invoices", new SupplierInvoiceCreate(Guid.NewGuid(), x.Property, "E2E-2026-1", Today, Today.AddDays(30), "1000.00", "20.00", "MAINTENANCE", "Door sensor", order.Record.Id, contract.Record.Id, null)));
        invoice = await Value<SupplierInvoiceMutation>(await admin.PutAsJsonAsync($"{root}/invoices/{invoice.Record.Id}/approve", new SupplierInvoiceAction(Guid.NewGuid(), invoice.Record.Version, null, null)));
        invoice = await Value<SupplierInvoiceMutation>(await accountant.PutAsJsonAsync($"{root}/invoices/{invoice.Record.Id}/pay", new SupplierInvoiceAction(Guid.NewGuid(), invoice.Record.Version, Today, "EFT-E2E")));
        invoice = await Value<SupplierInvoiceMutation>(await accountant.PutAsJsonAsync($"{root}/invoices/{invoice.Record.Id}/reconcile", new SupplierInvoiceAction(Guid.NewGuid(), invoice.Record.Version, Today, "Bank statement matched")));
        Assert.Equal("RECONCILED", invoice.Record.Status);
        var expenses = await accountant.GetStringAsync($"/api/v1/properties/{x.Property}/expenses?page=1&pageSize=10"); Assert.Contains("E2E-2026-1", expenses); Assert.Contains("1200.00", expenses);
        var row = Assert.Single((await Value<SupplierPerformanceReport>(await manager.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-performance?from={Today.AddDays(-30):yyyy-MM-dd}&to={Today:yyyy-MM-dd}"))).Items);
        Assert.Equal((1, 1, 1, 0, "1200.00", 1), (row.CompletedWorkOrders, row.SlaOnTimeCount, row.MaintenanceSampleCount, row.MaintenanceOnTimeCount, row.InvoicedTotal, row.InvoiceCount)); Assert.Null(row.Score);
        var alerts = await Value<SupplierAlertPage>(await manager.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-alerts?pageSize=50"));
        Assert.Contains(alerts.Items, a => a.Kind == "CONTRACT_EXPIRY" && a.RecordId == contract.Record.Id); Assert.Contains(alerts.Items, a => a.Kind == "MAINTENANCE_DUE" || a.Kind == "MAINTENANCE_OVERDUE");
        var advice = await Value<SupplierAdviceResponse>(await manager.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-advice", new SupplierAdviceRequest("MATCH", x.Property, x.Category, true, null)));
        Assert.Equal(x.Supplier, Assert.Single(advice.Items).SupplierId);
        var history = await Value<PagedRows<SupplierContractHistory>>(await manager.GetAsync($"{root}/invoices/{invoice.Record.Id}/history")); Assert.Equal(4, history.TotalCount);
        output.WriteLine("Uçtan uca: sözleşme ACTIVE → iş emri CLOSED → bakım tamam → fatura RECONCILED → gider kaydı → performans/uyarı/öneri");
    }

    [PostgresFact] public async Task SupplierReadsStayWithinP95UnderConcurrentLoad()
    {
        var x = await Seed(); using var manager = TestToken.Client(factory, x.Manager, "MANAGER");
        await factory.WithDb(async db =>
        {
            var tenant = x.Tenant; var category = await db.Set<SupplierCategory>().SingleAsync(c => c.Id == x.Category);
            for (var i = 0; i < 40; i++)
            {
                var s = new Supplier(tenant, $"Load supplier {i:D3}", SupplierKind.COMPANY); s.ChangeStatus(SupplierStatus.ACTIVE, null); var service = new BuildingFlow.Domain.SupplierService(tenant, s.Id, category.Id, "Repair"); var area = new SupplierAreaService(tenant, s.Id, x.Property, service.Id);
                db.AddRange(s, service, area, new SupplierManagementArea(tenant, s.Id, x.Property));
                for (var j = 0; j < 5; j++) { var r = new MobileRequest(Guid.NewGuid(), x.Property, null, "CENTER", x.Manager, $"Load request {i}-{j}"); db.Add(r); db.Add(new SupplierWorkOrder(tenant, s.Id, x.Property, area.Id, SupplierWorkOrderSource.REQUEST, r.Id, 1, r.Description, "Load order", "Load scope", DateTimeOffset.UtcNow.AddDays(3))); }
            }
            await db.SaveChangesAsync();
        });
        string[] paths = [$"/api/v2/tenants/{x.Tenant}/suppliers?page=1&pageSize=20", $"/api/v2/tenants/{x.Tenant}/supplier-work-orders?propertyId={x.Property}&page=1&pageSize=20", $"/api/v2/tenants/{x.Tenant}/supplier-alerts?page=1&pageSize=20",
            $"/api/v2/tenants/{x.Tenant}/supplier-performance?from={Today.AddDays(-90):yyyy-MM-dd}&to={Today:yyyy-MM-dd}", $"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices?page=1&pageSize=20"];
        foreach (var path in paths) Assert.True((await manager.GetAsync(path)).IsSuccessStatusCode, path);
        var durations = new List<double>(); var failures = 0; using var gate = new SemaphoreSlim(10);
        await Task.WhenAll(Enumerable.Range(0, 100).Select(async i =>
        {
            await gate.WaitAsync();
            try { var w = Stopwatch.StartNew(); using var r = await manager.GetAsync(paths[i % paths.Length]); w.Stop(); lock (durations) { durations.Add(w.Elapsed.TotalMilliseconds); if (!r.IsSuccessStatusCode) failures++; } }
            finally { gate.Release(); }
        }));
        durations.Sort(); var p95 = durations[(int)Math.Ceiling(durations.Count * .95) - 1];
        output.WriteLine($"Tedarikçi performansı: 100 istek, eşzamanlılık 10, 41 tedarikçi/200 iş emri, p50 {durations[50]:F0} ms, p95 {p95:F0} ms, max {durations[^1]:F0} ms, hata {failures}");
        Assert.Equal(0, failures); Assert.True(p95 < 2000, $"p95 {p95:F0} ms");
    }
}
