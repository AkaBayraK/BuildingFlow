using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MobileNoticeStore(BuildingFlowDbContext db,IMobileSessionStore sessions,IPersonMessageStore messages,ICurrentUserService actor,TimeProvider clock):IMobileNoticeStore{
 private string User()=>actor.IsAuthenticated&&!string.IsNullOrWhiteSpace(actor.UserId)?actor.UserId:throw new UnauthorizedAccessException();
 private async Task<Property> Authorize(Guid property,Guid? unit,string role,CancellationToken token){
  var user=User();var roles=new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"}.Where(actor.IsInRole).ToArray();
  var session=await sessions.ReadAsync(user,actor.Username,roles,token);
  if(session.HasMore||!session.Contexts.Any(c=>c.PropertyId==property&&c.UnitId==unit&&c.Role==role))throw new UnauthorizedAccessException();
  return await db.Properties.AsNoTracking().SingleAsync(p=>p.Id==property&&p.IsEnabled,token);
 }
 private IQueryable<Announcement> Announcements(Guid property,string role)=>db.Announcements.AsNoTracking().Where(a=>a.PropertyId==property&&a.IsEnabled&&a.Status==AnnouncementStatus.PUBLISHED&&a.PublishedAt!=null&&(a.Audience==ContentAudience.ALL_MEMBERS||((role=="SUPER_ADMIN"||role=="SITE_ADMIN"||role=="MANAGER")&&a.Audience==ContentAudience.MANAGEMENT)));
 private static void Kind(string kind){if(kind is not ("ANNOUNCEMENT" or "MESSAGE"))throw new ArgumentException("Invalid notice kind.");}
 public async Task<MobileNoticeSnapshot> ReadAsync(MobileNoticeQuery q,CancellationToken token){
  Kind(q.Kind);if(q.Page is <1 or >10000)throw new RequestValidationException(new(){["page"]=[ErrorCatalog.InvalidPage.Create()]});
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);var p=await Authorize(q.PropertyId,q.UnitId,q.Role,token);var user=User();
  var preference=await db.Set<MobileNoticePreference>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.OwnerUserId==user,token);if(preference is {IsEnabled:false} or {IsDeleted:true})throw new UnauthorizedAccessException();
  int total,unread;MobileNoticeItem[] items;var skip=(q.Page-1)*50;
  if(q.Kind=="ANNOUNCEMENT"){
   var all=Announcements(q.PropertyId,q.Role);unread=await all.CountAsync(a=>!db.Set<MobileAnnouncementRead>().Any(r=>r.OwnerUserId==user&&r.PropertyId==q.PropertyId&&r.AnnouncementId==a.Id&&r.IsEnabled),token);
   if(q.ItemId.HasValue){if(!await all.AnyAsync(x=>x.Id==q.ItemId,token))throw new UnauthorizedAccessException();all=all.Where(x=>x.Id==q.ItemId);}
   total=await all.CountAsync(token);items=await all.OrderByDescending(x=>x.PublishedAt).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new MobileNoticeItem(x.Id,x.Title,x.Content,x.PublishedAt!.Value,db.Set<MobileAnnouncementRead>().Where(r=>r.OwnerUserId==user&&r.PropertyId==q.PropertyId&&r.AnnouncementId==x.Id&&r.IsEnabled).Select(r=>(DateTimeOffset?)r.ReadAt).FirstOrDefault())).ToArrayAsync(token);
  }else{
   var all=PersonMessageStore.InboxQuery(db,user).AsNoTracking().Where(x=>x.PropertyId==q.PropertyId&&x.IsEnabled);unread=await all.CountAsync(x=>x.ReadAt==null,token);
   if(q.ItemId.HasValue){if(!await all.AnyAsync(x=>x.Id==q.ItemId,token))throw new UnauthorizedAccessException();all=all.Where(x=>x.Id==q.ItemId);}
   total=await all.CountAsync(token);items=await all.OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new MobileNoticeItem(x.Id,x.Title,x.Body,x.CreatedAt,x.ReadAt)).ToArrayAsync(token);
  }
  await tx.CommitAsync(token);
  return new(user,$"{q.PropertyId}:{(q.UnitId.HasValue?q.UnitId.Value.ToString():"-")}:{q.Role}",clock.GetUtcNow(),p.Name,q.Kind,q.Page,q.ItemId,total,unread,new(preference?.HighlightsEnabled??true,preference?.Version??0),false,items);
 }
 public async Task<MobileNoticeReadResult> MarkReadAsync(MobileNoticeReadCommand c,CancellationToken token){
  Kind(c.Kind);if(c.Id==Guid.Empty)throw new ArgumentException("Invalid notice identity.");var user=User();await using var tx=await db.Database.BeginTransactionAsync(token);
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"notice-read:"+user+":"+c.Id},0))",token);
  await Authorize(c.PropertyId,c.UnitId,c.Role,token);DateTimeOffset at;
  if(c.Kind=="ANNOUNCEMENT"){
   if(!await Announcements(c.PropertyId,c.Role).AnyAsync(a=>a.Id==c.Id,token))throw new UnauthorizedAccessException();
   var old=await db.Set<MobileAnnouncementRead>().IgnoreQueryFilters().SingleOrDefaultAsync(r=>r.OwnerUserId==user&&r.AnnouncementId==c.Id,token);
   if(old!=null&&(old.IsDeleted||!old.IsEnabled))throw new UnauthorizedAccessException();
   if(old==null){old=new(user,c.PropertyId,c.Id,clock.GetUtcNow());db.Add(old);await db.SaveChangesAsync(token);}at=old.ReadAt;
  }else{
   if(!await PersonMessageStore.InboxQuery(db,user).AnyAsync(x=>x.Id==c.Id&&x.PropertyId==c.PropertyId&&x.IsEnabled,token)||!await messages.ReadAsync(c.Id,user,token))throw new UnauthorizedAccessException();
   at=(await db.PersonMessages.AsNoTracking().SingleAsync(x=>x.Id==c.Id&&x.RecipientUserId==user&&x.PropertyId==c.PropertyId,token)).ReadAt!.Value;
  }
  await tx.CommitAsync(token);return new(c.Id,c.Kind,at);
 }
 public async Task<MobileNoticePreferenceDto> PreferenceAsync(MobileNoticePreferenceCommand c,CancellationToken token){
  if(c.ExpectedVersion<0)throw new ArgumentException("Invalid preference version.");var user=User();await using var tx=await db.Database.BeginTransactionAsync(token);
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"notice-preference:"+user},0))",token);
  await Authorize(c.PropertyId,c.UnitId,c.Role,token);
  var p=await db.Set<MobileNoticePreference>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==user,token);
  if(p!=null&&(p.IsDeleted||!p.IsEnabled))throw new UnauthorizedAccessException();
  if((p?.Version??0)!=c.ExpectedVersion){if(p!=null&&p.Version==c.ExpectedVersion+1&&p.HighlightsEnabled==c.HighlightsEnabled)return new(p.HighlightsEnabled,p.Version);throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());}
  if(p==null){p=new(user);db.Add(p);}p.SetHighlights(c.HighlightsEnabled);if(db.Entry(p).State!=EntityState.Added)db.Entry(p).Property(x=>x.HighlightsEnabled).IsModified=true;
  await db.SaveChangesAsync(token);await tx.CommitAsync(token);return new(p.HighlightsEnabled,p.Version);
 }
}
