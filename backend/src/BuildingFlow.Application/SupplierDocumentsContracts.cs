using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
public sealed record SupplierFileUpload(Guid OperationId, long SupplierVersion, Guid PropertyId, string FileName, string ContentType, byte[] Bytes, DateOnly? ExpiresOn, Guid? PreviousDocumentId);
public sealed record SupplierFileResponse(Guid Id, Guid TenantId, Guid SupplierId, Guid PropertyId, Guid DocumentId, string FileName, string ContentType, long Size, string Sha256, DateOnly? ExpiresOn, Guid? PreviousDocumentId, int RevisionNumber, bool IsCurrent, long Version, DateTimeOffset CreatedAt, string PropertyName);
public sealed record SupplierFileMutation(SupplierFileResponse Record, SupplierResponse Supplier, bool Replayed);
public sealed record SupplierFileDownload(SupplierFileResponse Metadata, Stream Content);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierFileRemove(Guid OperationId, long SupplierVersion, long Version);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierContractRequest(Guid OperationId, long SupplierVersion, long Version, Guid PropertyId, Guid AreaServiceId, string? Title, DateOnly StartDate, DateOnly EndDate, string? Amount, int ReminderDays = 30, Guid? DocumentId = null, Guid? PreviousContractId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierContractTransition(Guid OperationId, long SupplierVersion, long Version, string? Reason = null);
public sealed record SupplierContractResponse(Guid Id, Guid TenantId, Guid SupplierId, Guid PropertyId, Guid AreaServiceId, string Title, DateOnly StartDate, DateOnly EndDate, string Amount, string Currency, int ReminderDays, Guid? DocumentId, Guid? PreviousContractId, string Status, bool RenewalDue, string? CancellationReason, long Version, DateTimeOffset CreatedAt, string PropertyName, string ServiceName);
public sealed record SupplierContractMutation(SupplierContractResponse Record, SupplierResponse Supplier, bool Replayed, long AppliedVersion);
public sealed record SupplierContractHistory(Guid Id, string Action, long AppliedVersion, string Snapshot, DateTimeOffset CreatedAt);
public interface ISupplierDocumentsStore
{
    Task<PagedRows<SupplierFileResponse>?> Documents(Guid tenant, Guid supplier, int page, int pageSize, CancellationToken token);
    Task<SupplierFileMutation?> Upload(Guid tenant, Guid supplier, SupplierFileUpload command, CancellationToken token);
    Task<SupplierFileDownload?> Download(Guid tenant, Guid supplier, Guid document, CancellationToken token);
    Task<SupplierMutation?> RemoveDocument(Guid tenant, Guid supplier, Guid document, SupplierFileRemove command, CancellationToken token);
    Task<SupplierContractResponse?> Contract(Guid tenant, Guid supplier, Guid id, CancellationToken token);
    Task<PagedRows<SupplierContractResponse>?> Contracts(Guid tenant, Guid supplier, int page, int pageSize, bool renewalDue, CancellationToken token);
    Task<SupplierContractMutation?> SaveContract(Guid tenant, Guid supplier, Guid? id, SupplierContractRequest command, CancellationToken token);
    Task<SupplierContractMutation?> TransitionContract(Guid tenant, Guid supplier, Guid id, string action, SupplierContractTransition command, CancellationToken token);
    Task<PagedRows<SupplierContractHistory>?> ContractHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token);
}
