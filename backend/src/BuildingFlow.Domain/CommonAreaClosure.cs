namespace BuildingFlow.Domain;
public sealed class CommonAreaClosure : BaseEntity
{
    private CommonAreaClosure() { }
    public CommonAreaClosure(Guid property, Guid area, Guid plan, Guid request, string fingerprint, DateOnly date,
        TimeOnly start, TimeOnly end, string notice, Guid announcement)
    {
        if (property==Guid.Empty || area==Guid.Empty || plan==Guid.Empty || request==Guid.Empty || announcement==Guid.Empty || fingerprint.Length!=64 ||
            start>=end || !CommonAreaReservation.OnSlot(start) || !CommonAreaReservation.OnSlot(end)) throw new ArgumentException("Invalid maintenance closure.");
        PropertyId=property;CommonAreaId=area;MaintenancePlanId=plan;RequestId=request;Fingerprint=fingerprint;Date=date;StartsAt=start;EndsAt=end;
        Notice=Required(notice,nameof(notice),1000);AnnouncementId=announcement;
    }
    public Guid PropertyId {get;private set;} public Guid CommonAreaId {get;private set;} public Guid MaintenancePlanId {get;private set;}
    public Guid RequestId {get;private set;} public string Fingerprint {get;private set;}=null!; public DateOnly Date {get;private set;}
    public TimeOnly StartsAt {get;private set;} public TimeOnly EndsAt {get;private set;} public string Notice {get;private set;}=null!;
    public Guid AnnouncementId {get;private set;} public Guid? ReleaseAnnouncementId {get;private set;}
    public DateTimeOffset? ReleasedAt {get;private set;} public string? ReleaseReason {get;private set;}
    public int NotificationCount {get;private set;}
    public void SetNotificationCount(int value){if(value<0)throw new ArgumentOutOfRangeException(nameof(value));NotificationCount=value;}
    public void Release(string reason,DateTimeOffset at,Guid announcement)
    {if(ReleasedAt!=null)throw new InvalidOperationException("Already released.");ReleaseReason=Required(reason,nameof(reason),500);ReleasedAt=at;ReleaseAnnouncementId=announcement;}
}
