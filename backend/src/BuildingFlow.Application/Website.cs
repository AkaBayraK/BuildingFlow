using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record WebsiteCreateRequest(Guid RequestId, string? SiteKey, string? DisplayName);
public sealed record WebsiteUpdateRequest(string? DisplayName, string? Tagline, string? ContactEmail, string? ContactPhone, bool IsDemo, long Version);
public sealed record WebsiteThemeRequest(string? Template, string? PrimaryColor, string? AccentColor, long Version, Guid? LogoMediaId=null, Guid? HeroMediaId=null);
public sealed record WebsitePageRequest(string? Kind, string? Slug, string? Title, string? MenuLabel, bool ShowInMenu, int SortOrder, string? SeoTitle, string? SeoDescription, long Version = 0);
public sealed record WebsiteSectionRequest(string? Type, JsonElement Data, bool IsVisible, int SortOrder, long Version = 0);
public sealed record WebsiteOrderItem(Guid Id, int SortOrder, long Version);
public sealed record WebsiteOrderRequest(IReadOnlyList<WebsiteOrderItem>? Items);
public sealed record WebsitePublishRequest(Guid RequestId, long ExpectedSiteVersion, string? Note);
public sealed record WebsiteStatusRequest(string? Status, long Version);
public sealed record WebsiteVersionRequest(long Version);

public sealed record WebsiteSectionResponse(Guid Id, Guid PageId, string Type, JsonElement Data, bool IsVisible, int SortOrder, long Version);
public sealed record WebsitePageResponse(Guid Id, string Kind, string Slug, string Title, string? MenuLabel, bool ShowInMenu, int SortOrder, string? SeoTitle, string? SeoDescription, bool Required, long Version, IReadOnlyList<WebsiteSectionResponse> Sections);
public sealed record WebsiteThemeResponse(string Template, string PrimaryColor, string AccentColor, Guid? LogoMediaId=null, Guid? HeroMediaId=null);
public sealed record WebsitePublicationSummary(Guid Id, int Number, DateTimeOffset PublishedAt, string? Note, bool IsActive, Guid? SourcePublicationId, string SnapshotSha256, string PublishedBy);
public sealed record WebsiteResponse(Guid Id, Guid PropertyId, string SiteKey, string DisplayName, string? Tagline, string? ContactEmail, string? ContactPhone, string Status, bool IsDemo, WebsiteThemeResponse Theme, long Version, IReadOnlyList<WebsitePageResponse> Pages, WebsitePublicationSummary? ActivePublication, bool Replayed = false);

// Public snapshot contract: an explicit allow-list. Internal ids, versions, audit fields and property data never appear here.
public sealed record PublicWebsiteSection(string Type, JsonElement Data);
public sealed record PublicWebsitePage(string Kind, string Slug, string Title, string? MenuLabel, bool ShowInMenu, int SortOrder, string? SeoTitle, string? SeoDescription, IReadOnlyList<PublicWebsiteSection> Sections);
public sealed record PublicWebsiteTheme(string Template, string PrimaryColor, string AccentColor, Guid? LogoMediaId=null, Guid? HeroMediaId=null);
public sealed record PublicWebsite(string SiteKey, string DisplayName, string? Tagline, string? ContactEmail, string? ContactPhone, bool IsDemo, PublicWebsiteTheme Theme, IReadOnlyList<PublicWebsitePage> Pages, IReadOnlyList<PublicWebsiteMedia>? Media=null);
public sealed record PublicWebsiteDocument(int Number, DateTimeOffset PublishedAt, string Sha256, string Json);

