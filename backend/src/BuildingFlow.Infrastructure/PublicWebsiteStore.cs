using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

// Anonymous read path. It may only read website rows and the Property's availability flags;
// an architecture test keeps every other table (people, finance, announcements, documents…) out of this file.
public sealed class PublicWebsiteStore(BuildingFlowDbContext db) : IPublicWebsiteStore
{
    public async Task<PublicWebsiteDocument?> SiteAsync(string siteKey, CancellationToken token)
    {
        var row = await db.Set<WebsiteSite>().AsNoTracking()
            .Where(s => s.SiteKey == siteKey && s.IsEnabled && s.Status == WebsiteStatus.PUBLISHED &&
                db.Properties.Any(p => p.Id == s.PropertyId && p.IsEnabled && !p.IsDeleted))
            .SelectMany(s => db.Set<WebsitePublication>().Where(p => p.SiteId == s.Id && p.PropertyId == s.PropertyId && p.IsActive && p.IsEnabled))
            .Select(p => new { p.Number, p.PublishedAt, p.SnapshotSha256, p.Snapshot })
            .SingleOrDefaultAsync(token);
        return row is null ? null : new(row.Number, row.PublishedAt, row.SnapshotSha256, row.Snapshot);
    }
}
