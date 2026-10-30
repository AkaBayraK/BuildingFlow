using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    private static readonly string[] FinanceRoles = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT"];
    private static readonly SupplierInvoiceStatus[] OpenInvoices = [SupplierInvoiceStatus.RECORDED, SupplierInvoiceStatus.APPROVED];
    private bool IsAdmin => actor.IsInRole("SUPER_ADMIN") || actor.IsInRole("SITE_ADMIN");
    private async Task<bool> FinanceTenant(Guid tenant, CancellationToken token)
    {
        if (!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || string.IsNullOrWhiteSpace(actor.Issuer) || !FinanceRoles.Any(actor.IsInRole)) throw new UnauthorizedAccessException();
        if (!await db.Set<PersonTenant>().AnyAsync(x => x.Id == tenant && x.IsEnabled, token)) return false;
        return actor.IsInRole("SUPER_ADMIN") || await db.Set<PersonTenantMembership>().AnyAsync(m => m.TenantId == tenant && m.IsEnabled && db.Set<PersonUserAccount>().Any(a => a.Id == m.UserAccountId && a.IsEnabled && a.Issuer == actor.Issuer && a.Subject == actor.UserId), token);
    }
    // Finance users reach an invoice through the tenant scope, their own property membership and the supplier's area link; people permissions are not required.
    private IQueryable<Guid> FinanceProperties(Guid tenant, Guid supplier)
    {
        var all = actor.IsInRole("SUPER_ADMIN"); var user = actor.UserId;
        return db.Set<PersonPropertyScope>().Where(s => s.TenantId == tenant && s.IsEnabled
                && db.Set<SupplierManagementArea>().Any(a => a.TenantId == tenant && a.SupplierId == supplier && a.PropertyId == s.PropertyId && a.IsEnabled)
                && (all || db.PropertyMemberships.Any(m => m.PropertyId == s.PropertyId && m.UserId == user && m.IsEnabled)))
            .Select(s => s.PropertyId);
    }
    private IQueryable<SupplierInvoice> InvoiceRows(Guid tenant, Guid supplier) => db.Set<SupplierInvoice>().Where(x => x.TenantId == tenant && x.SupplierId == supplier);
    private SupplierInvoiceResponse InvoiceResponse(SupplierInvoice x, string propertyName)
    {
        var today = ContractToday;
        return new(x.Id, x.TenantId, x.SupplierId, x.PropertyId, propertyName, x.Number, x.IssueDate, x.DueDate, Money(x.NetAmount), x.VatRate.ToString("F2", CultureInfo.InvariantCulture), Money(x.VatAmount), Money(x.GrossAmount), x.Currency, x.Category.ToString(), x.Description,
            x.WorkOrderId, x.ContractId, x.CorrectsInvoiceId, x.ExpenseId, x.Status.ToString(), OpenInvoices.Contains(x.Status) && x.DueDate < today, x.IsRecordedBy(actor.Issuer ?? "", actor.UserId ?? ""),
            x.PaidOn, x.PaymentReference, x.ReconciledOn, x.ReconciliationNote, x.CancellationReason, x.Version, x.CreatedAt);
    }
    private async Task<SupplierInvoiceResponse> InvoiceResponse(SupplierInvoice x, CancellationToken token) => InvoiceResponse(x, await db.Properties.Where(p => p.Id == x.PropertyId).Select(p => p.Name).SingleAsync(token));
    public async Task<SupplierInvoicePage?> Invoices(Guid tenant, Guid supplier, string? status, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); SupplierInvoiceStatus? filter = status is null ? null : Parse<SupplierInvoiceStatus>(status, "status");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await FinanceTenant(tenant, token) || !await Rows(tenant).AnyAsync(x => x.Id == supplier, token)) return null;
        var properties = FinanceProperties(tenant, supplier); var visible = InvoiceRows(tenant, supplier).AsNoTracking().Where(x => properties.Contains(x.PropertyId));
        var rows = from x in filter.HasValue ? visible.Where(x => x.Status == filter.Value) : visible join p in db.Properties on x.PropertyId equals p.Id select new { Record = x, PropertyName = p.Name };
        var count = await rows.CountAsync(token); var items = await rows.OrderByDescending(x => x.Record.IssueDate).ThenBy(x => x.Record.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);
        var today = ContractToday;
        var totals = await visible.GroupBy(x => x.Status).Select(g => new { Status = g.Key, Total = g.Sum(x => x.GrossAmount) }).ToArrayAsync(token);
        var overdue = await visible.CountAsync(x => (x.Status == SupplierInvoiceStatus.RECORDED || x.Status == SupplierInvoiceStatus.APPROVED) && x.DueDate < today, token);
        string Total(SupplierInvoiceStatus s) => Money(totals.Where(x => x.Status == s).Select(x => x.Total).FirstOrDefault());
        await tx.CommitAsync(token);
        return new(items.Select(x => InvoiceResponse(x.Record, x.PropertyName)).ToArray(), page, pageSize, count, new(Total(SupplierInvoiceStatus.RECORDED), Total(SupplierInvoiceStatus.APPROVED), Total(SupplierInvoiceStatus.PAID), Total(SupplierInvoiceStatus.RECONCILED), overdue));
    }
    public async Task<SupplierInvoiceResponse?> Invoice(Guid tenant, Guid supplier, Guid id, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await FinanceTenant(tenant, token)) return null;
        var properties = FinanceProperties(tenant, supplier); var row = await InvoiceRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && properties.Contains(x.PropertyId), token);
        if (row is null) return null; var result = await InvoiceResponse(row, token); await tx.CommitAsync(token); return result;
    }
    private static decimal Decimal(string? value, string field, decimal max)
    {
        if (value is not { Length: <= 20 } || !Regex.IsMatch(value, @"^[0-9]+(?:\.[0-9]{1,2})?$") || !decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) || parsed > max) InvalidSupplier(field);
        return decimal.Parse(value!, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    }
    private async Task<Property?> LockFinance(Guid tenant, Guid supplier, Guid property, CancellationToken token)
    {
        if (!await FinanceTenant(tenant, token) || !await FinanceProperties(tenant, supplier).ContainsAsync(property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE", token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE", token);
        if (!await FinanceProperties(tenant, supplier).ContainsAsync(property, token) || !await Rows(tenant).AnyAsync(x => x.Id == supplier, token)) return null;
        return await db.Properties.SingleAsync(p => p.Id == property, token);
    }
    private async Task<SupplierInvoiceMutation?> InvoiceReplay(Guid tenant, Guid supplier, Guid operation, string fingerprint, CancellationToken token)
    {
        var receipt = await db.Set<SupplierInvoiceRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenant && x.ActorIssuer == actor.Issuer && x.ActorSubject == actor.UserId && x.OperationId == operation, token); if (receipt is null) return null;
        if (receipt.SupplierId != supplier || receipt.Fingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        var current = await InvoiceRows(tenant, supplier).AsNoTracking().SingleAsync(x => x.Id == receipt.InvoiceId, token);
        return new(await InvoiceResponse(current, token), true, receipt.AppliedVersion);
    }
    private static object InvoiceSnapshot(SupplierInvoice x, string? text = null) => new { x.Number, x.IssueDate, x.DueDate, Net = Money(x.NetAmount), x.VatRate, Gross = Money(x.GrossAmount), Category = x.Category.ToString(), x.WorkOrderId, x.ContractId, x.CorrectsInvoiceId, x.ExpenseId, Status = x.Status.ToString(), x.PaidOn, x.PaymentReference, x.ReconciledOn, x.ReconciliationNote, x.CancellationReason, Text = text };
    private async Task SaveInvoice(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceDuplicate.Create()); }
    }
    public async Task<SupplierInvoiceMutation?> CreateInvoice(Guid tenant, Guid supplier, SupplierInvoiceCreate c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.PropertyId == Guid.Empty || c.WorkOrderId == Guid.Empty || c.ContractId == Guid.Empty || c.CorrectsInvoiceId == Guid.Empty) InvalidSupplier("invoice");
        var number = MaintenanceText(c.Number, 50, true, "number")!; if (SupplierInvoice.Key(number).Length == 0) InvalidSupplier("number");
        var description = MaintenanceText(c.Description, 400, true, "description")!; var category = Parse<ExpenseCategory>(c.Category, "category");
        var net = Decimal(c.NetAmount, "netAmount", 99999999.99m); var rate = Decimal(c.VatRate, "vatRate", 100m);
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await LockFinance(tenant, supplier, c.PropertyId, token); if (parent is null) return null;
        var fingerprint = Hash(new { action = "INVOICE_CREATE", tenant, supplier, c.PropertyId, number, c.IssueDate, c.DueDate, net = Money(net), rate = rate.ToString("F2", CultureInfo.InvariantCulture), category = c.Category, description, c.WorkOrderId, c.ContractId, c.CorrectsInvoiceId });
        var replay = await InvoiceReplay(tenant, supplier, c.OperationId, fingerprint, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        var key = SupplierInvoice.Key(number);
        if (await InvoiceRows(tenant, supplier).AnyAsync(x => x.NumberKey == key && x.Status != SupplierInvoiceStatus.CANCELLED, token)) throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceDuplicate.Create());
        if (c.WorkOrderId.HasValue)
        {
            var order = await OrderRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.WorkOrderId && x.PropertyId == c.PropertyId, token); if (order is null) return null;
            if (order.Status is not (SupplierWorkOrderStatus.COMPLETED or SupplierWorkOrderStatus.VERIFIED or SupplierWorkOrderStatus.CLOSED)) throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceState.Create());
        }
        if (c.ContractId.HasValue)
        {
            var contract = await ContractRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.ContractId && x.PropertyId == c.PropertyId, token); if (contract is null) return null;
            if (contract.Status != SupplierContractStatus.ACTIVE) throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceState.Create());
        }
        if (c.CorrectsInvoiceId.HasValue)
        {
            var original = await InvoiceRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.CorrectsInvoiceId && x.PropertyId == c.PropertyId, token); if (original is null) return null;
            if (original.Status != SupplierInvoiceStatus.CANCELLED || await InvoiceRows(tenant, supplier).AnyAsync(x => x.CorrectsInvoiceId == original.Id, token)) throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceState.Create());
        }
        SupplierInvoice invoice;
        try { invoice = new(tenant, supplier, c.PropertyId, number, c.IssueDate, c.DueDate, net, rate, category, description, c.WorkOrderId, c.ContractId, c.CorrectsInvoiceId, actor.Issuer!, actor.UserId!, ContractToday); }
        catch (ArgumentException) { InvalidSupplier("invoice"); throw; }
        db.Add(invoice); db.Add(new SupplierInvoiceRevision(invoice, c.OperationId, actor.Issuer!, actor.UserId!, fingerprint, "CREATE", JsonSerializer.Serialize(InvoiceSnapshot(invoice)), 1));
        Touch(parent); await SaveInvoice(token);
        var result = new SupplierInvoiceMutation(await InvoiceResponse(invoice, token), false, invoice.Version); await tx.CommitAsync(token); return result;
    }
    private void Touch(Property parent) => db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
    public async Task<SupplierInvoiceMutation?> TransitionInvoice(Guid tenant, Guid supplier, Guid id, string action, SupplierInvoiceAction c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1 || action is not ("approve" or "pay" or "reconcile" or "cancel")) InvalidSupplier("version");
        var needsDate = action is "pay" or "reconcile"; var needsText = action != "approve";
        if (needsDate != c.Date.HasValue || c.Date?.Year < 2000) InvalidSupplier("date");
        if (!needsText && c.Text is not null) InvalidSupplier("text");
        var text = needsText ? MaintenanceText(c.Text, action == "pay" ? 100 : 1000, true, "text") : null;
        if (!await FinanceTenant(tenant, token)) return null;
        var property = await InvoiceRows(tenant, supplier).Where(x => x.Id == id).Select(x => (Guid?)x.PropertyId).SingleOrDefaultAsync(token); if (property is null) return null;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await LockFinance(tenant, supplier, property.Value, token); if (parent is null) return null;
        // Approval and cancellation change the books, so they stay with area administrators; paying and reconciling are finance bookkeeping.
        var decision = action is "approve" or "cancel";
        if (decision ? !IsAdmin : !(IsAdmin || actor.IsInRole("ACCOUNTANT"))) throw new UnauthorizedAccessException();
        if (decision && !actor.IsInRole("SUPER_ADMIN") && !(await management.EvaluateAsync(tenant, property.Value, "SITE_ADMIN", token)).Allowed) throw new UnauthorizedAccessException();
        var fingerprint = Hash(new { action = "INVOICE_" + action.ToUpperInvariant(), tenant, supplier, id, c.Version, c.Date, text });
        var replay = await InvoiceReplay(tenant, supplier, c.OperationId, fingerprint, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        var invoice = await InvoiceRows(tenant, supplier).SingleAsync(x => x.Id == id, token); Expected(c.Version, invoice.Version);
        var today = ContractToday;
        try
        {
            switch (action)
            {
                case "approve":
                    if (invoice.IsRecordedBy(actor.Issuer!, actor.UserId!)) throw new UnauthorizedAccessException();
                    if (invoice.Status != SupplierInvoiceStatus.RECORDED) throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceState.Create());
                    if (!parent.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.PaymentsInactiveProperty.Create());
                    var name = await Rows(tenant).Where(x => x.Id == supplier).Select(x => x.Name).SingleAsync(token);
                    var note = $"{name} · {invoice.Number} · {invoice.Description}";
                    var expense = new Expense(invoice.PropertyId, invoice.Id, Hash(new { invoice = invoice.Id, gross = Money(invoice.GrossAmount), invoice.IssueDate, invoice.Number }), invoice.Category, invoice.GrossAmount, invoice.IssueDate, note.Length > 500 ? note[..500] : note, invoice.Number);
                    db.Expenses.Add(expense); invoice.Approve(expense.Id); break;
                case "pay":
                    if (invoice.Status != SupplierInvoiceStatus.APPROVED) throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceState.Create());
                    invoice.Pay(c.Date!.Value, text!, today); break;
                case "reconcile":
                    if (invoice.Status != SupplierInvoiceStatus.PAID) throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceState.Create());
                    invoice.Reconcile(c.Date!.Value, text!, today); break;
                default:
                    if (!OpenInvoices.Contains(invoice.Status)) throw new HierarchyConflictException(ErrorCatalog.SupplierInvoiceState.Create());
                    if (invoice.ExpenseId.HasValue) (await db.Expenses.SingleAsync(x => x.Id == invoice.ExpenseId && x.PropertyId == invoice.PropertyId, token)).Cancel(text!, clock.GetUtcNow());
                    invoice.Cancel(text!); break;
            }
        }
        catch (ArgumentException) { InvalidSupplier("date"); throw; }
        db.Add(new SupplierInvoiceRevision(invoice, c.OperationId, actor.Issuer!, actor.UserId!, fingerprint, action.ToUpperInvariant(), JsonSerializer.Serialize(InvoiceSnapshot(invoice, text)), c.Version + 1));
        Touch(parent); await SaveInvoice(token);
        var result = new SupplierInvoiceMutation(await InvoiceResponse(invoice, token), false, invoice.Version); await tx.CommitAsync(token); return result;
    }
    public async Task<PagedRows<SupplierContractHistory>?> InvoiceHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await FinanceTenant(tenant, token)) return null;
        var properties = FinanceProperties(tenant, supplier); var invoice = await InvoiceRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && properties.Contains(x.PropertyId), token); if (invoice is null) return null;
        var rows = db.Set<SupplierInvoiceRevision>().AsNoTracking().Where(x => x.TenantId == tenant && x.SupplierId == supplier && x.PropertyId == invoice.PropertyId && x.InvoiceId == id);
        var count = await rows.CountAsync(token); var items = await rows.OrderByDescending(x => x.AppliedVersion).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierContractHistory(x.Id, x.Action, x.AppliedVersion, x.Snapshot, x.CreatedAt)).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, count);
    }
}
