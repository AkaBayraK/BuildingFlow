using System.Text.RegularExpressions;
namespace BuildingFlow.Domain;

public enum WebsiteStatus { DRAFT, PUBLISHED, SUSPENDED }
public enum WebsitePageKind { HOME, ABOUT, UNITS, AMENITIES, NOTICES, GALLERY, CONTACT, PRIVACY, COOKIES, CUSTOM }
public enum WebsiteSectionType { HERO, FEATURES, LISTINGS, NOTICES, AMENITIES, GALLERY, STATS, CONTACT, LOGIN_CARD, RICH_TEXT, VIDEO_LINK }

// One public site per Property; theme values live on the site row because they always change together.
public sealed partial class WebsiteSite : BaseEntity
{
    public const string DefaultTemplate = "PREMIUM_RESIDENCE";
    private static readonly string[] Reserved = ["api", "admin", "www", "login", "static", "assets", "health", "preview", "public", "sitemap", "robots"];
    private WebsiteSite() { }
    public WebsiteSite(Guid property, Guid requestId, string siteKey, string displayName)
    {
        if (property == Guid.Empty || requestId == Guid.Empty) throw new ArgumentException("Property and request required.", nameof(property));
        PropertyId = property; RequestId = requestId; SiteKey = NormalizeKey(siteKey); Status = WebsiteStatus.DRAFT; IsDemo = true;
        Template = DefaultTemplate; PrimaryColor = "#0B3A6E"; AccentColor = "#1E88E5";
        UpdateDetails(displayName, null, null, null, true);
    }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public string SiteKey { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string? Tagline { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public WebsiteStatus Status { get; private set; }
    public bool IsDemo { get; private set; }
    public Guid? LogoMediaId {get;private set;}
    public Guid? HeroMediaId {get;private set;}
    public void SetMedia(Guid? logo,Guid? hero){LogoMediaId=logo;HeroMediaId=hero;}
    public string Template { get; private set; } = null!;
    public string PrimaryColor { get; private set; } = null!;
    public string AccentColor { get; private set; } = null!;

    public static string NormalizeKey(string value)
    {
        value = Required(value, nameof(value), 63).ToLowerInvariant();
        if (!KeyPattern().IsMatch(value) || value.Contains("--") || Reserved.Contains(value)) throw new ArgumentException("Invalid site key.", nameof(value));
        return value;
    }
    public void UpdateDetails(string displayName, string? tagline, string? contactEmail, string? contactPhone, bool isDemo)
    {
        DisplayName = PlainText(Required(displayName, nameof(displayName), 120), nameof(displayName));
        Tagline = Optional(tagline, 200, nameof(tagline));
        ContactEmail = Optional(contactEmail, 120, nameof(contactEmail));
        if (ContactEmail is not null && !EmailPattern().IsMatch(ContactEmail)) throw new ArgumentException("Invalid contact email.", nameof(contactEmail));
        ContactPhone = Optional(contactPhone, 30, nameof(contactPhone));
        if (ContactPhone is not null && !PhonePattern().IsMatch(ContactPhone)) throw new ArgumentException("Invalid contact phone.", nameof(contactPhone));
        IsDemo = isDemo;
    }
    public void UpdateTheme(string template, string primaryColor, string accentColor)
    {
        if (template != DefaultTemplate) throw new ArgumentException("Unknown template.", nameof(template));
        Template = template; PrimaryColor = Color(primaryColor, nameof(primaryColor), 4.5); AccentColor = Color(accentColor, nameof(accentColor), 3);
    }
    public void MarkPublished()
    {
        if (Status == WebsiteStatus.SUSPENDED) throw new InvalidOperationException("Suspended sites cannot be published.");
        Status = WebsiteStatus.PUBLISHED;
    }
    public void Suspend() { if (Status != WebsiteStatus.PUBLISHED) throw new InvalidOperationException("Only published sites can be suspended."); Status = WebsiteStatus.SUSPENDED; }
    public void Resume() { if (Status != WebsiteStatus.SUSPENDED) throw new InvalidOperationException("Only suspended sites can be resumed."); Status = WebsiteStatus.PUBLISHED; }

    // White text must stay readable: 4.5:1 for body text on the primary colour, 3:1 for large text/buttons on the accent (WCAG AA).
    public static string Color(string value, string field, double minimumContrast)
    {
        value = Required(value, field, 7).ToUpperInvariant();
        if (!ColorPattern().IsMatch(value)) throw new ArgumentException("Invalid colour.", field);
        static double Channel(string hex) { var c = Convert.ToInt32(hex, 16) / 255d; return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
        var luminance = 0.2126 * Channel(value[1..3]) + 0.7152 * Channel(value[3..5]) + 0.0722 * Channel(value[5..7]);
        if (1.05 / (luminance + 0.05) < minimumContrast) throw new ArgumentException("Colour contrast is too low.", field);
        return value;
    }
    public static string PlainText(string value, string field)
    {
        if (value.Any(c => char.IsControl(c)) || value.Contains('<') || value.Contains('>')) throw new ArgumentException("Plain text required.", field);
        return value;
    }
    public static string? Optional(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (value.Length > max) throw new ArgumentException($"{field} exceeds {max} characters.", field);
        return PlainText(value, field);
    }
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])$")] private static partial Regex KeyPattern();
    [GeneratedRegex("^#[0-9A-F]{6}$")] private static partial Regex ColorPattern();
    [GeneratedRegex(@"^[^@\s]{1,64}@[^@\s]+\.[^@\s]{2,}$")] private static partial Regex EmailPattern();
    [GeneratedRegex(@"^\+?[0-9 ()-]{7,30}$")] private static partial Regex PhonePattern();
}

public sealed partial class WebsitePage : BaseEntity
{
    private WebsitePage() { }
    public WebsitePage(Guid property, Guid site, WebsitePageKind kind, string? slug, string title, string? menuLabel, bool showInMenu, int sortOrder)
    {
        if (property == Guid.Empty || site == Guid.Empty || !Enum.IsDefined(kind)) throw new ArgumentException("Invalid page scope.");
        PropertyId = property; SiteId = site; Kind = kind;
        Update(slug, title, menuLabel, showInMenu, sortOrder, null, null);
    }
    public Guid PropertyId { get; private set; }
    public Guid SiteId { get; private set; }
    public WebsitePageKind Kind { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? MenuLabel { get; private set; }
    public bool ShowInMenu { get; private set; }
    public int SortOrder { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    public bool IsRequired => Kind is WebsitePageKind.HOME or WebsitePageKind.PRIVACY or WebsitePageKind.COOKIES;

    public void Update(string? slug, string title, string? menuLabel, bool showInMenu, int sortOrder, string? seoTitle, string? seoDescription)
    {
        // HOME is the site root; every other page needs its own path segment.
        slug = string.IsNullOrWhiteSpace(slug) ? "" : slug.Trim().ToLowerInvariant();
        if (Kind == WebsitePageKind.HOME ? slug != "" : !SlugPattern().IsMatch(slug)) throw new ArgumentException("Invalid page slug.", nameof(slug));
        if (sortOrder is < 0 or > 1000) throw new ArgumentException("Invalid sort order.", nameof(sortOrder));
        Slug = slug; Title = WebsiteSite.PlainText(Required(title, nameof(title), 120), nameof(title));
        MenuLabel = WebsiteSite.Optional(menuLabel, 40, nameof(menuLabel)); ShowInMenu = showInMenu; SortOrder = sortOrder;
        SeoTitle = WebsiteSite.Optional(seoTitle, 70, nameof(seoTitle)); SeoDescription = WebsiteSite.Optional(seoDescription, 160, nameof(seoDescription));
    }
    public void Move(int sortOrder) { if (sortOrder is < 0 or > 1000) throw new ArgumentException("Invalid sort order.", nameof(sortOrder)); SortOrder = sortOrder; }
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,58}[a-z0-9])?$")] private static partial Regex SlugPattern();
}

// Section content is a closed, type-specific JSON document validated by the application layer before it reaches here.
public sealed class WebsiteSection : BaseEntity
{
    public const int MaxDataLength = 16384;
    private WebsiteSection() { }
    public WebsiteSection(Guid property, Guid page, WebsiteSectionType type, string data, bool isVisible, int sortOrder)
    {
        if (property == Guid.Empty || page == Guid.Empty || !Enum.IsDefined(type)) throw new ArgumentException("Invalid section scope.");
        PropertyId = property; PageId = page; Type = type; Update(data, isVisible, sortOrder);
    }
    public Guid PropertyId { get; private set; }
    public Guid PageId { get; private set; }
    public WebsiteSectionType Type { get; private set; }
    public string Data { get; private set; } = null!;
    public bool IsVisible { get; private set; }
    public int SortOrder { get; private set; }
    public void Update(string data, bool isVisible, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(data) || data.Length > MaxDataLength) throw new ArgumentException("Invalid section data.", nameof(data));
        if (sortOrder is < 0 or > 1000) throw new ArgumentException("Invalid sort order.", nameof(sortOrder));
        Data = data; IsVisible = isVisible; SortOrder = sortOrder;
    }
    public void Move(int sortOrder) { if (sortOrder is < 0 or > 1000) throw new ArgumentException("Invalid sort order.", nameof(sortOrder)); SortOrder = sortOrder; }
}