public interface IWebsiteStore
{
    Task<WebsiteResponse?> GetAsync(Guid property, AccessScope access, CancellationToken token);
    Task<WebsiteResponse?> CreateAsync(Guid property, Guid requestId, string siteKey, string displayName, AccessScope access, CancellationToken token);
    Task<WebsiteResponse?> UpdateAsync(Guid property, WebsiteUpdateRequest request, AccessScope access, CancellationToken token);
    Task<WebsiteResponse?> ThemeAsync(Guid property, WebsiteThemeRequest request, AccessScope access, CancellationToken token);
    Task<WebsitePageResponse?> SavePageAsync(Guid property, Guid? page, WebsitePageRequest request, WebsitePageKind kind, AccessScope access, CancellationToken token);
    Task<bool> DeletePageAsync(Guid property, Guid page, long version, AccessScope access, CancellationToken token);
    Task<WebsiteResponse?> OrderPagesAsync(Guid property, IReadOnlyList<WebsiteOrderItem> items, AccessScope access, CancellationToken token);
    Task<WebsiteSectionResponse?> SaveSectionAsync(Guid property, Guid page, Guid? section, WebsiteSectionType type, string data, WebsiteSectionRequest request, AccessScope access, CancellationToken token);
    Task<bool> DeleteSectionAsync(Guid property, Guid page, Guid section, long version, AccessScope access, CancellationToken token);
    Task<WebsitePageResponse?> OrderSectionsAsync(Guid property, Guid page, IReadOnlyList<WebsiteOrderItem> items, AccessScope access, CancellationToken token);
    Task<PublicWebsite?> PreviewAsync(Guid property, AccessScope access, CancellationToken token);
    Task<WebsitePublicationSummary?> PublishAsync(Guid property, WebsitePublishRequest request, AccessScope access, CancellationToken token);
    Task<WebsitePublicationSummary?> RestoreAsync(Guid property, Guid publication, WebsitePublishRequest request, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<WebsitePublicationSummary>?> PublicationsAsync(Guid property, AccessScope access, CancellationToken token);
    Task<WebsiteResponse?> StatusAsync(Guid property, WebsiteStatus status, long version, AccessScope access, CancellationToken token);
}
public interface IPublicWebsiteStore
{
    Task<PublicWebsiteDocument?> SiteAsync(string siteKey, CancellationToken token);
}

public sealed class WebsiteService(IWebsiteStore store, ICurrentUserService user)
{
    private static readonly string[] Editors = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER"];
    private static readonly string[] Publishers = ["SUPER_ADMIN", "SITE_ADMIN"];
    private AccessScope Access(string[] roles)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !roles.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    private static void Invalid(string field, ErrorDefinition definition) => throw new RequestValidationException(new() { [field] = [definition.Create()] });
    private static void Version(long version) { if (version < 1) Invalid("version", ErrorCatalog.VersionRequired); }
    private static T Guard<T>(string field, ErrorDefinition definition, Func<T> action)
    { try { return action(); } catch (ArgumentException) { Invalid(field, definition); return default!; } }
    private static TEnum Parse<TEnum>(string? value, string field, ErrorDefinition definition) where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, out var parsed) || !Enum.IsDefined(parsed) || value != parsed.ToString()) Invalid(field, definition);
        return parsed;
    }

    public Task<WebsiteResponse?> Get(Guid property, CancellationToken token) => store.GetAsync(property, Access(Editors), token);
    public Task<WebsiteResponse?> Create(Guid property, WebsiteCreateRequest request, CancellationToken token)
    {
        var access = Access(Publishers);
        if (request.RequestId == Guid.Empty) Invalid("requestId", ErrorCatalog.DuesErrorRequestId);
        var key = Guard("siteKey", ErrorCatalog.WebsiteSiteKeyInvalid, () => WebsiteSite.NormalizeKey(request.SiteKey ?? ""));
        Guard("displayName", ErrorCatalog.WebsiteDetailsInvalid, () => new WebsiteSite(Guid.NewGuid(), request.RequestId, key, request.DisplayName ?? ""));
        return store.CreateAsync(property, request.RequestId, key, request.DisplayName!.Trim(), access, token);
    }
    public Task<WebsiteResponse?> Update(Guid property, WebsiteUpdateRequest request, CancellationToken token)
    {
        var access = Access(Editors); Version(request.Version);
        Guard("displayName", ErrorCatalog.WebsiteDetailsInvalid, () => { new WebsiteSite(Guid.NewGuid(), Guid.NewGuid(), "probe", "probe").UpdateDetails(request.DisplayName ?? "", request.Tagline, request.ContactEmail, request.ContactPhone, request.IsDemo); return 0; });
        return store.UpdateAsync(property, request, access, token);
    }
    public Task<WebsiteResponse?> Theme(Guid property, WebsiteThemeRequest request, CancellationToken token)
    {
        var access = Access(Editors); Version(request.Version);
        Guard("theme", ErrorCatalog.WebsiteThemeInvalid, () => { new WebsiteSite(Guid.NewGuid(), Guid.NewGuid(), "probe", "probe").UpdateTheme(request.Template ?? "", request.PrimaryColor ?? "", request.AccentColor ?? ""); return 0; });
        return store.ThemeAsync(property, request, access, token);
    }
    public Task<WebsitePageResponse?> SavePage(Guid property, Guid? page, WebsitePageRequest request, CancellationToken token)
    {
        var access = Access(Editors); if (page.HasValue) Version(request.Version);
        var kind = Parse<WebsitePageKind>(request.Kind, "kind", ErrorCatalog.WebsitePageInvalid);
        Guard("page", ErrorCatalog.WebsitePageInvalid, () => { var probe = new WebsitePage(Guid.NewGuid(), Guid.NewGuid(), kind, request.Slug, request.Title ?? "", request.MenuLabel, request.ShowInMenu, request.SortOrder); probe.Update(request.Slug, request.Title ?? "", request.MenuLabel, request.ShowInMenu, request.SortOrder, request.SeoTitle, request.SeoDescription); return 0; });
        return store.SavePageAsync(property, page, request, kind, access, token);
    }
    public Task<bool> DeletePage(Guid property, Guid page, long version, CancellationToken token)
    { var access = Access(Editors); Version(version); return store.DeletePageAsync(property, page, version, access, token); }
    public Task<WebsiteResponse?> OrderPages(Guid property, WebsiteOrderRequest request, CancellationToken token)
    { var access = Access(Editors); return store.OrderPagesAsync(property, Order(request), access, token); }
    public Task<WebsiteSectionResponse?> SaveSection(Guid property, Guid page, Guid? section, WebsiteSectionRequest request, CancellationToken token)
    {
        var access = Access(Editors); if (section.HasValue) Version(request.Version);
        var type = Parse<WebsiteSectionType>(request.Type, "type", ErrorCatalog.WebsiteSectionInvalid);
        if (request.SortOrder is < 0 or > 1000) Invalid("sortOrder", ErrorCatalog.WebsiteSectionInvalid);
        var data = WebsiteContent.Validate(type, request.Data);
        return store.SaveSectionAsync(property, page, section, type, data, request, access, token);
    }
    public Task<bool> DeleteSection(Guid property, Guid page, Guid section, long version, CancellationToken token)
    { var access = Access(Editors); Version(version); return store.DeleteSectionAsync(property, page, section, version, access, token); }
    public Task<WebsitePageResponse?> OrderSections(Guid property, Guid page, WebsiteOrderRequest request, CancellationToken token)
    { var access = Access(Editors); return store.OrderSectionsAsync(property, page, Order(request), access, token); }
    public Task<PublicWebsite?> Preview(Guid property, CancellationToken token) => store.PreviewAsync(property, Access(Editors), token);
    public Task<IReadOnlyList<WebsitePublicationSummary>?> Publications(Guid property, CancellationToken token) => store.PublicationsAsync(property, Access(Publishers), token);
    public Task<WebsitePublicationSummary?> Publish(Guid property, WebsitePublishRequest request, CancellationToken token)
    { var access = Access(Publishers); Publication(request); return store.PublishAsync(property, request, access, token); }
    public Task<WebsitePublicationSummary?> Restore(Guid property, Guid publication, WebsitePublishRequest request, CancellationToken token)
    { var access = Access(Publishers); Publication(request); return store.RestoreAsync(property, publication, request, access, token); }
    public Task<WebsiteResponse?> Status(Guid property, WebsiteStatusRequest request, CancellationToken token)
    {
        var access = Access(["SUPER_ADMIN"]); Version(request.Version);
        var status = Parse<WebsiteStatus>(request.Status, "status", ErrorCatalog.WebsiteState);
        if (status == WebsiteStatus.DRAFT) Invalid("status", ErrorCatalog.WebsiteState);
        return store.StatusAsync(property, status, request.Version, access, token);
    }
    private static void Publication(WebsitePublishRequest request)
    {
        if (request.RequestId == Guid.Empty) Invalid("requestId", ErrorCatalog.DuesErrorRequestId);
        Version(request.ExpectedSiteVersion);
        Guard("note", ErrorCatalog.WebsiteDetailsInvalid, () => WebsiteSite.Optional(request.Note, 200, "note"));
    }
    private static IReadOnlyList<WebsiteOrderItem> Order(WebsiteOrderRequest request)
    {
        var items = request.Items ?? [];
        if (items.Count is < 1 or > 100 || items.Select(x => x.Id).Distinct().Count() != items.Count || items.Any(x => x.Id == Guid.Empty || x.Version < 1 || x.SortOrder is < 0 or > 1000)) Invalid("items", ErrorCatalog.WebsitePageInvalid);
        return items;
    }

    // Starter content for a new site: clearly marked demo text, no real prices, units or residents.
    public static IReadOnlyList<(WebsitePageKind Kind, string Slug, string Title, string? Menu, int Order, (WebsiteSectionType Type, object Data)[] Sections)> StarterPages(string name) =>
    [
        (WebsitePageKind.HOME, "", name, "Ana Sayfa", 0, [
            (WebsiteSectionType.HERO, new { eyebrow = "Modern · Güvenli · Huzurlu yaşam", title = name, subtitle = "Şehrin içinde, doğayla iç içe ayrıcalıklı bir yaşam.", body = "Bu metin demo içeriktir; yönetim panelinden değiştirin.", primaryCta = new { label = "Rezidansı Keşfet", href = "#ozellikler" } }),
            (WebsiteSectionType.FEATURES, new { items = new object[] { new { icon = "BUILDING", title = "Modern Daireler", text = "Konforlu yaşam alanları" }, new { icon = "USERS", title = "Sosyal Alanlar", text = "Spor, yüzme havuzu, ortak alanlar" }, new { icon = "SHIELD", title = "7/24 Güvenlik", text = "Kamera ve güvenlik sistemi" }, new { icon = "MANAGEMENT", title = "Profesyonel Yönetim", text = "Şeffaf ve çözüm odaklı hizmet" }, new { icon = "LEAF", title = "Yeşil Alanlar", text = "Doğayla iç içe yaşam" }, new { icon = "PIN", title = "Merkezi Konum", text = "Ulaşım ve yaşam kolaylığı" } } }),
            (WebsiteSectionType.LISTINGS, new { title = "Satılık / Kiralık Daireler", subtitle = "Güncel satılık ve kiralık daire ilanları", limit = 6 }),
            (WebsiteSectionType.NOTICES, new { title = "Duyurular", limit = 4 }),
            (WebsiteSectionType.AMENITIES, new { title = "Sosyal Alanlar", items = new object[] { new { title = "Yüzme Havuzu" }, new { title = "Spor Salonu" }, new { title = "Çocuk Oyun Alanı" }, new { title = "Ortak Salon" } } }),
            (WebsiteSectionType.STATS, new { items = new object[] { new { icon = "BUILDING", value = "—", label = "Blok" }, new { icon = "HOME", value = "—", label = "Daire" }, new { icon = "LEAF", value = "—", label = "Yeşil Alan" } } }),
            (WebsiteSectionType.LOGIN_CARD, new { title = "Yönetim Paneli", subtitle = name + " Yönetim Sistemi" }),
        ]),
        (WebsitePageKind.CONTACT, "iletisim", "İletişim", "İletişim", 60, [
            (WebsiteSectionType.CONTACT, new { title = "İletişim", body = "Yönetim ofisi iletişim bilgilerini buraya yazın." }),
        ]),
        (WebsitePageKind.PRIVACY, "kvkk-aydinlatma-metni", "KVKK Aydınlatma Metni", null, 90, [
            (WebsiteSectionType.RICH_TEXT, new { title = "Kişisel Verilerin Korunması Hakkında Aydınlatma Metni", body = WebsiteContent.Placeholder + " Veri sorumlusunun unvanı, adresi ve başvuru kanalı yazılmalı; metin hukukçu tarafından kontrol edilmelidir. Bu sitede yalnızca iletişim formu ile gönderdiğiniz bilgiler işlenir." }),
        ]),
        (WebsitePageKind.COOKIES, "cerez-politikasi", "Çerez Politikası", null, 91, [
            (WebsiteSectionType.RICH_TEXT, new { title = "Çerez Politikası", body = "Bu site yalnızca çalışması için zorunlu teknik çerezleri kullanır. Analitik, reklam veya üçüncü taraf izleme çerezi kullanılmaz." }),
        ]),
    ];

    public static PublicWebsite Snapshot(WebsiteSite site, IEnumerable<WebsitePage> pages, IEnumerable<WebsiteSection> sections)
    {
        var bySection = sections.Where(s => s.IsVisible && s.IsEnabled && !s.IsDeleted).GroupBy(s => s.PageId).ToDictionary(g => g.Key, g => g.OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt).ToArray());
        return new(site.SiteKey, site.DisplayName, site.Tagline, site.ContactEmail, site.ContactPhone, site.IsDemo, new(site.Template, site.PrimaryColor, site.AccentColor, site.LogoMediaId, site.HeroMediaId),
            pages.Where(p => p.IsEnabled && !p.IsDeleted).OrderBy(p => p.SortOrder).ThenBy(p => p.Slug).Select(p => new PublicWebsitePage(p.Kind.ToString(), p.Slug, p.Title, p.MenuLabel, p.ShowInMenu, p.SortOrder, p.SeoTitle, p.SeoDescription,
                (bySection.GetValueOrDefault(p.Id) ?? []).Select(s => new PublicWebsiteSection(s.Type.ToString(), JsonDocument.Parse(s.Data).RootElement.Clone())).ToArray())).ToArray());
    }
    // Turkish letters stay readable (and within size limits); markup characters are rejected by validation anyway.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
    public static (string Json, string Sha256) Serialize(PublicWebsite site)
    {
        var json = JsonSerializer.Serialize(site, Json);
        return (json, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant());
    }
    // Publishing needs the root page and filled-in legal pages; the starter privacy text carries a placeholder on purpose.
    public static bool ReadyToPublish(PublicWebsite site) =>
        site.Pages.Any(p => p.Kind == nameof(WebsitePageKind.HOME)) &&
        new[] { nameof(WebsitePageKind.PRIVACY), nameof(WebsitePageKind.COOKIES) }.All(kind => site.Pages.Any(p => p.Kind == kind && p.Sections.Count > 0)) &&
        site.Pages.All(p => p.Sections.All(s => !s.Data.GetRawText().Contains(WebsiteContent.Placeholder, StringComparison.Ordinal)));
}

