using System.Text.RegularExpressions;
namespace BuildingFlow.Domain;
public sealed class PropertyListing : BaseEntity
{
 private PropertyListing() { }
 public PropertyListing(Guid property,Guid site,Guid request,string fingerprint){PropertyId=property;SiteId=site;RequestId=request;Fingerprint=fingerprint;}
 public Guid PropertyId {get;private set;} public Guid SiteId {get;private set;} public Guid RequestId {get;private set;} public string Fingerprint {get;private set;}=null!;
 public Guid? UnitId {get;private set;} public string Kind {get;private set;}="SALE"; public string Title {get;private set;}=""; public string Description {get;private set;}="";
 public decimal Price {get;private set;} public string RoomLayout {get;private set;}=""; public decimal GrossArea {get;private set;} public decimal NetArea {get;private set;}
 public string? FloorLabel {get;private set;} public string? BuildingLabel {get;private set;} public string Features {get;private set;}="[]";
 public string Status {get;private set;}="DRAFT"; public DateTimeOffset? PublishUntil {get;private set;}
 public string? ConsentKind {get;private set;} public string? ConsentObjectKey {get;private set;} public string? ConsentSha256 {get;private set;} public string? ConsentContentType {get;private set;} public int? ConsentSize {get;private set;} public string? AgentLicenseNo {get;private set;}
 public string? ComplianceNote {get;private set;} public string? VerificationReference {get;private set;}
 public void Edit(Guid? unit,string kind,string title,string description,decimal price,string rooms,decimal gross,decimal net,string? floor,string? building,string features){
  if(kind is not("SALE" or "RENT")||price<=0||price>999999999999.99m||decimal.Round(price,2)!=price||gross<=0||gross>999999.99m||net<=0||net>gross||decimal.Round(gross,2)!=gross||decimal.Round(net,2)!=net||!Regex.IsMatch(rooms,@"^\d{1,2}\+\d$",RegexOptions.CultureInvariant))throw new ArgumentException("Invalid listing.");
  UnitId=unit;Kind=kind;Title=WebsiteSite.PlainText(Required(title,nameof(title),120),nameof(title));Description=WebsiteSite.PlainText(Required(description,nameof(description),4000),nameof(description));Price=price;RoomLayout=rooms;GrossArea=gross;NetArea=net;FloorLabel=WebsiteSite.Optional(floor,40,nameof(floor));BuildingLabel=WebsiteSite.Optional(building,60,nameof(building));Features=features;
  if(Status is "APPROVED" or "PUBLISHED" or "PENDING_APPROVAL")Invalidate(true);
 }
 public void Invalidate(bool review=false){Status=review?"PENDING_APPROVAL":"DRAFT";ComplianceNote=null;VerificationReference=null;PublishUntil=null;}
 public void Consent(string kind,string key,string sha,string mime,int size,string? license){if(kind is not("WRITTEN_STATEMENT" or "AUTHORIZED_AGENT")||kind=="AUTHORIZED_AGENT"&&string.IsNullOrWhiteSpace(license))throw new ArgumentException("Consent required.");ConsentKind=kind;ConsentObjectKey=key;ConsentSha256=sha;ConsentContentType=mime;ConsentSize=size;AgentLicenseNo=WebsiteSite.Optional(license,60,nameof(license));Invalidate(Status is "APPROVED" or "PUBLISHED" or "PENDING_APPROVAL");}
 public void Transition(string action,string? note,DateTimeOffset now,DateTimeOffset? until=null,string? verification=null){
  switch(action){
   case "SUBMIT" when Status is "DRAFT" or "WITHDRAWN" or "EXPIRED": if(ConsentObjectKey is null)throw new ArgumentException("Consent required.");Status="PENDING_APPROVAL";break;
   case "APPROVE" when Status=="PENDING_APPROVAL":ComplianceNote=Required(note!,nameof(note),500);Status="APPROVED";break;
   case "REJECT" when Status=="PENDING_APPROVAL":_=Required(note!,nameof(note),500);Invalidate();break;
   case "PUBLISH" when Status=="APPROVED":if(until is null||until<=now||until>now.AddDays(90)||string.IsNullOrWhiteSpace(verification))throw new ArgumentException("Verified authority required.");VerificationReference=Required(verification,nameof(verification),200);PublishUntil=until;Status="PUBLISHED";break;
   case "WITHDRAW" when Status is "PUBLISHED" or "APPROVED" or "PENDING_APPROVAL":_=Required(note!,nameof(note),500);Status="WITHDRAWN";PublishUntil=null;break;
   case "EXPIRE" when Status=="PUBLISHED"&&PublishUntil<=now:Status="EXPIRED";break;
   default:throw new ArgumentException("Invalid transition.");
  }
 }
}
public sealed class ListingPhoto : BaseEntity {
 private ListingPhoto(){} public ListingPhoto(Guid property,Guid site,Guid listing,Guid media,int order){PropertyId=property;SiteId=site;ListingId=listing;MediaId=media;SortOrder=order;}
 public Guid PropertyId{get;private set;} public Guid SiteId{get;private set;} public Guid ListingId{get;private set;} public Guid MediaId{get;private set;} public int SortOrder{get;private set;}
}
public sealed class ListingEvent : BaseEntity {
 private ListingEvent(){} public ListingEvent(Guid property,Guid listing,string action,string? note,long listingVersion){PropertyId=property;ListingId=listing;Action=action;Note=WebsiteSite.Optional(note,500,nameof(note));ListingVersion=listingVersion;}
 public Guid PropertyId{get;private set;} public Guid ListingId{get;private set;} public string Action{get;private set;}=null!; public string? Note{get;private set;} public long ListingVersion{get;private set;}
}
