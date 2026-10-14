namespace BuildingFlow.Application;
public sealed record MobileSyncCommand(Guid OperationId, Guid RecordId, Guid PropertyId, Guid? UnitId, string Role, string Action, long ExpectedVersion, string? Description);
public sealed record MobileRequestDto(Guid Id, Guid PropertyId, Guid? UnitId, string Role, string? Description, long Version, bool Deleted, DateTimeOffset ModifiedAt);
public sealed record MobileSyncResult(Guid OperationId, MobileRequestDto Record, bool Replayed);
public sealed record MobileRequestSnapshot(string UserId, string ContextKey, MobileRequestDto[] Records, bool HasMore);
public interface IMobileSyncStore {
    Task<MobileSyncResult> PushAsync(MobileSyncCommand command,CancellationToken token);
    Task<MobileRequestSnapshot> PullAsync(Guid property,Guid? unit,string role,CancellationToken token);
}
