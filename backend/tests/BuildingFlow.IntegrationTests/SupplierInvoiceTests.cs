using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierInvoiceTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private sealed record Fixture(Guid Tenant, Guid Supplier, Guid Property, Guid Contract, string Recorder, string Approver);
    private static readonly DateOnly Today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private async Task<Fixture> Seed()
    {
        var t = new PersonTenant("Invoice fixture"); var p = new Property("Invoice property", "City", "District", "Address"); var recorder = Guid.NewGuid().ToString(); var approver = Guid.NewGuid().ToString();
        var supplier = new Supplier(t.Id, "Invoice supplier", SupplierKind.COMPANY); supplier.ChangeStatus(SupplierStatus.ACTIVE, null); var category = new SupplierCategory(t.Id, "Elevator"); var service = new BuildingFlow.Domain.SupplierService(t.Id, supplier.Id, category.Id, "Repair"); var area = new SupplierAreaService(t.Id, supplier.Id, p.Id, service.Id);
        var contract = new SupplierContract(t.Id, supplier.Id, p.Id, area.Id, "Annual repair", Today.AddDays(-30), Today.AddDays(300), 5000m, 30); contract.Activate();
        await factory.WithDb(async db =>
        {
            db.AddRange(t, p, supplier, category, service, area, contract, new PersonPropertyScope(t.Id, p.Id), new SupplierManagementArea(t.Id, supplier.Id, p.Id));
            foreach (var user in new[] { recorder, approver }) { var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user); db.AddRange(account, new PersonTenantMembership(t.Id, account.Id), new PropertyMembership(p.Id, user)); }
            await db.SaveChangesAsync();
        });
        return new(t.Id, supplier.Id, p.Id, contract.Id, recorder, approver);
    }
    private static SupplierInvoiceCreate Create(Fixture x, string number = "INV-2027/001", Guid? corrects = null) => new(Guid.NewGuid(), x.Property, number, Today.AddDays(-5), Today.AddDays(25), "1000.00", "20.00", "MAINTENANCE", "Elevator repair", null, x.Contract, corrects);
    private static async Task<T> Value<T>(HttpResponseMessage r) { Assert.Equal(HttpStatusCode.OK, r.StatusCode); return (await r.Content.ReadFromJsonAsync<T>())!; }

    [PostgresFact] public async Task InvoiceLifecycleCreatesOneExpenseSeparatesDutiesAndKeepsAudit()
    {
        var x = await Seed(); using var accountant = TestToken.Client(factory, x.Recorder, "ACCOUNTANT"); using var selfAdmin = TestToken.Client(factory, x.Recorder, "SITE_ADMIN"); using var admin = TestToken.Client(factory, x.Approver, "SITE_ADMIN"); using var manager = TestToken.Client(factory, x.Approver, "MANAGER");
        var command = Create(x);
        var created = await Value<SupplierInvoiceMutation>(await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", command));
        Assert.Equal("1200.00", created.Record.GrossAmount); Assert.Equal("200.00", created.Record.VatAmount); Assert.Equal("RECORDED", created.Record.Status); Assert.True(created.Record.RecordedByMe);
        Assert.True((await Value<SupplierInvoiceMutation>(await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", command))).Replayed);
        var duplicate = await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", Create(x, "inv 2027.001"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode); Assert.Contains("BF_SUPPLIER_INVOICE_DUPLICATE", await duplicate.Content.ReadAsStringAsync());
        var id = created.Record.Id; var version = created.Record.Version;
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/approve", new SupplierInvoiceAction(Guid.NewGuid(), version, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/approve", new SupplierInvoiceAction(Guid.NewGuid(), version, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await selfAdmin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/approve", new SupplierInvoiceAction(Guid.NewGuid(), version, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await accountant.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/pay", new SupplierInvoiceAction(Guid.NewGuid(), version, Today, "EFT-1"))).StatusCode);
        var approvals = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/approve", new SupplierInvoiceAction(Guid.NewGuid(), version, null, null))));
        Assert.Single(approvals, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(approvals, r => r.StatusCode == HttpStatusCode.Conflict);
        var approved = await Value<SupplierInvoiceResponse>(await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}")); Assert.Equal("APPROVED", approved.Status); Assert.NotNull(approved.ExpenseId);
        var cancelExpense = await accountant.PutAsJsonAsync($"/api/v1/properties/{x.Property}/expenses/{approved.ExpenseId}/cancel", new CancelChargeRequest("Bypass invoice", 1));
        Assert.Equal(HttpStatusCode.Conflict, cancelExpense.StatusCode); Assert.Contains("BF_SUPPLIER_INVOICE_LINKED", await cancelExpense.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await accountant.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/pay", new SupplierInvoiceAction(Guid.NewGuid(), approved.Version, Today.AddDays(1), "EFT-1"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await accountant.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/pay", new SupplierInvoiceAction(Guid.NewGuid(), approved.Version, null, "EFT-1"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/pay", new SupplierInvoiceAction(Guid.NewGuid(), approved.Version, Today, "EFT-1"))).StatusCode);
        var paid = await Value<SupplierInvoiceMutation>(await accountant.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/pay", new SupplierInvoiceAction(Guid.NewGuid(), approved.Version, Today, "EFT-1")));
        Assert.Equal("PAID", paid.Record.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/cancel", new SupplierInvoiceAction(Guid.NewGuid(), paid.Record.Version, null, "After payment"))).StatusCode);
        var reconciled = await Value<SupplierInvoiceMutation>(await accountant.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/reconcile", new SupplierInvoiceAction(Guid.NewGuid(), paid.Record.Version, Today, "Matched bank statement")));
        Assert.Equal("RECONCILED", reconciled.Record.Status);
        var page = await Value<SupplierInvoicePage>(await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices?status=RECONCILED"));
        Assert.Equal(id, Assert.Single(page.Items).Id); Assert.Equal("1200.00", page.Summary.ReconciledTotal); Assert.Equal("0.00", page.Summary.RecordedTotal);
        var history = await Value<PagedRows<SupplierContractHistory>>(await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{id}/history"));
        Assert.Equal(new[] { "RECONCILE", "PAY", "APPROVE", "CREATE" }, history.Items.Select(h => h.Action));
        await factory.WithDb(async db =>
        {
            var expense = await db.Expenses.SingleAsync(e => e.Id == approved.ExpenseId); Assert.Equal(1200m, expense.Amount); Assert.Equal(id, expense.RequestId); Assert.Equal("INV-2027/001", expense.DocumentReference); Assert.False(expense.IsCancelled);
            Assert.Equal(1, await db.Expenses.CountAsync(e => e.PropertyId == x.Property));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_invoices SET gross_amount=1.00, net_amount=1.00, vat_amount=0 WHERE id={id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_invoices SET status='APPROVED', paid_on=NULL, payment_reference=NULL, reconciled_on=NULL, reconciliation_note=NULL WHERE id={id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM supplier_invoice_revisions WHERE invoice_id={id}"));
        });
    }

    [PostgresFact] public async Task CancellationReversesExpenseAndCorrectionReusesNumberOnce()
    {
        var x = await Seed(); using var accountant = TestToken.Client(factory, x.Recorder, "ACCOUNTANT"); using var admin = TestToken.Client(factory, x.Approver, "SITE_ADMIN");
        var created = await Value<SupplierInvoiceMutation>(await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", Create(x, "C-1")));
        var approved = await Value<SupplierInvoiceMutation>(await admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}/approve", new SupplierInvoiceAction(Guid.NewGuid(), created.Record.Version, null, null)));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}/cancel", new SupplierInvoiceAction(Guid.NewGuid(), approved.Record.Version, null, " "))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}/cancel", new SupplierInvoiceAction(Guid.NewGuid(), approved.Record.Version, null, "Wrong VAT"))).StatusCode);
        var cancel = new SupplierInvoiceAction(Guid.NewGuid(), approved.Record.Version, null, "Wrong VAT");
        var cancelled = await Value<SupplierInvoiceMutation>(await admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}/cancel", cancel));
        Assert.Equal("CANCELLED", cancelled.Record.Status); Assert.True((await Value<SupplierInvoiceMutation>(await admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}/cancel", cancel))).Replayed);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}/cancel", cancel with { Text = "Different reason" })).StatusCode);
        await factory.WithDb(async db => { var expense = await db.Expenses.SingleAsync(e => e.Id == approved.Record.ExpenseId); Assert.True(expense.IsCancelled); Assert.Equal("Wrong VAT", expense.CancellationReason); });
        var correction = await Value<SupplierInvoiceMutation>(await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", Create(x, "C-1", created.Record.Id) with { VatRate = "10.00" }));
        Assert.Equal(created.Record.Id, correction.Record.CorrectsInvoiceId); Assert.Equal("1100.00", correction.Record.GrossAmount);
        Assert.Equal(HttpStatusCode.Conflict, (await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", Create(x, "C-2", created.Record.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", Create(x, "C-3", correction.Record.Id))).StatusCode);
    }

    [PostgresFact] public async Task ValidationLinksScopeAndRolesAreEnforced()
    {
        var x = await Seed(); var foreign = await Seed(); using var accountant = TestToken.Client(factory, x.Recorder, "ACCOUNTANT"); using var resident = TestToken.Client(factory, x.Recorder, "RESIDENT"); using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await resident.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await accountant.GetAsync($"/api/v2/tenants/{foreign.Tenant}/suppliers/{foreign.Supplier}/invoices")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", Create(x) with { PropertyId = foreign.Property })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", Create(x) with { ContractId = foreign.Contract })).StatusCode);
        foreach (var bad in new[] { Create(x) with { NetAmount = "0.00" }, Create(x) with { NetAmount = "1.005" }, Create(x) with { VatRate = "100.01" }, Create(x) with { Category = "PAYROLL" }, Create(x) with { Number = " / " }, Create(x) with { IssueDate = Today.AddDays(1) }, Create(x) with { DueDate = Today.AddDays(-10) }, Create(x) with { Description = "" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", bad)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices?status=PAYABLE")).StatusCode);
        var created = await Value<SupplierInvoiceMutation>(await accountant.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices", Create(x)));
        Assert.Equal(HttpStatusCode.Conflict, (await accountant.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}/pay", new SupplierInvoiceAction(Guid.NewGuid(), created.Record.Version + 1, Today, "EFT"))).StatusCode);
        Assert.Equal(created.Record.Id, Assert.Single((await Value<SupplierInvoicePage>(await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices"))).Items).Id);
        await factory.WithDb(async db => { var m = await db.PropertyMemberships.SingleAsync(m => m.PropertyId == x.Property && m.UserId == x.Recorder); m.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Empty((await Value<SupplierInvoicePage>(await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices"))).Items);
        Assert.Equal(HttpStatusCode.NotFound, (await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/invoices/{created.Record.Id}/history")).StatusCode);
    }
}
