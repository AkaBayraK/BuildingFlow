namespace BuildingFlow.Domain;
public sealed class CommonAreaPhoto : BaseEntity
{
    private CommonAreaPhoto() { }
    public CommonAreaPhoto(Guid property, Guid area, Guid document, bool rightsConfirmed)
    {
        if (property == Guid.Empty || area == Guid.Empty || document == Guid.Empty || !rightsConfirmed) throw new ArgumentException("Photo scope and usage rights required.");
        PropertyId = property; CommonAreaId = area; DocumentId = document; RightsConfirmed = true;
    }
    public Guid PropertyId { get; private set; }
    public Guid CommonAreaId { get; private set; }
    public Guid DocumentId { get; private set; }
    public bool RightsConfirmed { get; private set; }
}
