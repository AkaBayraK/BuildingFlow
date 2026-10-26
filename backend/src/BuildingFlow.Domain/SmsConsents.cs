namespace BuildingFlow.Domain;

public enum CommercialMessageChannel { SMS }
public enum CommercialConsentStatus { APPROVED, REJECTED }
public enum CommercialConsentSource { WEB, MOBILE, ADMIN, SMS_REPLY, PROVIDER_CALLBACK, IYS }
public enum IysSyncStatus { NOT_REQUIRED, PENDING, SYNCED, FAILED }

// The legal service provider of commercial messages: one per Property, or the platform itself when PropertyId is null.
public sealed class SmsLegalSender : BaseEntity
{
    private SmsLegalSender() { }
    public SmsLegalSender(Guid? propertyId, string title, string? mersisNumber, string contactInformation, string optOutKeyword, string optOutShortCode,
        bool optOutEnabled, bool iysIntegrationEnabled, string? iysBrandCode)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Invalid Property identity.", nameof(propertyId));
        PropertyId = propertyId;
        Update(title, mersisNumber, contactInformation, optOutKeyword, optOutShortCode, optOutEnabled, iysIntegrationEnabled, iysBrandCode);
    }
    public Guid? PropertyId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? MersisNumber { get; private set; }
    public string ContactInformation { get; private set; } = null!;
    public string OptOutKeyword { get; private set; } = null!;
    public string OptOutShortCode { get; private set; } = null!;
    public bool OptOutEnabled { get; private set; }
    public bool IysIntegrationEnabled { get; private set; }
    public string? IysBrandCode { get; private set; }

    public void Update(string title, string? mersisNumber, string contactInformation, string optOutKeyword, string optOutShortCode,
        bool optOutEnabled, bool iysIntegrationEnabled, string? iysBrandCode)
    {
        mersisNumber = string.IsNullOrWhiteSpace(mersisNumber) ? null : mersisNumber.Trim();
        if (mersisNumber is not null && (mersisNumber.Length != 16 || !mersisNumber.All(char.IsAsciiDigit))) throw new ArgumentException("Invalid MERSIS number.", nameof(mersisNumber));
        optOutKeyword = Required(optOutKeyword, nameof(optOutKeyword), 20);
        if (optOutKeyword.Length < 3 || optOutKeyword.Any(c => !char.IsAsciiLetterUpper(c) && !char.IsAsciiDigit(c) && c != ' ') || !optOutKeyword.StartsWith("RET", StringComparison.Ordinal))
            throw new ArgumentException("Invalid opt-out keyword.", nameof(optOutKeyword));
        optOutShortCode = Required(optOutShortCode, nameof(optOutShortCode), 15);
        if (optOutShortCode.Length < 3 || !optOutShortCode.All(char.IsAsciiDigit)) throw new ArgumentException("Invalid opt-out short code.", nameof(optOutShortCode));
        iysBrandCode = string.IsNullOrWhiteSpace(iysBrandCode) ? null : Required(iysBrandCode, nameof(iysBrandCode), 20);
        if (iysIntegrationEnabled && iysBrandCode is null) throw new ArgumentException("IYS brand code is required.", nameof(iysBrandCode));
        Title = Required(title, nameof(title), 200); MersisNumber = mersisNumber; ContactInformation = Required(contactInformation, nameof(contactInformation), 200);
        OptOutKeyword = optOutKeyword; OptOutShortCode = optOutShortCode; OptOutEnabled = optOutEnabled;
        IysIntegrationEnabled = iysIntegrationEnabled; IysBrandCode = iysBrandCode;
    }
}

