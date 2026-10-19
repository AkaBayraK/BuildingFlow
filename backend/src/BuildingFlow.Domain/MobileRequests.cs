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
    public string Category {get;private set;}="OTHER";
    public string Priority {get;private set;}="NORMAL";
    public string ManagementStatus { get; private set; } = "RECORDED";
    public string? ManagementResponse { get; private set; }
    public bool HasManagementHistory { get; private set; }
    public void Manage(string status, string response) {
        if (!CanTransition(ManagementStatus, status)) throw new ArgumentException("Invalid request transition.");
        ManagementResponse=Required(response,nameof(response),2000); ManagementStatus=status; HasManagementHistory=true;
    }
    public static bool CanTransition(string from,string to) => (from,to) is
        ("RECORDED","IN_PROGRESS" or "REJECTED") or ("IN_PROGRESS","RESOLVED" or "REJECTED") or
        ("RESOLVED" or "REJECTED","RECORDED");
    public Guid? PhotoId {get;private set;}
    public void Details(string category,string priority,Guid? photo){
        if(category is not ("FAULT" or "CLEANING" or "SECURITY" or "COMMON_AREA" or "SUGGESTION" or "OTHER") || priority is not ("LOW" or "NORMAL" or "HIGH"))throw new ArgumentException("Invalid request details.");
        Category=category;Priority=priority;PhotoId=photo;
    }
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

public sealed class MobileRequestPhoto:BaseEntity {
 private MobileRequestPhoto(){}
 public MobileRequestPhoto(string user,Guid property,Guid record,Guid operation,string fingerprint,string sha,long size){OwnerUserId=user;PropertyId=property;RequestId=record;OperationId=operation;Fingerprint=fingerprint;Sha256=sha;Size=size;ObjectKey="requests/"+Id+"/photo.jpg";}
 public string OwnerUserId {get;private set;}=null!;
 public Guid PropertyId {get;private set;}
 public Guid RequestId {get;private set;}
 public Guid OperationId {get;private set;}
 public string Fingerprint {get;private set;}=null!;
 public string Sha256 {get;private set;}=null!;
 public string ObjectKey {get;private set;}=null!;
 public long Size {get;private set;}
 public string State {get;private set;}="STAGED";
 public void Current()=>State="CURRENT";
 public void Retire()=>State="RETIRED";
 public void Purged()=>State="PURGED";
}

public sealed class RequestManagementEvent : BaseEntity {
    private RequestManagementEvent() { }
    public RequestManagementEvent(Guid property,Guid request,string from,string to,string response,string actor,Guid operation,string fingerprint,long expectedVersion) {
        PropertyId=property; RequestId=request; FromStatus=from; ToStatus=to; Response=response; ActorUserId=actor; OperationId=operation; Fingerprint=fingerprint; ExpectedVersion=expectedVersion;
    }
    public Guid PropertyId {get;private set;}
    public Guid RequestId {get;private set;}
    public string FromStatus {get;private set;}=null!;
    public string ToStatus {get;private set;}=null!;
    public string Response {get;private set;}=null!;
    public string ActorUserId {get;private set;}=null!;
    public Guid OperationId {get;private set;}
    public string Fingerprint {get;private set;}=null!;
    public long ExpectedVersion {get;private set;}
}
