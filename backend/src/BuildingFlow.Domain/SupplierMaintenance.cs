namespace BuildingFlow.Domain;

public enum SupplierMaintenanceFrequency { WEEKLY, MONTHLY, QUARTERLY, SEMIANNUAL, YEARLY }
public enum SupplierNonWorkingDayRule { KEEP, NEXT_WORKING_DAY, PREVIOUS_WORKING_DAY }
public enum SupplierMaintenanceScheduleStatus { ACTIVE, ENDED }
public enum SupplierMaintenanceOccurrenceStatus { PLANNED, COMPLETED, SKIPPED }

public sealed class SupplierHoliday : BaseEntity
{
    private SupplierHoliday() { }
    public SupplierHoliday(Guid tenant, DateOnly date, string name)
    {
        if (tenant == Guid.Empty || date.Year is < 2000 or > 2100) throw new ArgumentException("Holiday scope required.");
        TenantId = tenant; Date = date; Name = SupplierMaintenanceSchedule.Text(name, nameof(name), 200);
    }
    public Guid TenantId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = null!;
}

public sealed class SupplierMaintenanceSchedule : BaseEntity
{
    public const int MaxSpanDays = 731, MaxOccurrences = 120, MaxReminderDays = 60;
    private SupplierMaintenanceSchedule() { }
    public SupplierMaintenanceSchedule(Guid tenant, Guid supplier, Guid property, Guid areaService, Guid? contract, string title, string? description, MaintenanceType type, SupplierMaintenanceFrequency frequency, DateOnly start, DateOnly end, int reminderDays, SupplierNonWorkingDayRule rule)
    {
        SupplierText.Scope(tenant, supplier); SupplierText.Scope(property, areaService);
        if (contract == Guid.Empty || !Enum.IsDefined(type) || !Enum.IsDefined(frequency) || !Enum.IsDefined(rule)) throw new ArgumentException("Invalid schedule scope.");
        if (start.Year < 2000 || end.Year > 2100 || end < start || end.DayNumber - start.DayNumber > MaxSpanDays || reminderDays is < 0 or > MaxReminderDays) throw new ArgumentException("Invalid schedule period.");
        TenantId = tenant; SupplierId = supplier; PropertyId = property; AreaServiceId = areaService; ContractId = contract;
        Title = Text(title, nameof(title), MaintenancePlan.TitleLength);
        Description = string.IsNullOrWhiteSpace(description) ? null : Text(description, nameof(description), MaintenancePlan.DescriptionLength);
        Type = type; Frequency = frequency; StartDate = start; EndDate = end; ReminderDays = reminderDays; NonWorkingDayRule = rule;
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid AreaServiceId { get; private set; }
    public Guid? ContractId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public MaintenanceType Type { get; private set; }
    public SupplierMaintenanceFrequency Frequency { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int ReminderDays { get; private set; }
    public SupplierNonWorkingDayRule NonWorkingDayRule { get; private set; }
    public SupplierMaintenanceScheduleStatus Status { get; private set; }
    public string? EndReason { get; private set; }
    public void End(string reason)
    {
        if (Status != SupplierMaintenanceScheduleStatus.ACTIVE) throw new InvalidOperationException("Schedule already ended.");
        EndReason = Text(reason, nameof(reason), 1000); Status = SupplierMaintenanceScheduleStatus.ENDED;
    }
    public static IReadOnlyList<DateOnly> NominalDates(DateOnly start, DateOnly end, SupplierMaintenanceFrequency frequency)
    {
        var months = frequency switch { SupplierMaintenanceFrequency.MONTHLY => 1, SupplierMaintenanceFrequency.QUARTERLY => 3, SupplierMaintenanceFrequency.SEMIANNUAL => 6, SupplierMaintenanceFrequency.YEARLY => 12, _ => 0 };
        var dates = new List<DateOnly>();
        // Month steps are taken from the start date each time so a 31st does not drift to the 28th after February.
        for (var i = 0; ; i++)
        {
            var date = months == 0 ? start.AddDays(7 * i) : start.AddMonths(months * i);
            if (date > end) return dates;
            if (dates.Count == MaxOccurrences) throw new ArgumentException("Too many occurrences.");
            dates.Add(date);
        }
    }
    public static bool IsWorkingDay(DateOnly date, IReadOnlySet<DateOnly> holidays) => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(date);
    public static DateOnly PlannedDate(DateOnly nominal, SupplierNonWorkingDayRule rule, IReadOnlySet<DateOnly> holidays, DateOnly start, DateOnly end)
    {
        if (rule == SupplierNonWorkingDayRule.KEEP || IsWorkingDay(nominal, holidays)) return nominal;
        var step = rule == SupplierNonWorkingDayRule.NEXT_WORKING_DAY ? 1 : -1; var date = nominal;
        for (var i = 0; i < 31 && !IsWorkingDay(date, holidays); i++) date = date.AddDays(step);
        if (!IsWorkingDay(date, holidays) || date < start || date > end) throw new ArgumentException("No working day inside the schedule period.");
        return date;
    }
    public static string Text(string value, string field, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field); var clean = value.Trim();
        if (clean.Length > max || clean.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t'))) throw new ArgumentException("Invalid maintenance text.", field);
        return clean;
    }
}

public sealed class SupplierMaintenanceOccurrence : BaseEntity
{
    private SupplierMaintenanceOccurrence() { }
    public SupplierMaintenanceOccurrence(SupplierMaintenanceSchedule schedule, int sequence, DateOnly nominal, DateOnly planned, Guid maintenancePlan)
    {
        if (sequence < 1 || maintenancePlan == Guid.Empty || planned < schedule.StartDate || planned > schedule.EndDate) throw new ArgumentException("Invalid occurrence.");
        TenantId = schedule.TenantId; SupplierId = schedule.SupplierId; PropertyId = schedule.PropertyId; ScheduleId = schedule.Id;
        Sequence = sequence; NominalDate = nominal; PlannedDate = planned; MaintenancePlanId = maintenancePlan;
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid ScheduleId { get; private set; }
    public int Sequence { get; private set; }
    public DateOnly NominalDate { get; private set; }
    public DateOnly PlannedDate { get; private set; }
    public Guid MaintenancePlanId { get; private set; }
    public SupplierMaintenanceOccurrenceStatus Status { get; private set; }
    public DateOnly? CompletedOn { get; private set; }
    public int? DelayDays { get; private set; }
    public string? CompletionNote { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public string? SkipReason { get; private set; }
    public bool IsOverdue(DateOnly today) => Status == SupplierMaintenanceOccurrenceStatus.PLANNED && PlannedDate < today;
    public bool IsReminderDue(DateOnly today, int reminderDays) => Status == SupplierMaintenanceOccurrenceStatus.PLANNED && PlannedDate.AddDays(-reminderDays) <= today;
    public void Complete(DateOnly completedOn, string note, Guid? workOrder, DateOnly today)
    {
        if (Status != SupplierMaintenanceOccurrenceStatus.PLANNED) throw new InvalidOperationException("Only a planned occurrence can be completed.");
        if (completedOn > today || completedOn < NominalDate.AddDays(-31) || workOrder == Guid.Empty) throw new ArgumentException("Invalid completion date.");
        CompletionNote = SupplierMaintenanceSchedule.Text(note, nameof(note), 4000); CompletedOn = completedOn; WorkOrderId = workOrder;
        DelayDays = Math.Max(0, completedOn.DayNumber - PlannedDate.DayNumber); Status = SupplierMaintenanceOccurrenceStatus.COMPLETED;
    }
    public void Skip(string reason)
    {
        if (Status != SupplierMaintenanceOccurrenceStatus.PLANNED) throw new InvalidOperationException("Only a planned occurrence can be skipped.");
        SkipReason = SupplierMaintenanceSchedule.Text(reason, nameof(reason), 1000); Status = SupplierMaintenanceOccurrenceStatus.SKIPPED;
    }
}

public sealed class SupplierMaintenanceRevision : BaseEntity
{
    private SupplierMaintenanceRevision() { }
    public SupplierMaintenanceRevision(SupplierMaintenanceSchedule schedule, Guid? occurrence, Guid operation, string issuer, string subject, string fingerprint, string action, string snapshot, long appliedVersion)
    {
        if (operation == Guid.Empty || occurrence == Guid.Empty || appliedVersion < 1 || action is not ("CREATE" or "END" or "COMPLETE" or "SKIP")) throw new ArgumentException("Invalid maintenance revision.");
        TenantId = schedule.TenantId; SupplierId = schedule.SupplierId; PropertyId = schedule.PropertyId; ScheduleId = schedule.Id; OccurrenceId = occurrence; OperationId = operation;
        ActorIssuer = Required(issuer, nameof(issuer), 500); ActorSubject = Required(subject, nameof(subject), 200); Fingerprint = Required(fingerprint, nameof(fingerprint), 64);
        Action = action; Snapshot = Required(snapshot, nameof(snapshot), 20000); AppliedVersion = appliedVersion;
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid ScheduleId { get; private set; }
    public Guid? OccurrenceId { get; private set; }
    public Guid OperationId { get; private set; }
    public string ActorIssuer { get; private set; } = null!;
    public string ActorSubject { get; private set; } = null!;
    public string Fingerprint { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Snapshot { get; private set; } = null!;
    public long AppliedVersion { get; private set; }
}
