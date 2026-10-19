namespace BuildingFlow.Domain;
public sealed class WebsiteInquiry:BaseEntity{
 private WebsiteInquiry(){} public WebsiteInquiry(Guid property,Guid site,Guid? listing,string name,string? phone,string? email,string message,string privacy,string ipHash,DateTimeOffset now){PropertyId=property;SiteId=site;ListingId=listing;FullName=Required(name,nameof(name),100);Phone=WebsiteSite.Optional(phone,40,nameof(phone));Email=WebsiteSite.Optional(email,200,nameof(email));Message=Required(message,nameof(message),2000);PrivacyNoticeVersion=privacy;IpHash=ipHash;RetainUntil=now.AddDays(180);}
 public Guid PropertyId{get;private set;} public Guid SiteId{get;private set;} public Guid? ListingId{get;private set;}
 public string FullName{get;private set;}=null!;public string? Phone{get;private set;}public string? Email{get;private set;}public string Message{get;private set;}=null!;public string PrivacyNoticeVersion{get;private set;}=null!;public string IpHash{get;private set;}=null!;public DateTimeOffset RetainUntil{get;private set;}public string Status{get;private set;}="NEW";
 public void SetStatus(string value){if(value is not("NEW" or "HANDLED" or "SPAM"))throw new ArgumentException("Invalid inquiry status.");Status=value;}
 public void Anonymize(){FullName="[ANONYMIZED]";Phone=null;Email=null;Message="[ANONYMIZED]";IpHash="";IsEnabled=false;}
}
