namespace BuildingFlow.Domain;

public enum SupplierKind { COMPANY, INDIVIDUAL }
public enum SupplierStatus { DRAFT, ACTIVE, INACTIVE, BLOCKED }

// Tenant catalogue; credentials, national identity and banking data are deliberately absent.
public sealed class Supplier : BaseEntity
{
    private Supplier() { }
    public Supplier(Guid tenant, string name, SupplierKind kind, string? description = null)
    {
        if (tenant == Guid.Empty) throw new ArgumentException("Tenant required.");
        TenantId = tenant; Edit(name, kind, description);
    }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string NameKey { get; private set; } = null!;
    public SupplierKind Kind { get; private set; }
    public string? Description { get; private set; }
    public SupplierStatus Status { get; private set; } = SupplierStatus.DRAFT;
    public string? StatusReason { get; private set; }
    public void Edit(string name, SupplierKind kind, string? description)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Invalid supplier kind.");
        var next = Required(name, nameof(name), 200);
        var details = SupplierText.Optional(description, 2000);
        Name = next; NameKey = next.ToUpperInvariant(); Kind = kind; Description = details;
    }
    public void ChangeStatus(SupplierStatus status, string? reason)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentException("Invalid supplier status.");
        var clean = SupplierText.Optional(reason, 1000);
        if (status is SupplierStatus.INACTIVE or SupplierStatus.BLOCKED && clean is null)
            throw new ArgumentException("Status reason required.");
        Status = status; StatusReason = clean; SetEnabled(status is SupplierStatus.DRAFT or SupplierStatus.ACTIVE);
    }
}
internal static class SupplierText
{
    public static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (value.Length > max || value.Any(char.IsControl)) throw new ArgumentException("Invalid supplier text.");
        return value;
    }
    public static void Scope(Guid tenant, Guid parent)
    { if (tenant == Guid.Empty || parent == Guid.Empty) throw new ArgumentException("Supplier scope required."); }
}
public sealed class SupplierCategory : BaseEntity
{
    private SupplierCategory() { }
    public SupplierCategory(Guid tenant, string name, Guid? id = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Category identity required.");
        if (id.HasValue) Id = id.Value; if (tenant == Guid.Empty) throw new ArgumentException("Tenant required."); TenantId = tenant; Name = Required(name, nameof(name), 100); NameKey = Name.ToUpperInvariant(); }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string NameKey { get; private set; } = null!;
    public void Rename(string name) { Name = Required(name, nameof(name), 100); NameKey = Name.ToUpperInvariant(); }
}
public sealed class SupplierService : BaseEntity
{
    private SupplierService() { }
    public SupplierService(Guid tenant, Guid supplier, Guid category, string name)
    { SupplierText.Scope(tenant, supplier); if (category == Guid.Empty) throw new ArgumentException("Category required."); TenantId = tenant; SupplierId = supplier; CategoryId = category; Name = Required(name, nameof(name), 200); NameKey = Name.ToUpperInvariant(); }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = null!;
    public string NameKey { get; private set; } = null!;
}
public sealed class SupplierManagementArea : BaseEntity
{
    private SupplierManagementArea() { }
    public SupplierManagementArea(Guid tenant, Guid supplier, Guid property)
    { SupplierText.Scope(tenant, supplier); if (property == Guid.Empty) throw new ArgumentException("Property required."); TenantId = tenant; SupplierId = supplier; PropertyId = property; }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
}
public sealed class SupplierContact : BaseEntity
{
    private SupplierContact() { }
    public SupplierContact(Guid tenant, Guid supplier, string name, string? phone, string? email)
    {
        SupplierText.Scope(tenant, supplier); TenantId = tenant; SupplierId = supplier;
        Name = Required(name, nameof(name), 200); Phone = SupplierText.Optional(phone, 30); Email = SupplierText.Optional(email, 254);
        if (Phone is null && Email is null) throw new ArgumentException("Contact channel required.");
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
}
// Metadata links a supplier's area to the existing private StoredDocument; no public URL.
public sealed class SupplierDocument : BaseEntity
{
    private SupplierDocument() { }
    public SupplierDocument(Guid tenant, Guid supplier, Guid property, Guid document, DateOnly? expiresOn, Guid? previousDocument = null, int revision = 1)
    { SupplierText.Scope(tenant, supplier); if (property == Guid.Empty || document == Guid.Empty) throw new ArgumentException("Document scope required."); TenantId = tenant; SupplierId = supplier; PropertyId = property; DocumentId = document; ExpiresOn = expiresOn; if (previousDocument == Guid.Empty || revision < 1 || (previousDocument.HasValue ? revision < 2 : revision != 1)) throw new ArgumentException("Invalid document revision."); PreviousDocumentId = previousDocument; RevisionNumber = revision; }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid DocumentId { get; private set; }
    public DateOnly? ExpiresOn { get; private set; }
    public Guid? PreviousDocumentId { get; private set; }
    public int RevisionNumber { get; private set; }
}

public sealed class SupplierOperation : BaseEntity
{
    private SupplierOperation() { }
    public SupplierOperation(Guid tenant, Guid supplier, Guid operation, string issuer, string subject, string fingerprint, long appliedVersion)
    { SupplierText.Scope(tenant, supplier); TenantId = tenant; SupplierId = supplier; OperationId = operation; ActorIssuer = Required(issuer, nameof(issuer), 500); ActorSubject = Required(subject, nameof(subject), 200); Fingerprint = Required(fingerprint, nameof(fingerprint), 64); AppliedVersion = appliedVersion; }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid OperationId { get; private set; }
    public string ActorIssuer { get; private set; } = null!;
    public string ActorSubject { get; private set; } = null!;
    public string Fingerprint { get; private set; } = null!;
    public long AppliedVersion { get; private set; }
}

public sealed class SupplierAreaService : BaseEntity
{
    private SupplierAreaService() { }
    public SupplierAreaService(Guid tenant, Guid supplier, Guid property, Guid service)
    { SupplierText.Scope(tenant, supplier); if (property == Guid.Empty || service == Guid.Empty) throw new ArgumentException("Service scope required."); TenantId = tenant; SupplierId = supplier; PropertyId = property; ServiceId = service; }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid ServiceId { get; private set; }
}
