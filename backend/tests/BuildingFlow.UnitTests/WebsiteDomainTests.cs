using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
// İŞ21 WEBSITE FAZ 02: site key, theme contrast, page rules, closed section schemas and the public snapshot allow-list.
public sealed class WebsiteDomainTests
{
    private static JsonElement J(object value) => JsonSerializer.SerializeToElement(value);

    [Theory][InlineData("altunbayrak")][InlineData("ALTUNBAYRAK-rezidans")][InlineData("a1b")][InlineData("abc-1-def")]
    public void ValidSiteKeysAreNormalised(string key) => Assert.Equal(key.ToLowerInvariant(), WebsiteSite.NormalizeKey(key));

    [Theory][InlineData("ab")][InlineData("-abc")][InlineData("abc-")][InlineData("a--b")][InlineData("api")][InlineData("www")][InlineData("rezidans_1")][InlineData("rezidans.com")][InlineData("çamlık")][InlineData("../x")]
    public void InvalidOrReservedSiteKeysAreRejected(string key) => Assert.Throws<ArgumentException>(() => WebsiteSite.NormalizeKey(key));

    [Fact] public void NewSiteStartsAsDemoDraftWithReadableDefaultTheme()
    {
        var site = new WebsiteSite(Guid.NewGuid(), Guid.NewGuid(), "Altunbayrak", " Altunbayrak Rezidans ");
        Assert.Equal(WebsiteStatus.DRAFT, site.Status); Assert.True(site.IsDemo); Assert.Equal("Altunbayrak Rezidans", site.DisplayName);
        site.UpdateTheme("PREMIUM_RESIDENCE", site.PrimaryColor, site.AccentColor);
        Assert.Throws<ArgumentException>(() => new WebsiteSite(Guid.Empty, Guid.NewGuid(), "abc", "x"));
        Assert.Throws<ArgumentException>(() => new WebsiteSite(Guid.NewGuid(), Guid.Empty, "abc", "x"));
    }

    [Theory][InlineData("#FFFF00", "#1E88E5")][InlineData("#0B3A6E", "#FFFFFF")][InlineData("0B3A6E", "#1E88E5")][InlineData("#0B3A6", "#1E88E5")][InlineData("#0B3A6E", "red")]
    public void ThemeRejectsUnreadableOrMalformedColours(string primary, string accent)
        => Assert.Throws<ArgumentException>(() => new WebsiteSite(Guid.NewGuid(), Guid.NewGuid(), "abc", "Site").UpdateTheme("PREMIUM_RESIDENCE", primary, accent));

    [Fact] public void ThemeAcceptsLowercaseHexAndOnlyKnownTemplate()
    {
        var site = new WebsiteSite(Guid.NewGuid(), Guid.NewGuid(), "abc", "Site");
        site.UpdateTheme("PREMIUM_RESIDENCE", "#123456", "#1565c0"); Assert.Equal("#1565C0", site.AccentColor);
        Assert.Throws<ArgumentException>(() => site.UpdateTheme("OTHER", "#123456", "#1565C0"));
    }

    [Fact] public void DetailsArePlainTextAndContactsAreChecked()
    {
        var site = new WebsiteSite(Guid.NewGuid(), Guid.NewGuid(), "abc", "Site");
        site.UpdateDetails("Site", "  ", "yonetim@example.com", "+90 (212) 555-00-00", false);
        Assert.Null(site.Tagline); Assert.False(site.IsDemo);
        Assert.Throws<ArgumentException>(() => site.UpdateDetails("<b>Site</b>", null, null, null, true));
        Assert.Throws<ArgumentException>(() => site.UpdateDetails("Site", null, "not-an-email", null, true));
        Assert.Throws<ArgumentException>(() => site.UpdateDetails("Site", null, null, "call me", true));
        Assert.Throws<ArgumentException>(() => site.UpdateDetails("Site\u0007", null, null, null, true));
    }

    [Fact] public void SiteStatusTransitionsAreExplicit()
    {
        var site = new WebsiteSite(Guid.NewGuid(), Guid.NewGuid(), "abc", "Site");
        Assert.Throws<InvalidOperationException>(site.Suspend); Assert.Throws<InvalidOperationException>(site.Resume);
        site.MarkPublished(); site.Suspend(); Assert.Throws<InvalidOperationException>(site.MarkPublished); site.Resume(); Assert.Equal(WebsiteStatus.PUBLISHED, site.Status);
    }

