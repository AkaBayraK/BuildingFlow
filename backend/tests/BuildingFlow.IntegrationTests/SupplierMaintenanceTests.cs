using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierMaintenanceTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private sealed record Fixture(Guid Tenant, Guid Supplier, Guid Property, Guid Area, Guid Contract, string User);
    private static readonly DateOnly Today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private static DateOnly NextMonday(int weeks) => Today.AddDays(((int)DayOfWeek.Monday - (int)Today.DayOfWeek + 7) % 7 + 7 * weeks);
    private async Task<Fixture> Seed()
    {
        var t = new PersonTenant("Maintenance fixture"); var p = new Property("Maintenance property", "City", "District", "Address"); var user = Guid.NewGuid().ToString(); var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user);
        var supplier = new Supplier(t.Id, "Maintenance supplier", SupplierKind.COMPANY); supplier.ChangeStatus(SupplierStatus.ACTIVE, null); var category = new SupplierCategory(t.Id, "Elevator"); var service = new BuildingFlow.Domain.SupplierService(t.Id, supplier.Id, category.Id, "Inspection"); var area = new SupplierAreaService(t.Id, supplier.Id, p.Id, service.Id);
        var contract = new SupplierContract(t.Id, supplier.Id, p.Id, area.Id, "Annual maintenance", Today.AddDays(-60), Today.AddDays(300), 12000m, 30); contract.Activate();
        await factory.WithDb(async db => { db.AddRange(t, p, account, supplier, category, service, area, contract, new PersonTenantMembership(t.Id, account.Id), new PersonPropertyScope(t.Id, p.Id), new PropertyMembership(p.Id, user), new SupplierManagementArea(t.Id, supplier.Id, p.Id)); await db.SaveChangesAsync(); });
        return new(t.Id, supplier.Id, p.Id, area.Id, contract.Id, user);
    }
    private static SupplierMaintenancePlanRequest Plan(Fixture x, DateOnly start, DateOnly end, string frequency = "WEEKLY", Guid? contract = null, string rule = "NEXT_WORKING_DAY") =>
        new(x.Property, x.Area, contract, "Elevator inspection", "Check cables\nSign report", "INSPECTION", frequency, start, end, 7, rule);
    private static async Task<T> Value<T>(HttpResponseMessage r) { Assert.Equal(HttpStatusCode.OK, r.StatusCode); return (await r.Content.ReadFromJsonAsync<T>())!; }

    [PostgresFact] public async Task HolidayShiftContractBoundsConflictsReplayAndCalendarAreEnforced()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); var monday = NextMonday(1);
        var holiday = await Value<SupplierHolidayMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-holidays", new SupplierHolidayRequest(monday, "Public holiday")));
        Assert.True((await Value<SupplierHolidayMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-holidays", new SupplierHolidayRequest(monday, " Public holiday ")))).Replayed);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/supplier-holidays", new SupplierHolidayRequest(monday, "Other name"))).StatusCode);
        Assert.Equal(monday, Assert.Single((await Value<PagedRows<SupplierHolidayResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-holidays?year={monday.Year}"))).Items).Date);
        var plan = Plan(x, monday, monday.AddDays(27), contract: x.Contract);
        var preview = await Value<SupplierMaintenancePreview>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/preview", plan));
        Assert.Equal(4, preview.Dates.Count); Assert.Equal(0, preview.ConflictCount); Assert.True(preview.Dates[0].Shifted); Assert.Equal(monday.AddDays(1), preview.Dates[0].PlannedDate); Assert.False(preview.Dates[1].Shifted);
        var command = new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), 1, plan);
        var created = await Value<SupplierMaintenanceScheduleMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", command));
        Assert.Equal(4, created.Record.PlannedCount); Assert.Equal("Annual maintenance", created.Record.ContractTitle); Assert.Equal("ACTIVE", created.Record.Status);
        var replay = await Value<SupplierMaintenanceScheduleMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", command)); Assert.True(replay.Replayed); Assert.Equal(created.Record.Id, replay.Record.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", command with { Plan = plan with { ReminderDays = 3 } })).StatusCode);
        var clash = Plan(x, monday.AddDays(1), monday.AddDays(20), "MONTHLY");
        Assert.Equal(1, (await Value<SupplierMaintenancePreview>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/preview", clash))).ConflictCount);
        var conflict = await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), created.Supplier.Version, clash));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode); Assert.Contains("BF_SUPPLIER_MAINTENANCE_CONFLICT", await conflict.Content.ReadAsStringAsync());
        var outside = await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), created.Supplier.Version, Plan(x, monday.AddDays(2), Today.AddDays(400), "QUARTERLY", x.Contract)));
        Assert.Equal(HttpStatusCode.Conflict, outside.StatusCode); Assert.Contains("BF_SUPPLIER_MAINTENANCE_STATE", await outside.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/preview", plan with { Frequency = "DAILY" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/preview", plan with { EndDate = monday.AddDays(800) })).StatusCode);
        var calendar = await Value<PagedRows<SupplierMaintenanceOccurrenceResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences?from={monday:yyyy-MM-dd}&to={monday.AddDays(13):yyyy-MM-dd}"));
        Assert.Equal(2, calendar.TotalCount); Assert.Equal(monday.AddDays(1), calendar.Items[0].PlannedDate); Assert.Equal(monday, calendar.Items[0].NominalDate);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences?from={monday:yyyy-MM-dd}&to={monday.AddDays(400):yyyy-MM-dd}")).StatusCode);
        var reminders = await Value<PagedRows<SupplierMaintenanceOccurrenceResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences?reminderDue=true"));
        Assert.All(reminders.Items, r => Assert.True(r.IsReminderDue)); Assert.True(reminders.TotalCount <= 2);
        Assert.Equal(created.Record.Id, Assert.Single((await Value<PagedRows<SupplierMaintenanceScheduleResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules"))).Items).Id);
        await factory.WithDb(async db => { Assert.Equal(4, await db.MaintenancePlans.CountAsync(m => m.PropertyId == x.Property && m.Status == MaintenanceStatus.PLANNED && m.Responsible == "Maintenance supplier")); });
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v2/tenants/{x.Tenant}/supplier-holidays/{holiday.Record.Id}?version={holiday.Record.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync($"/api/v2/tenants/{x.Tenant}/supplier-holidays/{holiday.Record.Id}?version={holiday.Record.Version}")).StatusCode);
        var detail = await Value<SupplierMaintenanceScheduleResponse>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/{created.Record.Id}")); Assert.Equal(4, detail.PlannedCount); Assert.Equal(x.Contract, detail.ContractId);
    }

    [PostgresFact] public async Task CompletionEvidenceDelayWorkOrderLinkSkipEndAndHistoryAreProtected()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var admin = TestToken.Client(factory, x.User, "SITE_ADMIN");
        var start = Today.AddDays(-21); var created = await Value<SupplierMaintenanceScheduleMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), 1, Plan(x, start, Today.AddDays(35), rule: "KEEP"))));
        Assert.Equal(3, created.Record.OverdueCount);
        var rows = (await Value<PagedRows<SupplierMaintenanceOccurrenceResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences?scheduleId={created.Record.Id}&pageSize=20"))).Items;
        Assert.Equal(9, rows.Count); Assert.True(rows[0].IsOverdue); Assert.True(rows[0].IsReminderDue);
        var first = rows[0]; var done = await Value<SupplierMaintenanceOccurrenceMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{first.Id}/complete", new SupplierMaintenanceComplete(Guid.NewGuid(), first.Version, Today, "Inspection report signed", null)));
        Assert.Equal("COMPLETED", done.Record.Status); Assert.Equal(21, done.Record.DelayDays); Assert.False(done.Record.IsOverdue);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{rows[1].Id}/complete", new SupplierMaintenanceComplete(Guid.NewGuid(), rows[1].Version, Today.AddDays(1), "Future", null))).StatusCode);
        var racing = await Task.WhenAll(new[] { "First report", "Second report" }.Select(note => c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{rows[1].Id}/complete", new SupplierMaintenanceComplete(Guid.NewGuid(), rows[1].Version, Today, note, null))));
        Assert.Single(racing, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(racing, r => r.StatusCode == HttpStatusCode.Conflict);

        var target = rows[2]; var due = DateTimeOffset.UtcNow.AddDays(3); var parent = await Value<SupplierResponse>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}"));
        var order = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", new SupplierWorkOrderCreate(Guid.NewGuid(), parent.Version, x.Property, x.Area, "MAINTENANCE", target.MaintenancePlanId, 1, "Inspection work", "Execute scheduled inspection", due)));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{target.Id}/complete", new SupplierMaintenanceComplete(Guid.NewGuid(), target.Version, Today, "Without the open work order", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{target.Id}/skip", new SupplierMaintenanceSkip(Guid.NewGuid(), target.Version, "Skip with open order"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{target.Id}/complete", new SupplierMaintenanceComplete(Guid.NewGuid(), target.Version, Today, "Order not verified", order.Record.Id))).StatusCode);
        var current = order;
        foreach (var status in new[] { "ASSIGNED", "ACCEPTED", "IN_PROGRESS", "COMPLETED" }) current = await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{order.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), current.Record.Version, status, "Evidence " + status)));
        foreach (var status in new[] { "VERIFIED", "CLOSED" }) current = await Value<SupplierWorkOrderMutation>(await admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{order.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), current.Record.Version, status, "Manager approval")));
        var linkedCommand = new SupplierMaintenanceComplete(Guid.NewGuid(), target.Version, Today, "Closed through work order", order.Record.Id);
        var linked = await Value<SupplierMaintenanceOccurrenceMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{target.Id}/complete", linkedCommand)); Assert.Equal(order.Record.Id, linked.Record.WorkOrderId);
        Assert.True((await Value<SupplierMaintenanceOccurrenceMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{target.Id}/complete", linkedCommand))).Replayed);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{target.Id}/complete", linkedCommand with { Note = "Changed" })).StatusCode);

        var skipped = await Value<SupplierMaintenanceOccurrenceMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-occurrences/{rows[3].Id}/skip", new SupplierMaintenanceSkip(Guid.NewGuid(), rows[3].Version, "Building closed")));
        Assert.Equal("SKIPPED", skipped.Record.Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/{created.Record.Id}/end", new SupplierMaintenanceScheduleEnd(Guid.NewGuid(), created.Record.Version, " "))).StatusCode);
        var ended = await Value<SupplierMaintenanceScheduleMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/{created.Record.Id}/end", new SupplierMaintenanceScheduleEnd(Guid.NewGuid(), created.Record.Version, "Contract terminated")));
        Assert.Equal("ENDED", ended.Record.Status); Assert.Equal(0, ended.Record.PlannedCount); Assert.Equal(3, ended.Record.CompletedCount); Assert.Equal(6, ended.Record.SkippedCount);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/{created.Record.Id}/end", new SupplierMaintenanceScheduleEnd(Guid.NewGuid(), ended.Record.Version, "Again"))).StatusCode);
        var history = await Value<PagedRows<SupplierContractHistory>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/{created.Record.Id}/history?pageSize=20"));
        Assert.Equal(6, history.TotalCount); Assert.Equal("END", history.Items[0].Action);
        await factory.WithDb(async db =>
        {
            Assert.Equal(MaintenanceStatus.COMPLETED, (await db.MaintenancePlans.SingleAsync(m => m.Id == first.MaintenancePlanId)).Status);
            Assert.Equal(MaintenanceStatus.CANCELLED, (await db.MaintenancePlans.SingleAsync(m => m.Id == rows[8].MaintenancePlanId)).Status);
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_maintenance_occurrences SET status='PLANNED', completed_on=NULL, delay_days=NULL, completion_note=NULL WHERE id={first.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_maintenance_occurrences SET planned_date=planned_date+1 WHERE id={rows[8].Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_maintenance_schedules SET status='ACTIVE', end_reason=NULL WHERE id={created.Record.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM supplier_maintenance_revisions WHERE schedule_id={created.Record.Id}"));
        });
    }

    [PostgresFact] public async Task RolesScopeAndRevokedMembershipHideMaintenanceData()
    {
        var x = await Seed(); var foreign = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT"); using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-holidays?year=2027")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v2/tenants/{foreign.Tenant}/supplier-holidays?year=2027")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/preview", Plan(x, NextMonday(1), NextMonday(3)) with { PropertyId = foreign.Property })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/preview", Plan(x, NextMonday(1), NextMonday(3), contract: foreign.Contract))).StatusCode);
        var created = await Value<SupplierMaintenanceScheduleMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), 1, Plan(x, NextMonday(1), NextMonday(3)))));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules", new SupplierMaintenanceScheduleCreate(Guid.NewGuid(), 1, Plan(x, NextMonday(5), NextMonday(6))))).StatusCode);
        await factory.WithDb(async db => { var m = await db.PropertyMemberships.SingleAsync(m => m.PropertyId == x.Property && m.UserId == x.User); m.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/{created.Record.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/{created.Record.Id}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/maintenance-schedules/{created.Record.Id}/end", new SupplierMaintenanceScheduleEnd(Guid.NewGuid(), created.Record.Version, "Revoked"))).StatusCode);
    }
}
