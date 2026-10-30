using BuildingFlow.Domain;
namespace BuildingFlow.Application;

// TNM 01: code-owned registry of parametric lists. Values come from the domain enums/arrays that already validate
// writes, so this endpoint can never advertise a code the backend would reject. Editing arrives in TNM 02/03.
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
