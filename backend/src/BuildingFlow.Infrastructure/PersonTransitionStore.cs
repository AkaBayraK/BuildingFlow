using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

// Shadow-only transition: legacy is the sole write authority until a separately accepted cutover.
public sealed class PersonTransitionStore(BuildingFlowDbContext db,ICurrentUserService actor,IManagementAccess management):IPersonTransitionStore
{
    private static void Conflict()=>throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
    private async Task<bool> Allowed(Guid tenant,Guid property,bool migration,CancellationToken token)
    {
        if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || string.IsNullOrWhiteSpace(actor.Issuer))throw new UnauthorizedAccessException();
        if(migration ? !actor.IsInRole("SUPER_ADMIN") : !new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(actor.IsInRole))throw new UnauthorizedAccessException();
        if(!await db.Set<PersonTenant>().AnyAsync(t=>t.Id==tenant && t.IsEnabled,token) || !await db.Properties.AnyAsync(p=>p.Id==property && p.IsEnabled,token))return false;
        if(actor.IsInRole("SUPER_ADMIN"))return true;
        return await db.Set<PersonTenantMembership>().AnyAsync(m=>m.TenantId==tenant && m.IsEnabled && db.Set<PersonUserAccount>().Any(a=>a.Id==m.UserAccountId && a.IsEnabled && a.Issuer==actor.Issuer && a.Subject==actor.UserId),token) &&
            await db.PropertyMemberships.AnyAsync(m=>m.PropertyId==property && m.IsEnabled && m.UserId==actor.UserId,token) && (await management.EvaluateAsync(tenant,property,"MANAGER",token)).Allowed;
    }
    private sealed record Source(Person[] People,UnitOccupancy[] Links,string Hash);
    private async Task<Source> Load(Guid tenant,Guid property,CancellationToken token)
    {
        // Historical filters deliberately removed only on explicitly property-scoped sources.
        var people=await db.People.IgnoreQueryFilters().AsNoTracking().Where(p=>p.PropertyId==property).OrderBy(p=>p.Id).ToArrayAsync(token);
        var links=await db.UnitOccupancies.IgnoreQueryFilters().AsNoTracking().Where(p=>p.PropertyId==property).OrderBy(p=>p.Id).ToArrayAsync(token);
        var units=await db.Units.IgnoreQueryFilters().AsNoTracking().Where(u=>u.PropertyId==property).OrderBy(u=>u.Id).Select(u=>new{u.Id,u.Version,u.IsDeleted,u.IsEnabled}).ToArrayAsync(token);
        var parent=await db.Properties.AsNoTracking().Where(p=>p.Id==property).Select(p=>new{p.Id,p.Version}).SingleAsync(token);
        var data=JsonSerializer.Serialize(new{tenant,property,parent,units,People=people.Select(p=>new{p.Id,p.Version,p.IsDeleted,p.IsEnabled,p.FirstName,p.LastName,p.PhoneNumber,p.Email,p.Address,p.PersonKind,p.Citizenship,p.CountryCode,p.IdentityNumber,p.BirthDate,p.Gender,p.LinkedUserId}),Links=links.Select(l=>new{l.Id,l.Version,l.UnitId,l.PersonId,l.Type,l.StartDate,l.EndDate,l.IsPrimaryContact,l.IsEnabled,l.IsDeleted})});
        return new(people,links,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))));
    }
    private static CentralPerson Canonical(Guid tenant,Person p)=>new(tenant,p.PersonKind!,p.Citizenship!,p.CountryCode!,p.IdentityNumber!,p.FirstName,p.LastName,p.BirthDate,p.Gender,p.PhoneNumber,p.Email);
    private static IPersonUnitRelation Relation(Guid tenant,UnitOccupancy l,Guid person)
    {
        VerifiableRecord relation=l.Type switch
        {
            OccupancyType.OWNER=>new UnitOwnership(tenant,l.PropertyId,l.UnitId,person,l.StartDate,l.EndDate,l.IsPrimaryContact),
            OccupancyType.TENANT=>new UnitTenancy(tenant,l.PropertyId,l.UnitId,person,l.StartDate,l.EndDate,l.IsPrimaryContact),
            _=>new UnitResidency(tenant,l.PropertyId,l.UnitId,person,l.StartDate,l.EndDate,l.IsPrimaryContact)
        };
        // Legacy occupancies were never verified in the new sense; the copy must not invent a verifier.
        relation.MarkLegacy();return (IPersonUnitRelation)relation;
    }
    private async Task<PersonTransitionPreview> Preview(Guid tenant,Guid property,Source source,CancellationToken token)
    {
        var invalid=source.People.Count(p=>p.PersonKind==null || p.Citizenship==null || p.CountryCode==null || p.IdentityNumber==null);
        foreach(var p in source.People.Where(p=>p.PersonKind!=null && p.Citizenship!=null && p.CountryCode!=null && p.IdentityNumber!=null))
            try{_ = Canonical(tenant,p);}catch(ArgumentException){invalid++;}
        var maps=await db.Set<LegacyPersonMap>().IgnoreQueryFilters().CountAsync(m=>m.PropertyId==property,token);
        var occupancyMaps=await db.Set<LegacyOccupancyMap>().IgnoreQueryFilters().CountAsync(m=>m.PropertyId==property,token);
        var foreign=await db.Set<PersonPropertyScope>().IgnoreQueryFilters().AnyAsync(s=>s.PropertyId==property && (s.TenantId!=tenant || s.IsDeleted || !s.IsEnabled),token);
        var persons=source.People.Select(p=>p.Id).ToHashSet();
        var valid=invalid==0 && !foreign && source.Links.All(l=>persons.Contains(l.PersonId));
        try{PersonUnitRules.ValidateSet(source.Links.Where(l=>!l.IsDeleted).Select(l=>Relation(tenant,l,l.PersonId)));}catch(InvalidOperationException){valid=false;}
        // An existing snapshot is never silently refreshed while legacy remains writable.
        var countsAgree=(maps==0 && occupancyMaps==0) || (maps==source.People.Length && occupancyMaps==source.Links.Length);
        if(maps>0 || occupancyMaps>0)valid=valid && (await Compare(tenant,property,source,token)).Matches;
        return new(tenant,property,source.Hash,source.People.Length,source.Links.Count(l=>l.Type==OccupancyType.OWNER),source.Links.Count(l=>l.Type==OccupancyType.TENANT),source.Links.Count(l=>l.Type==OccupancyType.RESIDENT),invalid,maps,occupancyMaps,valid && countsAgree);
    }
    public async Task<PersonTransitionPreview?> PreviewAsync(Guid tenant,Guid property,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        if(!await Allowed(tenant,property,true,token))return null;
        var result=await Preview(tenant,property,await Load(tenant,property,token),token);await tx.CommitAsync(token);return result;
    }
    private void CopyState(BaseEntity target,BaseEntity source)
    {target.SetEnabled(source.IsEnabled);db.Entry(target).Property(p=>p.IsDeleted).CurrentValue=source.IsDeleted;}
    public async Task<PersonTransitionComparison?> ApplyAsync(Guid tenant,Guid property,string expectedHash,CancellationToken token)
    {
        if(expectedHash is not {Length:64} || expectedHash.Any(c=>!char.IsAsciiHexDigit(c)))throw new ArgumentException("A dry-run hash is required.");
        if(!await Allowed(tenant,property,true,token))return null;
        await using var tx=await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE",token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",token);
        if(!await Allowed(tenant,property,true,token))return null;
        var source=await Load(tenant,property,token);var preview=await Preview(tenant,property,source,token);
        if(source.Hash!=expectedHash || !preview.CanApply)Conflict();
        if(preview.ExistingPersonMaps>0 || preview.ExistingOccupancyMaps>0)
        {var prior=await Compare(tenant,property,source,token);if(!prior.Matches)Conflict();await tx.CommitAsync(token);return prior;}
        if(!await db.Set<PersonPropertyScope>().AnyAsync(s=>s.TenantId==tenant && s.PropertyId==property,token))db.Add(new PersonPropertyScope(tenant,property));
        var canonical=new Dictionary<Guid,Guid>();
        foreach(var p in source.People)
        {
            var next=Canonical(tenant,p);db.Add(next);CopyState(next,p);canonical.Add(p.Id,next.Id);
            db.Add(new LegacyPersonMap(tenant,p,next.Id,JsonSerializer.Serialize(new LegacyLocalProfile(p.FirstName,p.LastName,p.PhoneNumber,p.Email,p.Address))));
        }
        foreach(var l in source.Links)
        {
            var relation=Relation(tenant,l,canonical[l.PersonId]);db.Add(relation);CopyState((BaseEntity)relation,l);
            db.Add(new LegacyOccupancyMap(tenant,l,relation is UnitOwnership ? ((BaseEntity)relation).Id:null,relation is UnitTenancy ? ((BaseEntity)relation).Id:null,relation is UnitResidency ? ((BaseEntity)relation).Id:null));
        }
        await db.SaveChangesAsync(token);var result=await Compare(tenant,property,source,token);if(!result.Matches)Conflict();await tx.CommitAsync(token);return result;
    }
    private async Task<PersonTransitionComparison> Compare(Guid tenant,Guid property,Source source,CancellationToken token)
    {
        var maps=await db.Set<LegacyPersonMap>().IgnoreQueryFilters().AsNoTracking().Where(m=>m.TenantId==tenant && m.PropertyId==property).ToArrayAsync(token);
        var links=await db.Set<LegacyOccupancyMap>().IgnoreQueryFilters().AsNoTracking().Where(m=>m.TenantId==tenant && m.PropertyId==property).ToArrayAsync(token);
        var persons=await db.Set<CentralPerson>().IgnoreQueryFilters().AsNoTracking().Where(p=>p.TenantId==tenant && maps.Select(m=>m.PersonId).Contains(p.Id)).ToDictionaryAsync(p=>p.Id,token);
        var drift=source.People.Count(p=>!maps.Any(m=>m.LegacyPersonId==p.Id && !m.IsDeleted && m.IsEnabled && m.SourceVersion==p.Version && persons.TryGetValue(m.PersonId,out var target) && target.Version==1 && target.IsDeleted==p.IsDeleted && target.IsEnabled==p.IsEnabled));
        var relationRows=new List<IPersonUnitRelation>();relationRows.AddRange(await db.Set<UnitOwnership>().IgnoreQueryFilters().AsNoTracking().Where(r=>r.TenantId==tenant && r.PropertyId==property).ToArrayAsync(token));relationRows.AddRange(await db.Set<UnitTenancy>().IgnoreQueryFilters().AsNoTracking().Where(r=>r.TenantId==tenant && r.PropertyId==property).ToArrayAsync(token));relationRows.AddRange(await db.Set<UnitResidency>().IgnoreQueryFilters().AsNoTracking().Where(r=>r.TenantId==tenant && r.PropertyId==property).ToArrayAsync(token));
        var targets=relationRows.ToDictionary(r=>((BaseEntity)r).Id);
        var relationDrift=source.Links.Count(l=>!links.Any(m=>m.LegacyOccupancyId==l.Id && !m.IsDeleted && m.IsEnabled && m.SourceVersion==l.Version && targets.TryGetValue(m.OwnershipId??m.TenancyId??m.ResidencyId!.Value,out var r) && r.Kind==l.Type && r.UnitId==l.UnitId && maps.Any(p=>p.LegacyPersonId==l.PersonId && p.PersonId==r.PersonId) && r.StartDate==l.StartDate && r.EndDate==l.EndDate && r.IsPrimaryContact==l.IsPrimaryContact && ((BaseEntity)r).Version==1 && ((BaseEntity)r).IsDeleted==l.IsDeleted && ((BaseEntity)r).IsEnabled==l.IsEnabled));
        return new(source.People.Length,maps.Length,source.Links.Length,links.Length,drift,relationDrift,maps.Length==source.People.Length && links.Length==source.Links.Length && relationRows.Count==source.Links.Length && drift==0 && relationDrift==0);
    }
    public async Task<PersonTransitionComparison?> CompareAsync(Guid tenant,Guid property,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);if(!await Allowed(tenant,property,false,token))return null;
        if(!await db.Set<PersonPropertyScope>().AnyAsync(s=>s.TenantId==tenant && s.PropertyId==property && s.IsEnabled,token))return null;
        var result=await Compare(tenant,property,await Load(tenant,property,token),token);await tx.CommitAsync(token);return result;
    }
    public async Task<CompatiblePerson?> ReadAsync(Guid tenant,Guid property,Guid legacyPerson,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);if(!await Allowed(tenant,property,false,token))return null;
        if(!await db.Set<PersonPropertyScope>().AnyAsync(s=>s.TenantId==tenant && s.PropertyId==property && s.IsEnabled,token))return null;
        var map=await db.Set<LegacyPersonMap>().SingleOrDefaultAsync(m=>m.TenantId==tenant && m.PropertyId==property && m.LegacyPersonId==legacyPerson && m.IsEnabled,token);if(map==null)return null;
        var legacy=await db.People.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==legacyPerson && p.PropertyId==property,token);if(legacy==null)return null;
        var p=await db.Set<CentralPerson>().AsNoTracking().SingleOrDefaultAsync(p=>p.TenantId==tenant && p.Id==map.PersonId,token);if(p==null)return null;
        if(legacy.Version!=map.SourceVersion || p.Version!=1)Conflict();
        await tx.CommitAsync(token);return new(legacyPerson,map.SourceVersion,p.Id,JsonSerializer.Deserialize<LegacyLocalProfile>(map.LocalProfile)!,new(p.Kind,p.Citizenship,p.CountryCode,p.IdentityNumber,p.FirstName,p.LastName,p.BirthDate,p.Gender,p.PhoneNumber,p.Email),p.IsEnabled);
    }
}
