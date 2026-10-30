using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierInquiryRequest(Guid OperationId, long SupplierVersion, Guid PropertyId, Guid CategoryId, string Title, string Description, DateOnly Deadline, Guid[] AreaServiceIds);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierQuoteRequest(Guid OperationId, long Version, Guid InvitationId, string NetAmount, string VatRate, string Currency, DateOnly ValidUntil, string Notes);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierInquiryAction(Guid OperationId, long Version, string? Reason = null);
public sealed record SupplierQuoteCandidate(Guid AreaServiceId, Guid SupplierId, string SupplierName, string ServiceName);
public sealed record SupplierInquirySummary(Guid Id, Guid TenantId, Guid PropertyId, string PropertyName, Guid CategoryId, string Title, DateOnly Deadline, string Status, long Version);
public sealed record SupplierQuoteRow(Guid InvitationId, Guid SupplierId, string SupplierName, Guid AreaServiceId, string ServiceName, Guid? QuoteId, string? NetAmount, string? VatRate, string? VatAmount, string? GrossAmount, string? Currency, DateOnly? ValidUntil, string? Notes, string Status, string? DecisionReason, bool IsOwnEntry);
public sealed record SupplierDraftWorkOrder(Guid Id, Guid QuoteId, string Status, string Title, string Amount, string Currency, long Version);
public sealed record SupplierInquiryDetail(SupplierInquirySummary Inquiry, string Description, string? CancellationReason, IReadOnlyList<SupplierQuoteRow> Quotes, SupplierDraftWorkOrder? WorkOrder);
public sealed record SupplierInquiryMutation(SupplierInquiryDetail Record, bool Replayed, long AppliedVersion);
public interface ISupplierQuoteStore
{
    Task<PagedRows<SupplierQuoteCandidate>?> QuoteCandidates(Guid tenant, Guid supplier, Guid property, Guid category, int page, int pageSize, CancellationToken token);
    Task<PagedRows<SupplierInquirySummary>?> Inquiries(Guid tenant, Guid supplier, int page, int pageSize, CancellationToken token);
    Task<SupplierInquiryDetail?> Inquiry(Guid tenant, Guid supplier, Guid id, CancellationToken token);
    Task<SupplierInquiryMutation?> CreateInquiry(Guid tenant, Guid supplier, SupplierInquiryRequest command, CancellationToken token);
    Task<SupplierInquiryMutation?> EnterQuote(Guid tenant, Guid supplier, Guid id, SupplierQuoteRequest command, CancellationToken token);
    Task<SupplierInquiryMutation?> ActOnInquiry(Guid tenant, Guid supplier, Guid id, Guid? quote, string action, SupplierInquiryAction command, CancellationToken token);
    Task<PagedRows<SupplierContractHistory>?> InquiryHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token);
}
