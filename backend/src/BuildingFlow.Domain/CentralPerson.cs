namespace BuildingFlow.Domain;

public sealed class PersonTenant : BaseEntity {
 private PersonTenant() { }
 public PersonTenant(string name) => Name=Required(name,nameof(name),200);
 public string Name { get; private set; } = null!;
}
public sealed class PersonUserAccount : BaseEntity {
 private PersonUserAccount() { }
 public PersonUserAccount(string issuer,string subject) { Issuer=Required(issuer,nameof(issuer),500);Subject=Required(subject,nameof(subject),200); }
 public string Issuer {get;private set;}=null!;
 public string Subject {get;private set;}=null!;
}
public sealed class PersonTenantMembership : BaseEntity {
 private PersonTenantMembership() { }
 public PersonTenantMembership(Guid tenant,Guid account) { if(tenant==Guid.Empty || account==Guid.Empty)throw new ArgumentException("Identity required.");TenantId=tenant;UserAccountId=account; }
 public Guid TenantId {get;private set;}
 public Guid UserAccountId {get;private set;}
}
// The legacy Property-bound Person stays mapped to people until the explicit phase04 transition.
public sealed class CentralPerson : BaseEntity {
 private CentralPerson() { }
 public CentralPerson(Guid tenant,string kind,string citizenship,string country,string identity,string first,string? last,DateOnly? birth,string? gender,string? phone,string? email) {
  if(tenant==Guid.Empty)throw new ArgumentException("Tenant required.");TenantId=tenant;Edit(kind,citizenship,country,identity,first,last,birth,gender,phone,email,true);
 }
 public Guid TenantId {get;private set;}
 public string Kind {get;private set;}=null!;
 public string Citizenship {get;private set;}=null!;
 public string CountryCode {get;private set;}=null!;
 public string IdentityNumber {get;private set;}=null!;
 public string FirstName {get;private set;}=null!;
 public string LastName {get;private set;}=null!;
 public DateOnly? BirthDate {get;private set;}
 public string? Gender {get;private set;}
 public string? PhoneNumber {get;private set;}
 public string? Email {get;private set;}
 public void Edit(string kind,string citizenship,string country,string identity,string first,string? last,DateOnly? birth,string? gender,string? phone,string? email,bool enabled) {
  if(kind is not("NATURAL" or "LEGAL") || citizenship is not("DOMESTIC" or "FOREIGN") || country.Length!=2 || country.Any(c=>c is <'A' or >'Z') || (citizenship=="DOMESTIC"?country!="TR":country=="TR"))throw new ArgumentException("Invalid person identity.");
  if(kind=="LEGAL" && (birth!=null || gender!=null) || gender is not(null or "FEMALE" or "MALE" or "OTHER" or "UNSPECIFIED"))throw new ArgumentException("Invalid personal details.");
  var id=Required(identity,nameof(identity),50);var name=Required(first,nameof(first),100);var surname=kind=="LEGAL" && string.IsNullOrWhiteSpace(last)?"":Required(last!,nameof(last),100);
  if(citizenship=="DOMESTIC" && (id.Length!=(kind=="LEGAL"?10:11) || id.Any(c=>!char.IsAsciiDigit(c)) || kind=="NATURAL" && id[0]=='0') || citizenship=="FOREIGN" && kind=="NATURAL" && (id.Length is <6 or >30 || id.Any(c=>!char.IsAsciiLetterOrDigit(c) && c!='-')))throw new ArgumentException("Invalid identity format.");
  var telephone=string.IsNullOrWhiteSpace(phone)?null:Required(phone,nameof(phone),30);var mail=string.IsNullOrWhiteSpace(email)?null:Required(email,nameof(email),254);
  Kind=kind;Citizenship=citizenship;CountryCode=country;IdentityNumber=id;FirstName=name;LastName=surname;BirthDate=birth;Gender=gender;PhoneNumber=telephone;Email=mail;SetEnabled(enabled);
 }
}
public sealed class CentralPersonAccountLink : BaseEntity {
 private CentralPersonAccountLink() { }
 public CentralPersonAccountLink(Guid tenant,Guid account,Guid person,DateTimeOffset linkedAt) { if(tenant==Guid.Empty||account==Guid.Empty||person==Guid.Empty)throw new ArgumentException("Identity required.");TenantId=tenant;UserAccountId=account;PersonId=person;LinkedAt=linkedAt; }
 public Guid TenantId {get;private set;}
 public Guid UserAccountId {get;private set;}
 public Guid PersonId {get;private set;}
 public DateTimeOffset LinkedAt {get;private set;}
 public DateTimeOffset? EndedAt {get;private set;}
 public void End(DateTimeOffset at) {if(EndedAt!=null||at<LinkedAt)throw new ArgumentException("Invalid link end.");EndedAt=at;}
}
public sealed class CentralPersonOperation : BaseEntity {
 private CentralPersonOperation() { }
 public CentralPersonOperation(Guid tenant,string issuer,string subject,Guid operation,string fingerprint,Guid person,long appliedVersion) {TenantId=tenant;ActorIssuer=Required(issuer,nameof(issuer),500);ActorSubject=Required(subject,nameof(subject),200);OperationId=operation;Fingerprint=Required(fingerprint,nameof(fingerprint),64);PersonId=person;AppliedVersion=appliedVersion;}
 public Guid TenantId {get;private set;}
 public string ActorIssuer {get;private set;}=null!;
 public string ActorSubject {get;private set;}=null!;
 public Guid OperationId {get;private set;}
 public string Fingerprint {get;private set;}=null!;
 public Guid PersonId {get;private set;}
 public long AppliedVersion {get;private set;}
}
// A manager-issued one-time code; only its HMAC is stored and a used or revoked code never becomes valid again.
public sealed class CentralPersonLinkCode : BaseEntity {
 public static readonly TimeSpan Lifetime=TimeSpan.FromHours(72);
 private CentralPersonLinkCode() { }
 public CentralPersonLinkCode(Guid tenant,Guid person,string codeHash,string hint,Guid issuedBy,DateTimeOffset issuedAt) {
  if(tenant==Guid.Empty||person==Guid.Empty||issuedBy==Guid.Empty||codeHash is not {Length:64}||!codeHash.All(char.IsAsciiHexDigit)||hint is not {Length:2}||!hint.All(char.IsAsciiDigit))throw new ArgumentException("Invalid link code.");
  TenantId=tenant;PersonId=person;CodeHash=codeHash;Hint=hint;IssuedByAccountId=issuedBy;ExpiresAt=issuedAt+Lifetime;
 }
 public Guid TenantId {get;private set;}
 public Guid PersonId {get;private set;}
 public string CodeHash {get;private set;}=null!;
 public string Hint {get;private set;}=null!;
 public Guid IssuedByAccountId {get;private set;}
 public DateTimeOffset ExpiresAt {get;private set;}
 public DateTimeOffset? UsedAt {get;private set;}
 public Guid? UsedByAccountId {get;private set;}
 public DateTimeOffset? RevokedAt {get;private set;}
 public bool IsUsable(DateTimeOffset now)=>IsEnabled && UsedAt==null && RevokedAt==null && now<ExpiresAt;
 public void Use(Guid account,DateTimeOffset at){if(!IsUsable(at)||account==Guid.Empty)throw new InvalidOperationException("Link code is not usable.");UsedAt=at;UsedByAccountId=account;}
 public void Revoke(DateTimeOffset at){if(UsedAt!=null||RevokedAt!=null)throw new InvalidOperationException("Link code is closed.");RevokedAt=at;}
}
