namespace BuildingFlow.Domain;

public enum SmsProvider { MOCK }
public enum SmsPurpose { TRANSACTIONAL, OPERATIONAL, MARKETING }
public enum SmsMessageStatus { QUEUED, SENT, FAILED }
public enum SmsCreditTransactionType { LOAD, CONSUME, REFUND, ADJUST }

public sealed class SmsAccount : BaseEntity
{
    public const int MaxCredit = 10_000_000;
    private SmsAccount() { }
    public SmsAccount(Guid propertyId, string name, SmsProvider provider, string senderHeader, int lowCreditThreshold)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property identity is required.");
        PropertyId = propertyId;
        Update(name, provider, senderHeader, null, null, lowCreditThreshold, true);
    }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = null!;
    public SmsProvider Provider { get; private set; }
    public string SenderHeader { get; private set; } = null!;
    public string? Username { get; private set; }
    public string? ProtectedSecret { get; private set; }
    public string? ApiUrl { get; private set; }
    public int CreditBalance { get; private set; }
    public int LowCreditThreshold { get; private set; }

    public void Update(string name, SmsProvider provider, string senderHeader, string? username, string? apiUrl, int lowCreditThreshold, bool enabled)
    {
        name = Required(name, nameof(name), 100);
        senderHeader = Required(senderHeader, nameof(senderHeader), 11);
        if (senderHeader.Length < 3 || senderHeader.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '-' or ' '))) throw new ArgumentException("Invalid sender header.");
        if (lowCreditThreshold is < 0 or > MaxCredit) throw new ArgumentException("Invalid low credit threshold.");
        username = string.IsNullOrWhiteSpace(username) ? null : Required(username, nameof(username), 100);
        apiUrl = string.IsNullOrWhiteSpace(apiUrl) ? null : Required(apiUrl, nameof(apiUrl), 300);
        if (apiUrl is not null && (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))) throw new ArgumentException("Invalid provider URL.");
        Name = name; Provider = provider; SenderHeader = senderHeader; Username = username; ApiUrl = apiUrl; LowCreditThreshold = lowCreditThreshold;
        SetEnabled(enabled);
    }
    public void SetSecret(string protectedSecret) => ProtectedSecret = Required(protectedSecret, nameof(protectedSecret), 1000);
    public int Apply(SmsCreditTransactionType type, int quantity)
    {
        var delta = type switch
        {
            SmsCreditTransactionType.LOAD or SmsCreditTransactionType.REFUND when quantity > 0 => quantity,
            SmsCreditTransactionType.CONSUME when quantity > 0 => -quantity,
            SmsCreditTransactionType.ADJUST when quantity != 0 => quantity,
            _ => throw new ArgumentException("Invalid credit quantity.")
        };
        var balance = (long)CreditBalance + delta;
        if (balance < 0) throw new InvalidOperationException("Insufficient SMS credit.");
        if (balance > MaxCredit) throw new ArgumentException("SMS credit limit exceeded.");
        CreditBalance = (int)balance;
        return delta;
    }
}

