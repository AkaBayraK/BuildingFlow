using BuildingFlow.Domain;
namespace BuildingFlow.Application;

// TNM 01: code-owned registry of parametric lists. Values come from the domain enums/arrays that already validate
// writes, so this endpoint can never advertise a code the backend would reject. TNM 02 adds system-wide presentation
// overrides for lists planned for TNM 02; area-specific settings arrive with TNM 03.
public enum DefinitionGroup { PROPERTY, UNIT, FINANCE, REQUEST, MAINTENANCE, PARKING, SUPPLIER, COMMUNICATION, DOCUMENT }
public enum DefinitionLevel { FIXED, SYSTEM, PROPERTY }
public sealed record DefinitionList(string Key, string Group, string Level, string Source, string? PlannedPhase, IReadOnlyList<string> Values);
public sealed record DefinitionCatalog(string Key, string Group, string Scope, string? Route);
public sealed record DefinitionsResponse(IReadOnlyList<DefinitionList> Lists, IReadOnlyList<DefinitionCatalog> Catalogs);

public static class DefinitionRegistry
{
    public static readonly DefinitionsResponse Current = new(
    [
        List<PropertyType>("PROPERTY_TYPE", DefinitionGroup.PROPERTY, DefinitionLevel.FIXED, null),
        List<CommonAreaType>("COMMON_AREA_TYPE", DefinitionGroup.PROPERTY, DefinitionLevel.PROPERTY, "TNM_02"),
        List<UnitType>("UNIT_TYPE", DefinitionGroup.UNIT, DefinitionLevel.FIXED, null),
        List<ChargeKind>("CHARGE_KIND", DefinitionGroup.FINANCE, DefinitionLevel.FIXED, null),
        List<ChargeCategory>("CHARGE_CATEGORY", DefinitionGroup.FINANCE, DefinitionLevel.SYSTEM, "TNM_05"),
        List<ExpenseCategory>("EXPENSE_CATEGORY", DefinitionGroup.FINANCE, DefinitionLevel.SYSTEM, "TNM_05"),
        List<PaymentMethod>("PAYMENT_METHOD", DefinitionGroup.FINANCE, DefinitionLevel.FIXED, null),
        new("REQUEST_CATEGORY", nameof(DefinitionGroup.REQUEST), nameof(DefinitionLevel.PROPERTY), nameof(MobileRequest) + "." + nameof(MobileRequest.Categories), "TNM_02", MobileRequest.Categories),
        new("REQUEST_PRIORITY", nameof(DefinitionGroup.REQUEST), nameof(DefinitionLevel.SYSTEM), nameof(MobileRequest) + "." + nameof(MobileRequest.Priorities), "TNM_02", MobileRequest.Priorities),
        List<MaintenanceType>("MAINTENANCE_TYPE", DefinitionGroup.MAINTENANCE, DefinitionLevel.PROPERTY, "TNM_02"),
        List<SupplierMaintenanceFrequency>("MAINTENANCE_FREQUENCY", DefinitionGroup.MAINTENANCE, DefinitionLevel.FIXED, null),
        List<VehicleType>("VEHICLE_TYPE", DefinitionGroup.PARKING, DefinitionLevel.SYSTEM, "TNM_02"),
        List<ParkingSpaceType>("PARKING_SPACE_TYPE", DefinitionGroup.PARKING, DefinitionLevel.FIXED, null),
        List<SupplierKind>("SUPPLIER_KIND", DefinitionGroup.SUPPLIER, DefinitionLevel.FIXED, null),
        List<AnnouncementCategory>("ANNOUNCEMENT_CATEGORY", DefinitionGroup.COMMUNICATION, DefinitionLevel.PROPERTY, "TNM_02"),
        List<SurveyCategory>("SURVEY_CATEGORY", DefinitionGroup.COMMUNICATION, DefinitionLevel.SYSTEM, "TNM_02"),
    ],
    [
        new("SUPPLIER_CATEGORIES", nameof(DefinitionGroup.SUPPLIER), "TENANT", "/suppliers"),
        new("DUES_CANCELLATION_REASONS", nameof(DefinitionGroup.FINANCE), "SYSTEM", null),
        new("CONSENT_TYPES", nameof(DefinitionGroup.COMMUNICATION), "SYSTEM", "/consents"),
        new("SMS_TEMPLATES", nameof(DefinitionGroup.COMMUNICATION), "PROPERTY", "/sms"),
        new("PAYMENT_PROVIDERS", nameof(DefinitionGroup.FINANCE), "SYSTEM", "/system-settings"),
    ]);

    private static DefinitionList List<T>(string key, DefinitionGroup group, DefinitionLevel level, string? phase) where T : struct, Enum =>
        new(key, group.ToString(), level.ToString(), typeof(T).Name, phase, Enum.GetNames<T>());
}

public sealed record DefinitionValueView(string Code, string? NameTr, string? NameEn, string? IconKey, string? Tone, int SortOrder, bool IsEnabled, bool Locked, long Version);
public sealed record DefinitionListView(string Key, string Group, string Level, string Source, string? PlannedPhase, bool Editable, IReadOnlyList<DefinitionValueView> Values);
public sealed record DefinitionsView(IReadOnlyList<DefinitionListView> Lists, IReadOnlyList<DefinitionCatalog> Catalogs);
public sealed record SaveDefinitionValueRequest(string? NameTr, string? NameEn, string? IconKey, string? Tone, int? SortOrder, bool? IsEnabled, long Version);

public interface IDefinitionStore
{
    Task<DefinitionsView> ListAsync(CancellationToken token);
    Task<DefinitionValueView?> SaveAsync(string listKey, string code, SaveDefinitionValueRequest request, CancellationToken token);
}

public static class DefinitionRules
{
    public const string EditablePhase = "TNM_02";
    // Fallback codes stay selectable so every form always has a neutral choice.
    public static readonly IReadOnlySet<string> LockedCodes = new HashSet<string>(["OTHER", "GENERAL", "NORMAL"], StringComparer.Ordinal);
    public static readonly IReadOnlySet<string> Tones = new HashSet<string>(["blue", "green", "amber", "teal", "red", "violet", "neutral"], StringComparer.Ordinal);
    // Icon keys the web and mobile clients can render from their existing icon sets; free text is rejected.
    public static readonly IReadOnlySet<string> IconKeys = new HashSet<string>([
        "wrench", "droplet", "zap", "flame", "paint-roller", "hammer", "sparkles", "shield-check", "building-2", "lightbulb",
        "ellipsis", "megaphone", "calendar-check-2", "car-front", "bike", "truck", "trees", "dog", "users", "coins",
        "concierge-bell", "party-popper", "square-parking", "messages-square", "waves", "dumbbell", "presentation", "baby",
        "flower-2", "trash-2", "package", "siren", "bell", "clipboard-check", "leaf", "key-round"], StringComparer.Ordinal);
    public static bool Editable(DefinitionList list) => list.Level != nameof(DefinitionLevel.FIXED) && list.PlannedPhase == EditablePhase;
    public static bool Locked(string code) => LockedCodes.Contains(code);
}
