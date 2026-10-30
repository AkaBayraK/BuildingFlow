namespace BuildingFlow.Domain;

public enum SupplierInvoiceStatus { RECORDED, APPROVED, PAID, RECONCILED, CANCELLED }

// A supplier bill tracked for accounting only: marking it paid records a payment made outside BuildingFlow, it never moves money.
public sealed class SupplierInvoice : BaseEntity
{
    private SupplierInvoice() { }
    public SupplierInvoice(Guid tenant, Guid supplier, Guid property, string number, DateOnly issueDate, DateOnly dueDate, decimal netAmount, decimal vatRate, ExpenseCategory category, string description, Guid? workOrder, Guid? contract, Guid? corrects, string issuer, string subject, DateOnly today)
    {
        SupplierText.Scope(tenant, supplier);
        if (property == Guid.Empty || workOrder == Guid.Empty || contract == Guid.Empty || corrects == Guid.Empty || !Enum.IsDefined(category)) throw new ArgumentException("Invalid invoice scope.");
        if (issueDate.Year < 2000 || issueDate > today || dueDate < issueDate || dueDate.Year > 2100) throw new ArgumentException("Invalid invoice dates.");
        Payment.ValidateAmount(netAmount);
        if (vatRate is < 0 or > 100 || decimal.Round(vatRate, 2) != vatRate) throw new ArgumentException("Invalid VAT rate.");
        var vat = decimal.Round(netAmount * vatRate / 100m, 2, MidpointRounding.AwayFromZero);
        Payment.ValidateAmount(netAmount + vat);
        Number = SupplierQuoteRules.Text(number, nameof(number), 50); NumberKey = Key(Number);
        TenantId = tenant; SupplierId = supplier; PropertyId = property; IssueDate = issueDate; DueDate = dueDate;
        NetAmount = netAmount; VatRate = vatRate; VatAmount = vat; GrossAmount = netAmount + vat; Category = category;
        Description = SupplierQuoteRules.Text(description, nameof(description), 400); WorkOrderId = workOrder; ContractId = contract; CorrectsInvoiceId = corrects;
        RecordedByIssuer = Required(issuer, nameof(issuer), 500); RecordedBySubject = Required(subject, nameof(subject), 200);
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Number { get; private set; } = null!;
    public string NumberKey { get; private set; } = null!;
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal GrossAmount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public ExpenseCategory Category { get; private set; }
    public string Description { get; private set; } = null!;
    public Guid? WorkOrderId { get; private set; }
    public Guid? ContractId { get; private set; }
    public Guid? CorrectsInvoiceId { get; private set; }
    public Guid? ExpenseId { get; private set; }
    public SupplierInvoiceStatus Status { get; private set; }
    public string RecordedByIssuer { get; private set; } = null!;
    public string RecordedBySubject { get; private set; } = null!;
    public DateOnly? PaidOn { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateOnly? ReconciledOn { get; private set; }
    public string? ReconciliationNote { get; private set; }
    public string? CancellationReason { get; private set; }
    public static string Key(string number) => new(number.Trim().ToUpperInvariant().Where(c => !char.IsWhiteSpace(c) && c is not ('-' or '/' or '.')).ToArray());
    public bool IsRecordedBy(string issuer, string subject) => RecordedByIssuer == issuer && RecordedBySubject == subject;
    public void Approve(Guid expense)
    {
        if (Status != SupplierInvoiceStatus.RECORDED || expense == Guid.Empty) throw new InvalidOperationException("Only a recorded invoice can be approved.");
        ExpenseId = expense; Status = SupplierInvoiceStatus.APPROVED;
    }
    public void Pay(DateOnly paidOn, string reference, DateOnly today)
    {
        if (Status != SupplierInvoiceStatus.APPROVED) throw new InvalidOperationException("Only an approved invoice can be marked paid.");
        if (paidOn < IssueDate || paidOn > today) throw new ArgumentException("Invalid payment date.");
        PaymentReference = SupplierQuoteRules.Text(reference, nameof(reference), 100); PaidOn = paidOn; Status = SupplierInvoiceStatus.PAID;
    }
    public void Reconcile(DateOnly reconciledOn, string note, DateOnly today)
    {
        if (Status != SupplierInvoiceStatus.PAID) throw new InvalidOperationException("Only a paid invoice can be reconciled.");
        if (reconciledOn < PaidOn || reconciledOn > today) throw new ArgumentException("Invalid reconciliation date.");
        ReconciliationNote = SupplierQuoteRules.Text(note, nameof(note), 1000); ReconciledOn = reconciledOn; Status = SupplierInvoiceStatus.RECONCILED;
    }
    public void Cancel(string reason)
    {
        if (Status is not (SupplierInvoiceStatus.RECORDED or SupplierInvoiceStatus.APPROVED)) throw new InvalidOperationException("Paid invoice history cannot be cancelled.");
        CancellationReason = SupplierQuoteRules.Text(reason, nameof(reason), 1000); Status = SupplierInvoiceStatus.CANCELLED;
    }
}

public sealed class SupplierInvoiceRevision : BaseEntity
{
    private SupplierInvoiceRevision() { }
    public SupplierInvoiceRevision(SupplierInvoice invoice, Guid operation, string issuer, string subject, string fingerprint, string action, string snapshot, long appliedVersion)
    {
        if (operation == Guid.Empty || appliedVersion < 1 || action is not ("CREATE" or "APPROVE" or "PAY" or "RECONCILE" or "CANCEL")) throw new ArgumentException("Invalid invoice revision.");
        TenantId = invoice.TenantId; SupplierId = invoice.SupplierId; PropertyId = invoice.PropertyId; InvoiceId = invoice.Id; OperationId = operation;
        ActorIssuer = Required(issuer, nameof(issuer), 500); ActorSubject = Required(subject, nameof(subject), 200); Fingerprint = Required(fingerprint, nameof(fingerprint), 64);
        Action = action; Snapshot = Required(snapshot, nameof(snapshot), 20000); AppliedVersion = appliedVersion;
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid OperationId { get; private set; }
    public string ActorIssuer { get; private set; } = null!;
    public string ActorSubject { get; private set; } = null!;
    public string Fingerprint { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Snapshot { get; private set; } = null!;
    public long AppliedVersion { get; private set; }
}
