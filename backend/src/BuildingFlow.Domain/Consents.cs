namespace BuildingFlow.Domain;

// INFORMATION is an acknowledged notice (KVKK aydınlatma); EXPLICIT_CONSENT is freely given and can be withdrawn.
public enum ConsentKind { INFORMATION, EXPLICIT_CONSENT }
// A feature type gates a code path (photo upload); a free type is only offered to the user.
public enum ConsentFeature { PROFILE_PHOTO, REQUEST_PHOTO }
public enum ConsentChannel { MOBILE, WEB }
public enum ConsentRevokeReason { WITHDRAWN, SUPERSEDED }

public sealed class ConsentType : BaseEntity
{
    public const int MaxCodeLength = 40;
    public const int MaxNameLength = 150;
    private ConsentType() { }
    public ConsentType(string code, string nameTr, string nameEn, ConsentKind kind, int sortOrder)
    {
        code = Required(code, nameof(code), MaxCodeLength);
        if (code.Length < 2 || !char.IsAsciiLetterUpper(code[0]) || code.Any(c => !char.IsAsciiLetterUpper(c) && !char.IsAsciiDigit(c) && c != '_'))
            throw new ArgumentException("Invalid consent type code.", nameof(code));
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Invalid consent kind.", nameof(kind));
        Code = code; Kind = kind;
        Rename(nameTr, nameEn, sortOrder);
    }
    public string Code { get; private set; } = null!;
    public string NameTr { get; private set; } = null!;
    public string NameEn { get; private set; } = null!;
    public ConsentKind Kind { get; private set; }
    public bool IsSystem { get; private set; }
    public ConsentFeature? Feature { get; private set; }
    public int SortOrder { get; private set; }

    public void Rename(string nameTr, string nameEn, int sortOrder)
    {
        if (sortOrder is < 0 or > 10_000) throw new ArgumentException("Invalid sort order.", nameof(sortOrder));
        NameTr = Required(nameTr, nameof(nameTr), MaxNameLength); NameEn = Required(nameEn, nameof(nameEn), MaxNameLength); SortOrder = sortOrder;
    }
}

// A published text is immutable: approvals point at the exact version and body hash the person saw.
public sealed class ConsentText : BaseEntity
{
    public const int MaxTitleLength = 200;
    public const int MaxBodyLength = 50_000;
    private ConsentText() { }
    public ConsentText(ConsentType type, int textVersion, string title, string body, string bodySha256, bool requiresReconsent, Guid operationId, string fingerprint)
    {
        if (textVersion < 1) throw new ArgumentException("Invalid text version.", nameof(textVersion));
        if (operationId == Guid.Empty) throw new ArgumentException("Operation identity is required.", nameof(operationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        if (body.Length > MaxBodyLength) throw new ArgumentException("Consent text is too long.", nameof(body));
        ConsentTypeId = type.Id; TextVersion = textVersion; Title = Required(title, nameof(title), MaxTitleLength); Body = body;
        BodySha256 = Hash(bodySha256, nameof(bodySha256)); RequiresReconsent = requiresReconsent; OperationId = operationId; Fingerprint = Hash(fingerprint, nameof(fingerprint));
    }
    public Guid ConsentTypeId { get; private set; }
    public int TextVersion { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string BodySha256 { get; private set; } = null!;
    public bool RequiresReconsent { get; private set; }
    public Guid OperationId { get; private set; }
    public string Fingerprint { get; private set; } = null!;

    internal static string Hash(string value, string field)
    {
        if (value is not { Length: 64 } || value.Any(c => !char.IsAsciiHexDigitLower(c) && !char.IsAsciiDigit(c))) throw new ArgumentException("Invalid SHA-256 value.", field);
        return value;
    }
}

// Rows are never removed: approval, supersession and withdrawal together are the consent history the centre sees.
public sealed class UserConsent : BaseEntity
{
    public const int MaxApproverLength = 200;
    private UserConsent() { }
    public UserConsent(string ownerUserId, string approverName, ConsentType type, ConsentText text, DateTimeOffset approvedAt, ConsentChannel channel,
        string? appVersion, Guid operationId, string fingerprint)
    {
        if (text.ConsentTypeId != type.Id) throw new ArgumentException("Consent text belongs to another type.", nameof(text));
        if (operationId == Guid.Empty) throw new ArgumentException("Operation identity is required.", nameof(operationId));
        if (!Enum.IsDefined(channel)) throw new ArgumentException("Invalid consent channel.", nameof(channel));
        OwnerUserId = Required(ownerUserId, nameof(ownerUserId), 200);
        ApproverName = Required(approverName, nameof(approverName), MaxApproverLength);
        ConsentTypeId = type.Id; ConsentTextId = text.Id; TextVersion = text.TextVersion; BodySha256 = text.BodySha256;
        // PostgreSQL keeps microseconds; trimming keeps the evidence hash reproducible after a round trip.
        ApprovedAt = approvedAt.ToUniversalTime().AddTicks(-(approvedAt.Ticks % 10));
        Channel = channel; AppVersion = string.IsNullOrWhiteSpace(appVersion) ? null : Required(appVersion, nameof(appVersion), 40);
        OperationId = operationId; Fingerprint = ConsentText.Hash(fingerprint, nameof(fingerprint));
    }
    public string OwnerUserId { get; private set; } = null!;
    public string ApproverName { get; private set; } = null!;
    public Guid ConsentTypeId { get; private set; }
    public Guid ConsentTextId { get; private set; }
    public int TextVersion { get; private set; }
    public string BodySha256 { get; private set; } = null!;
    public DateTimeOffset ApprovedAt { get; private set; }
    public ConsentChannel Channel { get; private set; }
    public string? AppVersion { get; private set; }
    public Guid OperationId { get; private set; }
    public string Fingerprint { get; private set; } = null!;
    public string EvidenceSha256 { get; private set; } = null!;
    public DateTimeOffset? RevokedAt { get; private set; }
    public ConsentRevokeReason? RevokeReason { get; private set; }
    public Guid? RevokeOperationId { get; private set; }

    public void Seal(string evidenceSha256)
    {
        if (EvidenceSha256 is not null) throw new InvalidOperationException("Consent evidence is already sealed.");
        EvidenceSha256 = ConsentText.Hash(evidenceSha256, nameof(evidenceSha256));
    }

    public void Revoke(ConsentRevokeReason reason, DateTimeOffset at, Guid? operationId)
    {
        if (RevokedAt is not null) throw new InvalidOperationException("Consent is already revoked.");
        if (!Enum.IsDefined(reason)) throw new ArgumentException("Invalid revoke reason.", nameof(reason));
        if (operationId == Guid.Empty) throw new ArgumentException("Invalid operation identity.", nameof(operationId));
        at = at.ToUniversalTime();
        RevokedAt = at.AddTicks(-(at.Ticks % 10)); RevokeReason = reason; RevokeOperationId = operationId;
    }
}
