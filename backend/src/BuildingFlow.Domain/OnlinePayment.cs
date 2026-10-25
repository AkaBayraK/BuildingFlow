using System.Numerics;
namespace BuildingFlow.Domain;

public enum PaymentEnvironment { SANDBOX, LIVE }
public enum CommissionPolicy { ABSORBED }
public enum BankAccountStatus { PENDING_APPROVAL, ACTIVE, REJECTED, CLOSED }
public enum BankAccountPurpose { ALL, DUES, EXTRA }
public enum PaymentIntentStatus { PENDING, SUCCEEDED, REVIEW, FAILED, EXPIRED, CANCELLED, REFUNDED, CHARGED_BACK }
public enum PaymentRefundStatus { REQUESTED, REJECTED, SUCCEEDED, FAILED }
public enum PaymentLedgerType { COLLECTION, COLLECTION_REVERSAL, UNALLOCATED_RECEIPT, ONLINE_REFUND, CHARGEBACK }
public enum ReconciliationIssueType { DEBT_CHANGED, CHARGEBACK_UNREVERSED, PROVIDER_MISMATCH, REFUND_UNCONFIRMED }
public enum ReconciliationIssueStatus { OPEN, RESOLVED }

public static class Iban
{
    public static string Normalize(string? value) => string.Concat((value ?? "").Where(item => !char.IsWhiteSpace(item))).ToUpperInvariant();
    // Only TRY accounts are supported, so only Turkish IBANs (TR + 24 digits) pass the ISO 13616 mod-97 check.
    public static bool IsValid(string value)
    {
        if (value.Length != 26 || !value.StartsWith("TR", StringComparison.Ordinal) || !value[2..].All(char.IsAsciiDigit)) return false;
        var rearranged = value[4..] + "2927" + value[2..4];
        return BigInteger.Parse(rearranged) % 97 == 1;
    }
    public static string Mask(string value) => value.Length < 8 ? "****" : value[..4] + new string('*', value.Length - 8) + value[^4..];
}

public sealed class PaymentProvider : BaseEntity
{
    private PaymentProvider() { }
    public PaymentProvider(string code, bool enabled)
    {
        Code = Required(code, nameof(code), 30).ToUpperInvariant(); SetEnabled(enabled);
    }
    public string Code { get; private set; } = null!;
}

public sealed class PropertyPaymentSettings : BaseEntity
{
    private PropertyPaymentSettings() { }
    public PropertyPaymentSettings(Guid propertyId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Valid property is required.");
        PropertyId = propertyId;
    }
    public Guid PropertyId { get; private set; }
    public bool OnlineEnabled { get; private set; }
    public bool PartialAllowed { get; private set; }
    public bool ShowIbanToResidents { get; private set; }
    public bool RequireDualApproval { get; private set; } = true;
    public string ReceiptPrefix { get; private set; } = "BF";
    public CommissionPolicy CommissionPolicy { get; private set; } = CommissionPolicy.ABSORBED;
    public void Update(bool online, bool partial, bool showIban, bool dualApproval, string receiptPrefix)
    {
        receiptPrefix = Required(receiptPrefix, nameof(receiptPrefix), 10).ToUpperInvariant();
        if (receiptPrefix.Length < 2 || !receiptPrefix.All(char.IsAsciiLetterOrDigit)) throw new ArgumentException("Invalid receipt prefix.");
        OnlineEnabled = online; PartialAllowed = partial; ShowIbanToResidents = showIban; RequireDualApproval = dualApproval; ReceiptPrefix = receiptPrefix;
    }
}

