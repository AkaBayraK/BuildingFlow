using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    public const int AlertCap = 500, MinimumScoreSample = 3;
    private static readonly string[] AlertKinds = ["CONTRACT_EXPIRY", "WORK_ORDER_OVERDUE", "MAINTENANCE_OVERDUE", "MAINTENANCE_DUE"];
    private static SupplierAlertPreferenceResponse PreferenceResponse(SupplierAlertPreference? p) => p is null ? new(true, true, true, 0) : new(p.ContractExpiry, p.WorkOrderOverdue, p.MaintenanceDue, p.Version);
    private Task<SupplierAlertPreference?> Preference(Guid tenant, CancellationToken token) =>
        db.Set<SupplierAlertPreference>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.ActorIssuer == actor.Issuer && x.ActorSubject == actor.UserId, token);
    public async Task<SupplierAlertPreferenceResponse?> AlertPreference(Guid tenant, CancellationToken token)
    { if (!await Allowed(tenant, token)) return null; return PreferenceResponse(await Preference(tenant, token)); }
    public async Task<SupplierAlertPreferenceResponse?> SaveAlertPreference(Guid tenant, SupplierAlertPreferenceRequest c, CancellationToken token)
    {
        if (c.Version < 0) InvalidSupplier("version");
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (!await Allowed(tenant, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE", token);
        var row = await Preference(tenant, token);
        Expected(c.Version, row?.Version ?? 0);
        if (row is null) { row = new(tenant, actor.Issuer!, actor.UserId!); db.Add(row); }
        else db.Entry(row).Property(x => x.IsEnabled).IsModified = true;
        row.Set(c.ContractExpiry, c.WorkOrderOverdue, c.MaintenanceDue);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return PreferenceResponse(row);
    }
    // Alerts are computed on read from live records, limited to the caller's property scope and own preferences; nothing is sent outside the app.
    public async Task<SupplierAlertPage?> Alerts(Guid tenant, string? kind, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); if (kind is not null && !AlertKinds.Contains(kind)) InvalidSupplier("kind");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null;
        var preference = PreferenceResponse(await Preference(tenant, token));
        var scopes = PropertyScopes(tenant).Select(s => s.PropertyId);
        var areas = db.Set<SupplierManagementArea>().Where(a => a.TenantId == tenant && a.IsEnabled);
        var today = ContractToday; var now = clock.GetUtcNow();
        var alerts = new List<SupplierAlert>(); var truncated = false;
        bool Want(string k) => kind is null || kind == k;
        void Add(IReadOnlyCollection<SupplierAlert> rows) { if (rows.Count > AlertCap) truncated = true; alerts.AddRange(rows.Take(AlertCap)); }
        if (preference.ContractExpiry && Want("CONTRACT_EXPIRY"))
            Add(await (from c in db.Set<SupplierContract>().AsNoTracking()
                       join s in db.Set<Supplier>() on new { c.TenantId, Id = c.SupplierId } equals new { s.TenantId, s.Id }
                       join p in db.Properties on c.PropertyId equals p.Id
                       where c.TenantId == tenant && c.Status == SupplierContractStatus.ACTIVE && c.EndDate >= today && c.EndDate.AddDays(-c.ReminderDays) <= today
                           && scopes.Contains(c.PropertyId) && areas.Any(a => a.SupplierId == c.SupplierId && a.PropertyId == c.PropertyId)
                       orderby c.EndDate, c.Id
                       select new SupplierAlert("CONTRACT_EXPIRY", s.Id, s.Name, p.Id, p.Name, c.Id, c.Title, c.EndDate, c.EndDate.DayNumber - today.DayNumber)).Take(AlertCap + 1).ToListAsync(token));
        if (preference.WorkOrderOverdue && Want("WORK_ORDER_OVERDUE"))
        {
            var rows = await (from o in db.Set<SupplierWorkOrder>().AsNoTracking()
                              join s in db.Set<Supplier>() on new { o.TenantId, Id = o.SupplierId } equals new { s.TenantId, s.Id }
                              join p in db.Properties on o.PropertyId equals p.Id
                              where o.TenantId == tenant && o.DueAt < now && (o.Status == SupplierWorkOrderStatus.ASSIGNED || o.Status == SupplierWorkOrderStatus.ACCEPTED || o.Status == SupplierWorkOrderStatus.IN_PROGRESS)
                                  && scopes.Contains(o.PropertyId) && areas.Any(a => a.SupplierId == o.SupplierId && a.PropertyId == o.PropertyId)
                              orderby o.DueAt, o.Id
                              select new { SupplierId = s.Id, SupplierName = s.Name, PropertyId = p.Id, PropertyName = p.Name, o.Id, o.Title, o.DueAt }).Take(AlertCap + 1).ToListAsync(token);
            Add(rows.Select(x => { var due = BusinessDate.Of(x.DueAt!.Value); return new SupplierAlert("WORK_ORDER_OVERDUE", x.SupplierId, x.SupplierName, x.PropertyId, x.PropertyName, x.Id, x.Title, due, due.DayNumber - today.DayNumber); }).ToList());
        }
        if (preference.MaintenanceDue && (Want("MAINTENANCE_OVERDUE") || Want("MAINTENANCE_DUE")))
        {
            var rows = await (from o in db.Set<SupplierMaintenanceOccurrence>().AsNoTracking()
                              join m in db.Set<SupplierMaintenanceSchedule>() on new { o.TenantId, o.SupplierId, o.PropertyId, Id = o.ScheduleId } equals new { m.TenantId, m.SupplierId, m.PropertyId, m.Id }
                              join s in db.Set<Supplier>() on new { o.TenantId, Id = o.SupplierId } equals new { s.TenantId, s.Id }
                              join p in db.Properties on o.PropertyId equals p.Id
                              where o.TenantId == tenant && o.Status == SupplierMaintenanceOccurrenceStatus.PLANNED && o.PlannedDate.AddDays(-m.ReminderDays) <= today
                                  && (kind == null || (kind == "MAINTENANCE_OVERDUE" ? o.PlannedDate < today : o.PlannedDate >= today))
                                  && scopes.Contains(o.PropertyId) && areas.Any(a => a.SupplierId == o.SupplierId && a.PropertyId == o.PropertyId)
                              orderby o.PlannedDate, o.Id
                              select new SupplierAlert(o.PlannedDate < today ? "MAINTENANCE_OVERDUE" : "MAINTENANCE_DUE", s.Id, s.Name, p.Id, p.Name, o.Id, m.Title, o.PlannedDate, o.PlannedDate.DayNumber - today.DayNumber)).Take(AlertCap + 1).ToListAsync(token);
            Add(rows);
        }
        await tx.CommitAsync(token);
        var ordered = alerts.OrderBy(a => a.Days).ThenBy(a => a.Kind, StringComparer.Ordinal).ThenBy(a => a.RecordId).ToArray();
        return new(ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), page, pageSize, ordered.Length, truncated, preference);
    }
    // Scores only appear with enough samples; every rate is shown next to the count it was computed from.
    public async Task<SupplierPerformanceReport?> Performance(Guid tenant, DateOnly from, DateOnly to, Guid? property, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize);
        if (from.Year < 2000 || to.Year > 2100 || to < from || to.DayNumber - from.DayNumber > 366 || property == Guid.Empty) InvalidSupplier("range");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null;
        var scopes = PropertyScopes(tenant).Select(s => s.PropertyId);
        if (property.HasValue && !await scopes.ContainsAsync(property.Value, token)) return null;
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime(); var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime();
        var orders = await db.Set<SupplierWorkOrder>().AsNoTracking().Where(o => o.TenantId == tenant && o.CompletedAt >= start && o.CompletedAt < end && scopes.Contains(o.PropertyId) && (property == null || o.PropertyId == property))
            .Select(o => new { o.SupplierId, o.AssignedAt, o.CompletedAt, o.DueAt }).ToListAsync(token);
        var visits = await db.Set<SupplierMaintenanceOccurrence>().AsNoTracking().Where(o => o.TenantId == tenant && o.Status == SupplierMaintenanceOccurrenceStatus.COMPLETED && o.CompletedOn >= from && o.CompletedOn <= to && scopes.Contains(o.PropertyId) && (property == null || o.PropertyId == property))
            .Select(o => new { o.SupplierId, o.DelayDays }).ToListAsync(token);
        var invoices = await db.Set<SupplierInvoice>().AsNoTracking().Where(i => i.TenantId == tenant && i.IssueDate >= from && i.IssueDate <= to && (i.Status == SupplierInvoiceStatus.APPROVED || i.Status == SupplierInvoiceStatus.PAID || i.Status == SupplierInvoiceStatus.RECONCILED) && scopes.Contains(i.PropertyId) && (property == null || i.PropertyId == property))
            .Select(i => new { i.SupplierId, i.GrossAmount }).ToListAsync(token);
        var ids = orders.Select(x => x.SupplierId).Concat(visits.Select(x => x.SupplierId)).Concat(invoices.Select(x => x.SupplierId)).Distinct().ToArray();
        var names = await Rows(tenant).AsNoTracking().Where(s => ids.Contains(s.Id)).Select(s => new { s.Id, s.Name, s.NameKey }).ToListAsync(token);
        await tx.CommitAsync(token);
        static string Rate(int part, int whole) => (100m * part / whole).ToString("F1", CultureInfo.InvariantCulture);
        var rows = names.OrderBy(n => n.NameKey).ThenBy(n => n.Id).Select(n =>
        {
            var done = orders.Where(o => o.SupplierId == n.Id).ToList(); var timed = done.Where(o => o.AssignedAt.HasValue).Select(o => (o.CompletedAt!.Value - o.AssignedAt!.Value).TotalHours).ToList();
            var sla = done.Where(o => o.DueAt.HasValue).ToList(); var slaOnTime = sla.Count(o => o.CompletedAt <= o.DueAt);
            var visit = visits.Where(v => v.SupplierId == n.Id).ToList(); var visitOnTime = visit.Count(v => v.DelayDays == 0);
            var bills = invoices.Where(i => i.SupplierId == n.Id).ToList(); var sample = sla.Count + visit.Count;
            var rate = sample == 0 ? null : Rate(slaOnTime + visitOnTime, sample);
            return new SupplierPerformanceRow(n.Id, n.Name, done.Count, timed.Count == 0 ? null : timed.Average().ToString("F1", CultureInfo.InvariantCulture), sla.Count, slaOnTime, visit.Count, visitOnTime, rate,
                bills.Count, Money(bills.Sum(b => b.GrossAmount)), sample >= MinimumScoreSample ? rate : null, sample);
        }).ToArray();
        return new(from, to, property, MinimumScoreSample, rows.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), page, pageSize, rows.Length);
    }
}
