using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record CommercialConsentCommand(Guid SenderId, string Recipient, bool RecipientVerified, CommercialConsentStatus Status, CommercialConsentSource Source,
    Guid OperationId, string? OwnerUserId = null, Guid? PersonId = null, int? TextVersion = null, string? TextSha256 = null);
public sealed record CommercialConsentState(Guid RecordId, Guid SenderId, string RecipientMasked, string Status, string Source, DateTimeOffset OccurredAt,
    int? TextVersion, string IysStatus, bool Replayed = false);

// Server-side only: callers (web/mobile endpoints, opt-out callbacks, the SMS gate) authorise before calling.
public interface ICommercialConsentStore
{
    Task<CommercialConsentState> RecordAsync(CommercialConsentCommand command, CancellationToken token);
    Task<CommercialConsentState?> CurrentAsync(Guid senderId, string recipient, CancellationToken token);
    Task<PagedRows<CommercialConsentState>> HistoryAsync(Guid senderId, string recipient, int page, int pageSize, CancellationToken token);
}