    [Fact] public void HomeHasNoSlugAndOtherPagesNeedOne()
    {
        var home = new WebsitePage(Guid.NewGuid(), Guid.NewGuid(), WebsitePageKind.HOME, null, "Ana Sayfa", "Ana Sayfa", true, 0);
        Assert.Equal("", home.Slug); Assert.True(home.IsRequired);
        Assert.Throws<ArgumentException>(() => new WebsitePage(Guid.NewGuid(), Guid.NewGuid(), WebsitePageKind.HOME, "home", "x", null, false, 0));
        Assert.Throws<ArgumentException>(() => new WebsitePage(Guid.NewGuid(), Guid.NewGuid(), WebsitePageKind.CUSTOM, null, "x", null, false, 0));
        foreach (var slug in new[] { "Bad Slug", "-x", "x-", "a/b", "ğ", new string('a', 61) })
            Assert.Throws<ArgumentException>(() => new WebsitePage(Guid.NewGuid(), Guid.NewGuid(), WebsitePageKind.CUSTOM, slug, "x", null, false, 0));
        var page = new WebsitePage(Guid.NewGuid(), Guid.NewGuid(), WebsitePageKind.CUSTOM, "Sosyal-Alanlar", "Sosyal", null, true, 10);
        Assert.Equal("sosyal-alanlar", page.Slug); Assert.False(page.IsRequired);
        Assert.Throws<ArgumentException>(() => page.Update("x", "x", null, true, 1001, null, null));
        Assert.Throws<ArgumentException>(() => page.Update("x", "x", null, true, 0, new string('a', 71), null));
    }

    [Fact] public void EveryStarterSectionPassesItsOwnSchemaAndContainsNoPersonalData()
    {
        foreach (var (_, _, _, _, _, sections) in WebsiteService.StarterPages("Altunbayrak Rezidans"))
            foreach (var (type, data) in sections) Assert.False(string.IsNullOrEmpty(WebsiteContent.Validate(type, J(data))));
    }

    public static TheoryData<WebsiteSectionType, object> InvalidSections => new()
    {
        { WebsiteSectionType.HERO, new { title = "x", script = "alert(1)" } },
        { WebsiteSectionType.HERO, new { subtitle = "no title" } },
        { WebsiteSectionType.HERO, new { title = "<img src=x onerror=alert(1)>" } },
        { WebsiteSectionType.HERO, new { title = "x", primaryCta = new { label = "Git", href = "javascript:alert(1)" } } },
        { WebsiteSectionType.HERO, new { title = "x", primaryCta = new { label = "Git", href = "//evil.example" } } },
        { WebsiteSectionType.HERO, new { title = "x", primaryCta = new { label = "Git", href = "http://plain.example" } } },
        { WebsiteSectionType.HERO, new { title = "line\nbreak" } },
        { WebsiteSectionType.FEATURES, new { items = Array.Empty<object>() } },
        { WebsiteSectionType.FEATURES, new { items = new[] { new { icon = "SKULL", title = "x" } } } },
        { WebsiteSectionType.FEATURES, new { items = Enumerable.Range(0, 9).Select(i => new { title = "x" + i }).ToArray() } },
        { WebsiteSectionType.LISTINGS, new { title = "x", limit = 99 } },
        { WebsiteSectionType.LISTINGS, new { title = "x", limit = "6" } },
        { WebsiteSectionType.STATS, new { items = new[] { new { value = 300, label = "Sakin" } } } },
        { WebsiteSectionType.RICH_TEXT, new { body = new string('a', 8001) } },
        { WebsiteSectionType.VIDEO_LINK, new { title = "Film", url = "https://user:pass@example.com/v.mp4" } },
        { WebsiteSectionType.CONTACT, new { title = "x", mapUrl = "data:text/html,hi" } },
    };
    [Theory][MemberData(nameof(InvalidSections))]
    public void SectionSchemasRejectUnknownFieldsHtmlAndUnsafeLinks(WebsiteSectionType type, object data)
        => Assert.Throws<RequestValidationException>(() => WebsiteContent.Validate(type, J(data)));

    [Fact] public void SectionDataMustBeAnObjectWithinTheSizeLimit()
    {
        Assert.Throws<RequestValidationException>(() => WebsiteContent.Validate(WebsiteSectionType.HERO, J(new[] { 1 })));
        Assert.Throws<RequestValidationException>(() => WebsiteContent.Validate(WebsiteSectionType.HERO, J("text")));
        var body = new string('ş', 8000);
        var big = new { items = Enumerable.Range(0, 24).Select(_ => new { caption = new string('ğ', 80) }).ToArray(), title = "x" };
        Assert.False(string.IsNullOrEmpty(WebsiteContent.Validate(WebsiteSectionType.RICH_TEXT, J(new { body }))));
        Assert.False(string.IsNullOrEmpty(WebsiteContent.Validate(WebsiteSectionType.GALLERY, J(big))));
        Assert.Equal("a\nb", JsonDocument.Parse(WebsiteContent.Validate(WebsiteSectionType.RICH_TEXT, J(new { body = "a\nb" }))).RootElement.GetProperty("body").GetString());
    }

