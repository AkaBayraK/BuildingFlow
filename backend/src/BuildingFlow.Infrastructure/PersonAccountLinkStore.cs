using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;
public sealed class PersonAccountLinkStore(BuildingFlowDbContext db,ManagementAccess access,ICurrentUserService actor,TimeProvider clock):IPersonAccountLinkStore
{
    private static HierarchyConflictException Unavailable()=>new(ErrorCatalog.PersonLinkUnavailable.Create());
    private IQueryable<PersonUserAccount> Actor()=>db.Set<PersonUserAccount>().Where(a=>a.Issuer==actor.Issuer && a.Subject==actor.UserId);
    private IQueryable<CentralPersonAccountLink> ActiveLinks(Guid tenant,Guid person)=>db.Set<CentralPersonAccountLink>().Where(l=>l.TenantId==tenant && l.PersonId==person && l.IsEnabled && l.EndedAt==null);
    private IQueryable<CentralPersonLinkCode> OpenCodes(Guid tenant,Guid person)=>db.Set<CentralPersonLinkCode>().Where(c=>c.TenantId==tenant && c.PersonId==person && c.UsedAt==null && c.RevokedAt==null);
    private Task<bool> Managed(Guid tenant,Guid person,CancellationToken token)=>db.Set<CentralPerson>().AnyAsync(p=>p.TenantId==tenant && p.Id==person && access.PersonTenants().Any(t=>t.Id==tenant),token);
    // A code is only issued for someone this manager already relates to a unit or duty; the link itself grants nothing more.
    private async Task<bool> HasCurrentScope(Guid tenant,Guid person,CancellationToken token)
    {
        var today=BusinessDate.Today(clock);var scopes=access.PropertyScopes();
        return await db.Set<UnitOwnership>().AnyAsync(r=>r.TenantId==tenant && r.PersonId==person && r.IsEnabled && r.VerificationStatus!=RelationVerificationStatus.REJECTED && (r.EndDate==null || r.EndDate>=today) && scopes.Any(s=>s.TenantId==tenant && s.PropertyId==r.PropertyId),token) ||
            await db.Set<UnitTenancy>().AnyAsync(r=>r.TenantId==tenant && r.PersonId==person && r.IsEnabled && r.VerificationStatus!=RelationVerificationStatus.REJECTED && (r.EndDate==null || r.EndDate>=today) && scopes.Any(s=>s.TenantId==tenant && s.PropertyId==r.PropertyId),token) ||
            await db.Set<UnitResidency>().AnyAsync(r=>r.TenantId==tenant && r.PersonId==person && r.IsEnabled && r.VerificationStatus!=RelationVerificationStatus.REJECTED && (r.EndDate==null || r.EndDate>=today) && scopes.Any(s=>s.TenantId==tenant && s.PropertyId==r.PropertyId),token) ||
            await db.Set<ManagementAssignment>().AnyAsync(a=>a.TenantId==tenant && a.PersonId==person && a.IsEnabled && a.CancelledAt==null && (a.EndDate==null || a.EndDate>=today) && scopes.Any(s=>s.TenantId==tenant && s.PropertyId==a.PropertyId),token);
    }
    private async Task Lock(Guid tenant,CancellationToken token)=>await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE",token);
    public async Task<IssuedPersonLinkCode?> IssueAsync(Guid tenant,Guid person,string code,string hash,CancellationToken token)
    {
        if(!await Managed(tenant,person,token))return null;
        var issuer=await Actor().Where(a=>a.IsEnabled).Select(a=>(Guid?)a.Id).SingleOrDefaultAsync(token)??throw new UnauthorizedAccessException();
        await using var tx=await db.Database.BeginTransactionAsync(token);await Lock(tenant,token);
        if(!await Managed(tenant,person,token))return null;
        var target=await db.Set<CentralPerson>().SingleAsync(p=>p.TenantId==tenant && p.Id==person,token);
        if(!target.IsEnabled || target.Kind!="NATURAL" || await ActiveLinks(tenant,person).AnyAsync(token) || !await HasCurrentScope(tenant,person,token))throw Unavailable();
        var now=clock.GetUtcNow();
        foreach(var open in await OpenCodes(tenant,person).ToArrayAsync(token))open.Revoke(now);
        await db.SaveChangesAsync(token);
        var issued=new CentralPersonLinkCode(tenant,person,hash,PersonCodePolicy.Hint(code),issuer,now);db.Add(issued);
        try{await db.SaveChangesAsync(token);}
        catch(DbUpdateException e) when(e.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation}){db.ChangeTracker.Clear();throw new DuplicateLinkCodeException();}
        await tx.CommitAsync(token);return new(code,issued.Hint,issued.ExpiresAt);
    }
    public async Task<PersonAccountStatus?> EndAsync(Guid tenant,Guid person,CancellationToken token)
    {
        if(!await Managed(tenant,person,token))return null;
        await using var tx=await db.Database.BeginTransactionAsync(token);await Lock(tenant,token);
        if(!await Managed(tenant,person,token))return null;
        var now=clock.GetUtcNow();
        foreach(var link in await ActiveLinks(tenant,person).ToArrayAsync(token))link.End(now);
        foreach(var open in await OpenCodes(tenant,person).ToArrayAsync(token))open.Revoke(now);
        await db.SaveChangesAsync(token);await tx.CommitAsync(token);return new(false,null);
    }
    // Every rejection returns null so the caller cannot learn whether the code, the person or the account was the reason.
    public async Task<LinkedCentralPerson?> LinkAsync(string hash,PersonIdentity identity,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(token);
        var tenant=await db.Set<CentralPersonLinkCode>().Where(c=>c.CodeHash==hash && c.UsedAt==null && c.RevokedAt==null).Select(c=>(Guid?)c.TenantId).SingleOrDefaultAsync(token);
        if(tenant==null)return null;await Lock(tenant.Value,token);
        var now=clock.GetUtcNow();
        var code=await db.Set<CentralPersonLinkCode>().SingleOrDefaultAsync(c=>c.TenantId==tenant && c.CodeHash==hash && c.UsedAt==null && c.RevokedAt==null,token);
        if(code==null || !code.IsUsable(now))return null;
        var person=await db.Set<CentralPerson>().SingleOrDefaultAsync(p=>p.TenantId==code.TenantId && p.Id==code.PersonId,token);
        var owner=await db.Set<PersonTenant>().SingleOrDefaultAsync(t=>t.Id==code.TenantId && t.IsEnabled,token);
        if(person==null || owner==null || !person.IsEnabled || person.Kind!="NATURAL" || !PersonMatching.Matches(identity,person.FirstName,person.LastName??"",person.PhoneNumber,person.Email) || await ActiveLinks(code.TenantId,person.Id).AnyAsync(token))return null;
        var account=await Actor().SingleOrDefaultAsync(token);
        if(account==null){account=new PersonUserAccount(actor.Issuer!,actor.UserId!);db.Add(account);}
        else if(!account.IsEnabled)return null;
        var membership=await db.Set<PersonTenantMembership>().SingleOrDefaultAsync(m=>m.TenantId==code.TenantId && m.UserAccountId==account.Id,token);
        if(membership==null){membership=new PersonTenantMembership(code.TenantId,account.Id);db.Add(membership);}
        else if(!membership.IsEnabled)return null;
        if(await db.Set<CentralPersonAccountLink>().AnyAsync(l=>l.TenantId==code.TenantId && l.UserAccountId==account.Id && l.IsEnabled && l.EndedAt==null,token))return null;
        var link=new CentralPersonAccountLink(code.TenantId,account.Id,person.Id,now);db.Add(link);code.Use(account.Id,now);
        db.PersonLinkAttempts.Add(new PersonLinkAttempt(actor.UserId!,true,null));
        try{await db.SaveChangesAsync(token);}
        catch(DbUpdateException e) when(e.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation}){return null;}
        await tx.CommitAsync(token);var identityNumber=person.IdentityNumber;
        return new(code.TenantId,owner.Name,new SelfPerson(person.Id,code.TenantId,(person.FirstName+" "+person.LastName).Trim(),person.Kind,new string('*',Math.Max(0,identityNumber.Length-4))+identityNumber[^Math.Min(4,identityNumber.Length)..]),now);
    }
}
