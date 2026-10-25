using System.Text.Json.Serialization;
namespace BuildingFlow.Application;

public sealed record EmailVerificationDto(string Status, string? Value, bool Available, DateTimeOffset CheckedAt);
public sealed record PhoneVerificationDto(string Status, string? ProfileValue, string? VerifiedValue, string? PendingValue,
    DateTimeOffset? VerifiedAt, bool CanSendCode = false, string DeliveryStatus = "NOT_REQUESTED", Guid? ChallengeId = null, DateTimeOffset? ExpiresAt = null, int? AttemptsRemaining = null, int RetryAfterSeconds = 0);
public sealed record ContactVerificationDto(long Version, EmailVerificationDto Email, PhoneVerificationDto Phone);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StagePhoneRequest(Guid OperationId, long ExpectedVersion, string? Phone);
public sealed record StagePhoneResult(Guid OperationId, bool Replayed, ContactVerificationDto Contact);
public interface IContactVerificationStore
{
    Task<ContactVerificationDto> ReadAsync(CancellationToken token);
    Task<StagePhoneResult> StagePhoneAsync(StagePhoneRequest request, CancellationToken token);
}