public sealed class PaymentBankAccount : BaseEntity
{
    private PaymentBankAccount() { }
    public PaymentBankAccount(Guid propertyId, Guid requestId, Guid? buildingId, string bankName, string accountHolder, string iban,
        BankAccountPurpose purpose, DateOnly validFrom, DateOnly? validTo)
    {
        if (propertyId == Guid.Empty || requestId == Guid.Empty || buildingId == Guid.Empty) throw new ArgumentException("Valid identities are required.");
        iban = Domain.Iban.Normalize(iban);
        if (!Domain.Iban.IsValid(iban)) throw new ArgumentException("Invalid IBAN.");
        if (!Enum.IsDefined(purpose) || validFrom.Year < 2000 || validTo < validFrom) throw new ArgumentException("Invalid account validity.");
        PropertyId = propertyId; RequestId = requestId; BuildingId = buildingId; Iban = iban; Purpose = purpose;
        BankName = Required(bankName, nameof(bankName), 100); AccountHolder = Required(accountHolder, nameof(accountHolder), 200);
        ValidFrom = validFrom; ValidTo = validTo;
    }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public string BankName { get; private set; } = null!;
    public string AccountHolder { get; private set; } = null!;
    public string Iban { get; private set; } = null!;
    public string Currency { get; private set; } = "TRY";
    public BankAccountPurpose Purpose { get; private set; }
    public bool IsDefault { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public BankAccountStatus Status { get; private set; } = BankAccountStatus.PENDING_APPROVAL;
    public string? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? StatusReason { get; private set; }
    public bool UsableOn(DateOnly day) => Status is BankAccountStatus.ACTIVE or BankAccountStatus.CLOSED && ValidFrom <= day && (ValidTo is null || ValidTo >= day);
    public bool Serves(ChargeKind kind) => Purpose == BankAccountPurpose.ALL || Purpose.ToString() == kind.ToString();
    // IBAN never changes: a new IBAN is a new account, so earlier payments keep the account they were received into.
    public void Approve(string approver, bool dualApproval, DateTimeOffset at)
    {
        approver = Required(approver, nameof(approver), 200);
        if (Status != BankAccountStatus.PENDING_APPROVAL) throw new InvalidOperationException("Only pending accounts can be approved.");
        if (dualApproval && approver == CreatedBy) throw new InvalidOperationException("A second authorized person must approve.");
        Status = BankAccountStatus.ACTIVE; DecidedBy = approver; DecidedAt = at;
    }
    public void Reject(string approver, string reason, DateTimeOffset at)
    {
        if (Status != BankAccountStatus.PENDING_APPROVAL) throw new InvalidOperationException("Only pending accounts can be rejected.");
        Status = BankAccountStatus.REJECTED; DecidedBy = Required(approver, nameof(approver), 200); DecidedAt = at;
        StatusReason = Required(reason, nameof(reason), 500); IsDefault = false;
    }
    public void Close(DateOnly validTo, string reason)
    {
        if (Status != BankAccountStatus.ACTIVE || validTo < ValidFrom || (ValidTo is not null && validTo > ValidTo)) throw new InvalidOperationException("Only active accounts can be closed.");
        Status = BankAccountStatus.CLOSED; ValidTo = validTo; StatusReason = Required(reason, nameof(reason), 500); IsDefault = false;
    }
    public void MarkDefault(bool value)
    {
        if (value && Status != BankAccountStatus.ACTIVE) throw new InvalidOperationException("Only active accounts can be default.");
        IsDefault = value;
    }
}

public sealed class PaymentMerchantAccount : BaseEntity
{
    private PaymentMerchantAccount() { }
    public PaymentMerchantAccount(Guid propertyId, Guid requestId, Guid? buildingId, string providerCode, string label, string merchantId,
        string secretReference, PaymentEnvironment environment)
    {
        if (propertyId == Guid.Empty || requestId == Guid.Empty || buildingId == Guid.Empty) throw new ArgumentException("Valid identities are required.");
        // Live merchants need a contract, legal review and explicit owner approval (BF-PAY FAZ 12).
        if (environment != PaymentEnvironment.SANDBOX) throw new InvalidOperationException("Only sandbox merchants are allowed.");
        secretReference = Required(secretReference, nameof(secretReference), 100);
        if (!secretReference.All(item => char.IsAsciiLetterOrDigit(item) || item is '_' or '-' or ':')) throw new ArgumentException("Invalid secret reference.");
        PropertyId = propertyId; RequestId = requestId; BuildingId = buildingId; Environment = environment;
        ProviderCode = Required(providerCode, nameof(providerCode), 30).ToUpperInvariant();
        Label = Required(label, nameof(label), 100); MerchantId = Required(merchantId, nameof(merchantId), 100); SecretReference = secretReference;
    }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public string ProviderCode { get; private set; } = null!;
    public string Label { get; private set; } = null!;
    public string MerchantId { get; private set; } = null!;
    public string SecretReference { get; private set; } = null!;
    public PaymentEnvironment Environment { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTimeOffset? LastCheckedAt { get; private set; }
    public bool? LastCheckSucceeded { get; private set; }
    public void MarkDefault(bool value)
    {
        if (value && !IsEnabled) throw new InvalidOperationException("Only active merchants can be default.");
        IsDefault = value;
    }
    public void Deactivate() { SetEnabled(false); IsDefault = false; }
    public void RecordCheck(bool succeeded, DateTimeOffset at) { LastCheckSucceeded = succeeded; LastCheckedAt = at; }
}

public sealed class PaymentIntent : BaseEntity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private static readonly Dictionary<PaymentIntentStatus, PaymentIntentStatus[]> Transitions = new()
    {
        [PaymentIntentStatus.PENDING] = [PaymentIntentStatus.SUCCEEDED, PaymentIntentStatus.REVIEW, PaymentIntentStatus.FAILED, PaymentIntentStatus.EXPIRED, PaymentIntentStatus.CANCELLED],
        [PaymentIntentStatus.REVIEW] = [PaymentIntentStatus.SUCCEEDED, PaymentIntentStatus.REFUNDED, PaymentIntentStatus.CHARGED_BACK],
        [PaymentIntentStatus.SUCCEEDED] = [PaymentIntentStatus.REFUNDED, PaymentIntentStatus.CHARGED_BACK],
    };
    private readonly List<PaymentIntentLine> _lines = [];
    private PaymentIntent() { }
    public PaymentIntent(Guid propertyId, Guid unitId, Guid requestId, string fingerprint, string payerUserId, string payerRole,
        PaymentMerchantAccount merchant, DateTimeOffset createdAt)
    {
        if (propertyId == Guid.Empty || unitId == Guid.Empty || requestId == Guid.Empty || merchant.PropertyId != propertyId) throw new ArgumentException("Valid identities are required.");
        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length != 64) throw new ArgumentException("Invalid fingerprint.");
        if (payerRole is not ("OWNER" or "TENANT")) throw new ArgumentException("Invalid payer role.");
        PropertyId = propertyId; UnitId = unitId; RequestId = requestId; RequestFingerprint = fingerprint;
        PayerUserId = Required(payerUserId, nameof(payerUserId), 200); PayerRole = payerRole;
        MerchantAccountId = merchant.Id; ProviderCode = merchant.ProviderCode; ExpiresAt = createdAt + Lifetime; StatusChangedAt = createdAt;
    }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = null!;
    public string PayerUserId { get; private set; } = null!;
    public string PayerRole { get; private set; } = null!;
    public Guid MerchantAccountId { get; private set; }
    public string ProviderCode { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public PaymentIntentStatus Status { get; private set; } = PaymentIntentStatus.PENDING;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset StatusChangedAt { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public DateOnly? PaidDate { get; private set; }
    public decimal? FeeAmount { get; private set; }
    public Guid? PaymentId { get; private set; }
    public string? FailureCode { get; private set; }
    public IReadOnlyCollection<PaymentIntentLine> Lines => _lines.AsReadOnly();
    public bool IsOpen(DateTimeOffset now) => Status == PaymentIntentStatus.PENDING && ExpiresAt > now;
    public void AddLine(DuesCharge charge, AllocationKind kind, decimal amount)
    {
        Payment.ValidateAmount(amount);
        if (ProviderReference is not null || charge.PropertyId != PropertyId || charge.UnitId != UnitId || charge.IsCancelled ||
            _lines.Any(value => value.ChargeId == charge.Id && value.Kind == kind) || _lines.Count >= 100)
            throw new InvalidOperationException("Invalid intent line.");
        _lines.Add(new PaymentIntentLine(this, charge, kind, amount)); Amount += amount;
    }
    public void Open(string providerReference, string checkoutUrl)
    {
        if (Status != PaymentIntentStatus.PENDING || ProviderReference is not null || _lines.Count == 0) throw new InvalidOperationException("Intent cannot be opened.");
        ProviderReference = Required(providerReference, nameof(providerReference), 100);
        CheckoutUrl = Required(checkoutUrl, nameof(checkoutUrl), 1000);
        if (!Uri.TryCreate(CheckoutUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")) throw new ArgumentException("Invalid checkout URL.");
    }
    public bool CanMoveTo(PaymentIntentStatus next) => Transitions.TryGetValue(Status, out var allowed) && allowed.Contains(next);
    private void MoveTo(PaymentIntentStatus next, DateTimeOffset at)
    {
        if (!CanMoveTo(next)) throw new InvalidOperationException($"Intent cannot move from {Status} to {next}.");
        Status = next; StatusChangedAt = at;
    }
    public void Fail(PaymentIntentStatus status, string code, DateTimeOffset at)
    {
        if (status is not (PaymentIntentStatus.FAILED or PaymentIntentStatus.EXPIRED or PaymentIntentStatus.CANCELLED)) throw new ArgumentException("Not a failure state.");
        MoveTo(status, at); FailureCode = Required(code, nameof(code), 50);
    }
    // A verified provider success is recorded once; without an allocatable payment the money waits in REVIEW.
    public void Succeed(string transactionId, DateOnly paidDate, decimal fee, Payment? payment, DateTimeOffset at)
    {
        if (fee < 0 || decimal.Round(fee, 2) != fee || fee > Amount) throw new ArgumentOutOfRangeException(nameof(fee));
        if (payment is not null && (payment.Amount != Amount || payment.PropertyId != PropertyId || payment.UnitId != UnitId)) throw new InvalidOperationException("Payment does not match intent.");
        MoveTo(payment is null ? PaymentIntentStatus.REVIEW : PaymentIntentStatus.SUCCEEDED, at);
        ProviderTransactionId = Required(transactionId, nameof(transactionId), 100); PaidDate = paidDate; FeeAmount = fee; PaymentId = payment?.Id;
    }
    public void Settle(Payment payment, DateTimeOffset at)
    {
        if (Status != PaymentIntentStatus.REVIEW || payment.Amount != Amount || payment.PropertyId != PropertyId || payment.UnitId != UnitId)
            throw new InvalidOperationException("Only reviewed intents can be settled.");
        MoveTo(PaymentIntentStatus.SUCCEEDED, at); PaymentId = payment.Id;
    }
    public void Refund(DateTimeOffset at) => MoveTo(PaymentIntentStatus.REFUNDED, at);
    public void ChargeBack(DateTimeOffset at) => MoveTo(PaymentIntentStatus.CHARGED_BACK, at);
}

public sealed class PaymentIntentLine : BaseEntity
{
    private PaymentIntentLine() { }
    internal PaymentIntentLine(PaymentIntent intent, DuesCharge charge, AllocationKind kind, decimal amount)
    {
        PropertyId = intent.PropertyId; UnitId = intent.UnitId; IntentId = intent.Id; ChargeId = charge.Id; Kind = kind; Amount = amount; ChargeVersion = charge.Version;
    }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid IntentId { get; private set; }
    public Guid ChargeId { get; private set; }
    public AllocationKind Kind { get; private set; }
    public decimal Amount { get; private set; }
    public long ChargeVersion { get; private set; }
}

public sealed class PaymentProviderEvent : BaseEntity
{
    private PaymentProviderEvent() { }
    public PaymentProviderEvent(string providerCode, string eventId, string eventType, string providerReference, DateTimeOffset occurredAt)
    {
        ProviderCode = Required(providerCode, nameof(providerCode), 30); EventId = Required(eventId, nameof(eventId), 100);
        EventType = Required(eventType, nameof(eventType), 40); ProviderReference = Required(providerReference, nameof(providerReference), 100); OccurredAt = occurredAt;
    }
    public string ProviderCode { get; private set; } = null!;
    public string EventId { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public string ProviderReference { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? PropertyId { get; private set; }
    public Guid? IntentId { get; private set; }
    public string? Outcome { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public void Processed(PaymentIntent? intent, string outcome, DateTimeOffset at)
    {
        PropertyId = intent?.PropertyId; IntentId = intent?.Id; Outcome = Required(outcome, nameof(outcome), 40); ProcessedAt = at;
    }
}

public sealed class PaymentReceipt : BaseEntity
{
    private PaymentReceipt() { }
    public PaymentReceipt(Payment payment, string prefix, int year, int sequence)
    {
        if (sequence < 1 || year < 2000) throw new ArgumentOutOfRangeException(nameof(sequence));
        PropertyId = payment.PropertyId; PaymentId = payment.Id; Year = year; Sequence = sequence;
        Number = $"{Required(prefix, nameof(prefix), 10)}-{year}-{sequence:D6}";
    }
    public Guid PropertyId { get; private set; }
    public Guid PaymentId { get; private set; }
    public int Year { get; private set; }
    public int Sequence { get; private set; }
    public string Number { get; private set; } = null!;
}

public sealed class PaymentLedgerEntry : BaseEntity
{
    private PaymentLedgerEntry() { }
    public PaymentLedgerEntry(Guid propertyId, Guid unitId, PaymentLedgerType type, decimal amount, DateOnly occurredOn,
        string method, Guid? paymentId = null, Guid? intentId = null, Guid? refundId = null)
    {
        if (propertyId == Guid.Empty || unitId == Guid.Empty || !Enum.IsDefined(type) || occurredOn.Year < 2000) throw new ArgumentException("Invalid ledger entry.");
        var credit = type is PaymentLedgerType.COLLECTION or PaymentLedgerType.UNALLOCATED_RECEIPT;
        if (amount == 0 || decimal.Round(amount, 2) != amount || (amount > 0) != credit) throw new ArgumentOutOfRangeException(nameof(amount));
        PropertyId = propertyId; UnitId = unitId; Type = type; Amount = amount; OccurredOn = occurredOn;
        Method = Required(method, nameof(method), 30); PaymentId = paymentId; IntentId = intentId; RefundId = refundId;
    }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public PaymentLedgerType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string Method { get; private set; } = null!;
    public Guid? PaymentId { get; private set; }
    public Guid? IntentId { get; private set; }
    public Guid? RefundId { get; private set; }
}

public sealed class PaymentRefund : BaseEntity
{
    private PaymentRefund() { }
    public PaymentRefund(PaymentIntent intent, Guid requestId, string reason)
    {
        if (requestId == Guid.Empty || intent.Status is not (PaymentIntentStatus.SUCCEEDED or PaymentIntentStatus.REVIEW)) throw new InvalidOperationException("Only collected intents can be refunded.");
        PropertyId = intent.PropertyId; IntentId = intent.Id; RequestId = requestId; Amount = intent.Amount; Reason = Required(reason, nameof(reason), 500);
    }
    public Guid PropertyId { get; private set; }
    public Guid IntentId { get; private set; }
    public Guid RequestId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = null!;
    public PaymentRefundStatus Status { get; private set; } = PaymentRefundStatus.REQUESTED;
    public string? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecisionNote { get; private set; }
    public string? ProviderRefundId { get; private set; }
    public string? FailureCode { get; private set; }
    private void Decide(string approver, string? note, DateTimeOffset at)
    {
        if (Status != PaymentRefundStatus.REQUESTED) throw new InvalidOperationException("Refund already decided.");
        approver = Required(approver, nameof(approver), 200);
        if (approver == CreatedBy) throw new InvalidOperationException("A second authorized person must decide.");
        DecidedBy = approver; DecidedAt = at; DecisionNote = string.IsNullOrWhiteSpace(note) ? null : Required(note, nameof(note), 500);
    }
    public void Reject(string approver, string note, DateTimeOffset at) { Decide(approver, note, at); Status = PaymentRefundStatus.REJECTED; }
    public void Succeed(string approver, string providerRefundId, DateTimeOffset at)
    {
        Decide(approver, null, at); Status = PaymentRefundStatus.SUCCEEDED; ProviderRefundId = Required(providerRefundId, nameof(providerRefundId), 100);
    }
    public void Fail(string approver, string code, DateTimeOffset at) { Decide(approver, null, at); Status = PaymentRefundStatus.FAILED; FailureCode = Required(code, nameof(code), 50); }
}

public sealed class PaymentReconciliationItem : BaseEntity
{
    private PaymentReconciliationItem() { }
    public PaymentReconciliationItem(Guid propertyId, Guid? intentId, ReconciliationIssueType type, string detail)
    {
        if (propertyId == Guid.Empty || intentId == Guid.Empty || !Enum.IsDefined(type)) throw new ArgumentException("Invalid reconciliation item.");
        PropertyId = propertyId; IntentId = intentId; Type = type; Detail = Required(detail, nameof(detail), 500);
    }
    public Guid PropertyId { get; private set; }
    public Guid? IntentId { get; private set; }
    public ReconciliationIssueType Type { get; private set; }
    public ReconciliationIssueStatus Status { get; private set; } = ReconciliationIssueStatus.OPEN;
    public string Detail { get; private set; } = null!;
    public string? ResolvedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionNote { get; private set; }
    public void Resolve(string actor, string note, DateTimeOffset at)
    {
        if (Status != ReconciliationIssueStatus.OPEN) throw new InvalidOperationException("Item already resolved.");
        Status = ReconciliationIssueStatus.RESOLVED; ResolvedBy = Required(actor, nameof(actor), 200); ResolvedAt = at; ResolutionNote = Required(note, nameof(note), 500);
    }
}