    [Theory][InlineData("https://maps.example.com/?q=1", true)][InlineData("/iletisim", true)][InlineData("#ozellikler", true)][InlineData("#", false)]
    [InlineData("//x.example", false)][InlineData("/\\x", false)][InlineData("ftp://x.example", false)][InlineData("https://", false)][InlineData("https://a.example/ b", false)][InlineData("JAVASCRIPT:alert(1)", false)]
    public void LinksAreHttpsOrSameSite(string link, bool ok) => Assert.Equal(ok, WebsiteContent.LinkOk(link));

    [Fact] public void SnapshotIsAnAllowListWithoutIdsVersionsOrHiddenContent()
    {
        var property = Guid.NewGuid(); var site = new WebsiteSite(property, Guid.NewGuid(), "abc", "Site");
        var home = new WebsitePage(property, site.Id, WebsitePageKind.HOME, null, "Ana", "Ana", true, 0);
        var hidden = new WebsiteSection(property, home.Id, WebsiteSectionType.RICH_TEXT, """{"body":"HIDDEN"}""", false, 0);
        var shown = new WebsiteSection(property, home.Id, WebsiteSectionType.RICH_TEXT, """{"body":"SHOWN"}""", true, 10);
        var snapshot = WebsiteService.Snapshot(site, [home], [hidden, shown]);
        var (json, sha) = WebsiteService.Serialize(snapshot);
        Assert.Contains("SHOWN", json); Assert.DoesNotContain("HIDDEN", json);
        foreach (var id in new[] { property, site.Id, home.Id, shown.Id }) Assert.DoesNotContain(id.ToString(), json, StringComparison.OrdinalIgnoreCase);
        foreach (var field in new[] { "\"id\"", "propertyId", "version", "createdBy", "modifiedBy", "requestId" }) Assert.DoesNotContain(field, json, StringComparison.OrdinalIgnoreCase);
        Assert.Matches("^[0-9a-f]{64}$", sha); Assert.Equal(sha, WebsiteService.Serialize(snapshot).Sha256);
    }

    [Fact] public void PublishingNeedsHomeAndFilledLegalPages()
    {
        var property = Guid.NewGuid(); var site = new WebsiteSite(property, Guid.NewGuid(), "abc", "Site");
        var pages = new List<WebsitePage>(); var sections = new List<WebsiteSection>();
        foreach (var (kind, slug, title, menu, order, rows) in WebsiteService.StarterPages("Site"))
        {
            var page = new WebsitePage(property, site.Id, kind, slug, title, menu, menu is not null, order); pages.Add(page);
            foreach (var (type, data) in rows) sections.Add(new WebsiteSection(property, page.Id, type, WebsiteContent.Validate(type, J(data)), true, 0));
        }
        Assert.False(WebsiteService.ReadyToPublish(WebsiteService.Snapshot(site, pages, sections)));
        var privacy = pages.Single(p => p.Kind == WebsitePageKind.PRIVACY); var text = sections.Single(s => s.PageId == privacy.Id);
        text.Update(WebsiteContent.Validate(WebsiteSectionType.RICH_TEXT, J(new { body = "Veri sorumlusu: Örnek Yönetim" })), true, 0);
        Assert.True(WebsiteService.ReadyToPublish(WebsiteService.Snapshot(site, pages, sections)));
        text.Update(text.Data, false, 0);
        Assert.False(WebsiteService.ReadyToPublish(WebsiteService.Snapshot(site, pages, sections)));
    }

    [Fact] public void PublicationRequiresCompleteImmutableData()
    {
        var at = DateTimeOffset.UtcNow; var sha = new string('a', 64);
        Assert.Throws<ArgumentException>(() => new WebsitePublication(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 1, "{}", sha, at, null, null));
        Assert.Throws<ArgumentException>(() => new WebsitePublication(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, "{}", sha, at, null, null));
        Assert.Throws<ArgumentException>(() => new WebsitePublication(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "{}", "short", at, null, null));
        var p = new WebsitePublication(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "{}", sha, at, "  ilk yayın ", null);
        Assert.True(p.IsActive); Assert.Equal("ilk yayın", p.Note); p.Deactivate(); Assert.False(p.IsActive);
    }
}
