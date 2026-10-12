namespace BuildingFlow.Domain;
public sealed class PersonMessage : BaseEntity
{
    private PersonMessage() { }
    public PersonMessage(Guid property,Guid person,Guid request,string fingerprint,string recipient,string title,string body)
    {
        if(property==Guid.Empty||person==Guid.Empty||request==Guid.Empty)throw new ArgumentException("Identity required.");
        PropertyId=property;PersonId=person;RequestId=request;Fingerprint=Required(fingerprint,nameof(fingerprint),64);
        RecipientUserId=Required(recipient,nameof(recipient),200);Title=Required(title,nameof(title),200);Body=Required(body,nameof(body),2000);
    }
    public Guid PropertyId {get;private set;}
    public Guid PersonId {get;private set;}
    public Guid RequestId {get;private set;}
    public string Fingerprint {get;private set;}=null!;
    public string RecipientUserId {get;private set;}=null!;
    public string Title {get;private set;}=null!;
    public string Body {get;private set;}=null!;
    public DateTimeOffset? ReadAt {get;private set;}
    public void MarkRead(DateTimeOffset now){ReadAt ??= now;}
}
