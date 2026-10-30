using System.Data;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    private sealed record ParsedPlan(SupplierMaintenancePlanRequest Request, MaintenanceType Type, SupplierMaintenanceFrequency Frequency, SupplierNonWorkingDayRule Rule);
    private sealed record PlannedOccurrence(int Sequence, DateOnly Nominal, DateOnly Planned, bool Conflict);
    private static readonly SupplierWorkOrderStatus[] FinishedOrders = [SupplierWorkOrderStatus.CLOSED, SupplierWorkOrderStatus.REJECTED, SupplierWorkOrderStatus.CANCELLED];
    private IQueryable<SupplierMaintenanceSchedule> ScheduleRows(Guid tenant, Guid supplier) => db.Set<SupplierMaintenanceSchedule>().Where(x => x.TenantId == tenant && x.SupplierId == supplier);
    private IQueryable<SupplierMaintenanceOccurrence> OccurrenceRows(Guid tenant, Guid supplier) => db.Set<SupplierMaintenanceOccurrence>().Where(x => x.TenantId == tenant && x.SupplierId == supplier);
    private static T Parse<T>(string? value, string field) where T : struct, Enum
    { if (!Enum.TryParse<T>(value, out var parsed) || !Enum.IsDefined(parsed) || parsed.ToString() != value) InvalidSupplier(field); return parsed; }
    private static string? MaintenanceText(string? value, int max, bool required, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) { if (required) InvalidSupplier(field); return null; }
        try { return SupplierMaintenanceSchedule.Text(value, field, max); } catch (ArgumentException) { InvalidSupplier(field); throw; }
    }
    private static ParsedPlan ParsePlan(SupplierMaintenancePlanRequest? p)
    {
        if (p is null || p.PropertyId == Guid.Empty || p.AreaServiceId == Guid.Empty || p.ContractId == Guid.Empty) { InvalidSupplier("plan"); throw new InvalidOperationException(); }
        MaintenanceText(p.Title, MaintenancePlan.TitleLength, true, "title"); MaintenanceText(p.Description, MaintenancePlan.DescriptionLength, false, "description");
        if (p.StartDate.Year < 2000 || p.EndDate.Year > 2100 || p.EndDate < p.StartDate || p.EndDate.DayNumber - p.StartDate.DayNumber > SupplierMaintenanceSchedule.MaxSpanDays || p.ReminderDays is < 0 or > SupplierMaintenanceSchedule.MaxReminderDays) InvalidSupplier("period");
        return new(p, Parse<MaintenanceType>(p.Type, "type"), Parse<SupplierMaintenanceFrequency>(p.Frequency, "frequency"), Parse<SupplierNonWorkingDayRule>(p.NonWorkingDayRule, "nonWorkingDayRule"));
    }
    private async Task<bool> ValidMaintenanceContract(Guid tenant, Guid supplier, ParsedPlan plan, CancellationToken token)
    {
        if (!plan.Request.ContractId.HasValue) return true;
        var contract = await ContractRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == plan.Request.ContractId && x.PropertyId == plan.Request.PropertyId && x.AreaServiceId == plan.Request.AreaServiceId, token);
        if (contract is null) return false;
        if (contract.Status != SupplierContractStatus.ACTIVE || plan.Request.StartDate < contract.StartDate || plan.Request.EndDate > contract.EndDate) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceState.Create());
        return true;
    }
    private async Task<IReadOnlyList<PlannedOccurrence>> PlanOccurrences(Guid tenant, ParsedPlan plan, CancellationToken token)
    {
        var p = plan.Request;
        IReadOnlyList<DateOnly> nominal;
        try { nominal = SupplierMaintenanceSchedule.NominalDates(p.StartDate, p.EndDate, plan.Frequency); } catch (ArgumentException) { InvalidSupplier("frequency"); throw; }
        var holidays = (await db.Set<SupplierHoliday>().AsNoTracking().Where(x => x.TenantId == tenant && x.Date >= p.StartDate.AddDays(-31) && x.Date <= p.EndDate.AddDays(31)).Select(x => x.Date).ToArrayAsync(token)).ToHashSet();
        var planned = new List<(int Sequence, DateOnly Nominal, DateOnly Planned)>();
        foreach (var date in nominal)
        {
            try { planned.Add((planned.Count + 1, date, SupplierMaintenanceSchedule.PlannedDate(date, plan.Rule, holidays, p.StartDate, p.EndDate))); }
            catch (ArgumentException) { throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceConflict.Create()); }
        }
        var dates = planned.Select(x => x.Planned).ToArray();
        var busy = (await (from o in db.Set<SupplierMaintenanceOccurrence>().AsNoTracking()
                           join s in db.Set<SupplierMaintenanceSchedule>() on new { o.TenantId, o.SupplierId, o.PropertyId, Id = o.ScheduleId } equals new { s.TenantId, s.SupplierId, s.PropertyId, s.Id }
                           where o.TenantId == tenant && o.PropertyId == p.PropertyId && s.AreaServiceId == p.AreaServiceId && s.Status == SupplierMaintenanceScheduleStatus.ACTIVE && o.Status == SupplierMaintenanceOccurrenceStatus.PLANNED && dates.Contains(o.PlannedDate)
                           select o.PlannedDate).ToArrayAsync(token)).ToHashSet();
        return planned.Select(x => new PlannedOccurrence(x.Sequence, x.Nominal, x.Planned, busy.Contains(x.Planned) || planned.Count(y => y.Planned == x.Planned) > 1)).ToArray();
    }
    public async Task<SupplierMaintenancePreview?> PreviewMaintenance(Guid tenant, Guid supplier, SupplierMaintenancePlanRequest request, CancellationToken token)
    {
        var plan = ParsePlan(request);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await AreaAllowed(tenant, supplier, request.PropertyId, token) || !await ValidContractLinks(tenant, supplier, request.PropertyId, request.AreaServiceId, null, token) || !await ValidMaintenanceContract(tenant, supplier, plan, token)) return null;
        var dates = await PlanOccurrences(tenant, plan, token); await tx.CommitAsync(token);
        return new(dates.Select(x => new SupplierMaintenancePreviewDate(x.Sequence, x.Nominal, x.Planned, x.Nominal != x.Planned, x.Conflict)).ToArray(), dates.Count(x => x.Conflict));
    }

    private IQueryable<SupplierMaintenanceScheduleResponse> ScheduleResponses(Guid tenant, Guid supplier)
    {
        var today = ContractToday;
        return from s in ScheduleRows(tenant, supplier).AsNoTracking()
               join p in db.Properties on s.PropertyId equals p.Id
               join a in db.Set<SupplierAreaService>() on new { s.TenantId, s.SupplierId, s.PropertyId, Id = s.AreaServiceId } equals new { a.TenantId, a.SupplierId, a.PropertyId, a.Id }
               join v in db.Set<BuildingFlow.Domain.SupplierService>() on new { a.TenantId, a.SupplierId, Id = a.ServiceId } equals new { v.TenantId, v.SupplierId, v.Id }
               let occurrences = db.Set<SupplierMaintenanceOccurrence>().Where(o => o.TenantId == s.TenantId && o.SupplierId == s.SupplierId && o.PropertyId == s.PropertyId && o.ScheduleId == s.Id)
               select new SupplierMaintenanceScheduleResponse(s.Id, s.TenantId, s.SupplierId, s.PropertyId, p.Name, s.AreaServiceId, v.Name, s.ContractId,
                   db.Set<SupplierContract>().Where(c => c.Id == s.ContractId).Select(c => c.Title).FirstOrDefault(), s.Title, s.Description, s.Type.ToString(), s.Frequency.ToString(), s.StartDate, s.EndDate, s.ReminderDays, s.NonWorkingDayRule.ToString(), s.Status.ToString(), s.EndReason,
                   occurrences.Count(o => o.Status == SupplierMaintenanceOccurrenceStatus.PLANNED), occurrences.Count(o => o.Status == SupplierMaintenanceOccurrenceStatus.COMPLETED), occurrences.Count(o => o.Status == SupplierMaintenanceOccurrenceStatus.SKIPPED),
                   occurrences.Count(o => o.Status == SupplierMaintenanceOccurrenceStatus.PLANNED && o.PlannedDate < today), s.Version, s.CreatedAt);
    }
    private sealed class OccurrenceData
    {
        public SupplierMaintenanceOccurrence Record { get; init; } = null!;
        public string PropertyName { get; init; } = null!;
        public string ScheduleTitle { get; init; } = null!;
        public int ReminderDays { get; init; }
    }
    private IQueryable<OccurrenceData> LabeledOccurrences(Guid tenant, Guid supplier) =>
        from o in OccurrenceRows(tenant, supplier).AsNoTracking()
        join s in db.Set<SupplierMaintenanceSchedule>() on new { o.TenantId, o.SupplierId, o.PropertyId, Id = o.ScheduleId } equals new { s.TenantId, s.SupplierId, s.PropertyId, s.Id }
        join p in db.Properties on o.PropertyId equals p.Id
        select new OccurrenceData { Record = o, PropertyName = p.Name, ScheduleTitle = s.Title, ReminderDays = s.ReminderDays };
    private SupplierMaintenanceOccurrenceResponse OccurrenceResponse(OccurrenceData x)
    {
        var o = x.Record; var today = ContractToday;
        return new(o.Id, o.ScheduleId, o.PropertyId, x.PropertyName, x.ScheduleTitle, o.Sequence, o.NominalDate, o.PlannedDate, o.MaintenancePlanId, o.Status.ToString(), o.IsOverdue(today), o.IsReminderDue(today, x.ReminderDays), o.CompletedOn, o.DelayDays, o.CompletionNote, o.WorkOrderId, o.SkipReason, o.Version);
    }
    public async Task<PagedRows<SupplierMaintenanceScheduleResponse>?> MaintenanceSchedules(Guid tenant, Guid supplier, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token) || !await Rows(tenant).AnyAsync(x => x.Id == supplier, token)) return null;
        var scopes = PropertyScopes(tenant); var rows = ScheduleResponses(tenant, supplier).Where(x => scopes.Any(s => s.PropertyId == x.PropertyId));
        var count = await rows.CountAsync(token); var items = await rows.OrderBy(x => x.Status).ThenBy(x => x.StartDate).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, count);
    }
    public async Task<SupplierMaintenanceScheduleResponse?> MaintenanceSchedule(Guid tenant, Guid supplier, Guid id, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        var row = await ScheduleResponses(tenant, supplier).SingleOrDefaultAsync(x => x.Id == id, token);
        if (row is null || !await AreaAllowed(tenant, supplier, row.PropertyId, token)) return null;
        await tx.CommitAsync(token); return row;
    }
    public async Task<PagedRows<SupplierMaintenanceOccurrenceResponse>?> MaintenanceOccurrences(Guid tenant, Guid supplier, SupplierMaintenanceOccurrenceQuery q, CancellationToken token)
    {
        Page(q.Page, q.PageSize);
        if (q.ScheduleId == Guid.Empty || q.From?.Year < 2000 || q.To?.Year > 2100 || q.From > q.To || (q.From.HasValue && q.To.HasValue && q.To.Value.DayNumber - q.From.Value.DayNumber > 366)) InvalidSupplier("range");
        SupplierMaintenanceOccurrenceStatus? status = q.Status is null ? null : Parse<SupplierMaintenanceOccurrenceStatus>(q.Status, "status");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token) || !await Rows(tenant).AnyAsync(x => x.Id == supplier, token)) return null;
        var scopes = PropertyScopes(tenant); var rows = LabeledOccurrences(tenant, supplier).Where(x => scopes.Any(s => s.PropertyId == x.Record.PropertyId));
        if (q.ScheduleId.HasValue) rows = rows.Where(x => x.Record.ScheduleId == q.ScheduleId);
        if (q.From.HasValue) rows = rows.Where(x => x.Record.PlannedDate >= q.From.Value);
        if (q.To.HasValue) rows = rows.Where(x => x.Record.PlannedDate <= q.To.Value);
        if (status.HasValue) rows = rows.Where(x => x.Record.Status == status.Value);
        if (q.ReminderDue) { var today = ContractToday; rows = rows.Where(x => x.Record.Status == SupplierMaintenanceOccurrenceStatus.PLANNED && x.Record.PlannedDate.AddDays(-x.ReminderDays) <= today); }
        var count = await rows.CountAsync(token); var items = await rows.OrderBy(x => x.Record.PlannedDate).ThenBy(x => x.Record.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items.Select(OccurrenceResponse).ToArray(), q.Page, q.PageSize, count);
    }
    public async Task<PagedRows<SupplierContractHistory>?> MaintenanceHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        var schedule = await ScheduleRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
        if (schedule is null || !await AreaAllowed(tenant, supplier, schedule.PropertyId, token)) return null;
        var rows = db.Set<SupplierMaintenanceRevision>().AsNoTracking().Where(x => x.TenantId == tenant && x.SupplierId == supplier && x.PropertyId == schedule.PropertyId && x.ScheduleId == id);
        var count = await rows.CountAsync(token); var items = await rows.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierContractHistory(x.Id, x.Action, x.AppliedVersion, x.Snapshot, x.CreatedAt)).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, count);
    }

    private async Task<SupplierMaintenanceRevision?> MaintenanceReceipt(Guid tenant, Guid supplier, Guid operation, string fingerprint, bool occurrence, CancellationToken token)
    {
        var receipt = await db.Set<SupplierMaintenanceRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenant && x.ActorIssuer == actor.Issuer && x.ActorSubject == actor.UserId && x.OperationId == operation, token);
        if (receipt is not null && (receipt.SupplierId != supplier || receipt.Fingerprint != fingerprint || receipt.OccurrenceId.HasValue != occurrence)) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        return receipt;
    }
    private void MaintenanceRevision(SupplierMaintenanceSchedule schedule, SupplierMaintenanceOccurrence? occurrence, Guid operation, string fingerprint, string action, long applied, object snapshot) =>
        db.Add(new SupplierMaintenanceRevision(schedule, occurrence?.Id, operation, actor.Issuer!, actor.UserId!, fingerprint, action, JsonSerializer.Serialize(snapshot), applied));
    private static object ScheduleSnapshot(SupplierMaintenanceSchedule s) => new { s.Title, s.Description, Type = s.Type.ToString(), Frequency = s.Frequency.ToString(), s.StartDate, s.EndDate, s.ReminderDays, Rule = s.NonWorkingDayRule.ToString(), s.AreaServiceId, s.ContractId, Status = s.Status.ToString(), s.EndReason };
    private static object OccurrenceSnapshot(SupplierMaintenanceOccurrence o, string? reason = null) => new { o.Sequence, o.PlannedDate, Status = o.Status.ToString(), o.CompletedOn, o.DelayDays, o.CompletionNote, o.WorkOrderId, o.SkipReason, Reason = reason };
    private async Task<SupplierMaintenanceScheduleMutation> ScheduleMutation(Guid tenant, Guid supplier, Guid id, Supplier parent, bool replayed, long applied, CancellationToken token) =>
        new(await ScheduleResponses(tenant, supplier).SingleAsync(x => x.Id == id, token), Response(parent), replayed, applied);
    private async Task<SupplierMaintenanceOccurrenceMutation> OccurrenceMutation(Guid tenant, Guid supplier, Guid id, bool replayed, long applied, CancellationToken token) =>
        new(OccurrenceResponse(await LabeledOccurrences(tenant, supplier).SingleAsync(x => x.Record.Id == id, token)), replayed, applied);
    private Task<bool> HasOpenOrder(Guid tenant, Guid property, Guid plan, CancellationToken token) =>
        db.Set<SupplierWorkOrder>().AnyAsync(x => x.TenantId == tenant && x.PropertyId == property && x.MaintenancePlanId == plan && !FinishedOrders.Contains(x.Status), token);
    private static void PlanStatus(MaintenancePlan plan, MaintenanceStatus status) => plan.Edit(plan.Title, plan.Description, plan.Type, plan.PlannedDate, plan.Location, plan.Responsible, status);

    public async Task<SupplierMaintenanceScheduleMutation?> CreateMaintenanceSchedule(Guid tenant, Guid supplier, SupplierMaintenanceScheduleCreate c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.SupplierVersion < 1) InvalidSupplier("version");
        var plan = ParsePlan(c.Plan); var p = plan.Request;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var scope = await LockArea(tenant, supplier, p.PropertyId, true, token); if (scope is null) return null;
        var fingerprint = Hash(new { action = "MAINTENANCE_CREATE", tenant, supplier, c.SupplierVersion, p.PropertyId, p.AreaServiceId, p.ContractId, title = p.Title.Trim(), description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim(), p.Type, p.Frequency, p.StartDate, p.EndDate, p.ReminderDays, p.NonWorkingDayRule });
        var receipt = await MaintenanceReceipt(tenant, supplier, c.OperationId, fingerprint, false, token);
        if (receipt is not null) { var replay = await ScheduleMutation(tenant, supplier, receipt.ScheduleId, scope.Value.Supplier, true, receipt.AppliedVersion, token); await tx.CommitAsync(token); return replay; }
        Expected(c.SupplierVersion, scope.Value.Supplier.Version);
        if (!await ValidContractLinks(tenant, supplier, p.PropertyId, p.AreaServiceId, null, token) || !await ValidMaintenanceContract(tenant, supplier, plan, token)) return null;
        var dates = await PlanOccurrences(tenant, plan, token);
        if (dates.Count == 0 || dates.Any(x => x.Conflict)) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceConflict.Create());
        var schedule = new SupplierMaintenanceSchedule(tenant, supplier, p.PropertyId, p.AreaServiceId, p.ContractId, p.Title, p.Description, plan.Type, plan.Frequency, p.StartDate, p.EndDate, p.ReminderDays, plan.Rule);
        db.Add(schedule);
        foreach (var date in dates)
        {
            var work = new MaintenancePlan(p.PropertyId, schedule.Title, schedule.Description, schedule.Type, date.Planned, null, scope.Value.Supplier.Name, MaintenanceStatus.PLANNED);
            db.Add(work); db.Add(new SupplierMaintenanceOccurrence(schedule, date.Sequence, date.Nominal, date.Planned, work.Id));
        }
        MaintenanceRevision(schedule, null, c.OperationId, fingerprint, "CREATE", 1, new { Schedule = ScheduleSnapshot(schedule), Dates = dates.Select(x => x.Planned) });
        TouchArea(scope.Value); await db.SaveChangesAsync(token);
        var result = await ScheduleMutation(tenant, supplier, schedule.Id, scope.Value.Supplier, false, 1, token); await tx.CommitAsync(token); return result;
    }
    public async Task<SupplierMaintenanceScheduleMutation?> EndMaintenanceSchedule(Guid tenant, Guid supplier, Guid id, SupplierMaintenanceScheduleEnd c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1) InvalidSupplier("version");
        var reason = MaintenanceText(c.Reason, 1000, true, "reason")!;
        if (!await Allowed(tenant, token)) return null;
        var property = await ScheduleRows(tenant, supplier).Where(x => x.Id == id).Select(x => (Guid?)x.PropertyId).SingleOrDefaultAsync(token); if (property is null) return null;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var scope = await LockArea(tenant, supplier, property.Value, false, token); if (scope is null) return null;
        var fingerprint = Hash(new { action = "MAINTENANCE_END", tenant, supplier, id, c.Version, reason });
        var receipt = await MaintenanceReceipt(tenant, supplier, c.OperationId, fingerprint, false, token);
        if (receipt is not null) { var replay = await ScheduleMutation(tenant, supplier, receipt.ScheduleId, scope.Value.Supplier, true, receipt.AppliedVersion, token); await tx.CommitAsync(token); return replay; }
        var schedule = await ScheduleRows(tenant, supplier).SingleAsync(x => x.Id == id, token); Expected(c.Version, schedule.Version);
        if (schedule.Status != SupplierMaintenanceScheduleStatus.ACTIVE) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceState.Create());
        var pending = await OccurrenceRows(tenant, supplier).Where(x => x.ScheduleId == id && x.Status == SupplierMaintenanceOccurrenceStatus.PLANNED).ToArrayAsync(token);
        var plans = pending.Select(x => x.MaintenancePlanId).ToArray();
        if (await db.Set<SupplierWorkOrder>().AnyAsync(x => x.TenantId == tenant && x.PropertyId == schedule.PropertyId && x.MaintenancePlanId != null && plans.Contains(x.MaintenancePlanId.Value) && !FinishedOrders.Contains(x.Status), token)) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceState.Create());
        foreach (var work in await db.MaintenancePlans.Where(x => x.PropertyId == schedule.PropertyId && plans.Contains(x.Id) && x.Status == MaintenanceStatus.PLANNED).ToArrayAsync(token)) PlanStatus(work, MaintenanceStatus.CANCELLED);
        foreach (var occurrence in pending) occurrence.Skip(reason);
        schedule.End(reason); MaintenanceRevision(schedule, null, c.OperationId, fingerprint, "END", c.Version + 1, new { Schedule = ScheduleSnapshot(schedule), Skipped = pending.Length });
        TouchArea(scope.Value); await db.SaveChangesAsync(token);
        var result = await ScheduleMutation(tenant, supplier, id, scope.Value.Supplier, false, schedule.Version, token); await tx.CommitAsync(token); return result;
    }
    private async Task<(SupplierMaintenanceOccurrence Occurrence, SupplierMaintenanceSchedule Schedule, (Supplier, Property) Scope)?> LockOccurrence(Guid tenant, Guid supplier, Guid id, CancellationToken token)
    {
        if (!await Allowed(tenant, token)) return null;
        var property = await OccurrenceRows(tenant, supplier).Where(x => x.Id == id).Select(x => (Guid?)x.PropertyId).SingleOrDefaultAsync(token); if (property is null) return null;
        var scope = await LockArea(tenant, supplier, property.Value, false, token); if (scope is null) return null;
        var occurrence = await OccurrenceRows(tenant, supplier).SingleAsync(x => x.Id == id, token);
        var schedule = await ScheduleRows(tenant, supplier).SingleAsync(x => x.Id == occurrence.ScheduleId && x.PropertyId == occurrence.PropertyId, token);
        return (occurrence, schedule, scope.Value);
    }
    public async Task<SupplierMaintenanceOccurrenceMutation?> CompleteMaintenance(Guid tenant, Guid supplier, Guid id, SupplierMaintenanceComplete c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1 || c.WorkOrderId == Guid.Empty || c.CompletedOn.Year < 2000) InvalidSupplier("version");
        var note = MaintenanceText(c.Note, 4000, true, "note")!;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await LockOccurrence(tenant, supplier, id, token); if (locked is null) return null; var (occurrence, schedule, scope) = locked.Value;
        var fingerprint = Hash(new { action = "MAINTENANCE_COMPLETE", tenant, supplier, id, c.Version, c.CompletedOn, note, c.WorkOrderId });
        var receipt = await MaintenanceReceipt(tenant, supplier, c.OperationId, fingerprint, true, token);
        if (receipt is not null) { var replay = await OccurrenceMutation(tenant, supplier, receipt.OccurrenceId!.Value, true, receipt.AppliedVersion, token); await tx.CommitAsync(token); return replay; }
        Expected(c.Version, occurrence.Version);
        if (occurrence.Status != SupplierMaintenanceOccurrenceStatus.PLANNED) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceState.Create());
        if (c.WorkOrderId.HasValue)
        {
            var order = await OrderRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.WorkOrderId && x.PropertyId == occurrence.PropertyId && x.MaintenancePlanId == occurrence.MaintenancePlanId, token);
            if (order is null) return null;
            if (order.Status is not (SupplierWorkOrderStatus.VERIFIED or SupplierWorkOrderStatus.CLOSED)) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceState.Create());
        }
        else if (await HasOpenOrder(tenant, occurrence.PropertyId, occurrence.MaintenancePlanId, token)) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceState.Create());
        var work = await db.MaintenancePlans.SingleOrDefaultAsync(x => x.PropertyId == occurrence.PropertyId && x.Id == occurrence.MaintenancePlanId, token);
        if (work is null) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceState.Create());
        try { occurrence.Complete(c.CompletedOn, note, c.WorkOrderId, ContractToday); } catch (ArgumentException) { InvalidSupplier("completedOn"); throw; }
        PlanStatus(work, MaintenanceStatus.COMPLETED); MaintenanceRevision(schedule, occurrence, c.OperationId, fingerprint, "COMPLETE", c.Version + 1, OccurrenceSnapshot(occurrence));
        TouchArea(scope); await db.SaveChangesAsync(token);
        var result = await OccurrenceMutation(tenant, supplier, id, false, occurrence.Version, token); await tx.CommitAsync(token); return result;
    }
    public async Task<SupplierMaintenanceOccurrenceMutation?> SkipMaintenance(Guid tenant, Guid supplier, Guid id, SupplierMaintenanceSkip c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1) InvalidSupplier("version");
        var reason = MaintenanceText(c.Reason, 1000, true, "reason")!;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await LockOccurrence(tenant, supplier, id, token); if (locked is null) return null; var (occurrence, schedule, scope) = locked.Value;
        var fingerprint = Hash(new { action = "MAINTENANCE_SKIP", tenant, supplier, id, c.Version, reason });
        var receipt = await MaintenanceReceipt(tenant, supplier, c.OperationId, fingerprint, true, token);
        if (receipt is not null) { var replay = await OccurrenceMutation(tenant, supplier, receipt.OccurrenceId!.Value, true, receipt.AppliedVersion, token); await tx.CommitAsync(token); return replay; }
        Expected(c.Version, occurrence.Version);
        if (occurrence.Status != SupplierMaintenanceOccurrenceStatus.PLANNED || await HasOpenOrder(tenant, occurrence.PropertyId, occurrence.MaintenancePlanId, token)) throw new HierarchyConflictException(ErrorCatalog.SupplierMaintenanceState.Create());
        var work = await db.MaintenancePlans.SingleOrDefaultAsync(x => x.PropertyId == occurrence.PropertyId && x.Id == occurrence.MaintenancePlanId, token);
        if (work is not null && work.Status == MaintenanceStatus.PLANNED) PlanStatus(work, MaintenanceStatus.CANCELLED);
        occurrence.Skip(reason); MaintenanceRevision(schedule, occurrence, c.OperationId, fingerprint, "SKIP", c.Version + 1, OccurrenceSnapshot(occurrence, reason));
        TouchArea(scope); await db.SaveChangesAsync(token);
        var result = await OccurrenceMutation(tenant, supplier, id, false, occurrence.Version, token); await tx.CommitAsync(token); return result;
    }

    public async Task<PagedRows<SupplierHolidayResponse>?> Holidays(Guid tenant, int year, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); if (year is < 2000 or > 2100) InvalidSupplier("year");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null;
        var from = new DateOnly(year, 1, 1); var to = new DateOnly(year, 12, 31);
        var rows = db.Set<SupplierHoliday>().AsNoTracking().Where(x => x.TenantId == tenant && x.Date >= from && x.Date <= to);
        var count = await rows.CountAsync(token); var items = await rows.OrderBy(x => x.Date).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierHolidayResponse(x.Id, x.TenantId, x.Date, x.Name, x.Version)).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, count);
    }
    public async Task<SupplierHolidayMutation?> AddHoliday(Guid tenant, SupplierHolidayRequest c, CancellationToken token)
    {
        var name = MaintenanceText(c.Name, 200, true, "name")!; if (c.Date.Year is < 2000 or > 2100) InvalidSupplier("date");
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (!await Allowed(tenant, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE", token);
        // The date is the natural key: retrying the same holiday returns it, a different name for the same day is a conflict.
        var existing = await db.Set<SupplierHoliday>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.Date == c.Date, token);
        if (existing is not null)
        {
            if (existing.Name != name) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            await tx.CommitAsync(token); return new(new(existing.Id, tenant, existing.Date, existing.Name, existing.Version), true);
        }
        var row = new SupplierHoliday(tenant, c.Date, name); db.Add(row); await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(new(row.Id, tenant, row.Date, row.Name, row.Version), false);
    }
    public async Task<bool> RemoveHoliday(Guid tenant, Guid id, long version, CancellationToken token)
    {
        if (version < 1) InvalidSupplier("version");
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (!await Allowed(tenant, token)) return false;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE", token);
        var row = await db.Set<SupplierHoliday>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.Id == id, token); if (row is null) return false;
        Expected(version, row.Version); db.Remove(row); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
    }
}
