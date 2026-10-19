namespace BuildingFlow.Application;
public sealed record MobileSyncCommand(Guid OperationId, Guid RecordId, Guid PropertyId, Guid? UnitId, string Role, string Action, long ExpectedVersion, string? Description,string? Category=null,string? Priority=null,string PhotoAction="KEEP",string? PhotoBase64=null,string? ContextKind=null,Guid? ContextId=null);
public sealed record MobileRequestDto(Guid Id, Guid PropertyId, Guid? UnitId, string Role, string? Description, long Version, bool Deleted, DateTimeOffset ModifiedAt,string Category="OTHER",string Priority="NORMAL",Guid? PhotoId=null,string? PhotoSha256=null,string ManagementStatus="RECORDED",string? ManagementResponse=null,bool HasManagementHistory=false);
public sealed record MobileSyncResult(Guid OperationId, MobileRequestDto Record, bool Replayed);
public sealed record MobileRequestSnapshot(string UserId, string ContextKey, MobileRequestDto[] Records, bool HasMore);
public interface IMobileSyncStore {
    Task<byte[]?> PhotoAsync(Guid request,CancellationToken token,string? contextKind=null,Guid? contextId=null);
    Task MaintainAsync(CancellationToken token);
    Task<MobileSyncResult> PushAsync(MobileSyncCommand command,CancellationToken token);
    Task<MobileRequestSnapshot> PullAsync(Guid property,Guid? unit,string role,CancellationToken token,string? contextKind=null,Guid? contextId=null);
}
