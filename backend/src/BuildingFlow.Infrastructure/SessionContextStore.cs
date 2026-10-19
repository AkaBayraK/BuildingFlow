using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

// Context IDs are navigation state, never a grant. Every read starts from current database facts.
public sealed class SessionContextStore(BuildingFlowDbContext db,ICurrentUserService actor,ManagementAccess management,TimeProvider clock,IConfiguration config):ISessionContextStore,IMobileAuthorizationStore
{
    public bool Enabled=>config.GetValue<bool>("PersonRefactor:SessionContextsEnabled");
    private DateOnly Today=>BusinessDate.Today(clock);
    private void Identity(){if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || string.IsNullOrWhiteSpace(actor.Issuer))throw new UnauthorizedAccessException();}
    private IQueryable<CentralPerson> OwnPeople()
    {
        var now=clock.GetUtcNow();var eligible=ManagementAccess.EligiblePeople(db);
        return eligible.Where(p=>db.Set<PersonTenant>().Any(t=>t.Id==p.TenantId && t.IsEnabled) && db.Set<CentralPersonAccountLink>().Any(l=>l.TenantId==p.TenantId && l.PersonId==p.Id && l.IsEnabled && l.EndedAt==null && l.LinkedAt<=now &&
            db.Set<PersonUserAccount>().Any(a=>a.Id==l.UserAccountId && a.IsEnabled && a.Issuer==actor.Issuer && a.Subject==actor.UserId && db.Set<PersonTenantMembership>().Any(m=>m.TenantId==p.TenantId && m.UserAccountId==a.Id && m.IsEnabled))));
    }
    internal IQueryable<PersonPropertyScope> ManagementProperties(string capability="ACCOUNTANT")
    {
        Identity();var scopes=db.Set<PersonPropertyScope>().Where(s=>s.IsEnabled && db.Set<PersonTenant>().Any(t=>t.Id==s.TenantId && t.IsEnabled) && db.Properties.Any(p=>p.Id==s.PropertyId && p.IsEnabled));
        if(actor.IsInRole("SUPER_ADMIN"))return scopes;
        var roles=management.BoundRoles(capability);
        var assignments=management.ActiveAssignments();return scopes.Where(s=>db.PropertyMemberships.Any(m=>m.PropertyId==s.PropertyId && m.UserId==actor.UserId && m.IsEnabled) && assignments.Any(a=>a.TenantId==s.TenantId && a.PropertyId==s.PropertyId && roles.Contains(a.Role)));
    }
    public Task<bool> PropertyAllowedAsync(Guid property,string capability,CancellationToken token)=>ManagementProperties(capability).AnyAsync(s=>s.PropertyId==property,token);
    private sealed class Entry
    {
        public Guid Id{get;set;} public string Kind{get;set;}=null!; public Guid TenantId{get;set;} public Guid PropertyId{get;set;} public Guid? UnitId{get;set;} public string Role{get;set;}=null!; public DateOnly? Start{get;set;} public DateOnly? End{get;set;}
    }
    private IQueryable<Entry> Entries()
    {
        Identity();var today=Today;var people=OwnPeople();var scopes=db.Set<PersonPropertyScope>().Where(s=>s.IsEnabled && db.Set<PersonTenant>().Any(t=>t.Id==s.TenantId && t.IsEnabled) && db.Properties.Any(p=>p.Id==s.PropertyId && p.IsEnabled));
        var roles=management.BoundRoles("ACCOUNTANT");
        var duties=management.ActiveAssignments().Where(a=>scopes.Any(s=>s.TenantId==a.TenantId && s.PropertyId==a.PropertyId) && roles.Contains(a.Role) && db.PropertyMemberships.Any(m=>m.PropertyId==a.PropertyId && m.UserId==actor.UserId && m.IsEnabled)).Select(a=>new Entry{Id=a.Id,Kind="MANAGEMENT",TenantId=a.TenantId,PropertyId=a.PropertyId,UnitId=null,Role=a.Role,Start=a.StartDate,End=a.EndDate});
        // The platform exception is explicit and does not invent an assignment/person for SUPER_ADMIN.
        var platform=scopes.Where(s=>actor.IsInRole("SUPER_ADMIN")).Select(s=>new Entry{Id=s.PropertyId,Kind="PLATFORM",TenantId=s.TenantId,PropertyId=s.PropertyId,UnitId=null,Role="SUPER_ADMIN",Start=null,End=null});
        duties=duties.Where(a=>!actor.IsInRole("SUPER_ADMIN"));
        var units=db.Units.Where(u=>u.IsEnabled && (u.BuildingId==null || db.Buildings.Any(b=>b.Id==u.BuildingId && b.PropertyId==u.PropertyId && b.IsEnabled)));
        var owners=db.Set<UnitOwnership>().Where(r=>r.IsEnabled && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && people.Any(p=>p.TenantId==r.TenantId && p.Id==r.PersonId) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId) && units.Any(u=>u.Id==r.UnitId && u.PropertyId==r.PropertyId) &&
            (!db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.OwnershipId==r.Id) || db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.PropertyId==r.PropertyId && m.OwnershipId==r.Id && m.IsEnabled && db.UnitOccupancies.Any(o=>o.Id==m.LegacyOccupancyId && o.PropertyId==r.PropertyId && o.UnitId==r.UnitId && o.Version==m.SourceVersion && o.IsEnabled)))).Select(r=>new Entry{Id=r.Id,Kind="OWNER",TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,Role="OWNER",Start=r.StartDate,End=r.EndDate});
        var tenants=db.Set<UnitTenancy>().Where(r=>r.IsEnabled && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && people.Any(p=>p.TenantId==r.TenantId && p.Id==r.PersonId) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId) && units.Any(u=>u.Id==r.UnitId && u.PropertyId==r.PropertyId) &&
            (!db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.TenancyId==r.Id) || db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.PropertyId==r.PropertyId && m.TenancyId==r.Id && m.IsEnabled && db.UnitOccupancies.Any(o=>o.Id==m.LegacyOccupancyId && o.PropertyId==r.PropertyId && o.UnitId==r.UnitId && o.Version==m.SourceVersion && o.IsEnabled)))).Select(r=>new Entry{Id=r.Id,Kind="TENANT",TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,Role="TENANT",Start=r.StartDate,End=r.EndDate});
        var residents=db.Set<UnitResidency>().Where(r=>r.IsEnabled && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && people.Any(p=>p.TenantId==r.TenantId && p.Id==r.PersonId) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId) && units.Any(u=>u.Id==r.UnitId && u.PropertyId==r.PropertyId) &&
            (!db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.ResidencyId==r.Id) || db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.PropertyId==r.PropertyId && m.ResidencyId==r.Id && m.IsEnabled && db.UnitOccupancies.Any(o=>o.Id==m.LegacyOccupancyId && o.PropertyId==r.PropertyId && o.UnitId==r.UnitId && o.Version==m.SourceVersion && o.IsEnabled)))).Select(r=>new Entry{Id=r.Id,Kind="RESIDENT",TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,Role="RESIDENT",Start=r.StartDate,End=r.EndDate});
        return platform.Concat(duties).Concat(owners).Concat(tenants).Concat(residents);
    }
    private IQueryable<SessionContext> Project(IQueryable<Entry> entries)=>from e in entries join t in db.Set<PersonTenant>() on e.TenantId equals t.Id join p in db.Properties on e.PropertyId equals p.Id
        select new SessionContext(e.Id,e.Kind,e.TenantId,t.Name,e.PropertyId,p.Name,e.UnitId,db.Units.Where(u=>u.Id==e.UnitId && u.PropertyId==e.PropertyId).Select(u=>u.Number).FirstOrDefault(),e.Role,e.Start,e.End,e.Role=="SUPER_ADMIN" || e.Role=="SITE_ADMIN" || e.Role=="MANAGER",e.Kind=="MANAGEMENT" || e.Kind=="PLATFORM",e.Role=="SUPER_ADMIN" || e.Role=="SITE_ADMIN");
    public async Task<PagedRows<SessionContext>> ListAsync(int page,int size,CancellationToken token)
    {
        if(page is <1 or >100000 || size is <1 or >100)throw new ArgumentException("Invalid context pagination.");
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token):null;var q=Entries();var count=await q.CountAsync(token);var rows=await Project(q.OrderBy(e=>e.TenantId).ThenBy(e=>e.PropertyId).ThenBy(e=>e.Kind).ThenBy(e=>e.Id).Skip((page-1)*size).Take(size)).ToArrayAsync(token);if(tx!=null)await tx.CommitAsync(token);return new(rows,page,size,count);
    }
    public async Task<SessionContextDetail?> GetAsync(string kind,Guid id,CancellationToken token)
    {
        if(kind is not("PLATFORM" or "MANAGEMENT" or "OWNER" or "TENANT" or "RESIDENT") || id==Guid.Empty)throw new ArgumentException("Invalid workspace identity.");
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token):null;var result=await Project(Entries().Where(e=>e.Id==id && e.Kind==kind)).SingleOrDefaultAsync(token);if(result==null)return null;
        var p=await OwnPeople().Where(p=>p.TenantId==result.TenantId).SingleOrDefaultAsync(token);SelfPerson? self=null;if(p!=null){var identity=p.IdentityNumber;self=new(p.Id,p.TenantId,(p.FirstName+" "+p.LastName).Trim(),p.Kind,new string('*',Math.Max(0,identity.Length-4))+identity[^Math.Min(4,identity.Length)..]);}if(tx!=null)await tx.CommitAsync(token);return new(result,self);
    }
    public async Task<MobileAuthorizationSnapshot> ReadAsync(CancellationToken token)
    {
        Identity();await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token):null;
        var contexts=await Project(Entries().OrderBy(e=>e.TenantId).ThenBy(e=>e.PropertyId).ThenBy(e=>e.Kind).ThenBy(e=>e.Id).Take(501)).ToArrayAsync(token);
        if(contexts.Length>500)throw new ArgumentException("Mobile authorization context limit exceeded.");
        var tenants=contexts.Select(c=>c.TenantId).Distinct().ToArray();
        var persons=await OwnPeople().Where(p=>tenants.Contains(p.TenantId)).OrderBy(p=>p.TenantId).Select(p=>new MobilePersonIdentity(p.Id,p.TenantId,(p.FirstName+" "+p.LastName).Trim())).ToArrayAsync(token);
        var now=clock.GetUtcNow();var expiry=now.AddHours(contexts.Length==0 || contexts.Any(c=>c.CanManageFinance)?1:24);
        var zone=TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        foreach(var end in contexts.Where(c=>c.EndDate.HasValue && c.EndDate.Value<DateOnly.MaxValue).Select(c=>c.EndDate!.Value))
        {
            var boundary=new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(end.AddDays(1).ToDateTime(TimeOnly.MinValue),zone),TimeSpan.Zero);
            if(boundary<expiry)expiry=boundary;
        }
        if(tx!=null)await tx.CommitAsync(token);return new(1,Guid.NewGuid(),actor.UserId!,now,expiry,true,persons,contexts);
    }

}
