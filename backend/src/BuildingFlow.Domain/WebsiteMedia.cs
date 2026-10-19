namespace BuildingFlow.Domain;
public sealed class WebsiteMedia : BaseEntity
{
    private WebsiteMedia() { }
    public WebsiteMedia(Guid property, Guid site, Guid requestId, string fingerprint, string contentType, int size, int width, int height, string sha256, string alt, string rights, string? note, bool people, bool consent)
    {
        if(property==Guid.Empty||site==Guid.Empty||requestId==Guid.Empty||size is <1 or >5242880||width is <1 or >4096||height is <1 or >4096||contentType is not ("image/jpeg" or "image/png")||fingerprint.Length!=64||sha256.Length!=64)throw new ArgumentException("Invalid media.");
        PropertyId=property;SiteId=site;RequestId=requestId;Fingerprint=fingerprint;ContentType=contentType;Size=size;Width=width;Height=height;Sha256=sha256;
        ObjectKey=$"websites/{site:N}/media/{Id:N}";Edit(alt,rights,note,people,consent);
    }
    public Guid PropertyId {get;private set;} public Guid SiteId {get;private set;} public Guid RequestId {get;private set;}
    public string Fingerprint {get;private set;}=null!; public string ObjectKey {get;private set;}=null!;
    public string ContentType {get;private set;}=null!; public int Size {get;private set;} public int Width {get;private set;} public int Height {get;private set;} public string Sha256 {get;private set;}=null!;
    public string AltText {get;private set;}=null!; public string RightsBasis {get;private set;}=null!; public string? RightsNote {get;private set;}
    public bool ContainsPeople {get;private set;} public bool PeopleConsentConfirmed {get;private set;}
    public void Edit(string alt,string rights,string? note,bool people,bool consent){AltText=WebsiteSite.PlainText(Required(alt,nameof(alt),200),nameof(alt));if(rights is not ("OWN_WORK" or "LICENSED" or "PERMISSION")||people&&!consent)throw new ArgumentException("Media rights required.");RightsBasis=rights;RightsNote=WebsiteSite.Optional(note,500,nameof(note));if(rights!="OWN_WORK"&&RightsNote is null)throw new ArgumentException("Rights note required.");ContainsPeople=people;PeopleConsentConfirmed=consent;}
}
