namespace BuildingFlow.Domain;

public enum SupplierContractStatus { DRAFT, ACTIVE, CANCELLED }
public sealed class SupplierContract : BaseEntity
{
    private SupplierContract() { }
    public SupplierContract(Guid tenant, Guid supplier, Guid property, Guid areaService, string title, DateOnly start, DateOnly end, decimal amount, int reminderDays, Guid? document = null, Guid? previous = null)
    {
        SupplierText.Scope(tenant, supplier);
        if (property == Guid.Empty || areaService == Guid.Empty || document == Guid.Empty || previous == Guid.Empty) throw new ArgumentException("Contract scope required.");
        TenantId = tenant; SupplierId = supplier; PropertyId = property; AreaServiceId = areaService; PreviousContractId = previous;
        Edit(title, start, end, amount, reminderDays, document);
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid AreaServiceId { get; private set; }
    public Guid? DocumentId { get; private set; }
    public Guid? PreviousContractId { get; private set; }
    public string Title { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public int ReminderDays { get; private set; }
    public SupplierContractStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public void Edit(string title, DateOnly start, DateOnly end, decimal amount, int reminderDays, Guid? document)
    {
        if (Status != SupplierContractStatus.DRAFT) throw new InvalidOperationException("Finalized contract is immutable.");
        var name = Required(title, nameof(title), 200);
        if (name.Any(char.IsControl) || start.Year < 2000 || end.Year > 2100 || end < start || reminderDays is < 0 or > 365 || document == Guid.Empty) throw new ArgumentException("Invalid contract details.");
        Payment.ValidateAmount(amount);
        Title = name; StartDate = start; EndDate = end; Amount = amount; ReminderDays = reminderDays; DocumentId = document;
    }
    public void Activate()
    { if (Status != SupplierContractStatus.DRAFT) throw new InvalidOperationException("Invalid contract transition."); Status = SupplierContractStatus.ACTIVE; }
    public void Cancel(string reason)
    {
        if (Status == SupplierContractStatus.CANCELLED) throw new InvalidOperationException("Contract already cancelled.");
        var clean = Required(reason, nameof(reason), 1000);
        if (clean.Any(char.IsControl)) throw new ArgumentException("Invalid cancellation reason.");
        CancellationReason = clean; Status = SupplierContractStatus.CANCELLED;
    }
}
public sealed class SupplierContractRevision : BaseEntity
{
    private SupplierContractRevision() { }
    public SupplierContractRevision(SupplierContract contract, Guid operation, string issuer, string subject, string fingerprint, string action, string snapshot, long appliedVersion)
    { TenantId = contract.TenantId; SupplierId = contract.SupplierId; PropertyId = contract.PropertyId; ContractId = contract.Id; OperationId = operation; ActorIssuer = Required(issuer, nameof(issuer), 500); ActorSubject = Required(subject, nameof(subject), 200); Fingerprint = Required(fingerprint, nameof(fingerprint), 64); Action = Required(action, nameof(action), 20); Snapshot = Required(snapshot, nameof(snapshot), 10000); AppliedVersion = appliedVersion; }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid ContractId { get; private set; }
    public Guid OperationId { get; private set; }
    public string ActorIssuer { get; private set; } = null!;
    public string ActorSubject { get; private set; } = null!;
    public string Fingerprint { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Snapshot { get; private set; } = null!;
    public long AppliedVersion { get; private set; }
}
