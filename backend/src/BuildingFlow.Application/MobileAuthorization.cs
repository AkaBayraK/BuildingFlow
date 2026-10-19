namespace BuildingFlow.Application;
public sealed record MobilePersonIdentity(Guid Id,Guid TenantId,string DisplayName);
public sealed record MobileAuthorizationSnapshot(int PolicyVersion,Guid SnapshotId,string UserId,DateTimeOffset IssuedAt,DateTimeOffset ExpiresAt,bool OfflineReadOnly,IReadOnlyList<MobilePersonIdentity> People,IReadOnlyList<SessionContext> Contexts);
public interface IMobileAuthorizationStore
{
    Task<MobileAuthorizationSnapshot> ReadAsync(CancellationToken token);
}
