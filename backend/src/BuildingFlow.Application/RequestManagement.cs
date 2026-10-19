namespace BuildingFlow.Application;
public sealed record RequestRow(Guid Id,Guid PropertyId,Guid? UnitId,string? UnitNumber,string Description,string Category,string Priority,string Status,string? Response,bool HasHistory,long Version,DateTimeOffset CreatedAt,DateTimeOffset ModifiedAt,Guid? PhotoId,string? PhotoSha256);
public sealed record RequestHistory(Guid Id,string FromStatus,string ToStatus,string Response,DateTimeOffset CreatedAt);
// Consent in effect when the photo became current, and the withdrawal that hid it from the owner (T17.5).
public sealed record RequestPhotoConsent(string State,Guid? RecordId,int? TextVersion,DateTimeOffset? ApprovedAt,DateTimeOffset? WithdrawnAt);
public sealed record RequestDetail(RequestRow Record,RequestHistory[] History,bool Replayed=false,RequestPhotoConsent? PhotoConsent=null);
public sealed record RequestPage(RequestRow[] Items,int Page,int PageSize,int TotalCount);
public sealed record ManageRequest(Guid OperationId,long Version,string Status,string Response);
public interface IRequestManagementStore {
 Task<RequestPage> ListAsync(Guid property,int page,int pageSize,string? search,string? status,string? category,string? priority,string sort,CancellationToken token);
 Task<RequestDetail?> DetailAsync(Guid property,Guid id,CancellationToken token);
 Task<RequestDetail?> ManageAsync(Guid property,Guid id,ManageRequest request,CancellationToken token);
}
