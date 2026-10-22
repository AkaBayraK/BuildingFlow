using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
namespace BuildingFlow.Infrastructure;
public sealed class LeaseContractStore(BuildingFlowDbContext db,ManagementAccess access,ICurrentUserService actor,TimeProvider clock,IConfiguration config):ILeaseContractStore
{
    private const int MaxParties=10;
    private bool Writes=>config.GetValue<bool>("PersonRefactor:UnitRelationWritesEnabled");
    private DateOnly Today=>BusinessDate.Today(clock);
    private static HierarchyConflictException Conflict(ErrorDefinition? error=null)=>new((error??ErrorCatalog.RecordChanged).Create());
    private async Task<bool> Allowed(Guid tenant,Guid property,CancellationToken token)=>
        await access.PropertyScopes().AnyAsync(s=>s.TenantId==tenant && s.PropertyId==property,token) && (await access.EvaluateAsync(tenant,property,"MANAGER",token)).Allowed;
    private Task<bool> UnitExists(Guid property,Guid unit,CancellationToken token)=>
        db.Units.AnyAsync(u=>u.Id==unit && u.PropertyId==property && u.IsEnabled && (u.BuildingId==null || db.Buildings.Any(b=>b.Id==u.BuildingId && b.PropertyId==property && b.IsEnabled)),token);
    private IQueryable<LeaseContract> Contracts(Guid tenant,Guid property,Guid unit)=>db.Set<LeaseContract>().Where(c=>c.TenantId==tenant && c.PropertyId==property && c.UnitId==unit);
    private string Status(LeaseContract c)=>!c.IsEnabled?"DISABLED":c.EndDate<Today?"ENDED":c.StartDate>Today?"UPCOMING":"ACTIVE";
    private async Task<IReadOnlyList<LeaseContractRow>> Rows(LeaseContract[] contracts,CancellationToken token)
    {
        if(contracts.Length==0)return [];
        var ids=contracts.Select(c=>c.Id).ToArray();var unit=contracts[0].UnitId;var property=contracts[0].PropertyId;
        var number=await db.Units.Where(u=>u.Id==unit && u.PropertyId==property).Select(u=>u.Number).SingleAsync(token);
        var parties=await (from t in db.Set<UnitTenancy>().AsNoTracking() join p in db.Set<CentralPerson>() on new{t.TenantId,Id=t.PersonId} equals new{p.TenantId,p.Id} where t.LeaseContractId!=null && ids.Contains(t.LeaseContractId.Value)
            orderby t.StartDate,t.Id select new{t.LeaseContractId,Row=new LeasePartyRow(t.Id,t.PersonId,(p.FirstName+" "+p.LastName).Trim(),t.StartDate,t.EndDate,t.IsPrimaryContact,t.VerificationStatus.ToString(),t.Version)}).ToArrayAsync(token);
        var byContract=parties.ToLookup(p=>p.LeaseContractId!.Value,p=>p.Row);
        return contracts.Select(c=>new LeaseContractRow(c.Id,c.TenantId,c.PropertyId,c.UnitId,number,c.StartDate,c.EndDate,c.ContractReference,Status(c),c.VerificationStatus.ToString(),c.Version,byContract[c.Id].ToArray())).ToArray();
    }
    public async Task<PagedRows<LeaseContractRow>?> ListAsync(Guid tenant,Guid property,Guid unit,int page,int size,CancellationToken token)
    {
        if(page is <1 or >100000 || size is <1 or >100)throw new ArgumentException("Invalid pagination.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        if(!await Allowed(tenant,property,token) || !await db.Units.AnyAsync(u=>u.Id==unit && u.PropertyId==property,token))return null;
        var q=Contracts(tenant,property,unit).AsNoTracking();var count=await q.CountAsync(token);
        var rows=await Rows(await q.OrderByDescending(c=>c.StartDate).ThenBy(c=>c.Id).Skip((page-1)*size).Take(size).ToArrayAsync(token),token);await tx.CommitAsync(token);return new(rows,page,size,count);
    }
    public async Task<LeaseContractRow?> GetAsync(Guid tenant,Guid property,Guid unit,Guid contract,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        if(!await Allowed(tenant,property,token))return null;var row=await Contracts(tenant,property,unit).AsNoTracking().SingleOrDefaultAsync(c=>c.Id==contract,token);
        var result=row==null?null:(await Rows([row],token))[0];await tx.CommitAsync(token);return result;
    }
    public Task<LeaseContractMutation?> CreateAsync(Guid tenant,Guid property,Guid unit,CreateLeaseContractRequest r,CancellationToken token)
    {
        if(r.Parties is not {Count:>=1 and <=MaxParties} || r.Parties.Any(p=>p==null || p.PersonId==Guid.Empty) || r.Parties.Select(p=>p.PersonId).Distinct().Count()!=r.Parties.Count)throw new ArgumentException("Lease parties are required.");
        var draft=new LeaseContract(tenant,property,unit,r.StartDate,r.EndDate,r.ContractReference);
        return Write(tenant,property,unit,null,"CREATE",r.OperationId,r,async(_,verifier)=>
        {
            var existing=await Contracts(tenant,property,unit).ToArrayAsync(token);
            try{LeaseContractRules.ValidateUnit(existing.Append(draft));}catch(InvalidOperationException){throw Conflict(ErrorCatalog.LeaseOverlap);}
            draft.Verify(verifier,clock.GetUtcNow());db.Add(draft);
            foreach(var party in r.Parties)db.Add(await Party(tenant,draft,party,verifier,token));
            return draft;
        },token);
    }
    public Task<LeaseContractMutation?> AddPartyAsync(Guid tenant,Guid property,Guid unit,Guid contract,AddLeasePartyRequest r,CancellationToken token)
    {
        if(r.ExpectedVersion<1 || r.Party==null || r.Party.PersonId==Guid.Empty)throw new ArgumentException("Invalid lease party.");
        return Write(tenant,property,unit,contract,"ADD_PARTY",r.OperationId,r,async(c,verifier)=>
        {
            if(c!.Version!=r.ExpectedVersion || !c.IsEnabled)throw Conflict();
            if(await db.Set<UnitTenancy>().CountAsync(t=>t.LeaseContractId==c.Id,token)>=MaxParties)throw Conflict(ErrorCatalog.LeasePartyOutside);
            db.Add(await Party(tenant,c,r.Party,verifier,token));return c;
        },token);
    }
    public Task<LeaseContractMutation?> AttachAsync(Guid tenant,Guid property,Guid unit,Guid contract,AttachLeasePartyRequest r,CancellationToken token)
    {
        if(r.ExpectedVersion<1 || r.TenancyVersion<1 || r.TenancyId==Guid.Empty)throw new ArgumentException("Invalid lease attachment.");
        return Write(tenant,property,unit,contract,"ATTACH",r.OperationId,r,async(c,_)=>
        {
            if(c!.Version!=r.ExpectedVersion || !c.IsEnabled)throw Conflict();
            var tenancy=await db.Set<UnitTenancy>().SingleOrDefaultAsync(t=>t.TenantId==tenant && t.PropertyId==property && t.UnitId==unit && t.Id==r.TenancyId,token)??throw Conflict();
            if(tenancy.Version!=r.TenancyVersion || !tenancy.IsEnabled || tenancy.LeaseContractId!=null)throw Conflict();
            if(!c.Covers(tenancy.StartDate,tenancy.EndDate))throw Conflict(ErrorCatalog.LeasePartyOutside);
            tenancy.AttachLease(c);return c;
        },token);
    }
    public Task<LeaseContractMutation?> EndAsync(Guid tenant,Guid property,Guid unit,Guid contract,EndLeaseContractRequest r,CancellationToken token)
    {
        if(r.ExpectedVersion<1)throw new ArgumentException("Invalid lease closure.");
        return Write(tenant,property,unit,contract,"END",r.OperationId,r,async(c,_)=>
        {
            if(c!.Version!=r.ExpectedVersion || !c.IsEnabled || c.EndDate!=null)throw Conflict();
            if(r.EndDate<c.StartDate)throw new ArgumentException("Invalid end date.");
            // Open parties end with the contract; a party already ending later or starting after it blocks the closure.
            foreach(var party in await db.Set<UnitTenancy>().Where(t=>t.LeaseContractId==c.Id).ToArrayAsync(token))
            {
                if(party.StartDate>r.EndDate || party.EndDate>r.EndDate)throw Conflict(ErrorCatalog.LeasePartyOutside);
                if(party.EndDate==null)party.End(r.EndDate);
            }
            c.End(r.EndDate);return c;
        },token);
    }
    private async Task<UnitTenancy> Party(Guid tenant,LeaseContract contract,LeasePartyDraft draft,Guid verifier,CancellationToken token)
    {
        var person=await db.Set<CentralPerson>().SingleOrDefaultAsync(p=>p.TenantId==tenant && p.Id==draft.PersonId,token)??throw Conflict();
        if(!person.IsEnabled || await db.Set<LegacyPersonMap>().AnyAsync(m=>m.TenantId==tenant && m.PersonId==person.Id,token))throw Conflict();
        if(draft.StartDate==DateOnly.MinValue || draft.EndDate<draft.StartDate)throw new ArgumentException("Invalid party dates.");
        if(!contract.Covers(draft.StartDate,draft.EndDate))throw Conflict(ErrorCatalog.LeasePartyOutside);
        var party=new UnitTenancy(contract,person.Id,draft.StartDate,draft.EndDate,draft.IsPrimaryContact);party.Verify(verifier,clock.GetUtcNow());return party;
    }
    private async Task<LeaseContractMutation?> Write(Guid tenant,Guid property,Guid unit,Guid? contract,string action,Guid operation,object payload,Func<LeaseContract?,Guid,Task<LeaseContract>> apply,CancellationToken token)
    {
        if(operation==Guid.Empty)throw new ArgumentException("Operation identity required.");if(!Writes)return null;
        if(!await Allowed(tenant,property,token))return null;
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{tenant,property,unit,contract,action,payload}))));
        await using var tx=await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE",token);await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",token);
        if(!await Allowed(tenant,property,token))return null;
        var prior=await db.Set<LeaseContractOperation>().IgnoreQueryFilters().SingleOrDefaultAsync(o=>o.TenantId==tenant && o.ActorIssuer==actor.Issuer && o.ActorSubject==actor.UserId && o.OperationId==operation,token);
        if(prior!=null)
        {
            if(prior.IsDeleted || !prior.IsEnabled || prior.Fingerprint!=fingerprint)throw Conflict();
            var current=await Contracts(prior.TenantId,prior.PropertyId,prior.UnitId).AsNoTracking().SingleOrDefaultAsync(c=>c.Id==prior.ContractId,token)??throw Conflict();
            var replay=(await Rows([current],token))[0];await tx.CommitAsync(token);return new(replay,true,prior.AppliedVersion);
        }
        // Imported shadow data stays owned by the v1 writer, exactly as for unit relations.
        if((await PersonCenterStore.LegacyPropertiesAsync(db,[property],token)).Count!=0)throw Conflict();
        if(!await UnitExists(property,unit,token))return null;
        var verifier=await db.Set<PersonUserAccount>().Where(a=>a.IsEnabled && a.Issuer==actor.Issuer && a.Subject==actor.UserId).Select(a=>(Guid?)a.Id).SingleOrDefaultAsync(token)??throw new UnauthorizedAccessException();
        LeaseContract? target=null;
        if(contract!=null){target=await Contracts(tenant,property,unit).SingleOrDefaultAsync(c=>c.Id==contract,token);if(target==null)return null;}
        var record=await apply(target,verifier);
        if(contract!=null)db.Entry(record).Property(c=>c.IsEnabled).IsModified=true;
        var parent=await db.Properties.SingleAsync(p=>p.Id==property,token);db.Entry(parent).Property(p=>p.IsEnabled).IsModified=true;
        try{await db.SaveChangesAsync(token);}
        catch(DbUpdateException e) when(e.InnerException is PostgresException {SqlState:PostgresErrorCodes.ExclusionViolation}){throw Conflict(ErrorCatalog.LeaseOverlap);}
        catch(DbUpdateException e) when(e.InnerException is PostgresException {SqlState:PostgresErrorCodes.CheckViolation or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation}){throw Conflict();}
        db.Add(new LeaseContractOperation(tenant,property,unit,record.Id,action,actor.Issuer!,actor.UserId!,operation,fingerprint,record.Version));await db.SaveChangesAsync(token);
        try{await tx.CommitAsync(token);}catch(PostgresException e) when(e.SqlState==PostgresErrorCodes.CheckViolation){throw Conflict(ErrorCatalog.LeasePartyOutside);}
        db.ChangeTracker.Clear();
        return new((await Rows([await Contracts(tenant,property,unit).AsNoTracking().SingleAsync(c=>c.Id==record.Id,token)],token))[0],false,record.Version);
    }
}
