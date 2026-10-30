namespace BuildingFlow.Domain;

public enum SupplierInquiryStatus { OPEN, AWARDED, CANCELLED }
public enum SupplierQuoteStatus { PENDING, APPROVED, REJECTED }

/// <summary>A management-area request shared by explicitly invited suppliers.</summary>
public sealed class SupplierInquiry : BaseEntity
{
    private SupplierInquiry() { }
    public SupplierInquiry(Guid tenant, Guid property, Guid category, string title, string description, DateOnly deadline)
    {
        SupplierText.Scope(tenant, property);
        if (category == Guid.Empty || deadline.Year is < 2000 or > 2100) throw new ArgumentException("Invalid inquiry scope or deadline.");
        var cleanTitle = SupplierQuoteRules.Text(title, nameof(title), 200);
        var cleanDescription = SupplierQuoteRules.Text(description, nameof(description), 4000);
        TenantId = tenant; PropertyId = property; CategoryId = category; Title = cleanTitle; Description = cleanDescription; Deadline = deadline;
    }
    public Guid TenantId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateOnly Deadline { get; private set; }
    public SupplierInquiryStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public void Award()
    {
        if (Status != SupplierInquiryStatus.OPEN) throw new InvalidOperationException("Inquiry is finalized.");
        Status = SupplierInquiryStatus.AWARDED;
    }
    public void Cancel(string reason)
    {
        if (Status != SupplierInquiryStatus.OPEN) throw new InvalidOperationException("Inquiry is finalized.");
        var clean = SupplierQuoteRules.Text(reason, nameof(reason), 1000);
        CancellationReason = clean; Status = SupplierInquiryStatus.CANCELLED;
    }
}

public sealed class SupplierInvitation : BaseEntity
{
    private SupplierInvitation() { }
    public SupplierInvitation(SupplierInquiry inquiry, Guid supplier, Guid areaService)
    {
        SupplierText.Scope(supplier, areaService);
        TenantId = inquiry.TenantId; PropertyId = inquiry.PropertyId; InquiryId = inquiry.Id; SupplierId = supplier; AreaServiceId = areaService;
    }
    public Guid TenantId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid InquiryId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid AreaServiceId { get; private set; }
}

public sealed class SupplierQuote : BaseEntity
{
    private SupplierQuote() { }
    public SupplierQuote(SupplierInvitation invitation, decimal netAmount, decimal vatRate, string currency, DateOnly validUntil, string notes, string issuer, string subject)
    {
        TenantId = invitation.TenantId; PropertyId = invitation.PropertyId; InquiryId = invitation.InquiryId; SupplierId = invitation.SupplierId; InvitationId = invitation.Id;
        Enter(netAmount, vatRate, currency, validUntil, notes, issuer, subject);
    }
    public Guid TenantId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid InquiryId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid InvitationId { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal GrossAmount { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateOnly ValidUntil { get; private set; }
    public string Notes { get; private set; } = null!;
    public string EnteredByIssuer { get; private set; } = null!;
    public string EnteredBySubject { get; private set; } = null!;
    public SupplierQuoteStatus Status { get; private set; }
    public string? DecisionReason { get; private set; }
    public void Enter(decimal netAmount, decimal vatRate, string currency, DateOnly validUntil, string notes, string issuer, string subject)
    {
        if (Status != SupplierQuoteStatus.PENDING) throw new InvalidOperationException("Decided quote is immutable.");
        if (EnteredByIssuer is not null && !IsEnteredBy(issuer, subject)) throw new InvalidOperationException("Only the original entrant can revise a pending quote.");
        Payment.ValidateAmount(netAmount);
        if (vatRate is < 0 or > 100 || decimal.Round(vatRate, 2) != vatRate || currency is not ("TRY" or "USD" or "EUR") || validUntil.Year is < 2000 or > 2100) throw new ArgumentException("Invalid quote values.");
        var vat = decimal.Round(netAmount * vatRate / 100m, 2, MidpointRounding.AwayFromZero);
        Payment.ValidateAmount(netAmount + vat);
        var cleanNotes = SupplierQuoteRules.Text(notes, nameof(notes), 2000);
        var cleanIssuer = Required(issuer, nameof(issuer), 500); var cleanSubject = Required(subject, nameof(subject), 200);
        NetAmount = netAmount; VatRate = vatRate; VatAmount = vat; GrossAmount = netAmount + vat; Currency = currency;
        ValidUntil = validUntil; Notes = cleanNotes; EnteredByIssuer = cleanIssuer; EnteredBySubject = cleanSubject;
    }
    public bool IsEnteredBy(string issuer, string subject) => EnteredByIssuer == issuer && EnteredBySubject == subject;
    public void Decide(bool approve, string reason, string issuer, string subject, DateOnly today)
    {
        if (Status != SupplierQuoteStatus.PENDING) throw new InvalidOperationException("Quote is already decided.");
        var clean = SupplierQuoteRules.Text(reason, nameof(reason), 1000);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer); ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        if (approve && (IsEnteredBy(issuer, subject) || ValidUntil < today)) throw new InvalidOperationException("Quote cannot be approved by its entrant or after expiry.");
        DecisionReason = clean; Status = approve ? SupplierQuoteStatus.APPROVED : SupplierQuoteStatus.REJECTED;
    }
}

public sealed class SupplierInquiryRevision : BaseEntity
{
    private SupplierInquiryRevision() { }
    public SupplierInquiryRevision(SupplierInquiry inquiry, Guid operation, string issuer, string subject, string fingerprint, string action, string snapshot, long appliedVersion)
    {
        if (operation == Guid.Empty || appliedVersion < 1 || action is not ("CREATE" or "QUOTE" or "APPROVE" or "REJECT" or "CANCEL" or "CONVERT")) throw new ArgumentException("Invalid inquiry revision.");
        TenantId = inquiry.TenantId; PropertyId = inquiry.PropertyId; InquiryId = inquiry.Id; OperationId = operation;
        ActorIssuer = Required(issuer, nameof(issuer), 500); ActorSubject = Required(subject, nameof(subject), 200);
        Fingerprint = Required(fingerprint, nameof(fingerprint), 64); Action = action; Snapshot = Required(snapshot, nameof(snapshot), 20000); AppliedVersion = appliedVersion;
    }
    public Guid TenantId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid InquiryId { get; private set; }
    public Guid OperationId { get; private set; }
    public string ActorIssuer { get; private set; } = null!;
    public string ActorSubject { get; private set; } = null!;
    public string Fingerprint { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Snapshot { get; private set; } = null!;
    public long AppliedVersion { get; private set; }
}

internal static class SupplierQuoteRules
{
    public static string Text(string value, string field, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field);
        var clean = value.Trim();
        if (clean.Length > max || clean.Any(char.IsControl)) throw new ArgumentException("Invalid text.", field);
        return clean;
    }
}
