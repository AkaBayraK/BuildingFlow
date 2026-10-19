using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// İŞ21 WEBSITE FAZ 02: management CMS API, publication snapshots and the anonymous public read API.
public sealed class WebsiteApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static string Root(Guid p) => $"/api/v1/properties/{p}/website";
    private static string Key() => "site-" + Guid.NewGuid().ToString("N")[..12];
    private async Task<(Property P, string Actor)> Seed()
    {
        var actor = Guid.NewGuid().ToString(); var p = new Property("Website " + actor, "City", "District", "Address", PropertyType.SITE);
        await factory.WithDb(async db => { db.Add(p); db.Add(new PropertyMembership(p.Id, actor)); await db.SaveChangesAsync(); }); return (p, actor);
    }
    private static async Task<string> Code(HttpResponseMessage r)
    {
        using var body = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); var root = body.RootElement;
        if (root.TryGetProperty("fieldIssues", out var issues)) foreach (var field in issues.EnumerateObject()) return field.Value[0].GetProperty("code").GetString()!;
        return root.GetProperty("code").GetString()!;
    }
    private static async Task<WebsiteResponse> Create(HttpClient c, Guid p, string key, string name = "Altunbayrak Rezidans")
    {
        var r = await c.PostAsJsonAsync(Root(p), new WebsiteCreateRequest(Guid.NewGuid(), key, name)); Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<WebsiteResponse>())!;
    }
    private static async Task<WebsiteResponse> FillPrivacy(HttpClient c, Guid p, WebsiteResponse site)
    {
        var page = site.Pages.Single(x => x.Kind == "PRIVACY"); var s = page.Sections.Single();
        var r = await c.PutAsJsonAsync($"{Root(p)}/pages/{page.Id}/sections/{s.Id}", new WebsiteSectionRequest("RICH_TEXT", JsonSerializer.SerializeToElement(new { title = "KVKK", body = "Veri sorumlusu: Örnek Site Yönetimi, Örnek Mah. 1, İstanbul." }), true, 0, s.Version));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await c.GetFromJsonAsync<WebsiteResponse>(Root(p)))!;
    }
    private static Task<HttpResponseMessage> Publish(HttpClient c, Guid p, long version, Guid? request = null, string? note = null) =>
        c.PostAsJsonAsync($"{Root(p)}/publications", new WebsitePublishRequest(request ?? Guid.NewGuid(), version, note));

    [PostgresFact] public async Task RolesScopeAndCreationReplayAreEnforced()
    {
        var x = await Seed(); var key = Key();
        using var anon = factory.CreateClient(); using var resident = TestToken.Client(factory, x.Actor, "RESIDENT"); using var accountant = TestToken.Client(factory, x.Actor, "ACCOUNTANT");
        using var manager = TestToken.Client(factory, x.Actor, "MANAGER"); using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN"); using var outsider = TestToken.Client(factory, Guid.NewGuid().ToString(), "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(Root(x.P.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await resident.GetAsync(Root(x.P.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync(Root(x.P.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync(Root(x.P.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync(Root(x.P.Id), new WebsiteCreateRequest(Guid.NewGuid(), key, "Site"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync(Root(x.P.Id), new WebsiteCreateRequest(Guid.NewGuid(), key, "Site"))).StatusCode);
        Assert.Equal("BF_WEBSITE_SITE_KEY_INVALID", await Code(await admin.PostAsJsonAsync(Root(x.P.Id), new WebsiteCreateRequest(Guid.NewGuid(), "api", "Site"))));

        var request = new WebsiteCreateRequest(Guid.NewGuid(), key.ToUpperInvariant(), "Altunbayrak Rezidans");
        var created = await admin.PostAsJsonAsync(Root(x.P.Id), request); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var site = (await created.Content.ReadFromJsonAsync<WebsiteResponse>())!;
        Assert.Equal(key, site.SiteKey); Assert.Equal("DRAFT", site.Status); Assert.True(site.IsDemo); Assert.Null(site.ActivePublication);
        Assert.Equal(["HOME", "CONTACT", "PRIVACY", "COOKIES"], site.Pages.Select(p => p.Kind));
        Assert.All(site.Pages.Where(p => p.Kind is "HOME" or "PRIVACY" or "COOKIES"), p => Assert.True(p.Required));
        var replay = await admin.PostAsJsonAsync(Root(x.P.Id), request); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.True((await replay.Content.ReadFromJsonAsync<WebsiteResponse>())!.Replayed);
        Assert.Equal("BF_WEBSITE_REQUEST_CONFLICT", await Code(await admin.PostAsJsonAsync(Root(x.P.Id), request with { DisplayName = "Başka" })));
        Assert.Equal("BF_WEBSITE_SITE_EXISTS", await Code(await admin.PostAsJsonAsync(Root(x.P.Id), new WebsiteCreateRequest(Guid.NewGuid(), Key(), "Site"))));
        var other = await Seed(); using var otherAdmin = TestToken.Client(factory, other.Actor, "SITE_ADMIN");
        Assert.Equal("BF_WEBSITE_SITE_KEY_TAKEN", await Code(await otherAdmin.PostAsJsonAsync(Root(other.P.Id), new WebsiteCreateRequest(Guid.NewGuid(), key, "Site"))));
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(Root(x.P.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await TestToken.Client(factory, other.Actor, "MANAGER").GetAsync(Root(x.P.Id))).StatusCode);
    }

    [PostgresFact] public async Task PagesSectionsAndThemeAreValidatedVersionedAndScoped()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.Actor, "MANAGER"); using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var site = await Create(admin, x.P.Id, Key());
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(Root(x.P.Id), new WebsiteUpdateRequest("Yeni", null, null, null, true, site.Version + 5))).StatusCode);
        Assert.Equal("BF_WEBSITE_DETAILS_INVALID", await Code(await c.PutAsJsonAsync(Root(x.P.Id), new WebsiteUpdateRequest("<b>x</b>", null, null, null, true, site.Version))));
        var updated = (await (await c.PutAsJsonAsync(Root(x.P.Id), new WebsiteUpdateRequest("Altunbayrak", "Sadece bir konut değil", "yonetim@example.com", "+90 212 555 00 00", true, site.Version))).Content.ReadFromJsonAsync<WebsiteResponse>())!;
        Assert.Equal("Sadece bir konut değil", updated.Tagline); Assert.True(updated.Version > site.Version);
        Assert.Equal("BF_WEBSITE_THEME_INVALID", await Code(await c.PutAsJsonAsync($"{Root(x.P.Id)}/theme", new WebsiteThemeRequest("PREMIUM_RESIDENCE", "#FFFF00", "#1E88E5", updated.Version))));
        var themed = (await (await c.PutAsJsonAsync($"{Root(x.P.Id)}/theme", new WebsiteThemeRequest("PREMIUM_RESIDENCE", "#123456", "#1565c0", updated.Version))).Content.ReadFromJsonAsync<WebsiteResponse>())!;
        Assert.Equal("#1565C0", themed.Theme.AccentColor);

        var pageResponse = await c.PostAsJsonAsync($"{Root(x.P.Id)}/pages", new WebsitePageRequest("AMENITIES", "sosyal-alanlar", "Sosyal Alanlar", "Sosyal Alanlar", true, 30, "Sosyal alanlar", null));
        Assert.Equal(HttpStatusCode.Created, pageResponse.StatusCode); var page = (await pageResponse.Content.ReadFromJsonAsync<WebsitePageResponse>())!;
        Assert.Equal("BF_WEBSITE_PAGE_SLUG_TAKEN", await Code(await c.PostAsJsonAsync($"{Root(x.P.Id)}/pages", new WebsitePageRequest("CUSTOM", "sosyal-alanlar", "Kopya", null, false, 31, null, null))));
        Assert.Equal("BF_WEBSITE_PAGE_SLUG_TAKEN", await Code(await c.PostAsJsonAsync($"{Root(x.P.Id)}/pages", new WebsitePageRequest("HOME", null, "İkinci ana sayfa", null, false, 1, null, null))));
        Assert.Equal("BF_WEBSITE_PAGE_INVALID", await Code(await c.PostAsJsonAsync($"{Root(x.P.Id)}/pages", new WebsitePageRequest("BLOG", "blog", "Blog", null, false, 1, null, null))));
        var home = themed.Pages.Single(p => p.Kind == "HOME");
        Assert.Equal("BF_WEBSITE_PAGE_REQUIRED", await Code(await c.DeleteAsync($"{Root(x.P.Id)}/pages/{home.Id}?version={home.Version}")));

        var invalid = await c.PostAsJsonAsync($"{Root(x.P.Id)}/pages/{page.Id}/sections", new WebsiteSectionRequest("HERO", JsonSerializer.SerializeToElement(new { title = "<script>alert(1)</script>" }), true, 0));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode); Assert.Equal("BF_WEBSITE_SECTION_INVALID", await Code(invalid));
        var sectionResponse = await c.PostAsJsonAsync($"{Root(x.P.Id)}/pages/{page.Id}/sections", new WebsiteSectionRequest("AMENITIES", JsonSerializer.SerializeToElement(new { title = "Sosyal Alanlar", items = new[] { new { title = "Yüzme Havuzu", text = "Yaz sezonu açık" } } }), true, 0));
        Assert.Equal(HttpStatusCode.Created, sectionResponse.StatusCode); var section = (await sectionResponse.Content.ReadFromJsonAsync<WebsiteSectionResponse>())!;
        Assert.Equal("BF_WEBSITE_SECTION_INVALID", await Code(await c.PutAsJsonAsync($"{Root(x.P.Id)}/pages/{page.Id}/sections/{section.Id}", new WebsiteSectionRequest("HERO", JsonSerializer.SerializeToElement(new { title = "x" }), true, 0, section.Version))));
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"{Root(x.P.Id)}/pages/{page.Id}/sections/{section.Id}?version={section.Version + 1}")).StatusCode);

        // A page or section of another Property's site is never reachable through this Property.
        var other = await Seed(); using var otherAdmin = TestToken.Client(factory, other.Actor, "SITE_ADMIN"); var otherSite = await Create(otherAdmin, other.P.Id, Key());
        var foreignPage = otherSite.Pages.Single(p => p.Kind == "CONTACT");
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"{Root(x.P.Id)}/pages/{foreignPage.Id}", new WebsitePageRequest("CONTACT", "iletisim", "x", null, true, 1, null, null, foreignPage.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync($"{Root(x.P.Id)}/pages/{foreignPage.Id}/sections", new WebsiteSectionRequest("RICH_TEXT", JsonSerializer.SerializeToElement(new { body = "x" }), true, 0))).StatusCode);

        var current = (await c.GetFromJsonAsync<WebsiteResponse>(Root(x.P.Id)))!; var amenities = current.Pages.Single(p => p.Id == page.Id);
        var ordered = await c.PutAsJsonAsync($"{Root(x.P.Id)}/pages/order", new WebsiteOrderRequest([new(amenities.Id, 5, amenities.Version)]));
        Assert.Equal(5, (await ordered.Content.ReadFromJsonAsync<WebsiteResponse>())!.Pages.Single(p => p.Id == page.Id).SortOrder);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"{Root(x.P.Id)}/pages/{page.Id}?version={amenities.Version + 1}")).StatusCode);
        var after = (await c.GetFromJsonAsync<WebsiteResponse>(Root(x.P.Id)))!; Assert.DoesNotContain(after.Pages, p => p.Id == page.Id);
        Assert.True(after.Version > current.Version);
    }

    [PostgresFact] public async Task PublishingCreatesImmutableSnapshotsThatThePublicApiServes()
    {
        var x = await Seed(); var key = Key(); using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN"); using var manager = TestToken.Client(factory, x.Actor, "MANAGER"); using var anon = factory.CreateClient();
        await factory.WithDb(async db => { db.Add(new Person(x.P.Id, "SECRET_NAME", "SECRET_SURNAME")); var a = new Announcement(x.P.Id, "SECRET_ANNOUNCEMENT", "SECRET_BODY", ContentAudience.ALL_MEMBERS); a.Publish(DateTimeOffset.UtcNow); db.Add(a); await db.SaveChangesAsync(); });
        var site = await Create(admin, x.P.Id, key);
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/api/public/v1/sites/{key}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Publish(manager, x.P.Id, site.Version)).StatusCode);
        Assert.Equal("BF_WEBSITE_PUBLISH_INCOMPLETE", await Code(await Publish(admin, x.P.Id, site.Version)));
        site = await FillPrivacy(admin, x.P.Id, site);
        Assert.Equal(HttpStatusCode.Conflict, (await Publish(admin, x.P.Id, site.Version - 1)).StatusCode);
        var request = Guid.NewGuid(); var first = await Publish(admin, x.P.Id, site.Version, request, "İlk yayın");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); var one = (await first.Content.ReadFromJsonAsync<WebsitePublicationSummary>())!;
        Assert.Equal(1, one.Number); Assert.True(one.IsActive);
        Assert.Equal(one.Id, (await (await Publish(admin, x.P.Id, site.Version, request, "İlk yayın")).Content.ReadFromJsonAsync<WebsitePublicationSummary>())!.Id);
        Assert.Equal("BF_WEBSITE_REQUEST_CONFLICT", await Code(await Publish(admin, x.P.Id, site.Version, request, "Farklı not")));

        var response = await anon.GetAsync($"/api/public/v1/sites/{key.ToUpperInvariant()}"); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(); var etag = response.Headers.ETag!.Tag;
        Assert.Contains("Altunbayrak Rezidans", json); Assert.Contains("Veri sorumlusu", json); Assert.Equal($"\"{one.SnapshotSha256}\"", etag);
        foreach (var secret in new[] { "SECRET", x.P.Id.ToString(), site.Id.ToString(), "\"version\"", "propertyId", x.Actor, "City" }) Assert.DoesNotContain(secret, json);
        using (var conditional = new HttpRequestMessage(HttpMethod.Get, $"/api/public/v1/sites/{key}")) { conditional.Headers.TryAddWithoutValidation("If-None-Match", etag); Assert.Equal(HttpStatusCode.NotModified, (await anon.SendAsync(conditional)).StatusCode); }

        // Draft edits stay private until the next publication.
        site = (await admin.GetFromJsonAsync<WebsiteResponse>(Root(x.P.Id)))!;
        site = (await (await admin.PutAsJsonAsync(Root(x.P.Id), new WebsiteUpdateRequest("Yeni Ad", null, null, null, false, site.Version))).Content.ReadFromJsonAsync<WebsiteResponse>())!;
        Assert.DoesNotContain("Yeni Ad", await anon.GetStringAsync($"/api/public/v1/sites/{key}"));
        var two = (await (await Publish(admin, x.P.Id, site.Version)).Content.ReadFromJsonAsync<WebsitePublicationSummary>())!;
        Assert.Equal(2, two.Number); Assert.Contains("Yeni Ad", await anon.GetStringAsync($"/api/public/v1/sites/{key}"));
        var restored = await admin.PostAsJsonAsync($"{Root(x.P.Id)}/publications/{one.Id}/restore", new WebsitePublishRequest(Guid.NewGuid(), site.Version + 1, "Geri al"));
        var three = (await restored.Content.ReadFromJsonAsync<WebsitePublicationSummary>())!;
        Assert.Equal(3, three.Number); Assert.Equal(one.SnapshotSha256, three.SnapshotSha256); Assert.Equal(one.Id, three.SourcePublicationId);
        Assert.Contains("Altunbayrak Rezidans", await anon.GetStringAsync($"/api/public/v1/sites/{key}"));
        var history = (await admin.GetFromJsonAsync<WebsitePublicationSummary[]>($"{Root(x.P.Id)}/publications"))!;
        Assert.Equal([3, 2, 1], history.Select(h => h.Number)); Assert.Single(history, h => h.IsActive);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"{Root(x.P.Id)}/publications")).StatusCode);

        // The stored snapshot cannot be rewritten or deleted, even with direct SQL.
        await factory.WithDb(async db => {
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE website_publications SET snapshot='{{}}' WHERE id={one.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM website_publications WHERE id={one.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE website_publications SET is_active=true WHERE id={one.Id}"));
        });
    }

    [PostgresFact] public async Task PublicApiAnswersTheSame404ForEveryUnavailableSite()
    {
        var x = await Seed(); var key = Key(); using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN"); using var platform = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); using var anon = factory.CreateClient();
        var site = await FillPrivacy(admin, x.P.Id, await Create(admin, x.P.Id, key));
        Assert.Equal(HttpStatusCode.OK, (await Publish(admin, x.P.Id, site.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/api/public/v1/sites/{key}")).StatusCode);
        foreach (var bad in new[] { "unknown-" + key, "a", "..%2Fetc", "UPPER_CASE", new string('a', 64) }) Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/api/public/v1/sites/{bad}")).StatusCode);

        site = (await admin.GetFromJsonAsync<WebsiteResponse>(Root(x.P.Id)))!;
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync($"{Root(x.P.Id)}/status", new WebsiteStatusRequest("SUSPENDED", site.Version))).StatusCode);
        var suspended = (await (await platform.PutAsJsonAsync($"{Root(x.P.Id)}/status", new WebsiteStatusRequest("SUSPENDED", site.Version))).Content.ReadFromJsonAsync<WebsiteResponse>())!;
        Assert.Equal("SUSPENDED", suspended.Status); Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/api/public/v1/sites/{key}")).StatusCode);
        Assert.Equal("BF_WEBSITE_STATE", await Code(await Publish(admin, x.P.Id, suspended.Version)));
        var resumed = (await (await platform.PutAsJsonAsync($"{Root(x.P.Id)}/status", new WebsiteStatusRequest("PUBLISHED", suspended.Version))).Content.ReadFromJsonAsync<WebsiteResponse>())!;
        Assert.Equal("PUBLISHED", resumed.Status); Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/api/public/v1/sites/{key}")).StatusCode);

        await factory.WithDb(async db => { var p = await db.Properties.SingleAsync(v => v.Id == x.P.Id); p.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/api/public/v1/sites/{key}")).StatusCode);
        Assert.Equal("BF_PARENT_INACTIVE", await Code(await admin.PutAsJsonAsync(Root(x.P.Id), new WebsiteUpdateRequest("x", null, null, null, true, resumed.Version))));
        await factory.WithDb(async db => { var p = await db.Properties.SingleAsync(v => v.Id == x.P.Id); p.SetEnabled(true); await db.SaveChangesAsync(); db.Remove(p); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/api/public/v1/sites/{key}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Root(x.P.Id))).StatusCode);

        var draft = await Seed(); var draftKey = Key(); using var draftAdmin = TestToken.Client(factory, draft.Actor, "SITE_ADMIN"); await Create(draftAdmin, draft.P.Id, draftKey);
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/api/public/v1/sites/{draftKey}")).StatusCode);
    }
}
