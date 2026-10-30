using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
public sealed record SupplierAlert(string Kind, Guid SupplierId, string SupplierName, Guid PropertyId, string PropertyName, Guid RecordId, string Title, DateOnly Date, int Days);
public sealed record SupplierAlertPreferenceResponse(bool ContractExpiry, bool WorkOrderOverdue, bool MaintenanceDue, long Version);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierAlertPreferenceRequest(bool ContractExpiry, bool WorkOrderOverdue, bool MaintenanceDue, long Version);
public sealed record SupplierAlertPage(IReadOnlyList<SupplierAlert> Items, int Page, int PageSize, int TotalCount, bool Truncated, SupplierAlertPreferenceResponse Preference);
public sealed record SupplierPerformanceRow(Guid SupplierId, string SupplierName, int CompletedWorkOrders, string? AverageCompletionHours, int SlaSampleCount, int SlaOnTimeCount, int MaintenanceSampleCount, int MaintenanceOnTimeCount, string? OnTimeRate, int InvoiceCount, string InvoicedTotal, string? Score, int ScoreSampleCount);
public sealed record SupplierPerformanceReport(DateOnly From, DateOnly To, Guid? PropertyId, int MinimumSample, IReadOnlyList<SupplierPerformanceRow> Items, int Page, int PageSize, int TotalCount);
public interface ISupplierInsightStore
{
    Task<SupplierAlertPage?> Alerts(Guid tenant, string? kind, int page, int pageSize, CancellationToken token);
    Task<SupplierAlertPreferenceResponse?> AlertPreference(Guid tenant, CancellationToken token);
    Task<SupplierAlertPreferenceResponse?> SaveAlertPreference(Guid tenant, SupplierAlertPreferenceRequest request, CancellationToken token);
    Task<SupplierPerformanceReport?> Performance(Guid tenant, DateOnly from, DateOnly to, Guid? property, int page, int pageSize, CancellationToken token);
}
