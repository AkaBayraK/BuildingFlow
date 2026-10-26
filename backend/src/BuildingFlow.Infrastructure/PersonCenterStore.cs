using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
namespace BuildingFlow.Infrastructure;
public sealed class PersonCenterStore(BuildingFlowDbContext db,ManagementAccess access,ICentralPeopleStore people,ICurrentUserService actor,TimeProvider clock,IConfiguration config):IPersonCenterStore
{
    private bool UnitWrites=>config.GetValue<bool>("PersonRefactor:UnitRelationWritesEnabled");
    private bool DutiesEnabled=>config.GetValue<bool>("PersonRefactor:AssignmentsEnabled");
    private DateOnly Today=>BusinessDate.Today(clock);
    private static void Page(int page,int size){if(page is <1 or >100000 || size is <1 or >100)throw new ArgumentException("Invalid pagination.");}
    private static void Conflict()=>throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
    private async Task<bool> PersonAllowed(Guid tenant,Guid person,CancellationToken token)=>await db.Set<CentralPerson>().AnyAsync(p=>p.TenantId==tenant && p.Id==person && access.PersonTenants().Any(t=>t.Id==tenant),token);
    public async Task<PagedRows<PersonWorkspace>> WorkspacesAsync(int page,int size,CancellationToken token)
    {
        Page(page,size);await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);var q=access.PersonTenants().AsNoTracking();var count=await q.CountAsync(token);var items=await q.OrderBy(t=>t.Name).ThenBy(t=>t.Id).Skip((page-1)*size).Take(size).Select(t=>new PersonWorkspace(t.Id,t.Name)).ToArrayAsync(token);await tx.CommitAsync(token);return new(items,page,size,count);
    }
    public Task<PersonWorkspace?> WorkspaceAsync(Guid tenant,CancellationToken token)=>access.PersonTenants().Where(t=>t.Id==tenant).Select(t=>new PersonWorkspace(t.Id,t.Name)).SingleOrDefaultAsync(token);
    private Task<HashSet<Guid>> LegacyProperties(Guid[] properties,CancellationToken token)=>LegacyPropertiesAsync(db,properties,token);
    internal static async Task<HashSet<Guid>> LegacyPropertiesAsync(BuildingFlowDbContext db,Guid[] properties,CancellationToken token)
    {
        // Isolated historical query: IgnoreQueryFilters cannot disable the authorization query.
        return (await db.People.IgnoreQueryFilters().Where(p=>properties.Contains(p.PropertyId)).Select(p=>p.PropertyId)
            .Union(db.UnitOccupancies.IgnoreQueryFilters().Where(p=>properties.Contains(p.PropertyId)).Select(p=>p.PropertyId)).Distinct().ToArrayAsync(token)).ToHashSet();
    }
    public async Task<PagedRows<PersonPropertyChoice>?> PropertiesAsync(Guid tenant,int page,int size,CancellationToken token)
    {
        Page(page,size);if(!await access.PersonTenants().AnyAsync(t=>t.Id==tenant,token))return null;
        var administrative=access.PropertyScopes("SITE_ADMIN");var q=from scope in access.PropertyScopes() join p in db.Properties on scope.PropertyId equals p.Id where scope.TenantId==tenant select new{p.Id,p.Name,CanManage=administrative.Any(s=>s.TenantId==tenant && s.PropertyId==p.Id)};
        var count=await q.CountAsync(token);var rows=await q.OrderBy(p=>p.Name).ThenBy(p=>p.Id).Skip((page-1)*size).Take(size).ToArrayAsync(token);var legacy=await LegacyProperties(rows.Select(r=>r.Id).ToArray(),token);
        return new(rows.Select(r=>new PersonPropertyChoice(r.Id,r.Name,DutiesEnabled && r.CanManage,UnitWrites && !legacy.Contains(r.Id))).ToArray(),page,size,count);
    }
    public async Task<PersonCenterRecord?> DetailAsync(Guid tenant,Guid person,CancellationToken token)
    {
        var value=await people.GetAsync(tenant,person,token);if(value==null)return null;
        var map=await db.Set<LegacyPersonMap>().AsNoTracking().SingleOrDefaultAsync(m=>m.TenantId==tenant && m.PersonId==person,token);var scopes=access.PropertyScopes();var permitted=map!=null && await scopes.AnyAsync(s=>s.TenantId==tenant && s.PropertyId==map.PropertyId,token);
        var changed=map!=null && !await db.People.AnyAsync(p=>p.Id==map.LegacyPersonId && p.PropertyId==map.PropertyId && p.Version==map.SourceVersion,token);
        var propertyIds=await scopes.Where(s=>s.TenantId==tenant).Select(s=>s.PropertyId).ToArrayAsync(token);var legacy=await LegacyProperties(propertyIds,token);
        var canManage=DutiesEnabled && value.IsEnabled && !changed && await access.PropertyScopes("SITE_ADMIN").AnyAsync(s=>s.TenantId==tenant,token);
        return new(value,map==null,permitted?map!.PropertyId:null,permitted?map!.LegacyPersonId:null,map!=null,changed,DutiesEnabled,UnitWrites && map==null && value.IsEnabled && propertyIds.Any(id=>!legacy.Contains(id)),canManage);
    }
    public async Task<PersonAccountStatus?> AccountAsync(Guid tenant,Guid person,CancellationToken token)
    {
        if(!await PersonAllowed(tenant,person,token))return null;var now=clock.GetUtcNow();
        var linked=await db.Set<CentralPersonAccountLink>().AsNoTracking().Where(l=>l.TenantId==tenant && l.PersonId==person && l.IsEnabled && l.EndedAt==null && l.LinkedAt<=now &&
            db.Set<PersonUserAccount>().Any(u=>u.Id==l.UserAccountId && u.IsEnabled && db.Set<PersonTenantMembership>().Any(m=>m.TenantId==tenant && m.UserAccountId==u.Id && m.IsEnabled))).Select(l=>(DateTimeOffset?)l.LinkedAt).SingleOrDefaultAsync(token);
        var code=linked!=null?null:await db.Set<CentralPersonLinkCode>().AsNoTracking().Where(c=>c.TenantId==tenant && c.PersonId==person && c.IsEnabled && c.UsedAt==null && c.RevokedAt==null && c.ExpiresAt>now).Select(c=>new{c.Hint,c.ExpiresAt}).SingleOrDefaultAsync(token);
        return new(linked!=null,linked,code?.Hint,code?.ExpiresAt);
    }
    private async Task<bool> ChoiceAllowed(Guid tenant,Guid property,CancellationToken token)=>await access.PropertyScopes().AnyAsync(s=>s.TenantId==tenant && s.PropertyId==property,token) && (await access.EvaluateAsync(tenant,property,"MANAGER",token)).Allowed;
    public async Task<PagedRows<PersonBuildingChoice>?> BuildingsAsync(Guid tenant,Guid property,int page,int size,CancellationToken token)
    {
        Page(page,size);if(!await ChoiceAllowed(tenant,property,token))return null;var q=db.Buildings.Where(b=>b.PropertyId==property && b.IsEnabled);
        var count=await q.CountAsync(token);return new(await q.OrderBy(b=>b.Name).ThenBy(b=>b.Id).Skip((page-1)*size).Take(size).Select(b=>new PersonBuildingChoice(b.Id,b.Name)).ToArrayAsync(token),page,size,count);
    }
    public async Task<PagedRows<PersonUnitChoice>?> UnitsAsync(Guid tenant,Guid property,Guid? building,int page,int size,CancellationToken token)
    {
        Page(page,size);if(!await ChoiceAllowed(tenant,property,token))return null;
        if(building!=null && !await db.Buildings.AnyAsync(b=>b.Id==building && b.PropertyId==property && b.IsEnabled,token))return null;
        var q=db.Units.Where(u=>u.PropertyId==property && u.IsEnabled && (building==null || u.BuildingId==building) && (u.BuildingId==null || db.Buildings.Any(b=>b.Id==u.BuildingId && b.PropertyId==property && b.IsEnabled)));
        var count=await q.CountAsync(token);return new(await q.OrderBy(u=>u.Number).ThenBy(u=>u.Id).Skip((page-1)*size).Take(size).Select(u=>new PersonUnitChoice(u.Id,u.Number,u.BuildingId)).ToArrayAsync(token),page,size,count);
    }
    private sealed class Link { public Guid Id {get;set;} public Guid TenantId {get;set;} public Guid PropertyId {get;set;} public Guid UnitId {get;set;} public Guid PersonId {get;set;} public string Kind {get;set;}=null!; public DateOnly StartDate {get;set;} public DateOnly? EndDate {get;set;} public bool IsPrimaryContact {get;set;} public long? N {get;set;} public long? D {get;set;} public long Version {get;set;} public bool Enabled {get;set;} public bool Imported {get;set;} public RelationVerificationStatus Verification {get;set;} public Guid? LeaseContractId {get;set;} }
    private IQueryable<Link> Links(Guid tenant,Guid person)=>db.Set<UnitOwnership>().Where(r=>r.TenantId==tenant && r.PersonId==person).Select(r=>new Link {Id=r.Id,TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,PersonId=r.PersonId,Kind="OWNER",StartDate=r.StartDate,EndDate=r.EndDate,IsPrimaryContact=r.IsPrimaryContact,N=r.ShareNumerator,D=r.ShareDenominator,Version=r.Version,Enabled=r.IsEnabled,Imported=db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==tenant && m.OwnershipId==r.Id),Verification=r.VerificationStatus,LeaseContractId=null})
        .Concat(db.Set<UnitTenancy>().Where(r=>r.TenantId==tenant && r.PersonId==person).Select(r=>new Link {Id=r.Id,TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,PersonId=r.PersonId,Kind="TENANT",StartDate=r.StartDate,EndDate=r.EndDate,IsPrimaryContact=r.IsPrimaryContact,N=null,D=null,Version=r.Version,Enabled=r.IsEnabled,Imported=db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==tenant && m.TenancyId==r.Id),Verification=r.VerificationStatus,LeaseContractId=r.LeaseContractId}))
        .Concat(db.Set<UnitResidency>().Where(r=>r.TenantId==tenant && r.PersonId==person).Select(r=>new Link {Id=r.Id,TenantId=r.TenantId,PropertyId=r.PropertyId,UnitId=r.UnitId,PersonId=r.PersonId,Kind="RESIDENT",StartDate=r.StartDate,EndDate=r.EndDate,IsPrimaryContact=r.IsPrimaryContact,N=null,D=null,Version=r.Version,Enabled=r.IsEnabled,Imported=db.Set<LegacyOccupancyMap>().Any(m=>m.TenantId==tenant && m.ResidencyId==r.Id),Verification=r.VerificationStatus,LeaseContractId=null}));
    private async Task<PersonUnitRelationRow> Row(Link r,CancellationToken token)
    {
        var property=await db.Properties.Where(p=>p.Id==r.PropertyId).Select(p=>p.Name).SingleAsync(token);var unit=await db.Units.Where(u=>u.PropertyId==r.PropertyId && u.Id==r.UnitId).Select(u=>u.Number).SingleOrDefaultAsync(token)??"";
        return new(r.Id,r.TenantId,r.PropertyId,property,r.UnitId,unit,r.PersonId,r.Kind,r.StartDate,r.EndDate,r.IsPrimaryContact,r.N?.ToString(CultureInfo.InvariantCulture),r.D?.ToString(CultureInfo.InvariantCulture),r.Version,!r.Enabled?"DISABLED":r.EndDate<Today?"ENDED":r.StartDate>Today?"UPCOMING":"ACTIVE",UnitWrites && !r.Imported && r.Enabled && r.EndDate==null,r.Verification.ToString(),r.LeaseContractId);
    }
    public async Task<PagedRows<PersonUnitRelationRow>?> RelationsAsync(Guid tenant,Guid person,int page,int size,CancellationToken token)
    {
        Page(page,size);if(!await PersonAllowed(tenant,person,token))return null;var scopes=access.PropertyScopes();var q=Links(tenant,person).Where(r=>scopes.Any(s=>s.TenantId==tenant && s.PropertyId==r.PropertyId));var count=await q.CountAsync(token);
        var rows=await (from r in q join p in db.Properties on r.PropertyId equals p.Id orderby r.StartDate descending,r.Id select new{Link=r,Property=p.Name,Unit=db.Units.Where(u=>u.Id==r.UnitId && u.PropertyId==r.PropertyId).Select(u=>u.Number).FirstOrDefault()}).Skip((page-1)*size).Take(size).ToArrayAsync(token);
        return new(rows.Select(x=>new PersonUnitRelationRow(x.Link.Id,tenant,x.Link.PropertyId,x.Property,x.Link.UnitId,x.Unit??"",person,x.Link.Kind,x.Link.StartDate,x.Link.EndDate,x.Link.IsPrimaryContact,x.Link.N?.ToString(CultureInfo.InvariantCulture),x.Link.D?.ToString(CultureInfo.InvariantCulture),x.Link.Version,!x.Link.Enabled?"DISABLED":x.Link.EndDate<Today?"ENDED":x.Link.StartDate>Today?"UPCOMING":"ACTIVE",UnitWrites && !x.Link.Imported && x.Link.Enabled && x.Link.EndDate==null,x.Link.Verification.ToString(),x.Link.LeaseContractId)).ToArray(),page,size,count);
    }
    public async Task<PersonUnitRelationRow?> RelationAsync(Guid tenant,Guid person,string kind,Guid id,CancellationToken token)
    {
        if(kind is not("OWNER" or "TENANT" or "RESIDENT"))throw new ArgumentException("Invalid relationship kind.");
        if(!await PersonAllowed(tenant,person,token))return null;var scopes=access.PropertyScopes();var row=await Links(tenant,person).SingleOrDefaultAsync(r=>r.Id==id && r.Kind==kind && scopes.Any(s=>s.TenantId==tenant && s.PropertyId==r.PropertyId),token);return row==null?null:await Row(row,token);
    }
    public async Task<PagedRows<PersonDutyRow>?> DutiesAsync(Guid tenant,Guid person,int page,int size,CancellationToken token)
    {
        Page(page,size);if(!DutiesEnabled || !await PersonAllowed(tenant,person,token))return null;var scopes=access.PropertyScopes();var administrative=access.PropertyScopes("SITE_ADMIN");
        var q=from a in db.Set<ManagementAssignment>() join p in db.Properties on a.PropertyId equals p.Id where a.TenantId==tenant && a.PersonId==person && scopes.Any(s=>s.TenantId==tenant && s.PropertyId==p.Id) select new{Assignment=a,Property=p.Name,CanManage=administrative.Any(s=>s.TenantId==tenant && s.PropertyId==p.Id)};
        var count=await q.CountAsync(token);var rows=await q.OrderByDescending(r=>r.Assignment.StartDate).ThenBy(r=>r.Assignment.Id).Skip((page-1)*size).Take(size).ToArrayAsync(token);
        return new(rows.Select(x=>new PersonDutyRow(new(x.Assignment.Id,tenant,x.Assignment.PropertyId,person,x.Assignment.Role,x.Assignment.StartDate,x.Assignment.EndDate,x.Assignment.CancelledAt!=null?"CANCELLED":!x.Assignment.IsEnabled?"DISABLED":x.Assignment.EndDate<Today?"ENDED":x.Assignment.StartDate>Today?"UPCOMING":"ACTIVE",x.Assignment.Version,x.Assignment.CancelledAt),x.Property,x.CanManage && x.Assignment.IsEnabled && (x.Assignment.Role!="SITE_ADMIN" || actor.IsInRole("SUPER_ADMIN")))).ToArray(),page,size,count);
    }
    private static string Mask(string identity)=>new string('*',Math.Max(0,identity.Length-4))+identity[^Math.Min(4,identity.Length)..];
    public async Task<PagedRows<PersonDirectoryRow>> DirectoryAsync(PersonDirectoryQuery q,CancellationToken token)
    {
        if(q.Page is <1 or >100000 || q.PageSize is <1 or >100 || q.Search?.Length>200 || q.SortBy is not("name" or "createdAt") || q.SortDirection is not("asc" or "desc") || q.Relation is not(null or "OWNER" or "TENANT" or "RESIDENT"))throw new ArgumentException("Invalid people query.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        var tenants=access.PersonTenants().Where(t=>q.TenantId==null || t.Id==q.TenantId);var scopes=access.PropertyScopes().Where(s=>q.PropertyId==null || s.PropertyId==q.PropertyId);var today=Today;
        var rows=db.Set<CentralPerson>().AsNoTracking().Where(p=>tenants.Any(t=>t.Id==p.TenantId));
        if(q.PropertyId!=null || q.Relation!=null)
        {
            // EXISTS keeps one row per person however many units or capacities match.
            var owners=db.Set<UnitOwnership>().Where(r=>r.IsEnabled && r.VerificationStatus!=RelationVerificationStatus.REJECTED && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId));
            var tenancies=db.Set<UnitTenancy>().Where(r=>r.IsEnabled && r.VerificationStatus!=RelationVerificationStatus.REJECTED && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId));
            var residents=db.Set<UnitResidency>().Where(r=>r.IsEnabled && r.VerificationStatus!=RelationVerificationStatus.REJECTED && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today) && scopes.Any(s=>s.TenantId==r.TenantId && s.PropertyId==r.PropertyId));
            bool owner=q.Relation is null or "OWNER",tenant=q.Relation is null or "TENANT",resident=q.Relation is null or "RESIDENT",duty=q.Relation==null;
            rows=rows.Where(p=>owner && owners.Any(r=>r.TenantId==p.TenantId && r.PersonId==p.Id) || tenant && tenancies.Any(r=>r.TenantId==p.TenantId && r.PersonId==p.Id) || resident && residents.Any(r=>r.TenantId==p.TenantId && r.PersonId==p.Id) ||
                duty && db.Set<ManagementAssignment>().Any(a=>a.TenantId==p.TenantId && a.PersonId==p.Id && a.IsEnabled && a.CancelledAt==null && (a.EndDate==null || a.EndDate>=today) && scopes.Any(s=>s.TenantId==a.TenantId && s.PropertyId==a.PropertyId)));
        }
        if(!string.IsNullOrWhiteSpace(q.Search)){var pattern="%"+q.Search.Trim().Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_")+"%";rows=rows.Where(p=>EF.Functions.ILike(p.FirstName+" "+p.LastName,pattern,"\\") || EF.Functions.ILike(p.PhoneNumber??"",pattern,"\\") || EF.Functions.ILike(p.Email??"",pattern,"\\"));}
        var count=await rows.CountAsync(token);var desc=q.SortDirection=="desc";
        var sorted=q.SortBy=="createdAt"?(desc?rows.OrderByDescending(p=>p.CreatedAt):rows.OrderBy(p=>p.CreatedAt)):(desc?rows.OrderByDescending(p=>p.LastName).ThenByDescending(p=>p.FirstName):rows.OrderBy(p=>p.LastName).ThenBy(p=>p.FirstName));
        var page=await (from p in sorted.ThenBy(p=>p.Id).Skip((q.Page-1)*q.PageSize).Take(q.PageSize) join t in db.Set<PersonTenant>() on p.TenantId equals t.Id select new{p.Id,p.TenantId,Tenant=t.Name,Name=(p.FirstName+" "+p.LastName).Trim(),p.Kind,p.IdentityNumber,p.IsEnabled,p.Version,p.CreatedAt}).ToArrayAsync(token);
        var photos=await PhotosAsync(page.Select(p=>p.Id).ToArray(),token);
        await tx.CommitAsync(token);
        return new(page.Select(p=>new PersonDirectoryRow(p.Id,p.TenantId,p.Tenant,p.Name,p.Kind,Mask(p.IdentityNumber),p.IsEnabled,p.Version,p.CreatedAt,photos.TryGetValue(p.Id,out var photo)?photo:null)).ToArray(),q.Page,q.PageSize,count);
    }
    public async Task<string?> PhotoAccountAsync(Guid tenant,Guid person,CancellationToken token)=>
        await PersonAllowed(tenant,person,token) && (await PhotoAccountsAsync([person],token)).TryGetValue(person,out var user)?user:null;
    // İŞ19 parity: the photo is the linked account's own; a migrated person falls back to the legacy record's account only inside the caller's Property scope.
    private async Task<Dictionary<Guid,string>> PhotoAccountsAsync(Guid[] ids,CancellationToken token)
    {
        var scopes=access.PropertyScopes();
        var linked=await (from l in db.Set<CentralPersonAccountLink>() where ids.Contains(l.PersonId) && l.IsEnabled && l.EndedAt==null join a in db.Set<PersonUserAccount>() on l.UserAccountId equals a.Id select new{l.PersonId,a.Subject}).AsNoTracking().ToArrayAsync(token);
        var legacy=await (from m in db.Set<LegacyPersonMap>() where ids.Contains(m.PersonId) && scopes.Any(s=>s.TenantId==m.TenantId && s.PropertyId==m.PropertyId) join p in db.Set<Person>() on m.LegacyPersonId equals p.Id where p.LinkedUserId!=null orderby m.CreatedAt select new{m.PersonId,Subject=p.LinkedUserId!}).AsNoTracking().ToArrayAsync(token);
        var users=new Dictionary<Guid,string>();
        foreach(var row in linked.Concat(legacy))users.TryAdd(row.PersonId,row.Subject);
        return users;
    }
    private async Task<Dictionary<Guid,Guid>> PhotosAsync(Guid[] ids,CancellationToken token)
    {
        var accounts=await PhotoAccountsAsync(ids,token);var users=accounts.Values.Distinct().ToArray();
        var photos=await db.Set<MobileProfile>().AsNoTracking().Where(p=>users.Contains(p.OwnerUserId) && p.IsEnabled && p.PhotoId!=null && db.Set<MobileProfilePhoto>().Any(x=>x.Id==p.PhotoId && x.OwnerUserId==p.OwnerUserId && x.State=="CURRENT" && x.IsEnabled))
            .ToDictionaryAsync(p=>p.OwnerUserId,p=>p.PhotoId!.Value,StringComparer.Ordinal,token);
        return accounts.Where(a=>photos.ContainsKey(a.Value)).ToDictionary(a=>a.Key,a=>photos[a.Value]);
    }
    public async Task<PersonPortfolioView?> PortfolioAsync(Guid tenant,Guid person,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        if(!await PersonAllowed(tenant,person,token))return null;var scopes=access.PropertyScopes();var today=Today;
        var own=await Links(tenant,person).Where(r=>scopes.Any(s=>s.TenantId==tenant && s.PropertyId==r.PropertyId)).AsNoTracking().ToArrayAsync(token);
        var units=own.Select(r=>r.UnitId).Distinct().ToArray();
        // Other people's tenancies only derive RENTED_OUT; their identity never leaves this method.
        var others=await db.Set<UnitTenancy>().AsNoTracking().Where(r=>r.TenantId==tenant && units.Contains(r.UnitId) && r.PersonId!=person && scopes.Any(s=>s.TenantId==tenant && s.PropertyId==r.PropertyId))
            .Select(r=>new{r.Id,r.PersonId,r.PropertyId,r.UnitId,r.StartDate,r.EndDate,r.IsEnabled,r.VerificationStatus,r.LeaseContractId}).ToArrayAsync(token);
        var leaseIds=own.Select(r=>r.LeaseContractId).Concat(others.Select(r=>r.LeaseContractId)).OfType<Guid>().Distinct().ToArray();
        var leases=(await db.Set<LeaseContract>().AsNoTracking().Where(c=>c.TenantId==tenant && leaseIds.Contains(c.Id)).ToArrayAsync(token)).ToDictionary(c=>c.Id,c=>new PortfolioLease(c.Id,c.StartDate,c.EndDate,c.ContractReference,c.VerificationStatus.ToString()));
        var names=(await (from u in db.Units where units.Contains(u.Id) join p in db.Properties on u.PropertyId equals p.Id join b in db.Buildings on u.BuildingId equals (Guid?)b.Id into blocks from b in blocks.DefaultIfEmpty() select new{u.Id,u.Number,PropertyName=p.Name,BuildingId=(Guid?)b.Id,BuildingName=b.Name}).ToArrayAsync(token)).ToDictionary(n=>n.Id);
        await tx.CommitAsync(token);
        PortfolioLease? Lease(Guid? id)=>id is Guid key && leases.TryGetValue(key,out var lease)?lease:null;
        var source=own.Where(r=>names.ContainsKey(r.UnitId)).Select(r=>{var n=names[r.UnitId];return new PortfolioRelationSource(r.Id,r.Kind,person,r.PropertyId,n.PropertyName,n.BuildingId,n.BuildingName,r.UnitId,n.Number,r.StartDate,r.EndDate,r.Enabled,r.Verification.ToString(),Lease(r.LeaseContractId));})
            .Concat(others.Where(r=>names.ContainsKey(r.UnitId)).Select(r=>{var n=names[r.UnitId];return new PortfolioRelationSource(r.Id,"TENANT",r.PersonId,r.PropertyId,n.PropertyName,n.BuildingId,n.BuildingName,r.UnitId,n.Number,r.StartDate,r.EndDate,r.IsEnabled,r.VerificationStatus.ToString(),Lease(r.LeaseContractId));}));
        return new(tenant,person,today,PersonPortfolio.Build(person,source,today));
    }
    private static long? Share(string? value){if(value==null)return null;if(!long.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var n) || n<=0)throw new ArgumentException("Invalid ownership share.");return n;}
    private static BaseEntity NewRelation(Guid tenant,Guid person,CreatePersonUnitRelationRequest r)=>r.Kind switch
    {
        "OWNER"=>new UnitOwnership(tenant,r.PropertyId,r.UnitId,person,r.StartDate,r.EndDate,r.IsPrimaryContact,Share(r.ShareNumerator),Share(r.ShareDenominator)),
        "TENANT" when r.ShareNumerator==null && r.ShareDenominator==null=>new UnitTenancy(tenant,r.PropertyId,r.UnitId,person,r.StartDate,r.EndDate,r.IsPrimaryContact),
        "RESIDENT" when r.ShareNumerator==null && r.ShareDenominator==null=>new UnitResidency(tenant,r.PropertyId,r.UnitId,person,r.StartDate,r.EndDate,r.IsPrimaryContact),
        _=>throw new ArgumentException("Invalid relationship kind.")
    };
    public Task<PersonUnitRelationMutation?> CreateRelationAsync(Guid tenant,Guid person,CreatePersonUnitRelationRequest r,CancellationToken token)
    {var row=NewRelation(tenant,person,r);return Write(tenant,person,r.PropertyId,r.Kind,null,r.OperationId,r,0,row,null,token);}
    public async Task<PersonUnitRelationMutation?> EndRelationAsync(Guid tenant,Guid person,string kind,Guid id,EndPersonUnitRelationRequest r,CancellationToken token)
    {
        if(r.ExpectedVersion<1 || kind is not("OWNER" or "TENANT" or "RESIDENT"))throw new ArgumentException("Invalid relationship closure.");
        if(!await PersonAllowed(tenant,person,token))return null;var link=await Links(tenant,person).SingleOrDefaultAsync(l=>l.Kind==kind && l.Id==id,token);if(link==null)return null;
        return await Write(tenant,person,link.PropertyId,kind,id,r.OperationId,r,r.ExpectedVersion,null,r.EndDate,token);
    }
    private async Task<PersonUnitRelationMutation?> Write(Guid tenant,Guid person,Guid property,string kind,Guid? id,Guid operation,object payload,long expected,BaseEntity? create,DateOnly? end,CancellationToken token)
    {
        if(operation==Guid.Empty)throw new ArgumentException("Operation identity required.");if(!UnitWrites)return null;
        if(!await PersonAllowed(tenant,person,token) || !(await access.EvaluateAsync(tenant,property,"MANAGER",token)).Allowed)return null;
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{tenant,person,property,kind,id,payload}))));
        await using var tx=await db.Database.BeginTransactionAsync(token);await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE",token);await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",token);
        if(!await PersonAllowed(tenant,person,token) || !(await access.EvaluateAsync(tenant,property,"MANAGER",token)).Allowed)return null;
        var prior=await db.Set<PersonUnitOperation>().IgnoreQueryFilters().SingleOrDefaultAsync(o=>o.TenantId==tenant && o.ActorIssuer==actor.Issuer && o.ActorSubject==actor.UserId && o.OperationId==operation,token);
        if(prior!=null){if(prior.IsDeleted || !prior.IsEnabled || prior.Fingerprint!=fingerprint)Conflict();var current=await Links(tenant,person).SingleOrDefaultAsync(l=>l.Kind==prior.Kind && l.Id==prior.RelationId,token);if(current==null)Conflict();return new(await Row(current!,token),true,prior.AppliedVersion);}
        // Imported shadow data remains owned by the v1 writer. Never create a second writer for legacy Property data.
        if((await LegacyProperties([property],token)).Count!=0 || await db.Set<LegacyPersonMap>().AnyAsync(m=>m.TenantId==tenant && m.PersonId==person,token))Conflict();
        BaseEntity record;
        if(create!=null)
        {
            var personRecord=await db.Set<CentralPerson>().SingleAsync(p=>p.TenantId==tenant && p.Id==person,token);if(!personRecord.IsEnabled || kind=="RESIDENT" && personRecord.Kind!="NATURAL")Conflict();
            var relation=(IPersonUnitRelation)create;if(!await db.Units.AnyAsync(u=>u.Id==relation.UnitId && u.PropertyId==property && u.IsEnabled && (u.BuildingId==null || db.Buildings.Any(b=>b.Id==u.BuildingId && b.PropertyId==property && b.IsEnabled)),token))return null;
            var verifier=await db.Set<PersonUserAccount>().Where(a=>a.IsEnabled && a.Issuer==actor.Issuer && a.Subject==actor.UserId).Select(a=>(Guid?)a.Id).SingleOrDefaultAsync(token)??throw new UnauthorizedAccessException();
            ((VerifiableRecord)create).Verify(verifier,clock.GetUtcNow());record=create;db.Add(record);
        }
        else
        {
            record=(kind switch{"OWNER"=>(BaseEntity?)await db.Set<UnitOwnership>().SingleOrDefaultAsync(r=>r.TenantId==tenant && r.PropertyId==property && r.PersonId==person && r.Id==id,token),"TENANT"=>await db.Set<UnitTenancy>().SingleOrDefaultAsync(r=>r.TenantId==tenant && r.PropertyId==property && r.PersonId==person && r.Id==id,token),_=>await db.Set<UnitResidency>().SingleOrDefaultAsync(r=>r.TenantId==tenant && r.PropertyId==property && r.PersonId==person && r.Id==id,token)})!;
            if(record==null)return null;var relation=(IPersonUnitRelation)record;if(record.Version!=expected || !record.IsEnabled || relation.EndDate!=null || end<relation.StartDate)Conflict();
            switch(record){case UnitOwnership owner:owner.End(end!.Value);break;case UnitTenancy tenantLink:tenantLink.End(end!.Value);break;case UnitResidency resident:resident.End(end!.Value);break;}
        }
        var parent=await db.Properties.SingleAsync(p=>p.Id==property,token);db.Entry(parent).Property(p=>p.IsEnabled).IsModified=true;
        try{await db.SaveChangesAsync(token);}catch(DbUpdateException e) when(e.InnerException is PostgresException {SqlState:PostgresErrorCodes.CheckViolation or PostgresErrorCodes.UniqueViolation}){Conflict();throw;}
        db.Add(new PersonUnitOperation(tenant,property,person,record.Id,kind,actor.Issuer!,actor.UserId!,operation,fingerprint,record.Version));await db.SaveChangesAsync(token);await tx.CommitAsync(token);
        return new(await Row(await Links(tenant,person).AsNoTracking().SingleAsync(l=>l.Id==record.Id && l.Kind==kind,token),token),false,record.Version);
    }
    internal static async Task<bool> HasCurrentNative(BuildingFlowDbContext db,Guid? property,Guid? unit,Guid? person,DateOnly today,CancellationToken token)=>
        await db.Set<UnitOwnership>().AnyAsync(r=>(property==null || r.PropertyId==property) && (unit==null || r.UnitId==unit) && (person==null || r.PersonId==person) && r.IsEnabled && (r.EndDate==null || r.EndDate>=today) && !db.Set<LegacyOccupancyMap>().Any(m=>m.OwnershipId==r.Id),token) ||
        await db.Set<UnitTenancy>().AnyAsync(r=>(property==null || r.PropertyId==property) && (unit==null || r.UnitId==unit) && (person==null || r.PersonId==person) && r.IsEnabled && (r.EndDate==null || r.EndDate>=today) && !db.Set<LegacyOccupancyMap>().Any(m=>m.TenancyId==r.Id),token) ||
        await db.Set<UnitResidency>().AnyAsync(r=>(property==null || r.PropertyId==property) && (unit==null || r.UnitId==unit) && (person==null || r.PersonId==person) && r.IsEnabled && (r.EndDate==null || r.EndDate>=today) && !db.Set<LegacyOccupancyMap>().Any(m=>m.ResidencyId==r.Id),token);
    internal static async Task<bool> NativeHistory(BuildingFlowDbContext db,Guid property,CancellationToken token)=>
        await db.Set<UnitOwnership>().AnyAsync(r=>r.PropertyId==property && !db.Set<LegacyOccupancyMap>().Any(m=>m.OwnershipId==r.Id),token) ||
        await db.Set<UnitTenancy>().AnyAsync(r=>r.PropertyId==property && !db.Set<LegacyOccupancyMap>().Any(m=>m.TenancyId==r.Id),token) ||
        await db.Set<UnitResidency>().AnyAsync(r=>r.PropertyId==property && !db.Set<LegacyOccupancyMap>().Any(m=>m.ResidencyId==r.Id),token);
}
