using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierWorkOrderTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private sealed record Fixture(Guid Tenant, Guid Supplier, Guid Property, Guid Area, Guid Request, Guid Plan, string User);
    private async Task<Fixture> Seed()
    {
        var t = new PersonTenant("Work order fixture"); var p = new Property("Work property", "City", "District", "Address"); var user = Guid.NewGuid().ToString(); var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user);
        var supplier = new Supplier(t.Id, "Work supplier", SupplierKind.COMPANY); supplier.ChangeStatus(SupplierStatus.ACTIVE, null); var category = new SupplierCategory(t.Id, "Maintenance"); var service = new BuildingFlow.Domain.SupplierService(t.Id, supplier.Id, category.Id, "Repair"); var area = new SupplierAreaService(t.Id, supplier.Id, p.Id, service.Id);
        var request = new MobileRequest(Guid.NewGuid(), p.Id, null, "CENTER", user, "Elevator does not work\nOriginal request scope");
        var plan = new MaintenancePlan(p.Id, "Annual inspection", "Inspection scope", MaintenanceType.INSPECTION, DateOnly.FromDateTime(DateTime.UtcNow), null, null, MaintenanceStatus.PLANNED);
        await factory.WithDb(async db => { db.AddRange(t, p, account, supplier, category, service, area, request, plan, new PersonTenantMembership(t.Id, account.Id), new PersonPropertyScope(t.Id, p.Id), new PropertyMembership(p.Id, user), new SupplierManagementArea(t.Id, supplier.Id, p.Id)); await db.SaveChangesAsync(); });
        return new(t.Id, supplier.Id, p.Id, area.Id, request.Id, plan.Id, user);
    }
    private static SupplierWorkOrderCreate Create(Fixture x, string source = "REQUEST", long supplierVersion = 1) => new(Guid.NewGuid(), supplierVersion, x.Property, x.Area, source, source == "REQUEST" ? x.Request : x.Plan, 1, "Repair elevator", "Repair scope\nInclude evidence", DateTimeOffset.UtcNow.AddDays(2));
    private static async Task<T> Value<T>(HttpResponseMessage r) { Assert.Equal(HttpStatusCode.OK, r.StatusCode); return (await r.Content.ReadFromJsonAsync<T>())!; }
    private static MultipartFormDataContent Photo(Guid op, long version, byte[] bytes, string caption = "Before repair", string type = "image/jpeg", string name = "evidence.jpg")
    {
        var form = new MultipartFormDataContent(); var file = new ByteArrayContent(bytes); file.Headers.ContentType = new(type); form.Add(file, "file", name); form.Add(new StringContent(op.ToString()), "operationId"); form.Add(new StringContent(version.ToString()), "version"); form.Add(new StringContent(caption), "caption"); return form;
    }
    [PostgresFact] public async Task RequestLifecycleApprovalAndCurrentReplayKeepSourceIndependentAndHistoryImmutable()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var admin = TestToken.Client(factory, x.User, "SITE_ADMIN"); var command = Create(x);
        var choices = await Value<PagedRows<SupplierWorkOrderSourceChoice>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-order-sources?propertyId={x.Property}&source=REQUEST")); Assert.Equal(x.Request, Assert.Single(choices.Items).Id); Assert.Equal(x.Tenant, choices.Items[0].TenantId); Assert.Equal(x.Property, choices.Items[0].PropertyId);
        var created = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", command)); Assert.Null(created.Record.Amount); Assert.Equal("REQUEST", created.Record.Source); Assert.Contains("Original request", created.Record.SourceSnapshot);
        var url = $"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{created.Record.Id}"; var current = created;
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", command with { OperationId = Guid.NewGuid(), SupplierVersion = current.Supplier.Version })).StatusCode);
        Assert.Empty((await Value<PagedRows<SupplierWorkOrderSourceChoice>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-order-sources?propertyId={x.Property}&source=REQUEST"))).Items);
        foreach (var status in new[] { "ASSIGNED", "ACCEPTED", "IN_PROGRESS", "COMPLETED" }) current = await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{created.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), current.Record.Version, status, "Recorded evidence: " + status)));
        Assert.NotNull(current.Record.CompletedAt); Assert.Equal("Recorded evidence: COMPLETED", current.Record.CompletionNote);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{created.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), current.Record.Version, "VERIFIED", "Check complete"))).StatusCode);
        foreach (var status in new[] { "VERIFIED", "CLOSED" }) current = await Value<SupplierWorkOrderMutation>(await admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{created.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), current.Record.Version, status, "Manager approval")));
        Assert.Equal("CLOSED", current.Record.Status); Assert.NotNull(current.Record.ClosedAt);
        var replay = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", command)); Assert.True(replay.Replayed); Assert.Equal("CLOSED", replay.Record.Status); Assert.Equal(1, replay.AppliedVersion);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(url, new SupplierWorkOrderEdit(Guid.NewGuid(), current.Record.Version, "Overwrite final scope", null))).StatusCode);
        var history = await Value<PagedRows<SupplierContractHistory>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{created.Record.Id}/history")); Assert.Equal(7, history.TotalCount); Assert.Equal("CLOSED", history.Items[0].Action); Assert.Contains("Manager approval", history.Items[0].Snapshot);
        await factory.WithDb(async db => {
            var source = await db.Set<MobileRequest>().SingleAsync(r => r.Id == x.Request); Assert.Equal("RECORDED", source.ManagementStatus); Assert.Equal(1, source.Version);
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_work_orders SET status='DRAFT' WHERE id={created.Record.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_work_order_revisions SET snapshot='{{}}'::jsonb WHERE work_order_id={created.Record.Id}"));
        });
    }
    [PostgresFact] public async Task PlanDraftEditSourceVersionNegativeScopeAndConcurrentTransitionAreProtected()
    {
        var x = await Seed(); var foreign = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT"); using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders")).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", Create(x) with { SourceId = foreign.Request })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", Create(x) with { SourceVersion = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-order-sources?propertyId={foreign.Property}&source=REQUEST")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders?status=1")).StatusCode);
        var one = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", Create(x, "MAINTENANCE"))); var url = $"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}";
        var edit = new SupplierWorkOrderEdit(Guid.NewGuid(), one.Record.Version, "Updated execution scope", DateTimeOffset.UtcNow.AddHours(12));
        var two = await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync(url, edit)); Assert.Equal("Updated execution scope", two.Record.Description); Assert.Equal("MAINTENANCE", two.Record.Source);
        Assert.True((await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync(url, edit))).Replayed);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(url, edit with { Description = "Changed UUID content" })).StatusCode);
        var transitions = await Task.WhenAll(new[] { "ASSIGNED", "CANCELLED" }.Select(status => c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), two.Record.Version, status, "Concurrent manager decision"))));
        Assert.Single(transitions, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(transitions, r => r.StatusCode == HttpStatusCode.Conflict);
        await factory.WithDb(async db => { var plan = await db.MaintenancePlans.SingleAsync(p => p.Id == x.Plan); Assert.Equal(MaintenanceStatus.PLANNED, plan.Status); Assert.Equal(1, plan.Version); });
        await factory.WithDb(async db => { var m = await db.PropertyMemberships.SingleAsync(m => m.PropertyId == x.Property && m.UserId == x.User); m.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(url)).StatusCode); Assert.Empty((await Value<PagedRows<SupplierWorkOrderResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders"))).Items);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync(url, edit)).StatusCode);
    }
    [PostgresFact] public async Task PrivatePhotoUploadReplayDownloadAndEvidenceRemovalAreGuarded()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); var one = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", Create(x)));
        var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "avatar.jpg")); var op = Guid.NewGuid();
        using var form = Photo(op, one.Record.Version, bytes); var photo = await Value<SupplierWorkOrderMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/photos", form)); Assert.Equal(1, photo.Record.PhotoCount);
        using var retry = Photo(op, one.Record.Version, bytes); Assert.True((await Value<SupplierWorkOrderMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/photos", retry))).Replayed);
        using var changed = Photo(op, one.Record.Version, bytes, "Different caption"); Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/photos", changed)).StatusCode);
        var metadata = Assert.Single((await Value<PagedRows<SupplierWorkOrderPhotoResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/photos"))).Items);
        var download = await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{metadata.File.Id}/download"); Assert.Equal(bytes, await download.Content.ReadAsByteArrayAsync()); Assert.Equal("no-store", download.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/properties/{x.Property}/documents/{metadata.File.DocumentId}/download")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{metadata.File.Id}/remove", new SupplierFileRemove(Guid.NewGuid(), photo.Supplier.Version, metadata.File.Version))).StatusCode);
        foreach (var bad in new[] { Photo(Guid.NewGuid(), photo.Record.Version, "%PDF-1.7 fixture"u8.ToArray(), type: "application/pdf", name: "evidence.pdf"), Photo(Guid.NewGuid(), photo.Record.Version, "bad"u8.ToArray()), Photo(Guid.NewGuid(), photo.Record.Version, bytes, caption: " ") })
        { using (bad) Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/photos", bad)).StatusCode); }
        await factory.WithDb(async db => {
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_documents SET is_deleted=true WHERE id={metadata.File.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE stored_documents SET is_enabled=false WHERE id={metadata.File.DocumentId}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM supplier_work_order_photos WHERE id={metadata.Id}"));
        });
    }
    [PostgresFact] public async Task SlaAssignmentAndReasonValidationDoNotAllowSkippedCompletion()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); var one = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", Create(x) with { DueAt = null }));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), 1, "ASSIGNED", "Assign"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), 1, "COMPLETED", "Skip"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), 1, "CANCELLED", " "))).StatusCode);
        var cancelled = await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), 1, "CANCELLED", "No longer needed"))); Assert.Equal("CANCELLED", cancelled.Record.Status);
        Assert.Single((await Value<PagedRows<SupplierWorkOrderSourceChoice>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-order-sources?propertyId={x.Property}&source=REQUEST"))).Items);
    }
    [PostgresFact] public async Task PhotoLimitAndFinalizedEvidenceAreEnforcedWithoutLosingExistingPhotos()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER");
        var current = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", Create(x)));
        var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "avatar.jpg"));
        for (var n = 0; n < 10; n++) { using var form = Photo(Guid.NewGuid(), current.Record.Version, bytes, "Evidence " + n); current = await Value<SupplierWorkOrderMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{current.Record.Id}/photos", form)); }
        Assert.Equal(10, current.Record.PhotoCount); using var extra = Photo(Guid.NewGuid(), current.Record.Version, bytes); Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{current.Record.Id}/photos", extra)).StatusCode);
        foreach (var status in new[] { "ASSIGNED", "ACCEPTED", "IN_PROGRESS", "COMPLETED" }) current = await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{current.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), current.Record.Version, status, "Evidence recorded")));
        using var finalized = Photo(Guid.NewGuid(), current.Record.Version, bytes); Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{current.Record.Id}/photos", finalized)).StatusCode);
        Assert.Equal(10, (await Value<PagedRows<SupplierWorkOrderPhotoResponse>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{current.Record.Id}/photos"))).TotalCount);
    }

    [PostgresFact] public async Task SourceClosedBeforeAssignmentRejectsExecutionButAllowsReasonedCancellation()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER");
        var one = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", Create(x, "MAINTENANCE")));
        await factory.WithDb(async db => { var source = await db.MaintenancePlans.SingleAsync(p => p.Id == x.Plan); source.Edit(source.Title, source.Description, source.Type, source.PlannedDate, source.Location, source.Responsible, MaintenanceStatus.COMPLETED); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), 1, "ASSIGNED", "Assign closed source"))).StatusCode);
        var cancelled = await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), 1, "CANCELLED", "Source already complete"))); Assert.Equal("CANCELLED", cancelled.Record.Status);
    }

    [PostgresFact] public async Task FieldNotesAndPropertyListServeMobileManagersWithinScope()
    {
        var x = await Seed(); var foreign = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT");
        var one = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders", Create(x)));
        var note = new SupplierWorkOrderNote(Guid.NewGuid(), one.Record.Version, "Checked cabin on site\nWaiting for part");
        var noted = await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/notes", note));
        Assert.Equal(one.Record.Version + 1, noted.Record.Version); Assert.Equal("DRAFT", noted.Record.Status);
        Assert.True((await Value<SupplierWorkOrderMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/notes", note))).Replayed);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/notes", note with { Note = "Changed" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/notes", note with { OperationId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/notes", new SupplierWorkOrderNote(Guid.NewGuid(), noted.Record.Version, " "))).StatusCode);
        var history = await Value<PagedRows<SupplierContractHistory>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/history"));
        Assert.Equal("NOTE", history.Items[0].Action); Assert.Contains("Waiting for part", history.Items[0].Snapshot);
        var rows = await Value<PagedRows<SupplierWorkOrderRow>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-work-orders?propertyId={x.Property}"));
        var row = Assert.Single(rows.Items); Assert.Equal("Work supplier", row.SupplierName); Assert.Equal(noted.Record.Version, row.Record.Version);
        Assert.Single((await Value<PagedRows<SupplierWorkOrderRow>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-work-orders?propertyId={x.Property}&status=DRAFT"))).Items);
        Assert.Empty((await Value<PagedRows<SupplierWorkOrderRow>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-work-orders?propertyId={x.Property}&status=CLOSED"))).Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-work-orders?propertyId={x.Property}&status=DONE")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-work-orders?propertyId={foreign.Property}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-work-orders?propertyId={x.Property}")).StatusCode);
        var cancelled = await Value<SupplierWorkOrderMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/transition", new SupplierWorkOrderTransition(Guid.NewGuid(), noted.Record.Version, "CANCELLED", "Not needed")));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/work-orders/{one.Record.Id}/notes", new SupplierWorkOrderNote(Guid.NewGuid(), cancelled.Record.Version, "After cancel"))).StatusCode);
        await factory.WithDb(async db => { var m = await db.PropertyMemberships.SingleAsync(m => m.PropertyId == x.Property && m.UserId == x.User); m.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/supplier-work-orders?propertyId={x.Property}")).StatusCode);
    }
}
