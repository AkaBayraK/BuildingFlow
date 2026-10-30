using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierInvoiceCreate(Guid OperationId, Guid PropertyId, string Number, DateOnly IssueDate, DateOnly DueDate, string NetAmount, string VatRate, string Category, string Description, Guid? WorkOrderId, Guid? ContractId, Guid? CorrectsInvoiceId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierInvoiceAction(Guid OperationId, long Version, DateOnly? Date, string? Text);
public sealed record SupplierInvoiceResponse(Guid Id, Guid TenantId, Guid SupplierId, Guid PropertyId, string PropertyName, string Number, DateOnly IssueDate, DateOnly DueDate, string NetAmount, string VatRate, string VatAmount, string GrossAmount, string Currency, string Category, string Description, Guid? WorkOrderId, Guid? ContractId, Guid? CorrectsInvoiceId, Guid? ExpenseId, string Status, bool IsOverdue, bool RecordedByMe, DateOnly? PaidOn, string? PaymentReference, DateOnly? ReconciledOn, string? ReconciliationNote, string? CancellationReason, long Version, DateTimeOffset CreatedAt);
public sealed record SupplierInvoiceMutation(SupplierInvoiceResponse Record, bool Replayed, long AppliedVersion);
public sealed record SupplierInvoiceSummary(string RecordedTotal, string ApprovedTotal, string PaidTotal, string ReconciledTotal, int OverdueCount);
public sealed record SupplierInvoicePage(IReadOnlyList<SupplierInvoiceResponse> Items, int Page, int PageSize, int TotalCount, SupplierInvoiceSummary Summary);
public interface ISupplierInvoiceStore
{
    Task<SupplierInvoicePage?> Invoices(Guid tenant, Guid supplier, string? status, int page, int pageSize, CancellationToken token);
    Task<SupplierInvoiceResponse?> Invoice(Guid tenant, Guid supplier, Guid id, CancellationToken token);
    Task<SupplierInvoiceMutation?> CreateInvoice(Guid tenant, Guid supplier, SupplierInvoiceCreate command, CancellationToken token);
    Task<SupplierInvoiceMutation?> TransitionInvoice(Guid tenant, Guid supplier, Guid id, string action, SupplierInvoiceAction command, CancellationToken token);
    Task<PagedRows<SupplierContractHistory>?> InvoiceHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token);
}
