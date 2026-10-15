namespace BuildingFlow.Application;
public sealed record MobileSyncCommand(Guid OperationId, Guid RecordId, Guid PropertyId, Guid? UnitId, string Role, string Action, long ExpectedVersion, string? Description, string? Category=null, string? Priority=null);
public sealed record MobileRequestPhotoDto(Guid Id, string Sha256, long Size);
public sealed record MobileRequestDto(Guid Id, Guid PropertyId, Guid? UnitId, string Role, string? Description, long Version, bool Deleted, DateTimeOffset ModifiedAt, string? Category=null, string? Priority=null, MobileRequestPhotoDto[]? Photos=null);
public sealed record MobileSyncResult(Guid OperationId, MobileRequestDto Record, bool Replayed);
public sealed record MobileRequestSnapshot(string UserId, string ContextKey, MobileRequestDto[] Records, bool HasMore);
// Photo upload is a separate idempotent operation so a 256 KiB JPEG never inflates the text command queue.
public sealed record MobileRequestPhotoCommand(Guid OperationId, Guid PropertyId, Guid? UnitId, string Role, string PhotoBase64);
public sealed record MobileRequestPhotoResult(Guid OperationId, Guid RequestId, MobileRequestPhotoDto Photo, bool Replayed);
public interface IMobileSyncStore {
    Task<MobileSyncResult> PushAsync(MobileSyncCommand command,CancellationToken token);
    Task<MobileRequestSnapshot> PullAsync(Guid property,Guid? unit,string role,CancellationToken token);
    Task<MobileRequestPhotoResult> AttachPhotoAsync(Guid request,MobileRequestPhotoCommand command,CancellationToken token);
    Task<byte[]?> PhotoAsync(Guid request,Guid photo,Guid property,Guid? unit,string role,CancellationToken token);
    Task MaintainPhotosAsync(CancellationToken token);
}