// Append-only: the newest row for a sender/channel/recipient is the current state; earlier approvals and rejections stay as evidence.
// Kept apart from UserConsent on purpose: a KVKK explicit consent is not a commercial message approval (6563 m.6).
public sealed class CommercialConsentRecord : BaseEntity
{
    private CommercialConsentRecord() { }
    public CommercialConsentRecord(SmsLegalSender sender, CommercialMessageChannel channel, string recipient, bool recipientVerified,
        CommercialConsentStatus status, CommercialConsentSource source, DateTimeOffset occurredAt, string? ownerUserId, Guid? personId,
        int? textVersion, string? textSha256, Guid operationId, string evidenceSha256)
    {
        if (!Enum.IsDefined(channel) || !Enum.IsDefined(status) || !Enum.IsDefined(source)) throw new ArgumentException("Invalid commercial consent value.");
        if (recipient is not { Length: >= 9 and <= 16 } || recipient[0] != '+' || recipient[1] == '0' || !recipient[1..].All(char.IsAsciiDigit)) throw new ArgumentException("Recipient must be E.164.", nameof(recipient));
        if (personId == Guid.Empty || operationId == Guid.Empty) throw new ArgumentException("Invalid identity.");
        if (status == CommercialConsentStatus.APPROVED)
        {
            // Approval may not be collected by SMS or arrive without the exact text the person saw.
            if (source is CommercialConsentSource.SMS_REPLY or CommercialConsentSource.PROVIDER_CALLBACK) throw new ArgumentException("Approval cannot come from an SMS reply.", nameof(source));
            if (textVersion is not > 0 || textSha256 is null || !recipientVerified) throw new ArgumentException("Approval needs a verified recipient and text evidence.");
        }
        SenderId = sender.Id; PropertyId = sender.PropertyId; Channel = channel; Recipient = recipient; RecipientVerified = recipientVerified;
        Status = status; Source = source; OccurredAt = occurredAt.ToUniversalTime().AddTicks(-(occurredAt.Ticks % 10));
        OwnerUserId = string.IsNullOrWhiteSpace(ownerUserId) ? null : Required(ownerUserId, nameof(ownerUserId), 200); PersonId = personId;
        TextVersion = textVersion; TextSha256 = textSha256 is null ? null : ConsentText.Hash(textSha256, nameof(textSha256));
        OperationId = operationId; EvidenceSha256 = ConsentText.Hash(evidenceSha256, nameof(evidenceSha256));
        IysStatus = sender.IysIntegrationEnabled ? IysSyncStatus.PENDING : IysSyncStatus.NOT_REQUIRED;
    }
    public Guid SenderId { get; private set; }
    public Guid? PropertyId { get; private set; }
    public CommercialMessageChannel Channel { get; private set; }
    public string Recipient { get; private set; } = null!;
    public bool RecipientVerified { get; private set; }
    public CommercialConsentStatus Status { get; private set; }
    public CommercialConsentSource Source { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? OwnerUserId { get; private set; }
    public Guid? PersonId { get; private set; }
    public int? TextVersion { get; private set; }
    public string? TextSha256 { get; private set; }
    public Guid OperationId { get; private set; }
    public string EvidenceSha256 { get; private set; } = null!;
    public IysSyncStatus IysStatus { get; private set; }
    public string? IysReference { get; private set; }
    public DateTimeOffset? IysSyncedAt { get; private set; }
    public string? IysErrorCode { get; private set; }

    public void MarkIysSynced(string reference, DateTimeOffset at)
    {
        IysPending(); IysStatus = IysSyncStatus.SYNCED; IysReference = Required(reference, nameof(reference), 100); IysSyncedAt = at.ToUniversalTime().AddTicks(-(at.Ticks % 10)); IysErrorCode = null;
    }
    public void MarkIysFailed(string errorCode) { IysPending(); IysStatus = IysSyncStatus.FAILED; IysErrorCode = Required(errorCode, nameof(errorCode), 50); }
    private void IysPending() { if (IysStatus is not (IysSyncStatus.PENDING or IysSyncStatus.FAILED)) throw new InvalidOperationException("IYS sync is not pending."); }
}