public sealed class SmsCreditTransaction : BaseEntity
{
    private SmsCreditTransaction() { }
    public SmsCreditTransaction(SmsAccount account, SmsCreditTransactionType type, int quantity, decimal? unitPrice, string? currency, string? referenceNo, string? description, Guid? messageId)
    {
        if (unitPrice is < 0 or > 1_000_000) throw new ArgumentException("Invalid unit price.");
        currency = string.IsNullOrWhiteSpace(currency) ? null : Required(currency, nameof(currency), 3).ToUpperInvariant();
        if (currency is not null && (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z'))) throw new ArgumentException("Invalid currency.");
        if ((unitPrice is null) != (currency is null)) throw new ArgumentException("Unit price and currency go together.");
        referenceNo = string.IsNullOrWhiteSpace(referenceNo) ? null : Required(referenceNo, nameof(referenceNo), 100);
        description = string.IsNullOrWhiteSpace(description) ? null : Required(description, nameof(description), 500);
        PropertyId = account.PropertyId; AccountId = account.Id; Type = type;
        Quantity = account.Apply(type, quantity); BalanceAfter = account.CreditBalance;
        UnitPrice = unitPrice; Currency = currency; MessageId = messageId; ReferenceNo = referenceNo; Description = description;
    }
    public Guid PropertyId { get; private set; }
    public Guid AccountId { get; private set; }
    public SmsCreditTransactionType Type { get; private set; }
    public int Quantity { get; private set; }
    public int BalanceAfter { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public string? Currency { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Description { get; private set; }
    public Guid? MessageId { get; private set; }
}

public sealed class SmsTemplate : BaseEntity
{
    public const int MaxContentLength = 1000;
    private SmsTemplate() { }
    public SmsTemplate(Guid propertyId, string typeCode, string name, string content)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property identity is required.");
        PropertyId = propertyId; TypeCode = Required(typeCode, nameof(typeCode), 50);
        Update(name, content, true);
    }
    public Guid PropertyId { get; private set; }
    public string TypeCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public void Update(string name, string content, bool enabled)
    {
        name = Required(name, nameof(name), 100); content = Required(content, nameof(content), MaxContentLength);
        if (content.Any(c => char.IsControl(c) && c != '\n')) throw new ArgumentException("Invalid template content.");
        Name = name; Content = content; SetEnabled(enabled);
    }
}

public sealed class SmsMessage : BaseEntity
{
    public const int MaxAttempts = 5;
    private SmsMessage() { }
    public SmsMessage(Guid propertyId, Guid? accountId, Guid? personId, Guid? requestId, string typeCode, SmsPurpose purpose,
        string phoneMasked, string maskedBody, int segments, DateTimeOffset now)
    {
        if (propertyId == Guid.Empty || accountId == Guid.Empty || personId == Guid.Empty || requestId == Guid.Empty) throw new ArgumentException("Invalid SMS identity.");
        if (segments is < 1 or > 10) throw new ArgumentException("Invalid SMS segment count.");
        PropertyId = propertyId; AccountId = accountId; PersonId = personId; RequestId = requestId;
        TypeCode = Required(typeCode, nameof(typeCode), 50); Purpose = purpose;
        PhoneMasked = Required(phoneMasked, nameof(phoneMasked), 20); MaskedBody = Required(maskedBody, nameof(maskedBody), SmsTemplate.MaxContentLength * 2);
        Segments = segments; CreditCost = segments; Status = SmsMessageStatus.QUEUED; NextAttemptAt = now;
    }
    public Guid PropertyId { get; private set; }
    public Guid? AccountId { get; private set; }
    public Guid? PersonId { get; private set; }
    public Guid? RequestId { get; private set; }
    public string TypeCode { get; private set; } = null!;
    public SmsPurpose Purpose { get; private set; }
    public string PhoneMasked { get; private set; } = null!;
    public string MaskedBody { get; private set; } = null!;
    public string? ProtectedPayload { get; private set; }
    public int Segments { get; private set; }
    public int CreditCost { get; private set; }
    public SmsMessageStatus Status { get; private set; }
    public SmsProvider? Provider { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ErrorCode { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    public void Protect(string payload) { Pending(); ProtectedPayload = Required(payload, nameof(payload), 8000); }
    public void MarkSent(SmsProvider provider, string providerMessageId, DateTimeOffset now)
    {
        Pending(); Attempts++; Status = SmsMessageStatus.SENT; Provider = provider; SentAt = now; ErrorCode = null; ProtectedPayload = null;
        ProviderMessageId = Required(providerMessageId, nameof(providerMessageId), 100);
    }
    public void RecordFailure(string errorCode, DateTimeOffset nextAttemptAt, bool permanent)
    {
        Pending(); Attempts++; ErrorCode = Required(errorCode, nameof(errorCode), 50); NextAttemptAt = nextAttemptAt;
        if (permanent || Attempts >= MaxAttempts) { Status = SmsMessageStatus.FAILED; ProtectedPayload = null; }
    }
    public void Reject(string errorCode) { Pending(); Status = SmsMessageStatus.FAILED; ErrorCode = Required(errorCode, nameof(errorCode), 50); ProtectedPayload = null; }
    private void Pending() { if (Status != SmsMessageStatus.QUEUED) throw new InvalidOperationException("SMS message is already final."); }
}
