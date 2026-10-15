namespace BuildingFlow.Domain;
public sealed class MobileRequest : BaseEntity
{
    private MobileRequest() { }
    public static readonly string[] Categories=["PLUMBING","ELECTRICAL","HEATING","ELEVATOR","CLEANING","SECURITY","OTHER"];
    public static readonly string[] Priorities=["LOW","NORMAL","HIGH","URGENT"];
    public const int MaxPhotos=3;
    public MobileRequest(Guid id, Guid property, Guid? unit, string role, string owner, string description, string category="OTHER", string priority="NORMAL") {
        Id=id; PropertyId=property; UnitId=unit; ContextRole=role; OwnerUserId=owner; Edit(description,category,priority);
    }
    public Guid PropertyId { get; private set; }
    public Guid? UnitId { get; private set; }
    public string ContextRole { get; private set; } = null!;
    public string OwnerUserId { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Category { get; private set; } = "OTHER";
    public string Priority { get; private set; } = "NORMAL";
    public void Edit(string description, string category, string priority) {
        if(!Categories.Contains(category)) throw new ArgumentException("Invalid category.",nameof(category));
        if(!Priorities.Contains(priority)) throw new ArgumentException("Invalid priority.",nameof(priority));
        Description=Required(description,nameof(description),2000); Category=category; Priority=priority;
    }
}
public sealed class MobileRequestPhoto : BaseEntity
{
    private MobileRequestPhoto() { }
    public MobileRequestPhoto(Guid request, Guid property, string user, Guid operation, string fingerprint, string sha256, long size) {
        RequestId=request; PropertyId=property; OwnerUserId=user; OperationId=operation; Fingerprint=fingerprint; Sha256=sha256; Size=size; ObjectKey="requests/"+request+"/"+Id+".jpg";
    }
    public Guid RequestId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string OwnerUserId { get; private set; } = null!;
    public Guid OperationId { get; private set; }
    public string Fingerprint { get; private set; } = null!;
    public string ObjectKey { get; private set; } = null!;
    public string Sha256 { get; private set; } = null!;
    public long Size { get; private set; }
    public string State { get; private set; } = "STAGED";
    public void Current() => State="CURRENT";
    public void Retire() => State="RETIRED";
    public void Purged() => State="PURGED";
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
