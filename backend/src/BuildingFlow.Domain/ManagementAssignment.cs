namespace BuildingFlow.Domain;

public sealed class ManagementAssignment:BaseEntity
{
    private ManagementAssignment() { }
    public ManagementAssignment(Guid tenantId,Guid propertyId,Guid personId,string role,DateOnly startDate,DateOnly? endDate=null)
    {
        if(tenantId==Guid.Empty || propertyId==Guid.Empty || personId==Guid.Empty)throw new ArgumentException("Assignment scope required.");
        if(role is not("SITE_ADMIN" or "MANAGER" or "ACCOUNTANT"))throw new ArgumentException("Invalid management role.");
        if(startDate==DateOnly.MinValue || endDate<startDate)throw new ArgumentException("Invalid assignment dates.");
        TenantId=tenantId;PropertyId=propertyId;PersonId=personId;Role=role;StartDate=startDate;EndDate=endDate;
    }
    public Guid TenantId {get;private set;}
    public Guid PropertyId {get;private set;}
    public Guid PersonId {get;private set;}
    public string Role {get;private set;}=null!;
    public DateOnly StartDate {get;private set;}
    public DateOnly? EndDate {get;private set;}
    public string? EndReason {get;private set;}
    public DateTimeOffset? CancelledAt {get;private set;}
    public string? CancellationReason {get;private set;}
    public bool Contains(DateOnly date)=>!IsDeleted && IsEnabled && CancelledAt==null && StartDate<=date && (EndDate==null || EndDate>=date);
    public bool Overlaps(DateOnly start,DateOnly? end)=>CancelledAt==null && (end==null || StartDate<=end) && (EndDate==null || EndDate>=start);
    public void End(DateOnly end,string reason)
    {
        if(CancelledAt!=null || EndDate!=null)throw new InvalidOperationException("Assignment already closed.");
        if(end<StartDate)throw new ArgumentException("Invalid assignment end.");
        reason=CleanReason(reason);EndDate=end;EndReason=reason;
    }
    public void Cancel(DateTimeOffset at,string reason)
    {
        if(CancelledAt!=null)throw new InvalidOperationException("Assignment already cancelled.");
        reason=CleanReason(reason);CancelledAt=at;CancellationReason=reason;
    }
    public static string CleanReason(string value)
    {
        value=Required(value,nameof(value),500);
        if(value.Any(char.IsControl))throw new ArgumentException("Invalid reason.");return value;
    }
}

public sealed class ManagementAssignmentEvent:BaseEntity
{
    private ManagementAssignmentEvent() { }
    public ManagementAssignmentEvent(Guid tenant,Guid property,Guid assignment,string action,string reason,long appliedVersion)
    {TenantId=tenant;PropertyId=property;AssignmentId=assignment;Action=action;Reason=ManagementAssignment.CleanReason(reason);AppliedVersion=appliedVersion;}
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;} public Guid AssignmentId {get;private set;}
    public string Action {get;private set;}=null!;public string Reason {get;private set;}=null!;public long AppliedVersion {get;private set;}
}
public sealed class ManagementAssignmentOperation:BaseEntity
{
    private ManagementAssignmentOperation() { }
    public ManagementAssignmentOperation(Guid tenant,Guid property,Guid assignment,string issuer,string subject,Guid operation,string fingerprint,long appliedVersion)
    {TenantId=tenant;PropertyId=property;AssignmentId=assignment;ActorIssuer=Required(issuer,nameof(issuer),500);ActorSubject=Required(subject,nameof(subject),200);OperationId=operation;Fingerprint=fingerprint;AppliedVersion=appliedVersion;}
    public Guid TenantId {get;private set;}public Guid PropertyId {get;private set;}public Guid AssignmentId {get;private set;}
    public string ActorIssuer {get;private set;}=null!;public string ActorSubject {get;private set;}=null!;
    public Guid OperationId {get;private set;}public string Fingerprint {get;private set;}=null!;public long AppliedVersion {get;private set;}
}
