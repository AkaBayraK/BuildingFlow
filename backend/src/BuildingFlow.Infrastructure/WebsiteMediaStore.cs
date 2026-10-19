using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class WebsiteMediaStore(BuildingFlowDbContext db,IFileStorage storage):IWebsiteMediaStore{
 private IQueryable<WebsiteSite> Sites(Guid property,AccessScope access)=>db.Set<WebsiteSite>().Where(s=>s.PropertyId==property&&s.IsEnabled&&db.Properties.Any(p=>p.Id==property&&!p.IsDeleted&&(access.ReadAll||db.PropertyMemberships.Any(m=>m.PropertyId==property&&m.UserId==access.UserId&&m.IsEnabled&&!m.IsDeleted))));
 private async Task<WebsiteSite?> Lock(Guid property,AccessScope access,CancellationToken t){if(!await Sites(property,access).AnyAsync(t))return null;await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",t);var site=await Sites(property,access).SingleOrDefaultAsync(t);if(site is not null&&!await db.Properties.AnyAsync(p=>p.Id==property&&p.IsEnabled,t))throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());return site;}
 private void Touch(BaseEntity entity)=>db.Entry(entity).Property(x=>x.IsEnabled).IsModified=true;
 private static void Version(long expected,long actual){if(expected!=actual)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());}
 private static WebsiteMediaDto Dto(WebsiteMedia m,bool replay=false)=>new(m.Id,m.SiteId,m.ContentType,m.Size,m.Width,m.Height,m.Sha256,m.AltText,m.RightsBasis,m.RightsNote,m.ContainsPeople,m.PeopleConsentConfirmed,m.Version,replay);
 public async Task<IReadOnlyList<WebsiteMediaDto>?> List(Guid p,AccessScope a,CancellationToken t){var site=await Sites(p,a).AsNoTracking().SingleOrDefaultAsync(t);if(site is null)return null;return (await db.Set<WebsiteMedia>().AsNoTracking().Where(m=>m.SiteId==site.Id&&m.PropertyId==p).OrderByDescending(m=>m.CreatedAt).ToListAsync(t)).Select(m=>Dto(m)).ToArray();}
 public async Task<WebsiteMediaDto?> Upload(Guid p,WebsiteMediaUpload r,byte[] clean,int width,int height,AccessScope a,CancellationToken t){await using var tx=await db.Database.BeginTransactionAsync(t);var site=await Lock(p,a,t);if(site is null)return null;
  var fingerprint=WebsiteImage.Sha(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{sha=WebsiteImage.Sha(clean),r.ContentType,r.Details.AltText,r.Details.RightsBasis,r.Details.RightsNote,r.Details.ContainsPeople,r.Details.PeopleConsentConfirmed})));
  var old=await db.Set<WebsiteMedia>().IgnoreQueryFilters().SingleOrDefaultAsync(m=>m.PropertyId==p&&m.SiteId==site.Id&&m.RequestId==r.RequestId,t);if(old is not null){if(old.IsDeleted||old.Fingerprint!=fingerprint)throw new HierarchyConflictException(ErrorCatalog.WebsiteRequestConflict.Create());return Dto(old,true);}
  if(await db.Set<WebsiteMedia>().CountAsync(m=>m.PropertyId==p&&m.SiteId==site.Id,t)>=100)throw new HierarchyConflictException(ErrorCatalog.WebsiteState.Create());
  var d=r.Details;var media=new WebsiteMedia(p,site.Id,r.RequestId,fingerprint,r.ContentType,clean.Length,width,height,WebsiteImage.Sha(clean),d.AltText,d.RightsBasis,d.RightsNote,d.ContainsPeople,d.PeopleConsentConfirmed);
  // Do not delete on an uncertain DB commit. The UUID object key is independent of user filenames.
  await storage.UploadAsync(media.ObjectKey,new MemoryStream(clean,false),media.ContentType,t);db.Add(media);Touch(site);await db.SaveChangesAsync(t);await tx.CommitAsync(t);return Dto(media);
 }
 public async Task<WebsiteMediaDto?> Edit(Guid p,Guid id,WebsiteMediaEdit r,AccessScope a,CancellationToken t){await using var tx=await db.Database.BeginTransactionAsync(t);var site=await Lock(p,a,t);if(site is null)return null;var m=await db.Set<WebsiteMedia>().SingleOrDefaultAsync(m=>m.PropertyId==p&&m.SiteId==site.Id&&m.Id==id,t);if(m is null)return null;Version(r.Version,m.Version);m.Edit(r.AltText,r.RightsBasis,r.RightsNote,r.ContainsPeople,r.PeopleConsentConfirmed);Touch(m);Touch(site);await db.SaveChangesAsync(t);await tx.CommitAsync(t);return Dto(m);}
 public async Task<bool> Delete(Guid p,Guid id,long version,AccessScope a,CancellationToken t){await using var tx=await db.Database.BeginTransactionAsync(t);var site=await Lock(p,a,t);if(site is null)return false;var media=await db.Set<WebsiteMedia>().SingleOrDefaultAsync(m=>m.PropertyId==p&&m.SiteId==site.Id&&m.Id==id,t);if(media is null)return false;Version(version,media.Version);
  var sections=await db.Set<WebsiteSection>().Where(s=>s.PropertyId==p&&db.Set<WebsitePage>().Any(pg=>pg.PropertyId==p&&pg.SiteId==site.Id&&pg.Id==s.PageId)).Select(s=>s.Data).ToListAsync(t);
  var publications=await db.Set<WebsitePublication>().Where(x=>x.PropertyId==p&&x.SiteId==site.Id).Select(x=>x.Snapshot).ToListAsync(t);
  if(await db.Set<ListingPhoto>().AnyAsync(x=>x.PropertyId==p&&x.SiteId==site.Id&&x.MediaId==id,t)||site.LogoMediaId==id||site.HeroMediaId==id||sections.Any(s=>WebsiteMediaReferences.Contains(s,id))||publications.Any(s=>WebsiteMediaReferences.Contains(s,id)))throw new HierarchyConflictException(ErrorCatalog.WebsiteMediaInUse.Create());
  db.Remove(media);Touch(site);await db.SaveChangesAsync(t);await tx.CommitAsync(t);return true;
 }

 public async Task<WebsiteMediaContent?> Content(Guid p,Guid id,AccessScope a,CancellationToken t){var m=await db.Set<WebsiteMedia>().AsNoTracking().SingleOrDefaultAsync(m=>m.Id==id&&m.PropertyId==p&&Sites(p,a).Any(s=>s.Id==m.SiteId),t);return m is null?null:await WebsiteMediaBytes.Read(storage,m,t);}

}
