using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierHolidayRequest(DateOnly Date, string Name);
public sealed record SupplierHolidayResponse(Guid Id, Guid TenantId, DateOnly Date, string Name, long Version);
public sealed record SupplierHolidayMutation(SupplierHolidayResponse Record, bool Replayed);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierMaintenancePlanRequest(Guid PropertyId, Guid AreaServiceId, Guid? ContractId, string Title, string? Description, string Type, string Frequency, DateOnly StartDate, DateOnly EndDate, int ReminderDays, string NonWorkingDayRule);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierMaintenanceScheduleCreate(Guid OperationId, long SupplierVersion, SupplierMaintenancePlanRequest Plan);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierMaintenanceScheduleEnd(Guid OperationId, long Version, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierMaintenanceComplete(Guid OperationId, long Version, DateOnly CompletedOn, string Note, Guid? WorkOrderId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierMaintenanceSkip(Guid OperationId, long Version, string Reason);
public sealed record SupplierMaintenancePreviewDate(int Sequence, DateOnly NominalDate, DateOnly PlannedDate, bool Shifted, bool Conflict);
public sealed record SupplierMaintenancePreview(IReadOnlyList<SupplierMaintenancePreviewDate> Dates, int ConflictCount);
public sealed record SupplierMaintenanceScheduleResponse(Guid Id, Guid TenantId, Guid SupplierId, Guid PropertyId, string PropertyName, Guid AreaServiceId, string ServiceName, Guid? ContractId, string? ContractTitle, string Title, string? Description, string Type, string Frequency, DateOnly StartDate, DateOnly EndDate, int ReminderDays, string NonWorkingDayRule, string Status, string? EndReason, int PlannedCount, int CompletedCount, int SkippedCount, int OverdueCount, long Version, DateTimeOffset CreatedAt);
public sealed record SupplierMaintenanceScheduleMutation(SupplierMaintenanceScheduleResponse Record, SupplierResponse Supplier, bool Replayed, long AppliedVersion);
public sealed record SupplierMaintenanceOccurrenceResponse(Guid Id, Guid ScheduleId, Guid PropertyId, string PropertyName, string ScheduleTitle, int Sequence, DateOnly NominalDate, DateOnly PlannedDate, Guid MaintenancePlanId, string Status, bool IsOverdue, bool IsReminderDue, DateOnly? CompletedOn, int? DelayDays, string? CompletionNote, Guid? WorkOrderId, string? SkipReason, long Version);
public sealed record SupplierMaintenanceOccurrenceMutation(SupplierMaintenanceOccurrenceResponse Record, bool Replayed, long AppliedVersion);
public sealed record SupplierMaintenanceOccurrenceQuery(Guid? ScheduleId, DateOnly? From, DateOnly? To, string? Status, bool ReminderDue, int Page, int PageSize);
public interface ISupplierMaintenanceStore
{
    Task<PagedRows<SupplierHolidayResponse>?> Holidays(Guid tenant, int year, int page, int pageSize, CancellationToken token);
    Task<SupplierHolidayMutation?> AddHoliday(Guid tenant, SupplierHolidayRequest request, CancellationToken token);
    Task<bool> RemoveHoliday(Guid tenant, Guid id, long version, CancellationToken token);
    Task<SupplierMaintenancePreview?> PreviewMaintenance(Guid tenant, Guid supplier, SupplierMaintenancePlanRequest request, CancellationToken token);
    Task<PagedRows<SupplierMaintenanceScheduleResponse>?> MaintenanceSchedules(Guid tenant, Guid supplier, int page, int pageSize, CancellationToken token);
    Task<SupplierMaintenanceScheduleResponse?> MaintenanceSchedule(Guid tenant, Guid supplier, Guid id, CancellationToken token);
    Task<SupplierMaintenanceScheduleMutation?> CreateMaintenanceSchedule(Guid tenant, Guid supplier, SupplierMaintenanceScheduleCreate command, CancellationToken token);
    Task<SupplierMaintenanceScheduleMutation?> EndMaintenanceSchedule(Guid tenant, Guid supplier, Guid id, SupplierMaintenanceScheduleEnd command, CancellationToken token);
    Task<PagedRows<SupplierContractHistory>?> MaintenanceHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token);
    Task<PagedRows<SupplierMaintenanceOccurrenceResponse>?> MaintenanceOccurrences(Guid tenant, Guid supplier, SupplierMaintenanceOccurrenceQuery query, CancellationToken token);
    Task<SupplierMaintenanceOccurrenceMutation?> CompleteMaintenance(Guid tenant, Guid supplier, Guid id, SupplierMaintenanceComplete command, CancellationToken token);
    Task<SupplierMaintenanceOccurrenceMutation?> SkipMaintenance(Guid tenant, Guid supplier, Guid id, SupplierMaintenanceSkip command, CancellationToken token);
}
