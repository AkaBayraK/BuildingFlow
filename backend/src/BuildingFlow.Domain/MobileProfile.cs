namespace BuildingFlow.Domain;
public sealed class MobileProfile : BaseEntity {
    private MobileProfile() { }
    public MobileProfile(string owner) { OwnerUserId=Required(owner,nameof(owner),200); }
    public string OwnerUserId {get;private set;}=null!;
    public string FirstName {get;private set;}="";
    public string LastName {get;private set;}="";
    public string? Phone {get;private set;}
    public Guid? PhotoId {get;private set;}
    public void SetPhoto(Guid? photo) { PhotoId=photo; }
    public void Edit(string first,string last,string? phone,Guid? photo) {FirstName=Required(first,nameof(first),80);LastName=Required(last,nameof(last),80);Phone=phone;PhotoId=photo;}
}
public sealed class MobileProfilePhoto : BaseEntity {
    private MobileProfilePhoto() { }
    public MobileProfilePhoto(string user,Guid operation,string fingerprint,string sha256,long size) {
        OwnerUserId=user;OperationId=operation;Fingerprint=fingerprint;Sha256=sha256;Size=size;ObjectKey="profiles/"+Id+"/avatar.jpg";
    }
    public string OwnerUserId {get;private set;}=null!;
    public Guid OperationId {get;private set;}
    public string Fingerprint {get;private set;}=null!;
    public string ObjectKey {get;private set;}=null!;
    public string Sha256 {get;private set;}=null!;
    public long Size {get;private set;}
    public string State {get;private set;}="STAGED";
    public void Current()=>State="CURRENT";
    public void Retire()=>State="RETIRED";
    public void Purged()=>State="PURGED";
}
