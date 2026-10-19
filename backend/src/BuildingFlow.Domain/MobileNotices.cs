namespace BuildingFlow.Domain;
public sealed class MobileAnnouncementRead:BaseEntity{
 private MobileAnnouncementRead(){}
 public MobileAnnouncementRead(string user,Guid property,Guid announcement,DateTimeOffset at){OwnerUserId=Required(user,nameof(user),200);PropertyId=property;AnnouncementId=announcement;ReadAt=at.AddTicks(-(at.Ticks%10));}// PostgreSQL keeps microseconds; the stored value and the first response must agree.
 public string OwnerUserId{get;private set;}=null!;
 public Guid PropertyId{get;private set;}
 public Guid AnnouncementId{get;private set;}
 public DateTimeOffset ReadAt{get;private set;}
}
public sealed class MobileNoticePreference:BaseEntity{
 private MobileNoticePreference(){}
 public MobileNoticePreference(string user){OwnerUserId=Required(user,nameof(user),200);}
 public string OwnerUserId{get;private set;}=null!;
 public bool HighlightsEnabled{get;private set;}=true;
 public void SetHighlights(bool enabled)=>HighlightsEnabled=enabled;
}
