using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

// Management side of the public website. Every write locks the Property row and bumps the site version,
// so a publish request can prove which draft state the publisher reviewed.
public sealed class WebsiteStore(BuildingFlowDbContext db, TimeProvider clock) : IWebsiteStore
{
    private IQueryable<Property> Properties(AccessScope scope) => db.Properties.Where(p => !p.IsDeleted && (scope.ReadAll || db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == scope.UserId && m.IsEnabled && !m.IsDeleted)));
    private IQueryable<WebsiteSite> Sites(Guid property, AccessScope scope) => db.Set<WebsiteSite>().Where(s => s.PropertyId == property && Properties(scope).Any(p => p.Id == property));
    private async Task<(Property Parent, WebsiteSite Site)?> Lock(Guid property, AccessScope scope, CancellationToken token, bool allowMissingSite = false)
    {
        if (!await Properties(scope).AnyAsync(p => p.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var parent = await Properties(scope).SingleOrDefaultAsync(p => p.Id == property, token);
        if (parent is null) return null;
        if (!parent.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        var site = await db.Set<WebsiteSite>().SingleOrDefaultAsync(s => s.PropertyId == property, token);
        if (site is null && !allowMissingSite) return null;
        return (parent, site!);
    }
    private void Touch(BaseEntity entity) => db.Entry(entity).Property(x => x.IsEnabled).IsModified = true;
    private static void Version(long expected, long actual) { if (expected != actual) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());

    private static WebsiteSectionResponse Response(WebsiteSection s) => new(s.Id, s.PageId, s.Type.ToString(), JsonDocument.Parse(s.Data).RootElement.Clone(), s.IsVisible, s.SortOrder, s.Version);
    private static WebsitePageResponse Response(WebsitePage p, IEnumerable<WebsiteSection> sections) => new(p.Id, p.Kind.ToString(), p.Slug, p.Title, p.MenuLabel, p.ShowInMenu, p.SortOrder, p.SeoTitle, p.SeoDescription, p.IsRequired, p.Version,
        sections.Where(s => s.PageId == p.Id).OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt).Select(Response).ToArray());
    private static WebsitePublicationSummary Summary(WebsitePublication p) => new(p.Id, p.Number, p.PublishedAt, p.Note, p.IsActive, p.SourcePublicationId, p.SnapshotSha256, p.CreatedBy);

    private async Task<WebsiteResponse> Load(WebsiteSite site, CancellationToken token, bool replayed = false)
    {
        var pages = await db.Set<WebsitePage>().AsNoTracking().Where(p => p.SiteId == site.Id && p.PropertyId == site.PropertyId).OrderBy(p => p.SortOrder).ThenBy(p => p.Slug).ToListAsync(token);
        var ids = pages.Select(p => p.Id).ToArray();
        var sections = await db.Set<WebsiteSection>().AsNoTracking().Where(s => s.PropertyId == site.PropertyId && ids.Contains(s.PageId)).ToListAsync(token);
        var active = await db.Set<WebsitePublication>().AsNoTracking().SingleOrDefaultAsync(p => p.SiteId == site.Id && p.IsActive, token);
        return new(site.Id, site.PropertyId, site.SiteKey, site.DisplayName, site.Tagline, site.ContactEmail, site.ContactPhone, site.Status.ToString(), site.IsDemo,
            new(site.Template, site.PrimaryColor, site.AccentColor, site.LogoMediaId, site.HeroMediaId), site.Version, pages.Select(p => Response(p, sections)).ToArray(), active is null ? null : Summary(active), replayed);
    }

    public async Task<WebsiteResponse?> GetAsync(Guid property, AccessScope access, CancellationToken token)
    {
        var site = await Sites(property, access).AsNoTracking().SingleOrDefaultAsync(token);
        return site is null ? null : await Load(site, token);
    }

    public async Task<WebsiteResponse?> CreateAsync(Guid property, Guid requestId, string siteKey, string displayName, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token, allowMissingSite: true); if (locked is null) return null;
        var (parent, existing) = locked.Value;
        if (existing is not null)
        {
            // Same request replays the original result; anything else is a second site for the Property.
            if (existing.RequestId == requestId && existing.SiteKey == siteKey && existing.DisplayName == displayName) { await tx.CommitAsync(token); return await Load(existing, token, true); }
            throw Conflict(existing.RequestId == requestId ? ErrorCatalog.WebsiteRequestConflict : ErrorCatalog.WebsiteSiteExists);
        }
        if (await db.Set<WebsiteSite>().IgnoreQueryFilters().AnyAsync(s => s.RequestId == requestId, token)) throw Conflict(ErrorCatalog.WebsiteRequestConflict);
        if (await db.Set<WebsiteSite>().AnyAsync(s => s.SiteKey == siteKey, token)) throw Conflict(ErrorCatalog.WebsiteSiteKeyTaken);
        var site = new WebsiteSite(property, requestId, siteKey, displayName); db.Add(site);
        foreach (var (kind, slug, title, menu, order, sections) in WebsiteService.StarterPages(displayName))
        {
            var page = new WebsitePage(property, site.Id, kind, slug, title, menu, menu is not null, order); db.Add(page);
            var position = 0;
            foreach (var (type, data) in sections)
                db.Add(new WebsiteSection(property, page.Id, type, WebsiteContent.Validate(type, JsonSerializer.SerializeToElement(data)), true, position++ * 10));
        }
        Touch(parent); await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return await Load(site, token);
    }

    public async Task<WebsiteResponse?> UpdateAsync(Guid property, WebsiteUpdateRequest request, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return null;
        var site = locked.Value.Site; Version(request.Version, site.Version);
        site.UpdateDetails(request.DisplayName!, request.Tagline, request.ContactEmail, request.ContactPhone, request.IsDemo); Touch(site);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return await Load(site, token);
    }

    public async Task<WebsiteResponse?> ThemeAsync(Guid property, WebsiteThemeRequest request, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return null;
        var site = locked.Value.Site; Version(request.Version, site.Version);
        await ValidateMedia(site, new[]{request.LogoMediaId,request.HeroMediaId}.Where(x=>x.HasValue).Select(x=>x!.Value), token);
        site.SetMedia(request.LogoMediaId,request.HeroMediaId);
        site.UpdateTheme(request.Template!, request.PrimaryColor!, request.AccentColor!); Touch(site);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return await Load(site, token);
    }

    public async Task<WebsitePageResponse?> SavePageAsync(Guid property, Guid? id, WebsitePageRequest request, WebsitePageKind kind, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return null;
        var site = locked.Value.Site; var pages = db.Set<WebsitePage>().Where(p => p.SiteId == site.Id && p.PropertyId == property);
        WebsitePage page;
        if (id.HasValue)
        {
            page = (await pages.SingleOrDefaultAsync(p => p.Id == id, token))!; if (page is null) return null;
            Version(request.Version, page.Version);
            if (page.Kind != kind) throw new RequestValidationException(new() { ["kind"] = [ErrorCatalog.WebsitePageInvalid.Create()] });
            page.Update(request.Slug, request.Title!, request.MenuLabel, request.ShowInMenu, request.SortOrder, request.SeoTitle, request.SeoDescription); Touch(page);
        }
        else
        {
            if(await pages.CountAsync(token)>=100)throw Conflict(ErrorCatalog.WebsiteState);
            if (kind is WebsitePageKind.HOME or WebsitePageKind.PRIVACY or WebsitePageKind.COOKIES && await pages.AnyAsync(p => p.Kind == kind, token)) throw Conflict(ErrorCatalog.WebsitePageSlugTaken);
            page = new(property, site.Id, kind, request.Slug, request.Title!, request.MenuLabel, request.ShowInMenu, request.SortOrder);
            page.Update(request.Slug, request.Title!, request.MenuLabel, request.ShowInMenu, request.SortOrder, request.SeoTitle, request.SeoDescription); db.Add(page);
        }
        if (await pages.AnyAsync(p => p.Id != page.Id && p.Slug == page.Slug, token)) throw Conflict(ErrorCatalog.WebsitePageSlugTaken);
        Touch(site); await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return Response(page, await db.Set<WebsiteSection>().AsNoTracking().Where(s => s.PropertyId == property && s.PageId == page.Id).ToListAsync(token));
    }

    public async Task<bool> DeletePageAsync(Guid property, Guid id, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return false;
        var site = locked.Value.Site;
        var page = await db.Set<WebsitePage>().SingleOrDefaultAsync(p => p.SiteId == site.Id && p.PropertyId == property && p.Id == id, token); if (page is null) return false;
        Version(version, page.Version);
        if (page.IsRequired) throw Conflict(ErrorCatalog.WebsitePageRequired);
        foreach (var section in await db.Set<WebsiteSection>().Where(s => s.PropertyId == property && s.PageId == page.Id).ToListAsync(token)) db.Remove(section);
        db.Remove(page); Touch(site); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
    }

    public async Task<WebsiteResponse?> OrderPagesAsync(Guid property, IReadOnlyList<WebsiteOrderItem> items, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return null;
        var site = locked.Value.Site; var ids = items.Select(x => x.Id).ToArray();
        var pages = await db.Set<WebsitePage>().Where(p => p.SiteId == site.Id && p.PropertyId == property && ids.Contains(p.Id)).ToListAsync(token);
        if (pages.Count != items.Count) return null;
        foreach (var item in items) { var page = pages.Single(p => p.Id == item.Id); Version(item.Version, page.Version); page.Move(item.SortOrder); Touch(page); }
        Touch(site); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return await Load(site, token);
    }

    public async Task<WebsiteSectionResponse?> SaveSectionAsync(Guid property, Guid pageId, Guid? id, WebsiteSectionType type, string data, WebsiteSectionRequest request, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return null;
        var site = locked.Value.Site;
        var page = await db.Set<WebsitePage>().SingleOrDefaultAsync(p => p.SiteId == site.Id && p.PropertyId == property && p.Id == pageId, token); if (page is null) return null;
        await ValidateMedia(site,WebsiteMediaReferences.Read(data),token);
        WebsiteSection section;
        if (id.HasValue)
        {
            section = (await db.Set<WebsiteSection>().SingleOrDefaultAsync(s => s.PropertyId == property && s.PageId == pageId && s.Id == id, token))!; if (section is null) return null;
            Version(request.Version, section.Version);
            if (section.Type != type) throw new RequestValidationException(new() { ["type"] = [ErrorCatalog.WebsiteSectionInvalid.Create()] });
            section.Update(data, request.IsVisible, request.SortOrder); Touch(section);
        }
        else
        {
            if (await db.Set<WebsiteSection>().CountAsync(s => s.PropertyId == property && s.PageId == pageId, token) >= 30) throw Conflict(ErrorCatalog.WebsiteState);
            section = new(property, pageId, type, data, request.IsVisible, request.SortOrder); db.Add(section);
        }
        Touch(page); Touch(site); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return Response(section);
    }

    public async Task<bool> DeleteSectionAsync(Guid property, Guid pageId, Guid id, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return false;
        var site = locked.Value.Site;
        var page = await db.Set<WebsitePage>().SingleOrDefaultAsync(p => p.SiteId == site.Id && p.PropertyId == property && p.Id == pageId, token); if (page is null) return false;
        var section = await db.Set<WebsiteSection>().SingleOrDefaultAsync(s => s.PropertyId == property && s.PageId == pageId && s.Id == id, token); if (section is null) return false;
        Version(version, section.Version);
        db.Remove(section); Touch(page); Touch(site); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
    }

    public async Task<WebsitePageResponse?> OrderSectionsAsync(Guid property, Guid pageId, IReadOnlyList<WebsiteOrderItem> items, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return null;
        var site = locked.Value.Site;
        var page = await db.Set<WebsitePage>().SingleOrDefaultAsync(p => p.SiteId == site.Id && p.PropertyId == property && p.Id == pageId, token); if (page is null) return null;
        var ids = items.Select(x => x.Id).ToArray();
        var sections = await db.Set<WebsiteSection>().Where(s => s.PropertyId == property && s.PageId == pageId && ids.Contains(s.Id)).ToListAsync(token);
        if (sections.Count != items.Count) return null;
        foreach (var item in items) { var section = sections.Single(s => s.Id == item.Id); Version(item.Version, section.Version); section.Move(item.SortOrder); Touch(section); }
        Touch(page); Touch(site); await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return Response(page, await db.Set<WebsiteSection>().AsNoTracking().Where(s => s.PropertyId == property && s.PageId == pageId).ToListAsync(token));
    }

    private async Task ValidateMedia(WebsiteSite site,IEnumerable<Guid> ids,CancellationToken token){var refs=ids.Distinct().ToArray();if(refs.Length>0&&await db.Set<WebsiteMedia>().CountAsync(m=>m.PropertyId==site.PropertyId&&m.SiteId==site.Id&&m.IsEnabled&&refs.Contains(m.Id),token)!=refs.Length)throw new RequestValidationException(new(){["media"]=[ErrorCatalog.WebsiteMediaInvalid.Create()]});}
    private async Task<PublicWebsite> Draft(WebsiteSite site, CancellationToken token)
    {
        var pages = await db.Set<WebsitePage>().AsNoTracking().Where(p => p.SiteId == site.Id && p.PropertyId == site.PropertyId).ToListAsync(token);
        var ids = pages.Select(p => p.Id).ToArray();
        var sections = await db.Set<WebsiteSection>().AsNoTracking().Where(s => s.PropertyId == site.PropertyId && ids.Contains(s.PageId)).ToListAsync(token);
        var snapshot=WebsiteService.Snapshot(site, pages, sections);
        var refs=WebsiteMediaReferences.Read(JsonSerializer.Serialize(snapshot,WebsiteService.Json));
        await ValidateMedia(site,refs,token);
        var media=await db.Set<WebsiteMedia>().AsNoTracking().Where(m=>m.PropertyId==site.PropertyId&&m.SiteId==site.Id&&refs.Contains(m.Id)).ToListAsync(token);
        return snapshot with {Media=media.Select(m=>new PublicWebsiteMedia(m.Id,m.ContentType,m.Width,m.Height,m.Sha256,m.AltText)).ToArray()};
    }

    public async Task<PublicWebsite?> PreviewAsync(Guid property, AccessScope access, CancellationToken token)
    {
        var site = await Sites(property, access).AsNoTracking().SingleOrDefaultAsync(token);
        return site is null ? null : await Draft(site, token);
    }

    public Task<WebsitePublicationSummary?> PublishAsync(Guid property, WebsitePublishRequest request, AccessScope access, CancellationToken token) => Publish(property, null, request, access, token);
    public Task<WebsitePublicationSummary?> RestoreAsync(Guid property, Guid publication, WebsitePublishRequest request, AccessScope access, CancellationToken token) => Publish(property, publication, request, access, token);

    private async Task<WebsitePublicationSummary?> Publish(Guid property, Guid? source, WebsitePublishRequest request, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return null;
        var site = locked.Value.Site; var publications = db.Set<WebsitePublication>().Where(p => p.SiteId == site.Id && p.PropertyId == property);
        var replay = await publications.SingleOrDefaultAsync(p => p.RequestId == request.RequestId, token);
        if (replay is not null)
        {
            if (replay.SourcePublicationId != source || replay.Note != WebsiteSite.Optional(request.Note, 200, "note")) throw Conflict(ErrorCatalog.WebsiteRequestConflict);
            await tx.CommitAsync(token); return Summary(replay);
        }
        Version(request.ExpectedSiteVersion, site.Version);
        if (site.Status == WebsiteStatus.SUSPENDED) throw Conflict(ErrorCatalog.WebsiteState);
        string json, sha;
        if (source.HasValue)
        {
            var origin = await publications.AsNoTracking().SingleOrDefaultAsync(p => p.Id == source, token); if (origin is null) return null;
            (json, sha) = (origin.Snapshot, origin.SnapshotSha256);
        }
        else
        {
            var draft = await Draft(site, token);
            if (!WebsiteService.ReadyToPublish(draft)) throw Conflict(ErrorCatalog.WebsitePublishIncomplete);
            (json, sha) = WebsiteService.Serialize(draft);
        }
        var current = await publications.SingleOrDefaultAsync(p => p.IsActive, token);
        if (current is not null) { current.Deactivate(); await db.SaveChangesAsync(token); }
        var number = (await publications.MaxAsync(p => (int?)p.Number, token) ?? 0) + 1;
        var value = new WebsitePublication(property, site.Id, request.RequestId, number, json, sha, clock.GetUtcNow(), request.Note, source);
        db.Add(value); site.MarkPublished(); Touch(site);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return Summary(value);
    }

    public async Task<IReadOnlyList<WebsitePublicationSummary>?> PublicationsAsync(Guid property, AccessScope access, CancellationToken token)
    {
        var site = await Sites(property, access).AsNoTracking().SingleOrDefaultAsync(token); if (site is null) return null;
        var rows = await db.Set<WebsitePublication>().AsNoTracking().Where(p => p.SiteId == site.Id && p.PropertyId == property).OrderByDescending(p => p.Number).Take(100).ToListAsync(token);
        return rows.Select(Summary).ToArray();
    }

    public async Task<WebsiteResponse?> StatusAsync(Guid property, WebsiteStatus status, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var locked = await Lock(property, access, token); if (locked is null) return null;
        var site = locked.Value.Site; Version(version, site.Version);
        try { if (status == WebsiteStatus.SUSPENDED) site.Suspend(); else site.Resume(); }
        catch (InvalidOperationException) { throw Conflict(ErrorCatalog.WebsiteState); }
        Touch(site); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return await Load(site, token);
    }
}
