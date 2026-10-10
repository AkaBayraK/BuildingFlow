namespace BuildingFlow.Domain;
public enum ContentAudience { ALL_MEMBERS, MANAGEMENT }
public enum AnnouncementStatus { DRAFT, PUBLISHED, ARCHIVED }
public sealed class Announcement : BaseEntity
{
    private Announcement() { }
    public Announcement(Guid property, string title, string content, ContentAudience audience)
    {
        if (property == Guid.Empty) throw new ArgumentException("Property required.");
        PropertyId = property; Edit(title, content, audience);
    }
    public Guid PropertyId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public ContentAudience Audience { get; private set; }
    public AnnouncementStatus Status { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public void Edit(string title, string content, ContentAudience audience)
    {
        if (Status != AnnouncementStatus.DRAFT) throw new InvalidOperationException("Published history is immutable.");
        if (!Enum.IsDefined(audience)) throw new ArgumentException("Invalid audience.");
        var name = Required(title, nameof(title), 200); var body = Required(content, nameof(content), 10000);
        Title = name; Content = body; Audience = audience;
    }
    public void Publish(DateTimeOffset at)
    {
        if (Status != AnnouncementStatus.DRAFT || at == default) throw new InvalidOperationException("Only drafts can be published.");
        Status = AnnouncementStatus.PUBLISHED; PublishedAt = at;
    }
    public void Archive()
    {
        if (Status != AnnouncementStatus.PUBLISHED) throw new InvalidOperationException("Only published announcements can be archived.");
        Status = AnnouncementStatus.ARCHIVED;
    }
}
