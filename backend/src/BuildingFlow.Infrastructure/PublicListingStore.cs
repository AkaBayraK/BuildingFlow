using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class PublicListingStore(BuildingFlowDbContext db,TimeProvider clock):IPublicListingStore{
 private IQueryable<WebsiteSite> Sites(string key)=>db.Set<WebsiteSite>().Where(s=>s.SiteKey==key&&s.IsEnabled&&s.Status==WebsiteStatus.PUBLISHED&&db.Properties.Any(p=>p.Id==s.PropertyId&&p.IsEnabled&&!p.IsDeleted)&&db.Set<WebsitePublication>().Any(x=>x.SiteId==s.Id&&x.PropertyId==s.PropertyId&&x.IsActive&&x.IsEnabled));
 private IQueryable<PropertyListing> Rows(string key){var now=clock.GetUtcNow();return db.Set<PropertyListing>().AsNoTracking().Where(x=>x.IsEnabled&&x.Status=="PUBLISHED"&&x.PublishUntil>now&&Sites(key).Any(s=>s.Id==x.SiteId&&s.PropertyId==x.PropertyId));}
 private async Task<PublicListing> Dto(PropertyListing l,CancellationToken t){var media=await (from photo in db.Set<ListingPhoto>() join m in db.Set<WebsiteMedia>() on new{photo.PropertyId,photo.SiteId,Id=photo.MediaId} equals new{m.PropertyId,m.SiteId,m.Id} where photo.ListingId==l.Id&&photo.PropertyId==l.PropertyId&&photo.IsEnabled&&m.IsEnabled orderby photo.SortOrder select new PublicWebsiteMedia(m.Id,m.ContentType,m.Width,m.Height,m.Sha256,m.AltText)).ToArrayAsync(t);return new(l.Id,l.Kind,l.Title,l.Description,l.Price,"TRY",l.Kind=="RENT"?"MONTHLY":null,l.RoomLayout,l.GrossArea,l.NetArea,l.FloorLabel,l.BuildingLabel,JsonSerializer.Deserialize<string[]>(l.Features)!,media);}
 public async Task<IReadOnlyList<PublicListing>?> List(string key,string? kind,int page,CancellationToken t){if(page<1||page>1000||kind is not(null or "SALE" or "RENT"))ListingValidation.Invalid();if(!await Sites(key).AnyAsync(t))return null;var rows=await Rows(key).Where(x=>kind==null||x.Kind==kind).OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id).Skip((page-1)*12).Take(12).ToArrayAsync(t);var result=new List<PublicListing>();foreach(var l in rows)result.Add(await Dto(l,t));return result;}
 public async Task<PublicListing?> Get(string key,Guid id,CancellationToken t){var l=await Rows(key).SingleOrDefaultAsync(x=>x.Id==id,t);return l is null?null:await Dto(l,t);}
}
