using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class CommonAreaStore
{
    internal static void MaintenanceConflict()=>throw new HierarchyConflictException(ErrorCatalog.CommonAreaMaintenanceConflict.Create());
    private static CommonAreaClosureResponse ClosureResult(CommonAreaClosure c,bool replay=false)=>new(c.Id,c.PropertyId,c.CommonAreaId,c.MaintenancePlanId,
        c.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),Hm(c.StartsAt),Hm(c.EndsAt),c.Notice,c.AnnouncementId,c.ReleasedAt,c.ReleaseReason,c.Version,c.NotificationCount,replay);
    public async Task<IReadOnlyList<CommonAreaClosureResponse>?> Closures(Guid property,Guid area,DateOnly date,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        if(!await Allowed(property,token)||!await db.Set<CommonArea>().AnyAsync(a=>a.Id==area&&a.PropertyId==property,token))return null;
        var rows=await db.Set<CommonAreaClosure>().AsNoTracking().Where(c=>c.PropertyId==property&&c.CommonAreaId==area&&c.Date==date).OrderBy(c=>c.StartsAt).ThenBy(c=>c.Id).ToArrayAsync(token);
        await tx.CommitAsync(token);return rows.Select(c=>ClosureResult(c)).ToArray();
    }
    public async Task<CommonAreaClosureResponse?> Close(Guid property,Guid area,CommonAreaClosureCommand c,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(token);var parent=await Lock(property,token);if(parent==null)return null;
        var target=await db.Set<CommonArea>().SingleOrDefaultAsync(a=>a.PropertyId==property&&a.Id==area,token);if(target==null)return null;
        var old=await db.Set<CommonAreaClosure>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.PropertyId==property&&x.RequestId==c.RequestId,token);
        if(old!=null){if(old.IsDeleted||old.Fingerprint!=c.Fingerprint||old.CommonAreaId!=area)MaintenanceConflict();return ClosureResult(old,true);}
        var plan=await db.MaintenancePlans.SingleOrDefaultAsync(p=>p.Id==c.Plan&&p.PropertyId==property&&p.IsEnabled,token);
        if(plan==null)return null;Expected(c.PlanVersion,plan.Version);
        if(!target.IsEnabled||plan.Status is MaintenanceStatus.COMPLETED or MaintenanceStatus.CANCELLED||plan.PlannedDate!=c.Date||c.Date<BusinessDate.Today(clock)||
            c.Date>BusinessDate.Today(clock).AddDays(365)||Started(c.Date,c.End)||
            await db.Set<CommonAreaClosure>().AnyAsync(x=>x.PropertyId==property&&x.CommonAreaId==area&&x.ReleasedAt==null&&
              (x.MaintenancePlanId==c.Plan||x.Date==c.Date&&x.StartsAt<c.End&&c.Start<x.EndsAt),token)||
            await Confirmed(property,area,c.Date).AnyAsync(r=>r.StartsAt<c.End&&c.Start<r.EndsAt,token))MaintenanceConflict();
        var announcement=CommonAreaMaintenanceEvents.Announcement(property,target,"Bakım nedeniyle kapalı",$"{c.Date:yyyy-MM-dd} {Hm(c.Start)}–{Hm(c.End)}\n{c.Notice}",clock.GetUtcNow());db.Add(announcement);
        var closure=new CommonAreaClosure(property,area,c.Plan,c.RequestId,c.Fingerprint,c.Date,c.Start,c.End,c.Notice,announcement.Id);db.Add(closure);
        closure.SetNotificationCount(await CommonAreaMaintenanceEvents.Notify(db,target,announcement,token));
        db.Entry(plan).Property(p=>p.IsEnabled).IsModified=true;db.Entry(target).Property(a=>a.IsEnabled).IsModified=true;db.Entry(parent).Property(p=>p.IsEnabled).IsModified=true;
        await db.SaveChangesAsync(token);await tx.CommitAsync(token);return ClosureResult(closure);
    }
    public async Task<CommonAreaClosureResponse?> Release(Guid property,Guid area,Guid id,long version,string reason,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(token);var parent=await Lock(property,token);if(parent==null)return null;
        var closure=await db.Set<CommonAreaClosure>().SingleOrDefaultAsync(c=>c.PropertyId==property&&c.CommonAreaId==area&&c.Id==id,token);if(closure==null)return null;
        Expected(version,closure.Version);if(closure.ReleasedAt!=null)MaintenanceConflict();
        await CommonAreaMaintenanceEvents.Release(db,closure,reason,clock.GetUtcNow(),token);db.Entry(parent).Property(p=>p.IsEnabled).IsModified=true;
        await db.SaveChangesAsync(token);await tx.CommitAsync(token);return ClosureResult(closure);
    }
}
internal static class CommonAreaMaintenanceEvents
{
    internal static Announcement Announcement(Guid property,CommonArea area,string action,string content,DateTimeOffset now)
    {var a=new Announcement(property,action+" · "+area.Name,content,area.Visibility==CommonAreaVisibility.PROPERTY?ContentAudience.ALL_MEMBERS:ContentAudience.MANAGEMENT,AnnouncementCategory.MAINTENANCE);a.Publish(now);return a;}
    internal static async Task<int> Notify(BuildingFlowDbContext db,CommonArea area,Announcement announcement,CancellationToken token)
    {
        // Existing personal inbox is legacy PropertyMembership-scoped. Never leak a building-scoped notice through it.
        if(area.Visibility!=CommonAreaVisibility.PROPERTY)return 0;
        var people=await db.People.Where(p=>p.PropertyId==area.PropertyId&&p.IsEnabled&&p.LinkedUserId!=null&&
            db.PropertyMemberships.Any(m=>m.PropertyId==area.PropertyId&&m.UserId==p.LinkedUserId&&m.IsEnabled)).OrderBy(p=>p.Id).ToArrayAsync(token);
        foreach(var person in people.DistinctBy(p=>p.LinkedUserId))db.PersonMessages.Add(new PersonMessage(area.PropertyId,person.Id,Guid.NewGuid(),new string('0',64),person.LinkedUserId!,announcement.Title,announcement.Content));
        return people.Select(p=>p.LinkedUserId).Distinct().Count();
    }
    internal static async Task Release(BuildingFlowDbContext db,CommonAreaClosure closure,string reason,DateTimeOffset now,CancellationToken token)
    {
        var area=await db.Set<CommonArea>().SingleAsync(a=>a.PropertyId==closure.PropertyId&&a.Id==closure.CommonAreaId,token);
        var old=await db.Announcements.SingleAsync(a=>a.PropertyId==closure.PropertyId&&a.Id==closure.AnnouncementId,token);
        if(old.Status==AnnouncementStatus.PUBLISHED)old.Archive();
        var announcement=Announcement(closure.PropertyId,area,"Bakım kapanışı kaldırıldı",$"{closure.Date:yyyy-MM-dd} {closure.StartsAt:HH:mm}–{closure.EndsAt:HH:mm}\nKullanıma kapatma kaldırıldı.",now);
        db.Add(announcement);closure.Release(reason,now,announcement.Id);closure.SetNotificationCount(closure.NotificationCount+await Notify(db,area,announcement,token));
        db.Entry(area).Property(a=>a.IsEnabled).IsModified=true;
    }
    internal static async Task PlanChanged(BuildingFlowDbContext db,MaintenancePlan plan,bool removed,DateTimeOffset now,CancellationToken token)
    {
        var closures=await db.Set<CommonAreaClosure>().Where(c=>c.PropertyId==plan.PropertyId&&c.MaintenancePlanId==plan.Id&&c.ReleasedAt==null).ToArrayAsync(token);
        if(!removed&&plan.Status is MaintenanceStatus.PLANNED or MaintenanceStatus.IN_PROGRESS){if(closures.Any(c=>c.Date!=plan.PlannedDate))CommonAreaStore.MaintenanceConflict();return;}
        foreach(var c in closures)await Release(db,c,removed?"Bakım planı kaldırıldı":"Bakım planı tamamlandı veya iptal edildi",now,token);
    }
}
