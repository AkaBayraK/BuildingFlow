namespace BuildingFlow.Domain;

public sealed class SupplierAlertPreference : BaseEntity
{
    private SupplierAlertPreference() { }
    public SupplierAlertPreference(Guid tenant, string issuer, string subject)
    {
        if (tenant == Guid.Empty) throw new ArgumentException("Tenant required.");
        TenantId = tenant; ActorIssuer = Required(issuer, nameof(issuer), 500); ActorSubject = Required(subject, nameof(subject), 200);
    }
    public Guid TenantId { get; private set; }
    public string ActorIssuer { get; private set; } = null!;
    public string ActorSubject { get; private set; } = null!;
    public bool ContractExpiry { get; private set; } = true;
    public bool WorkOrderOverdue { get; private set; } = true;
    public bool MaintenanceDue { get; private set; } = true;
    public void Set(bool contractExpiry, bool workOrderOverdue, bool maintenanceDue) { ContractExpiry = contractExpiry; WorkOrderOverdue = workOrderOverdue; MaintenanceDue = maintenanceDue; }
}
