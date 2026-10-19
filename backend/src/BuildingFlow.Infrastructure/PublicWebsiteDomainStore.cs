using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class PublicWebsiteDomainStore(BuildingFlowDbContext db,TimeProvider clock):IPublicWebsiteDomainStore{
 private IQueryable<WebsiteDomain> Rows(){var recent=clock.GetUtcNow().AddHours(-24);return db.Set<WebsiteDomain>().Where(d=>d.IsEnabled&&d.Status=="VERIFIED"&&d.LastCheckedAt>=recent&&d.TlsCheckedAt>=recent&&db.Set<WebsiteSite>().Any(s=>s.Id==d.SiteId&&s.PropertyId==d.PropertyId&&s.IsEnabled&&s.Status==WebsiteStatus.PUBLISHED&&db.Properties.Any(p=>p.Id==s.PropertyId&&p.IsEnabled)&&db.Set<WebsitePublication>().Any(p=>p.SiteId==s.Id&&p.PropertyId==s.PropertyId&&p.IsActive&&p.IsEnabled)));}
 public async Task<PublicWebsiteDomain?> Resolve(string host,CancellationToken t){try{host=WebsiteDomain.Normalize(host);}catch(ArgumentException){return null;}return await(from d in Rows() join s in db.Set<WebsiteSite>() on new{Id=d.SiteId,d.PropertyId} equals new{s.Id,s.PropertyId} where d.HostName==host select new PublicWebsiteDomain(s.SiteKey,d.HostName,d.IsPrimary)).SingleOrDefaultAsync(t);}
 public Task<string?> Primary(string key,CancellationToken t)=>(from d in Rows() join s in db.Set<WebsiteSite>() on new{Id=d.SiteId,d.PropertyId} equals new{s.Id,s.PropertyId} where s.SiteKey==key&&d.IsPrimary select d.HostName).SingleOrDefaultAsync(t);
}
