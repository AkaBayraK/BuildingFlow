using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

// Context IDs are navigation state, never a grant. Every read starts from current database facts.
public sealed class SessionContextStore(BuildingFlowDbContext db,ICurrentUserService actor,ManagementAccess management,TimeProvider clock,IConfiguration config):ISessionContextStore,IMobileAuthorizationStore,IMyPortfolioStore
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
        public Guid? PersonId{get;set;} public Guid? LeaseContractId{get;set;} public string? Verification{get;set;}
    }
    private IQueryable<Entry> Entries()
    {
        Identity();var today=Today;var people=OwnPeople();var scopes=db.Set<PersonPropertyScope>().Where(s=>s.IsEnabled && db.Set<PersonTenant>().Any(t=>t.Id==s.TenantId && t.IsEnabled) && db.Properties.Any(p=>p.Id==s.PropertyId && p.IsEnabled));
        var roles=management.BoundRoles("ACCOUNTANT");
        var duties=management.ActiveAssignments().Where(a=>scopes.Any(s=>s.TenantId==a.TenantId && s.PropertyId==a.PropertyId) && roles.Contains(a.Role) && db.PropertyMemberships.Any(m=>m.PropertyId==a.PropertyId && m.UserId==actor.UserId && m.IsEnabled)).Select(a=>new Entry{Id=a.Id,Kind="MANAGEMENT",TenantId=a.TenantId,PropertyId=a.PropertyId,UnitId=null,Role=a.Role,Start=a.StartDate,End=a.EndDate,PersonId=null,LeaseContractId=null,Verification=null});
        // The platform exception is explicit and does not invent an assignment/person for SUPER_ADMIN.
        var platform=scopes.Where(s=>actor.IsInRole("SUPER_ADMIN")).Select(s=>new Entry{Id=s.PropertyId,Kind="PLATFORM",TenantId=s.TenantId,PropertyId=s.PropertyId,UnitId=null,Role="SUPER_ADMIN",Start=null,End=null,PersonId=null,LeaseContractId=null,Verification=null});
        duties=duties.Where(a=>!actor.IsInRole("SUPER_ADMIN"));
        var units=db.Units.Where(u=>u.IsEnabled && (u.BuildingId==null || db.Buildings.Any(b=>b.Id==u.BuildingId && b.PropertyId==u.PropertyId && b.IsEnabled)));
        var owners=db.Set<UnitOwnership>().Where(r=>r.IsEnabled && (r.VerificationStatus==RelationVerificationStatus.VERIFIED || r.VerificationStatus==RelationVerificationStatus.LEGACY) && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && people.Any(p=>p.TenantId==r.TenantId && p.Id==r.PersonId) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId) && units.Any(u=>u.Id==r.UnitId && u.PropertyId==r.PropertyId) &&
            (!db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.OwnershipId==r.Id) || db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.PropertyId==r.PropertyId && m.OwnershipId==r.Id && m.IsEnabled && db.UnitOccupancies.Any(o=>o.Id==m.LegacyOccupancyId && o.PropertyId==r.PropertyId && o.UnitId==r.UnitId && o.Version==m.SourceVersion && o.IsEnabled)))).Select(r=>new Entry{Id=r.Id,Kind="OWNER",TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,Role="OWNER",Start=r.StartDate,End=r.EndDate,PersonId=r.PersonId,LeaseContractId=null,Verification=r.VerificationStatus==RelationVerificationStatus.VERIFIED?"VERIFIED":"LEGACY"});
        var tenants=db.Set<UnitTenancy>().Where(r=>r.IsEnabled && (r.LeaseContractId==null || db.Set<LeaseContract>().Any(c=>c.Id==r.LeaseContractId && c.IsEnabled && c.VerificationStatus!=RelationVerificationStatus.REJECTED)) && (r.VerificationStatus==RelationVerificationStatus.VERIFIED || r.VerificationStatus==RelationVerificationStatus.LEGACY) && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && people.Any(p=>p.TenantId==r.TenantId && p.Id==r.PersonId) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId) && units.Any(u=>u.Id==r.UnitId && u.PropertyId==r.PropertyId) &&
            (!db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.TenancyId==r.Id) || db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.PropertyId==r.PropertyId && m.TenancyId==r.Id && m.IsEnabled && db.UnitOccupancies.Any(o=>o.Id==m.LegacyOccupancyId && o.PropertyId==r.PropertyId && o.UnitId==r.UnitId && o.Version==m.SourceVersion && o.IsEnabled)))).Select(r=>new Entry{Id=r.Id,Kind="TENANT",TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,Role="TENANT",Start=r.StartDate,End=r.EndDate,PersonId=r.PersonId,LeaseContractId=r.LeaseContractId,Verification=r.VerificationStatus==RelationVerificationStatus.VERIFIED?"VERIFIED":"LEGACY"});
        var residents=db.Set<UnitResidency>().Where(r=>r.IsEnabled && (r.VerificationStatus==RelationVerificationStatus.VERIFIED || r.VerificationStatus==RelationVerificationStatus.LEGACY) && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && people.Any(p=>p.TenantId==r.TenantId && p.Id==r.PersonId) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId) && units.Any(u=>u.Id==r.UnitId && u.PropertyId==r.PropertyId) &&
            (!db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.ResidencyId==r.Id) || db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==r.TenantId && m.PropertyId==r.PropertyId && m.ResidencyId==r.Id && m.IsEnabled && db.UnitOccupancies.Any(o=>o.Id==m.LegacyOccupancyId && o.PropertyId==r.PropertyId && o.UnitId==r.UnitId && o.Version==m.SourceVersion && o.IsEnabled)))).Select(r=>new Entry{Id=r.Id,Kind="RESIDENT",TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,Role="RESIDENT",Start=r.StartDate,End=r.EndDate,PersonId=r.PersonId,LeaseContractId=null,Verification=r.VerificationStatus==RelationVerificationStatus.VERIFIED?"VERIFIED":"LEGACY"});
        return platform.Concat(duties).Concat(owners).Concat(tenants).Concat(residents);
    }
    private IQueryable<SessionContext> Project(IQueryable<Entry> entries)=>from e in entries join t in db.Set<PersonTenant>() on e.TenantId equals t.Id join p in db.Properties on e.PropertyId equals p.Id
        select new SessionContext(e.Id,e.Kind,e.TenantId,t.Name,e.PropertyId,p.Name,e.UnitId,db.Units.Where(u=>u.Id==e.UnitId && u.PropertyId==e.PropertyId).Select(u=>u.Number).FirstOrDefault(),e.Role,e.Start,e.End,e.Role=="SUPER_ADMIN" || e.Role=="SITE_ADMIN" || e.Role=="MANAGER",e.Kind=="MANAGEMENT" || e.Kind=="PLATFORM",e.Role=="SUPER_ADMIN" || e.Role=="SITE_ADMIN");
    public async Task<PagedRows<SessionContext>> ListAsync(int page,int size,CancellationToken token)
    {
        if(page is <1 or >100000 || size is <1 or >100)throw new ArgumentException("Invalid context pagination.");
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token):null;var q=Entries();var count=await q.CountAsync(token);var rows=await Project(q.OrderBy(e=>e.TenantId).ThenBy(e=>e.PropertyId).ThenBy(e=>e.Kind).ThenBy(e=>e.Id).Skip((page-1)*size).Take(size)).ToArrayAsync(token);if(tx!=null)await tx.CommitAsync(token);return new(rows,page,size,count);
    }
    public async Task<PagedRows<SessionContext>?> WebsiteAsync(string siteKey,string audience,int page,int size,CancellationToken token)
    {
        Identity();
        if(page is <1 or >100000 || size is <1 or >100)throw new ArgumentException("Invalid context pagination.");
        if(!System.Text.RegularExpressions.Regex.IsMatch(siteKey,@"\A[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])\z") || audience is not("all" or "management" or "resident"))return null;
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token):null;
        var property=await db.Set<WebsiteSite>().Where(s=>s.SiteKey==siteKey && s.IsEnabled && s.Status==WebsiteStatus.PUBLISHED && db.Properties.Any(p=>p.Id==s.PropertyId && p.IsEnabled) && db.Set<WebsitePublication>().Any(p=>p.SiteId==s.Id && p.PropertyId==s.PropertyId && p.IsActive && p.IsEnabled)).Select(s=>(Guid?)s.PropertyId).SingleOrDefaultAsync(token);
        if(property==null)return null;
        var q=Entries().Where(e=>e.PropertyId==property.Value);
        if(audience=="management")q=q.Where(e=>e.Kind=="MANAGEMENT" || e.Kind=="PLATFORM");
        else if(audience=="resident")q=q.Where(e=>e.Kind=="OWNER" || e.Kind=="TENANT" || e.Kind=="RESIDENT");
        var count=await q.CountAsync(token);
        // Do not disclose site existence to an account without a matching current context.
        if(count==0)return null;
        var rows=await Project(q.OrderBy(e=>e.Kind).ThenBy(e=>e.Id).Skip((page-1)*size).Take(size)).ToArrayAsync(token);
        if(tx!=null)await tx.CommitAsync(token);return new(rows,page,size,count);
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

    private static readonly string[] UnitKinds=["OWNER","TENANT","RESIDENT"];
    public async Task<MyPortfolio> PortfolioAsync(CancellationToken token)
    {
        Identity();var today=Today;await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token):null;
        var entries=await Entries().OrderBy(e=>e.TenantId).ThenBy(e=>e.PropertyId).ThenBy(e=>e.Kind).ThenBy(e=>e.Id).Take(501).ToArrayAsync(token);
        if(entries.Length>500)throw new ArgumentException("Portfolio context limit exceeded.");
        var workspaceIds=entries.Where(e=>!UnitKinds.Contains(e.Kind)).Select(e=>e.Id).ToArray();
        var workspaces=workspaceIds.Length==0?[]:(await Project(Entries().Where(e=>(e.Kind=="MANAGEMENT" || e.Kind=="PLATFORM") && workspaceIds.Contains(e.Id))).ToArrayAsync(token)).OrderBy(c=>c.TenantName,StringComparer.Ordinal).ThenBy(c=>c.PropertyName,StringComparer.Ordinal).ThenBy(c=>c.Id).ToArray();
        var own=entries.Where(e=>UnitKinds.Contains(e.Kind)).ToArray();var units=own.Select(e=>e.UnitId!.Value).Distinct().ToArray();var persons=own.Select(e=>e.PersonId!.Value).Distinct().ToArray();
        // Other people's effective tenancies only derive RENTED_OUT; their identity never leaves this method.
        var others=await db.Set<UnitTenancy>().AsNoTracking().Where(r=>units.Contains(r.UnitId) && !persons.Contains(r.PersonId) && r.IsEnabled && (r.VerificationStatus==RelationVerificationStatus.VERIFIED || r.VerificationStatus==RelationVerificationStatus.LEGACY) && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today))
            .Select(r=>new{r.Id,r.PersonId,r.TenantId,r.PropertyId,r.UnitId,r.StartDate,r.EndDate,r.VerificationStatus,r.LeaseContractId}).ToArrayAsync(token);
        var leaseIds=own.Select(e=>e.LeaseContractId).Concat(others.Select(o=>o.LeaseContractId)).OfType<Guid>().Distinct().ToArray();
        var leases=(await db.Set<LeaseContract>().AsNoTracking().Where(c=>leaseIds.Contains(c.Id)).ToArrayAsync(token)).ToDictionary(c=>c.Id,c=>new PortfolioLease(c.Id,c.StartDate,c.EndDate,c.ContractReference,c.VerificationStatus.ToString()));
        var names=(await (from u in db.Units where units.Contains(u.Id) join p in db.Properties on u.PropertyId equals p.Id join b in db.Buildings on u.BuildingId equals (Guid?)b.Id into blocks from b in blocks.DefaultIfEmpty() select new{u.Id,u.Number,PropertyName=p.Name,BuildingId=(Guid?)b.Id,BuildingName=b.Name}).ToArrayAsync(token)).ToDictionary(n=>n.Id);
        var people=(await (from p in OwnPeople() where persons.Contains(p.Id) join t in db.Set<PersonTenant>() on p.TenantId equals t.Id select new{p.Id,p.TenantId,Tenant=t.Name,Name=(p.FirstName+" "+p.LastName).Trim()}).ToArrayAsync(token)).ToDictionary(p=>p.Id);
        if(tx!=null)await tx.CommitAsync(token);
        PortfolioLease? Lease(Guid? id)=>id is Guid key && leases.TryGetValue(key,out var lease)?lease:null;
        var result=new List<PortfolioPerson>();var access=new List<PortfolioUnitAccess>();
        foreach(var person in own.GroupBy(e=>e.PersonId!.Value).Where(g=>people.ContainsKey(g.Key)).OrderBy(g=>people[g.Key].Tenant,StringComparer.Ordinal).ThenBy(g=>g.Key))
        {
            var info=people[person.Key];var mine=person.Where(e=>names.ContainsKey(e.UnitId!.Value)).ToArray();var mineUnits=mine.Select(e=>e.UnitId).ToHashSet();
            var source=mine.Select(e=>{var n=names[e.UnitId!.Value];return new PortfolioRelationSource(e.Id,e.Kind,person.Key,e.PropertyId,n.PropertyName,n.BuildingId,n.BuildingName,e.UnitId!.Value,n.Number,e.Start!.Value,e.End,true,e.Verification!,Lease(e.LeaseContractId));})
                .Concat(others.Where(o=>o.TenantId==info.TenantId && mineUnits.Contains(o.UnitId)).Select(o=>{var n=names[o.UnitId];return new PortfolioRelationSource(o.Id,"TENANT",o.PersonId,o.PropertyId,n.PropertyName,n.BuildingId,n.BuildingName,o.UnitId,n.Number,o.StartDate,o.EndDate,true,o.VerificationStatus.ToString(),Lease(o.LeaseContractId));}));
            var properties=PersonPortfolio.Build(person.Key,source,today);result.Add(new(info.TenantId,info.Tenant,person.Key,info.Name,properties));
            foreach(var property in properties)foreach(var unit in property.Buildings.SelectMany(b=>b.Units))
                access.Add(new(info.TenantId,property.PropertyId,unit.UnitId,unit.Status,PortfolioCapabilities.ForUnit(unit.Capacities,unit.Status),unit.Relations.Select(r=>new PortfolioContextRef(r.Kind,r.RelationId,PortfolioCapabilities.ContextKey(info.TenantId,r.Kind,r.RelationId))).ToArray()));
        }
        // Clients key cached queries by context and drop them when this fingerprint changes.
        var fingerprint=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('\n',entries.Select(e=>$"{e.TenantId:D}|{e.Kind}|{e.Id:D}|{e.PropertyId:D}|{e.UnitId:D}|{e.Start:O}|{e.End:O}|{e.Verification}|{e.LeaseContractId:D}").Order(StringComparer.Ordinal).Prepend(today.ToString("O"))))));
        return new(today,fingerprint,result,access,workspaces);
    }
    public async Task<ActiveContext?> ResolveAsync(ActiveContextQuery q,CancellationToken token)
    {
        var unitKind=UnitKinds.Contains(q.Capacity);
        if(q.Capacity is not("PLATFORM" or "MANAGEMENT" or "OWNER" or "TENANT" or "RESIDENT") || q.RelationId==Guid.Empty || q.PropertyId==Guid.Empty || unitKind!=(q.UnitId!=null) || q.UnitId==Guid.Empty)throw new ArgumentException("Invalid active context.");
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token):null;
        var entry=await Entries().Where(e=>e.Id==q.RelationId && e.Kind==q.Capacity && e.PropertyId==q.PropertyId && e.UnitId==q.UnitId).SingleOrDefaultAsync(token);
        if(entry==null)return null;
        var context=await Project(Entries().Where(e=>e.Id==entry.Id && e.Kind==entry.Kind)).SingleAsync(token);string[] capabilities;
        if(unitKind)
        {
            var today=Today;var rented=entry.Kind=="OWNER" && await db.Set<UnitTenancy>().AnyAsync(r=>r.TenantId==entry.TenantId && r.PropertyId==entry.PropertyId && r.UnitId==entry.UnitId && r.PersonId!=entry.PersonId && r.IsEnabled && (r.VerificationStatus==RelationVerificationStatus.VERIFIED || r.VerificationStatus==RelationVerificationStatus.LEGACY) && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today),token);
            capabilities=PortfolioCapabilities.ForUnit([entry.Kind],rented?PersonPortfolio.RentedOut:"");
        }
        else capabilities=PortfolioCapabilities.ForWorkspace(context);
        if(tx!=null)await tx.CommitAsync(token);
        return new(context,PortfolioCapabilities.ContextKey(entry.TenantId,entry.Kind,entry.Id),capabilities);
    }
}
