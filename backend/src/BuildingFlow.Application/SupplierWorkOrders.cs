using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierWorkOrderCreate(Guid OperationId, long SupplierVersion, Guid PropertyId, Guid AreaServiceId, string Source, Guid SourceId, long SourceVersion, string Title, string Description, DateTimeOffset? DueAt);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierWorkOrderEdit(Guid OperationId, long Version, string Description, DateTimeOffset? DueAt);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierWorkOrderTransition(Guid OperationId, long Version, string Status, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierWorkOrderNote(Guid OperationId, long Version, string Note);
public sealed record SupplierWorkOrderRow(SupplierWorkOrderResponse Record, string SupplierName);
public sealed record SupplierWorkOrderUpload(Guid OperationId, long Version, string FileName, string ContentType, byte[] Bytes, string Caption);
public sealed record SupplierWorkOrderSourceChoice(Guid Id, Guid TenantId, Guid PropertyId, string Source, string Description, long Version);
public sealed record SupplierWorkOrderResponse(Guid Id, Guid TenantId, Guid SupplierId, Guid PropertyId, string PropertyName, Guid AreaServiceId, string ServiceName, string Title, string Description, string Source, Guid? InquiryId, Guid? QuoteId, Guid? RequestId, Guid? MaintenancePlanId, long? SourceVersion, string SourceSnapshot, string? Amount, string? Currency, string Status, DateTimeOffset? DueAt, bool IsOverdue, DateTimeOffset? AssignedAt, DateTimeOffset? AcceptedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, DateTimeOffset? VerifiedAt, DateTimeOffset? ClosedAt, string? CompletionNote, string? DecisionReason, int PhotoCount, long Version, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);
public sealed record SupplierWorkOrderPhotoResponse(Guid Id, SupplierFileResponse File, string Caption, DateTimeOffset CreatedAt);
public sealed record SupplierWorkOrderMutation(SupplierWorkOrderResponse Record, SupplierResponse Supplier, bool Replayed, long AppliedVersion);
public interface ISupplierWorkOrderStore
{
    Task<PagedRows<SupplierWorkOrderSourceChoice>?> WorkOrderSources(Guid tenant, Guid supplier, Guid property, string source, int page, int pageSize, CancellationToken token);
    Task<PagedRows<SupplierWorkOrderResponse>?> WorkOrders(Guid tenant, Guid supplier, string? status, int page, int pageSize, CancellationToken token);
    Task<SupplierWorkOrderResponse?> WorkOrder(Guid tenant, Guid supplier, Guid id, CancellationToken token);
    Task<SupplierWorkOrderMutation?> CreateWorkOrder(Guid tenant, Guid supplier, SupplierWorkOrderCreate command, CancellationToken token);
    Task<SupplierWorkOrderMutation?> EditWorkOrder(Guid tenant, Guid supplier, Guid id, SupplierWorkOrderEdit command, CancellationToken token);
    Task<SupplierWorkOrderMutation?> TransitionWorkOrder(Guid tenant, Guid supplier, Guid id, SupplierWorkOrderTransition command, CancellationToken token);
    Task<PagedRows<SupplierContractHistory>?> WorkOrderHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token);
    Task<PagedRows<SupplierWorkOrderPhotoResponse>?> WorkOrderPhotos(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token);
    Task<SupplierWorkOrderMutation?> AddWorkOrderNote(Guid tenant, Guid supplier, Guid id, SupplierWorkOrderNote command, CancellationToken token);
    Task<PagedRows<SupplierWorkOrderRow>?> PropertyWorkOrders(Guid tenant, Guid property, string? status, int page, int pageSize, CancellationToken token);
    Task<SupplierWorkOrderMutation?> UploadWorkOrderPhoto(Guid tenant, Guid supplier, Guid id, SupplierWorkOrderUpload command, CancellationToken token);
}
