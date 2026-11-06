using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record AutomationSourceField(string Name, string[] Options);
public sealed record AutomationSourceInfo(string Code, AutomationSourceField[] Fields, string[] DateFields, string[] Triggers, string[] RecipientScopes, string[] Variables);
public sealed record AutomationTypeRequest(string? Name, string? Icon, string? SourceCode, AutomationCondition[]? Conditions, string? Trigger, string? DateField,
    int[]? DefaultOffsets, int? DayOfMonth, int? DayOfWeek, string[]? RecipientScopes, string? DefaultTitle, string? DefaultBody, bool Platform, string? Reason, long Version = 0);
public sealed record AutomationTypeStatusRequest(string? Reason, long Version);
public sealed record AutomationTypeResponse(Guid Id, Guid? PropertyId, bool IsPlatform, string Code, string Name, string Icon, string SourceCode, AutomationCondition[] Conditions,
    string Trigger, string? DateField, int[] DefaultOffsets, int? DayOfMonth, int? DayOfWeek, string[] RecipientScopes, string DefaultTitle, string DefaultBody,
    string Status, int TypeVersion, int RuleCount, bool CanEdit, long Version);
public sealed record AutomationTypePreview(int Records, int Recipients, bool Truncated, IReadOnlyList<AutomationPreviewItem> Items);

// Code-owned building blocks. A user-defined type may only pick from these; every source query is written and Property-scoped in code.
public static class AutomationSources
{
    public const string Request = "REQUEST", SupplierContract = "SUPPLIER_CONTRACT", Unit = "UNIT", Maintenance = "MAINTENANCE_PLAN", Meter = "METER", Reservation = "RESERVATION";
    public const string Managers = "MANAGERS", Owners = "OWNERS", Tenants = "TENANTS", Residents = "RESIDENTS", PrimaryContact = "PRIMARY_CONTACT", Requester = "REQUESTER";
    public const string Eq = "EQ", In = "IN", NotIn = "NOT_IN";
    public static readonly string[] Operators = [Eq, In, NotIn];
    public static readonly string[] Icons = ["BellRing", "Wrench", "FileSignature", "Building2", "CalendarCheck2", "Gauge", "CalendarDays", "Megaphone", "ShieldAlert", "Sparkles"];
    private static readonly string[] Common = ["propertyName", "today"];
    private static readonly string[] Unit3 = [Owners, Tenants, Residents, PrimaryContact];
    private static readonly string[] AllTriggers = [nameof(AutomationTriggerKind.DATE_OFFSET), nameof(AutomationTriggerKind.EVENT), nameof(AutomationTriggerKind.CALENDAR)];
    private static readonly string[] DateAndCalendar = [nameof(AutomationTriggerKind.DATE_OFFSET), nameof(AutomationTriggerKind.CALENDAR)];

    public static IReadOnlyList<AutomationSourceInfo> All { get; } = Array.AsReadOnly(new[]
    {
        new AutomationSourceInfo(Request,
            [new("category", MobileRequest.Categories), new("priority", MobileRequest.Priorities), new("status", ["RECORDED", "IN_PROGRESS", "RESOLVED", "REJECTED"])],
            ["createdAt", "updatedAt"], AllTriggers, [Managers, Requester, .. Unit3],
            [.. Common, "unitNumber", "requestCategory", "requestPriority", "requestStatus", "createdDate", "daysOpen"]),
        new AutomationSourceInfo(SupplierContract,
            [new("status", Enum.GetNames<SupplierContractStatus>())], ["endDate", "startDate"], AllTriggers, [Managers],
            [.. Common, "contractTitle", "startDate", "endDate", "daysLeft", "amount"]),
        new AutomationSourceInfo(Unit, [new("type", Enum.GetNames<UnitType>())], [], [nameof(AutomationTriggerKind.CALENDAR)], [.. Unit3, Managers],
            [.. Common, "unitNumber"]),
        new AutomationSourceInfo(Maintenance,
            [new("type", Enum.GetNames<MaintenanceType>()), new("status", Enum.GetNames<MaintenanceStatus>())], ["plannedDate"], AllTriggers, [Managers, .. Unit3],
            [.. Common, "maintenanceTitle", "maintenanceType", "plannedDate", "location", "daysLeft"]),
        new AutomationSourceInfo(Meter, [new("kind", Enum.GetNames<MeterKind>())], ["lastReadingDate"], DateAndCalendar, [Managers, .. Unit3],
            [.. Common, "meterName", "meterKind", "lastReadingDate", "unitNumber"]),
        new AutomationSourceInfo(Reservation, [new("status", Enum.GetNames<CommonAreaReservationStatus>())], ["date"], AllTriggers, [Managers, .. Unit3],
            [.. Common, "commonAreaName", "reservationDate", "startsAt", "endsAt", "unitNumber"])
    });
    public static AutomationSourceInfo? Find(string? code) => All.SingleOrDefault(s => s.Code == code);

    // Returns the field name of the first rule the type breaks, or null when it only uses allowed building blocks.
    public static string? Problem(AutomationTypeSettings s)
    {
        var source = Find(s.SourceCode);
        if (source is null) return "sourceCode";
        if (!source.Triggers.Contains(s.Trigger.ToString())) return "trigger";
        if (s.Trigger == AutomationTriggerKind.DATE_OFFSET && !source.DateFields.Contains(s.DateField)) return "dateField";
        if (!Icons.Contains(s.Icon)) return "icon";
        if (s.RecipientScopes.Any(r => !source.RecipientScopes.Contains(r))) return "recipientScopes";
        foreach (var c in s.Conditions)
        {
            var field = source.Fields.SingleOrDefault(f => f.Name == c.Field);
            if (field is null || !Operators.Contains(c.Operator) || c.Values is null || c.Values.Length < 1 || c.Values.Length > 20 ||
                c.Operator == Eq && c.Values.Length != 1 || c.Values.Any(v => !field.Options.Contains(v))) return "conditions";
        }
        if (s.Conditions.Select(c => c.Field).Distinct().Count() != s.Conditions.Length) return "conditions";
        if (SmsText.Placeholders(s.DefaultTitle).Any(v => !source.Variables.Contains(v)) || SmsText.Placeholders(s.DefaultBody).Any(v => !source.Variables.Contains(v))) return "defaultBody";
        return null;
    }
}
