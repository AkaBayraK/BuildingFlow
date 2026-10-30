using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierInsightTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private sealed record Fixture(Guid Tenant, Guid Supplier, Guid Property, Guid Area, Guid Request, Guid Contract, string User);
    private static readonly DateOnly Today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private async Task<Fixture> Seed(string name = "Insight supplier")
    {
        var t = new PersonTenant("Insight fixture"); var p = new Property("Insight property", "City", "District", "Address"); var user = Guid.NewGuid().ToString(); var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user);
        var supplier = new Supplier(t.Id, name, SupplierKind.COMPANY); supplier.ChangeStatus(SupplierStatus.ACTIVE, null); var category = new SupplierCategory(t.Id, "Elevator"); var service = new BuildingFlow.Domain.SupplierService(t.Id, supplier.Id, category.Id, "Repair"); var area = new SupplierAreaService(t.Id, supplier.Id, p.Id, service.Id);
        var contract = new SupplierContract(t.Id, supplier.Id, p.Id, area.Id, "Annual elevator", Today.AddDays(-300), Today.AddDays(10), 9000m, 30); contract.Activate();
        var request = new MobileRequest(Guid.NewGuid(), p.Id, null, "CENTER", user, "Lift stopped");
        await factory.WithDb(async db => { db.AddRange(t, p, account, supplier, category, service, area, contract, request, new PersonTenantMembership(t.Id, account.Id), new PersonPropertyScope(t.Id, p.Id), new PropertyMembership(p.Id, user), new SupplierManagementArea(t.Id, supplier.Id, p.Id)); await db.SaveChangesAsync(); });
        return new(t.Id, supplier.Id, p.Id, area.Id, request.Id, contract.Id, user);
    }
    private static async Task<T> Value<T>(HttpResponseMessage r) { Assert.Equal(HttpStatusCode.OK, r.StatusCode); return (await r.Content.ReadFromJsonAsync<T>())!; }
    private static async Task<SupplierWorkOrderMutation> Advance(HttpClient c, Fixture x, SupplierWorkOrderMutation order, params string[] statuses)
    {
        foreach (var status in statuses) order = await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{order.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), order.Record.Version, status, "Recorded " + status)));
        return order;
    }

    [PostgresFact] public async Task AlertsCoverContractsOverdueWorkAndMaintenanceAndFollowPreferences()
    {
        var x = await Seed(); var foreign = await Seed("Foreign supplier"); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var other = TestToken.Client(factory, foreign.User, "MANAGER");
        var order = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", new SupplierWorkOrderCreate(Guid.NewGuid(), 1, x.Property, x.Area, "REQUEST", x.Request, 1, "Lift repair", "Repair scope", DateTimeOffset.UtcNow.AddSeconds(4))));
        await Advance(c, x, order, "ASSIGNED"); await Task.Delay(4500);
        var parent = await Value<SupplierResponse>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}"));
        await Value<SupplierMaintenanceScheduleMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), parent.Version, new(x.Property, x.Area, null, "Weekly check", null, "INSPECTION", "WEEKLY", Today.AddDays(-7), Today.AddDays(21), 3, "KEEP"))));
        var alerts = await Value<SupplierAlertPage>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-alerts?pageSize=50"));
        Assert.Equal(new[] { "MAINTENANCE_OVERDUE", "MAINTENANCE_DUE", "WORK_ORDER_OVERDUE", "CONTRACT_EXPIRY" }, alerts.Items.Select(a => a.Kind).Distinct());
        Assert.Equal(-7, alerts.Items[0].Days); Assert.Equal(10, alerts.Items.Single(a => a.Kind == "CONTRACT_EXPIRY").Days); Assert.Equal(1, alerts.Items.Count(a => a.Kind == "MAINTENANCE_DUE"));
        Assert.All(alerts.Items, a => Assert.Equal(x.Property, a.PropertyId)); Assert.False(alerts.Truncated); Assert.True(alerts.Preference.WorkOrderOverdue);
        Assert.Single((await Value<SupplierAlertPage>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-alerts?kind=CONTRACT_EXPIRY"))).Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-alerts?kind=INVOICE")).StatusCode);
        var preference = await Value<SupplierAlertPreferenceResponse>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-alert-preferences")); Assert.Equal(0, preference.Version);
        var saved = await Value<SupplierAlertPreferenceResponse>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-alert-preferences", new SupplierAlertPreferenceRequest(true, false, true, 0)));
        Assert.False(saved.WorkOrderOverdue); Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-alert-preferences", new SupplierAlertPreferenceRequest(false, false, false, 0))).StatusCode);
        Assert.DoesNotContain((await Value<SupplierAlertPage>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-alerts?pageSize=50"))).Items, a => a.Kind == "WORK_ORDER_OVERDUE");
        Assert.Empty((await Value<SupplierAlertPage>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-alerts?kind=WORK_ORDER_OVERDUE"))).Items);
        Assert.True((await Value<SupplierAlertPreferenceResponse>(await other.GetAsync($"/api/v2/tenants/{foreign.Tenant}/supplier-alert-preferences"))).WorkOrderOverdue);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v2/tenants/{foreign.Tenant}/supplier-alerts")).StatusCode);
        Assert.All((await Value<SupplierAlertPage>(await other.GetAsync($"/api/v2/tenants/{foreign.Tenant}/supplier-alerts"))).Items, a => Assert.Equal(foreign.Property, a.PropertyId));
    }

    [PostgresFact] public async Task PerformanceShowsSampleCountsCostAndScoresOnlyWithEnoughEvidence()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT");
        var order = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", new SupplierWorkOrderCreate(Guid.NewGuid(), 1, x.Property, x.Area, "REQUEST", x.Request, 1, "Lift repair", "Repair scope", DateTimeOffset.UtcNow.AddHours(4))));
        await Advance(c, x, order, "ASSIGNED", "ACCEPTED", "IN_PROGRESS", "COMPLETED");
        var url = $"/api/v2/tenants/{x.Tenant}/supplier-performance?from={Today.AddDays(-30):yyyy-MM-dd}&to={Today:yyyy-MM-dd}";
        var first = Assert.Single((await Value<SupplierPerformanceReport>(await c.GetAsync(url))).Items);
        Assert.Equal(1, first.CompletedWorkOrders); Assert.Equal(1, first.SlaSampleCount); Assert.Equal(1, first.SlaOnTimeCount); Assert.Equal("100.0", first.OnTimeRate); Assert.Null(first.Score); Assert.Equal(1, first.ScoreSampleCount); Assert.NotNull(first.AverageCompletionHours);
        var parent = await Value<SupplierResponse>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}"));
        var plan = await Value<SupplierMaintenanceScheduleMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), parent.Version, new(x.Property, x.Area, null, "Weekly check", null, "INSPECTION", "WEEKLY", Today.AddDays(-7), Today, 3, "KEEP"))));
        var visits = (await Value<PagedRows<SupplierMaintenanceOccurrenceResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences?scheduleId={plan.Record.Id}"))).Items;
        foreach (var visit in visits) await Value<SupplierMaintenanceOccurrenceMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{visit.Id}/complete", new SupplierMaintenanceComplete(Guid.NewGuid(), visit.Version, Today, "Signed report", null)));
        await factory.WithDb(async db =>
        {
            var invoice = new SupplierInvoice(x.Tenant, x.Supplier, x.Property, "PERF-1", Today.AddDays(-2), Today.AddDays(20), 1000m, 20m, ExpenseCategory.MAINTENANCE, "Repair", null, null, null, "issuer", "recorder", Today);
            var expense = new Expense(x.Property, invoice.Id, new string('A', 64), ExpenseCategory.MAINTENANCE, 1200m, Today.AddDays(-2), "Repair", "PERF-1"); invoice.Approve(expense.Id);
            var ignored = new SupplierInvoice(x.Tenant, x.Supplier, x.Property, "PERF-2", Today.AddDays(-2), Today.AddDays(20), 50m, 0m, ExpenseCategory.MAINTENANCE, "Unapproved", null, null, null, "issuer", "recorder", Today);
            db.AddRange(expense, invoice, ignored); await db.SaveChangesAsync();
        });
        var report = await Value<SupplierPerformanceReport>(await c.GetAsync(url + $"&propertyId={x.Property}"));
        var row = Assert.Single(report.Items); Assert.Equal(3, report.MinimumSample);
        Assert.Equal(2, row.MaintenanceSampleCount); Assert.Equal(1, row.MaintenanceOnTimeCount); Assert.Equal(3, row.ScoreSampleCount); Assert.Equal("66.7", row.Score); Assert.Equal("66.7", row.OnTimeRate);
        Assert.Equal(1, row.InvoiceCount); Assert.Equal("1200.00", row.InvoicedTotal);
        Assert.Empty((await Value<SupplierPerformanceReport>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-performance?from={Today.AddDays(-400):yyyy-MM-dd}&to={Today.AddDays(-200):yyyy-MM-dd}"))).Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-performance?from={Today.AddDays(-400):yyyy-MM-dd}&to={Today:yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-performance?from={Today:yyyy-MM-dd}&to={Today.AddDays(-1):yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(url + $"&propertyId={Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync(url)).StatusCode);
    }
}