// Closed per-type schemas: unknown fields, HTML and unsafe links are rejected before anything is stored.
public static class WebsiteContent
{
    public const string Placeholder = "[DOLDURUN]";
    private abstract record Field(bool Required);
    private sealed record Text(int Max, bool Required = false, bool Multiline = false) : Field(Required);
    private sealed record Link(bool Required = false) : Field(Required);
    private sealed record Number(int Min, int Max, bool Required = false) : Field(Required);
    private sealed record Choice(string[] Values, bool Required = false) : Field(Required);
    private sealed record Shape(Dictionary<string, Field> Fields, bool Required = false) : Field(Required);
    private sealed record List(Shape Item, int Max, bool Required = false) : Field(Required);
    private static readonly string[] Icons = ["BUILDING", "HOME", "USERS", "SHIELD", "MANAGEMENT", "LEAF", "PIN", "POOL", "GYM", "PARK", "CAR", "STAR"];
    private static Shape Cta() => new(new() { ["label"] = new Text(40, true), ["href"] = new Link(true) });
    private static readonly Dictionary<WebsiteSectionType, Shape> Schemas = new()
    {
        [WebsiteSectionType.HERO] = new(new() { ["eyebrow"] = new Text(80), ["title"] = new Text(80, true), ["highlight"] = new Text(80), ["subtitle"] = new Text(200), ["body"] = new Text(600, Multiline: true), ["primaryCta"] = Cta(), ["secondaryCta"] = Cta() }),
        [WebsiteSectionType.FEATURES] = new(new() { ["title"] = new Text(80), ["items"] = new List(new(new() { ["icon"] = new Choice(Icons), ["title"] = new Text(60, true), ["text"] = new Text(120) }), 8, true) }),
        [WebsiteSectionType.LISTINGS] = new(new() { ["title"] = new Text(80, true), ["subtitle"] = new Text(200), ["limit"] = new Number(1, 12) }),
        [WebsiteSectionType.NOTICES] = new(new() { ["title"] = new Text(80, true), ["limit"] = new Number(1, 10) }),
        [WebsiteSectionType.AMENITIES] = new(new() { ["title"] = new Text(80, true), ["items"] = new List(new(new() { ["title"] = new Text(60, true), ["text"] = new Text(200), ["mediaId"] = new Text(36) }), 12, true) }),
        [WebsiteSectionType.GALLERY] = new(new() { ["title"] = new Text(80, true), ["items"] = new List(new(new() { ["caption"] = new Text(80), ["mediaId"] = new Text(36) }), 24) }),
        [WebsiteSectionType.STATS] = new(new() { ["items"] = new List(new(new() { ["icon"] = new Choice(Icons), ["value"] = new Text(20, true), ["label"] = new Text(40, true) }), 6, true) }),
        [WebsiteSectionType.CONTACT] = new(new() { ["title"] = new Text(80, true), ["body"] = new Text(600, Multiline: true), ["address"] = new Text(300, Multiline: true), ["phone"] = new Text(30), ["email"] = new Text(120), ["mapUrl"] = new Link() }),
        [WebsiteSectionType.LOGIN_CARD] = new(new() { ["title"] = new Text(60, true), ["subtitle"] = new Text(120) }),
        [WebsiteSectionType.RICH_TEXT] = new(new() { ["title"] = new Text(120), ["body"] = new Text(8000, true, true) }),
        [WebsiteSectionType.VIDEO_LINK] = new(new() { ["title"] = new Text(80, true), ["url"] = new Link(true) }),
    };

