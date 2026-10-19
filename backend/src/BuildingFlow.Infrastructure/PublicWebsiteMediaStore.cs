using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class PublicWebsiteMediaStore(BuildingFlowDbContext db,IFileStorage storage):IPublicWebsiteMediaStore{
 public async Task<WebsiteMediaContent?> Public(string key,Guid id,string sha,CancellationToken t){var site=await db.Set<WebsiteSite>().AsNoTracking().SingleOrDefaultAsync(s=>s.SiteKey==key&&s.IsEnabled&&s.Status==WebsiteStatus.PUBLISHED&&db.Properties.Any(p=>p.Id==s.PropertyId&&p.IsEnabled&&!p.IsDeleted),t);if(site is null)return null;var snapshot=await db.Set<WebsitePublication>().Where(x=>x.PropertyId==site.PropertyId&&x.SiteId==site.Id&&x.IsActive&&x.IsEnabled).Select(x=>x.Snapshot).SingleOrDefaultAsync(t);if(snapshot is null)return null;var now=DateTimeOffset.UtcNow;var listingReference=await db.Set<ListingPhoto>().AnyAsync(x=>x.PropertyId==site.PropertyId&&x.SiteId==site.Id&&x.MediaId==id&&x.IsEnabled&&db.Set<PropertyListing>().Any(l=>l.Id==x.ListingId&&l.SiteId==site.Id&&l.PropertyId==site.PropertyId&&l.IsEnabled&&l.Status=="PUBLISHED"&&l.PublishUntil>now),t);if(!WebsiteMediaReferences.Contains(snapshot,id)&&!listingReference)return null;var m=await db.Set<WebsiteMedia>().AsNoTracking().SingleOrDefaultAsync(m=>m.Id==id&&m.PropertyId==site.PropertyId&&m.SiteId==site.Id&&m.Sha256==sha&&m.IsEnabled,t);return m is null?null:await WebsiteMediaBytes.Read(storage,m,t);}

}
