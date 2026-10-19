using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;
public sealed class ManagementAccess(BuildingFlowDbContext db,ICurrentUserService actor,TimeProvider clock,IConfiguration config):IManagementAccess
{
    public string Mode(Guid tenant)
    {
        var mode=config[$"PersonRefactor:ManagementModes:{tenant:D}"]??"LEGACY";
        if(mode is not("LEGACY" or "SHADOW" or "ENFORCED"))throw new InvalidOperationException("Invalid management access mode.");return config.GetValue<bool>("PersonRefactor:SessionContextsEnabled")?"ENFORCED":mode;
    }
    private bool Cap(string role)=>role switch
    {
        "SITE_ADMIN"=>actor.IsInRole("SITE_ADMIN"),
        "MANAGER"=>actor.IsInRole("SITE_ADMIN") || actor.IsInRole("MANAGER"),
        "ACCOUNTANT"=>actor.IsInRole("SITE_ADMIN") || actor.IsInRole("MANAGER") || actor.IsInRole("ACCOUNTANT"),
        _=>throw new ArgumentException("Invalid management capability.")
    };
    internal static IQueryable<CentralPerson> EligiblePeople(BuildingFlowDbContext db,Guid? tenant=null) =>
        db.Set<CentralPerson>().Where(p=>(tenant==null || p.TenantId==tenant) && p.IsEnabled && p.Kind=="NATURAL" &&
            (!db.Set<LegacyPersonMap>().Any(m=>m.TenantId==p.TenantId && m.PersonId==p.Id) ||
             db.Set<LegacyPersonMap>().Any(m=>m.TenantId==p.TenantId && m.PersonId==p.Id && m.IsEnabled &&
                db.People.Any(old=>old.PropertyId==m.PropertyId && old.Id==m.LegacyPersonId && old.IsEnabled && old.Version==m.SourceVersion))));
    internal IQueryable<ManagementAssignment> ActiveAssignments(Guid? tenant=null)
    {
        var today=BusinessDate.Today(clock);var now=clock.GetUtcNow();var persons=EligiblePeople(db);
        return db.Set<ManagementAssignment>().Where(a=>(tenant==null || a.TenantId==tenant) && a.IsEnabled && a.CancelledAt==null && a.StartDate<=today && (a.EndDate==null || a.EndDate>=today) &&
            persons.Any(p=>p.TenantId==a.TenantId && p.Id==a.PersonId) &&
            db.Set<PersonPropertyScope>().Any(s=>s.TenantId==a.TenantId && s.PropertyId==a.PropertyId && s.IsEnabled) && db.Properties.Any(p=>p.Id==a.PropertyId && p.IsEnabled) &&
            db.Set<CentralPersonAccountLink>().Any(l=>l.TenantId==a.TenantId && l.PersonId==a.PersonId && l.IsEnabled && l.EndedAt==null && l.LinkedAt<=now &&
                db.Set<PersonUserAccount>().Any(u=>u.Id==l.UserAccountId && u.IsEnabled && u.Issuer==actor.Issuer && u.Subject==actor.UserId &&
                    db.Set<PersonTenantMembership>().Any(m=>m.TenantId==a.TenantId && m.UserAccountId==u.Id && m.IsEnabled))));
    }
    private Guid[] EnforcedTenants()=>config.GetSection("PersonRefactor:ManagementModes").GetChildren().Select(c=>
    {
        if(!Guid.TryParse(c.Key,out var id))throw new InvalidOperationException("Invalid management tenant configuration.");
        return (Id:id,Mode:Mode(id));
    }).Where(x=>x.Mode=="ENFORCED").Select(x=>x.Id).ToArray();
    internal IQueryable<PersonTenant> PersonTenants()
    {
        if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || string.IsNullOrWhiteSpace(actor.Issuer) || !new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(actor.IsInRole))throw new UnauthorizedAccessException();
        var rows=db.Set<PersonTenant>().Where(t=>t.IsEnabled);if(actor.IsInRole("SUPER_ADMIN"))return rows;
        var strict=config.GetValue<bool>("PersonRefactor:SessionContextsEnabled");var enforced=EnforcedTenants();var roles=new[]{"SITE_ADMIN","MANAGER"}.Where(RoleBound).ToArray();var assignments=ActiveAssignments();
        return rows.Where(t=>db.Set<PersonTenantMembership>().Any(m=>m.TenantId==t.Id && m.IsEnabled && db.Set<PersonUserAccount>().Any(u=>u.Id==m.UserAccountId && u.IsEnabled && u.Subject==actor.UserId && u.Issuer==actor.Issuer)) &&
            (!strict && !enforced.Contains(t.Id) || assignments.Any(a=>a.TenantId==t.Id && roles.Contains(a.Role) && db.PropertyMemberships.Any(m=>m.PropertyId==a.PropertyId && m.UserId==actor.UserId && m.IsEnabled))));
    }
    internal IQueryable<PersonPropertyScope> PropertyScopes(string capability="MANAGER")
    {
        var tenants=PersonTenants();var rows=db.Set<PersonPropertyScope>().Where(s=>s.IsEnabled && tenants.Any(t=>t.Id==s.TenantId) && db.Properties.Any(p=>p.Id==s.PropertyId && p.IsEnabled));
        if(actor.IsInRole("SUPER_ADMIN"))return rows;
        if(!Cap(capability))return rows.Where(s=>false);
        var strict=config.GetValue<bool>("PersonRefactor:SessionContextsEnabled");var enforced=EnforcedTenants();var roles=(capability=="SITE_ADMIN"?new[]{"SITE_ADMIN"}:new[]{"SITE_ADMIN","MANAGER"}).Where(RoleBound).ToArray();var assignments=ActiveAssignments();
        return rows.Where(s=>db.PropertyMemberships.Any(m=>m.PropertyId==s.PropertyId && m.UserId==actor.UserId && m.IsEnabled) && (!strict && !enforced.Contains(s.TenantId) || assignments.Any(a=>a.TenantId==s.TenantId && a.PropertyId==s.PropertyId && roles.Contains(a.Role))));
    }
    private bool RoleBound(string role)=>role=="SITE_ADMIN"?actor.IsInRole("SITE_ADMIN"):role=="MANAGER"?(actor.IsInRole("SITE_ADMIN")||actor.IsInRole("MANAGER")):(actor.IsInRole("SITE_ADMIN")||actor.IsInRole("MANAGER")||actor.IsInRole("ACCOUNTANT"));
    internal string[] BoundRoles(string capability)=>(capability switch
    {
        "SITE_ADMIN"=>new[]{"SITE_ADMIN"},
        "MANAGER"=>new[]{"SITE_ADMIN","MANAGER"},
        "ACCOUNTANT"=>new[]{"SITE_ADMIN","MANAGER","ACCOUNTANT"},
        _=>throw new ArgumentException("Invalid management capability.")
    }).Where(RoleBound).ToArray();
    public async Task<ManagementAccessDecision> EvaluateAsync(Guid tenant,Guid property,string role,CancellationToken token)
    {
        if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || string.IsNullOrWhiteSpace(actor.Issuer))throw new UnauthorizedAccessException();
        var cap=Cap(role);var mode=Mode(tenant);
        var scope=await db.Set<PersonTenant>().AnyAsync(t=>t.Id==tenant && t.IsEnabled,token) && await db.Properties.AnyAsync(p=>p.Id==property && p.IsEnabled,token) &&
            await db.Set<PersonPropertyScope>().AnyAsync(s=>s.TenantId==tenant && s.PropertyId==property && s.IsEnabled,token);
        if(actor.IsInRole("SUPER_ADMIN"))return new(scope?mode:"UNAVAILABLE",scope,scope,scope);
        var membership=scope && await db.Set<PersonTenantMembership>().AnyAsync(m=>m.TenantId==tenant && m.IsEnabled && db.Set<PersonUserAccount>().Any(u=>u.Id==m.UserAccountId && u.IsEnabled && u.Issuer==actor.Issuer && u.Subject==actor.UserId),token) &&
            await db.PropertyMemberships.AnyAsync(m=>m.PropertyId==property && m.UserId==actor.UserId && m.IsEnabled,token);
        var roles=role=="SITE_ADMIN"?new[]{"SITE_ADMIN"}:role=="MANAGER"?new[]{"SITE_ADMIN","MANAGER"}:new[]{"SITE_ADMIN","MANAGER","ACCOUNTANT"};
        roles=roles.Where(RoleBound).ToArray();
        if(!membership)return new("UNAVAILABLE",false,false,false);
        var legacy=cap;
        var assignment=legacy && await ActiveAssignments(tenant).AnyAsync(a=>a.PropertyId==property && roles.Contains(a.Role),token);
        return new(mode,legacy,assignment,mode=="ENFORCED"?assignment:legacy);
    }
    public async Task<bool> TenantPeopleAllowedAsync(Guid tenant,CancellationToken token)
    {
        if(Mode(tenant)!="ENFORCED" || actor.IsInRole("SUPER_ADMIN"))return true;
        if(!actor.IsInRole("SITE_ADMIN") && !actor.IsInRole("MANAGER"))return false;
        var roles=new[]{"SITE_ADMIN","MANAGER"}.Where(RoleBound).ToArray();
        return await ActiveAssignments(tenant).AnyAsync(a=>roles.Contains(a.Role) && db.PropertyMemberships.Any(m=>m.PropertyId==a.PropertyId && m.UserId==actor.UserId && m.IsEnabled),token);
    }
}