// Immutable public snapshot. Only the active flag changes after creation; a rollback creates a new publication.
public sealed class WebsitePublication : BaseEntity
{
    private WebsitePublication() { }
    public WebsitePublication(Guid property, Guid site, Guid requestId, int number, string snapshot, string sha256, DateTimeOffset publishedAt, string? note, Guid? source)
    {
        if (property == Guid.Empty || site == Guid.Empty || requestId == Guid.Empty || number < 1 || publishedAt == default || source == Guid.Empty) throw new ArgumentException("Invalid publication.");
        if (string.IsNullOrWhiteSpace(snapshot) || sha256.Length != 64) throw new ArgumentException("Invalid snapshot.");
        PropertyId = property; SiteId = site; RequestId = requestId; Number = number; Snapshot = snapshot; SnapshotSha256 = sha256; PublishedAt = publishedAt;
        Note = WebsiteSite.Optional(note, 200, nameof(note)); SourcePublicationId = source; IsActive = true;
    }
    public Guid PropertyId { get; private set; }
    public Guid SiteId { get; private set; }
    public Guid RequestId { get; private set; }
    public int Number { get; private set; }
    public string Snapshot { get; private set; } = null!;
    public string SnapshotSha256 { get; private set; } = null!;
    public DateTimeOffset PublishedAt { get; private set; }
    public string? Note { get; private set; }
    public Guid? SourcePublicationId { get; private set; }
    public bool IsActive { get; private set; }
    public void Deactivate() => IsActive = false;
}
