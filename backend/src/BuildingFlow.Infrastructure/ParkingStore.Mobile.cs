using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

public sealed partial class ParkingStore
{
    private sealed record MobileAccess(Guid? Person, bool Manager, string Key);
    private async Task<MobileAccess> AuthorizeMobile(MobileParkingContext q, CancellationToken token)
    {
        if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || q.Role=="ACCOUNTANT") throw new UnauthorizedAccessException();
        var manager=q.UnitId is null && q.Role is "SUPER_ADMIN" or "SITE_ADMIN" or "MANAGER";
        Guid? person=null; string key;
        if(config.GetValue<bool>("PersonRefactor:SessionContextsEnabled"))
        {
            if(!config.GetValue<bool>("PersonRefactor:Enabled") || !config.GetValue<bool>("PersonRefactor:MobileAuthorizationEnabled") || q.ContextId is not Guid contextId || q.ContextKind is not string kind)throw new UnauthorizedAccessException();
            var value=await contexts.GetAsync(kind,contextId,token);var c=value?.Context;
            if(c is null || c.PropertyId!=q.PropertyId || c.UnitId!=q.UnitId || c.Role!=q.Role || manager!=(c.Kind is "PLATFORM" or "MANAGEMENT"))throw new UnauthorizedAccessException();
            key=$"{c.TenantId}:{c.Kind}:{c.Id}";
            if(!manager)
            {
                if(value!.Self is null)throw new UnauthorizedAccessException();
                // An explicit current alias is required; no name/phone matching and no automatic backfill.
                var ids=await (from m in db.Set<LegacyPersonMap>() join p in db.People on m.LegacyPersonId equals p.Id
                    where m.TenantId==c.TenantId && m.PropertyId==c.PropertyId && m.PersonId==value.Self.Id && m.IsEnabled && p.IsEnabled && p.PropertyId==c.PropertyId && p.Version==m.SourceVersion
                    select p.Id).Take(2).ToArrayAsync(token);
                if(ids.Length!=1 || !await HasRelation(c.PropertyId,c.UnitId!.Value,ids[0],token))throw new UnauthorizedAccessException();
                person=ids[0];
            }
        }
        else
        {
            if(q.ContextKind is not null || q.ContextId is not null)throw new UnauthorizedAccessException();
            var session=await sessions.ReadAsync(actor.UserId,actor.Username,new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Where(actor.IsInRole).ToArray(),token);
            if(session.HasMore || !session.Contexts.Any(c=>c.PropertyId==q.PropertyId && c.UnitId==q.UnitId && c.Role==q.Role))throw new UnauthorizedAccessException();
            key=$"{q.PropertyId}:{(q.UnitId?.ToString()??"-")}:{q.Role}";
            if(!manager){person=await ResidentPerson(q.PropertyId,q.UnitId!.Value,actor.UserId,token);if(person==Guid.Empty)throw new UnauthorizedAccessException();}
        }
        if(!await Visible(q.PropertyId,new(actor.UserId,actor.IsInRole("SUPER_ADMIN")),token))throw new UnauthorizedAccessException();
        return new(person,manager,key);
    }
    private async Task LockMobileTenant(MobileParkingContext q, CancellationToken token)
    {
        if(q.ContextKind is not null && q.ContextId is Guid id)
        {
            var c=(await contexts.GetAsync(q.ContextKind,id,token))?.Context ?? throw new UnauthorizedAccessException();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={c.TenantId} FOR UPDATE",token);
        }
    }
    public async Task<MobileParkingSnapshot> MobileRead(MobileParkingContext q,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        var a=await AuthorizeMobile(q,token);var skip=(q.Page-1)*20;var today=Today;
        var vehicles=db.Vehicles.AsNoTracking().Where(v=>!a.Manager && v.PropertyId==q.PropertyId && v.UnitId==q.UnitId && v.PersonId==a.Person && v.IsEnabled && v.PersonalDataRedactedAt == null);
        var allocations=db.ParkingAllocations.AsNoTracking().Where(v=>!a.Manager && v.PropertyId==q.PropertyId && v.UnitId==q.UnitId && v.IsEnabled && (v.EndDate==null || v.EndDate>=today));
        var guests=db.GuestVehicles.AsNoTracking().Where(v=>a.Manager && v.PropertyId==q.PropertyId && v.IsEnabled && v.PersonalDataRedactedAt == null);
        var units=db.Units.AsNoTracking().Where(u=>a.Manager && u.PropertyId==q.PropertyId && u.IsEnabled);
        var spaces=db.ParkingSpaces.AsNoTracking().Where(s=>a.Manager && s.PropertyId==q.PropertyId && s.Type==ParkingSpaceType.VISITOR && s.IsEnabled);
        var vehicleTotal=await vehicles.CountAsync(token);var allocationTotal=await allocations.CountAsync(token);var guestTotal=await guests.CountAsync(token);var unitTotal=await units.CountAsync(token);var spaceTotal=await spaces.CountAsync(token);
        var vr=await vehicles.OrderBy(v=>v.Id).Skip(skip).Take(20).ToListAsync(token);
        var ar=await (from v in allocations join s in db.ParkingSpaces on v.SpaceId equals s.Id orderby v.StartDate descending,v.Id select new MobileParkingAllocation(v.Id,s.Code,v.StartDate,v.EndDate)).Skip(skip).Take(20).ToListAsync(token);
        var gr=await GuestRows(guests.OrderByDescending(g=>g.StartsAt).ThenBy(g=>g.Id).Skip(skip).Take(20)).ToListAsync(token);
        // Mobile intentionally has no guest name/note, including online and local snapshots.
        gr=gr.Select(g=>g with {GuestName=null,Note=null}).ToList();
        var ur=await units.OrderBy(u=>u.Id).Skip(skip).Take(20).Select(u=>new ParkingUnitOption(u.Id,u.Number,u.BuildingId==null?null:db.Buildings.Where(b=>b.Id==u.BuildingId && b.PropertyId==q.PropertyId && b.IsEnabled).Select(b=>b.Name).SingleOrDefault())).ToListAsync(token);
        var sr=await spaces.OrderBy(s=>s.Code).ThenBy(s=>s.Id).Skip(skip).Take(20).ToListAsync(token);var spaceRows=await SpaceResponses(q.PropertyId,sr,token);
        var policy=await CurrentPolicy(q.PropertyId,token);
        await tx.CommitAsync(token);
        return new(actor.UserId!,a.Key,q.PropertyId,q.UnitId,q.Role,clock.GetUtcNow(),q.Page,vehicleTotal,allocationTotal,guestTotal,unitTotal,spaceTotal,vr.Select(ResidentResponse).ToArray(),ar,gr,ur,spaceRows,policy!=null,policy?.GuestMaxHours??0);
    }
    public async Task<ResidentVehicleResponse> MobileRegister(MobileParkingContext q,ResidentVehicleRequest r,VehicleType type,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(token);
        await LockMobileTenant(q,token);
        var parent=await Lock(q.PropertyId,new(actor.UserId!,actor.IsInRole("SUPER_ADMIN")),token)??throw new UnauthorizedAccessException();
        var a=await AuthorizeMobile(q,token);if(a.Manager || a.Person is null || r.PropertyId!=q.PropertyId || r.UnitId!=q.UnitId)throw new UnauthorizedAccessException();
        var plate=VehiclePlate.Normalize(r.Plate!);
        var existing=await Replay(q.PropertyId,r.RequestId,v=>v.PersonId==a.Person && v.UnitId==q.UnitId && v.Plate==plate && v.Type==type && v.Brand==Clean(r.Brand) && v.Model==Clean(r.Model) && v.Color==Clean(r.Color),token);
        if(existing is not null){await tx.CommitAsync(token);return ResidentResponse(existing);}
        var value=await NewVehicle(q.PropertyId,a.Person.Value,q.UnitId!.Value,plate,type,r.Brand,r.Model,r.Color,r.RequestId,token);
        await Commit(parent,tx,token);return ResidentResponse(value);
    }
}
