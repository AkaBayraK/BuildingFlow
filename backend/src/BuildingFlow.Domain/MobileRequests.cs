namespace BuildingFlow.Domain;
public sealed class MobileRequest : BaseEntity
{
    private MobileRequest() { }
    public MobileRequest(Guid id, Guid property, Guid? unit, string role, string owner, string description) {
        Id=id; PropertyId=property; UnitId=unit; ContextRole=role; OwnerUserId=owner; Edit(description);
    }
    public Guid PropertyId { get; private set; }
    public Guid? UnitId { get; private set; }
    public string ContextRole { get; private set; } = null!;
    public string OwnerUserId { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public void Edit(string description) => Description=Required(description,nameof(description),2000);
}
public sealed class MobileOperationReceipt : BaseEntity
{
    private MobileOperationReceipt() { }
    public MobileOperationReceipt(string user, Guid operation, string fingerprint, string result) { OwnerUserId=user; OperationId=operation; Fingerprint=fingerprint; Result=result; }
    public string OwnerUserId { get; private set; } = null!;
    public Guid OperationId { get; private set; }
    public string Fingerprint { get; private set; } = null!;
    public string Result { get; private set; } = null!;
}
