namespace BuildingFlow.Application;
// Authorization is owned by UserService, rechecked inside the existing profile transaction.
// No consent is granted and no existing profile details or Keycloak account fields are overwritten.
public sealed record UserPhotoCommand(Guid OperationId, long ExpectedVersion, string PhotoAction, string? PhotoBase64 = null);
public sealed record UserPhotoState(string UserId, Guid? PhotoId, string? PhotoSha256, long Version, bool CanReplace);
public sealed record UserPhotoResult(Guid OperationId, UserPhotoState Photo, bool Replayed);
public interface IManagedUserPhotoStore
{
    Task<UserPhotoState> ReadPhotoAsync(string target, CancellationToken token);
    Task<MobileProfileResult> SavePhotoAsync(string target, UserPhotoCommand command, string firstName, string lastName, Func<CancellationToken, Task> authorize, CancellationToken token);
}