    public static string Validate(WebsiteSectionType type, JsonElement data)
    {
        JsonNode? node;
        try { node = data.ValueKind == JsonValueKind.Object ? JsonNode.Parse(data.GetRawText()) : null; } catch (JsonException) { node = null; }
        if (node is not JsonObject root || !Check(Schemas[type], root, 0)) Fail();
        var json = node!.ToJsonString(WebsiteService.Json);
        if (json.Length > WebsiteSection.MaxDataLength) Fail();
        return json;
    }
    private static void Fail() => throw new RequestValidationException(new() { ["data"] = [ErrorCatalog.WebsiteSectionInvalid.Create()] });
    private static bool Check(Shape shape, JsonObject value, int depth)
    {
        if (depth > 3 || value.Any(p => !shape.Fields.ContainsKey(p.Key))) return false;
        foreach (var (name, field) in shape.Fields)
        {
            var present = value.TryGetPropertyValue(name, out var item) && item is not null;
            if (!present) { if (field.Required) return false; continue; }
            if(name=="mediaId" && (item is not JsonValue media || !media.TryGetValue<string>(out var mediaId) || !Guid.TryParseExact(mediaId,"D",out var parsedMedia) || parsedMedia==Guid.Empty))return false;
            var ok = field switch
            {
                Text t => item is JsonValue v && v.TryGetValue<string>(out var s) && TextOk(s, t),
                Link l => item is JsonValue v && v.TryGetValue<string>(out var s) && LinkOk(s),
                Number n => item is JsonValue v && v.TryGetValue<int>(out var i) && i >= n.Min && i <= n.Max,
                Choice c => item is JsonValue v && v.TryGetValue<string>(out var s) && c.Values.Contains(s),
                Shape sh => item is JsonObject o && Check(sh, o, depth + 1),
                List list => item is JsonArray a && a.Count <= list.Max && (!list.Required || a.Count > 0) && a.All(x => x is JsonObject o && Check(list.Item, o, depth + 1)),
                _ => false
            };
            if (!ok) return false;
        }
        return true;
    }
    private static bool TextOk(string value, Text field) =>
        value.Length <= field.Max && (!field.Required || value.Trim().Length > 0) && !value.Contains('<') && !value.Contains('>') &&
        value.All(c => !char.IsControl(c) || (field.Multiline && c == '\n'));
    // Only https links or same-site paths/anchors; protocol-relative and script URLs never pass.
    public static bool LinkOk(string value)
    {
        if (value.Length is < 1 or > 300 || value.Any(char.IsControl) || value.Contains(' ') || value.Contains('<') || value.Contains('>') || value.Contains('"')) return false;
        if (value.StartsWith('#')) return value.Length > 1;
        if (value.StartsWith('/')) return !value.StartsWith("//") && !value.Contains('\\');
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && uri.UserInfo.Length == 0 && !string.IsNullOrEmpty(uri.Host);
    }
}
