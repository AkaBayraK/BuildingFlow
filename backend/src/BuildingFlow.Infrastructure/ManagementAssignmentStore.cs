using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;
public sealed class ManagementAssignmentStore(BuildingFlowDbContext db,ICurrentUserService actor,TimeProvider clock,IManagementAccess access,IKeycloakAdmin identity):IManagementAssignmentStore
{
    private DateOnly Today=>BusinessDate.Today(clock);
    private static void Conflict()=>throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
    private static void Page(int page,int size){if(page is <1 or >100000 || size is <1 or >100)throw new ArgumentException("Invalid pagination.");}
    private static string Reason(Guid operation,string reason){if(operation==Guid.Empty)throw new ArgumentException("Operation identity required.");return ManagementAssignment.CleanReason(reason);}
    private IQueryable<ManagementAssignment> Rows(Guid tenant,Guid property)=>db.Set<ManagementAssignment>().Where(a=>a.TenantId==tenant && a.PropertyId==property);
    private ManagementAssignmentRow Row(ManagementAssignment a)=>new(a.Id,a.TenantId,a.PropertyId,a.PersonId,a.Role,a.StartDate,a.EndDate,a.CancelledAt!=null?"CANCELLED":!a.IsEnabled?"DISABLED":a.EndDate<Today?"ENDED":a.StartDate>Today?"UPCOMING":"ACTIVE",a.Version,a.CancelledAt);
    private async Task<bool> Allowed(Guid tenant,Guid property,bool write,CancellationToken token)
    {
        if(write ? !actor.IsInRole("SUPER_ADMIN") && !actor.IsInRole("SITE_ADMIN") : !new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(actor.IsInRole))throw new UnauthorizedAccessException();
        return (await access.EvaluateAsync(tenant,property,write?"SITE_ADMIN":"MANAGER",token)).Allowed;
    }
    private void Role(string role){if(role is not("SITE_ADMIN" or "MANAGER" or "ACCOUNTANT"))throw new ArgumentException("Invalid assignment role.");if(role=="SITE_ADMIN" && !actor.IsInRole("SUPER_ADMIN"))throw new UnauthorizedAccessException();}
    public async Task<PagedRows<ManagementAssignmentRow>?> ListAsync(Guid tenant,Guid property,int page,int pageSize,CancellationToken token)
    {
        Page(page,pageSize);await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);if(!await Allowed(tenant,property,false,token))return null;
        var q=Rows(tenant,property).AsNoTracking();var total=await q.CountAsync(token);var rows=await q.OrderByDescending(a=>a.StartDate).ThenBy(a=>a.Id).Skip((page-1)*pageSize).Take(pageSize).ToArrayAsync(token);await tx.CommitAsync(token);return new(rows.Select(Row).ToArray(),page,pageSize,total);
    }
    public async Task<ManagementAssignmentRow?> GetAsync(Guid tenant,Guid property,Guid id,CancellationToken token)
    {if(!await Allowed(tenant,property,false,token))return null;var row=await Rows(tenant,property).AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id,token);return row==null?null:Row(row);}
    public async Task<PagedRows<ManagementAssignmentHistory>?> HistoryAsync(Guid tenant,Guid property,Guid id,int page,int pageSize,CancellationToken token)
    {
        Page(page,pageSize);await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);if(!await Allowed(tenant,property,false,token) || !await Rows(tenant,property).AnyAsync(a=>a.Id==id,token))return null;
        var q=db.Set<ManagementAssignmentEvent>().AsNoTracking().Where(e=>e.TenantId==tenant && e.PropertyId==property && e.AssignmentId==id);var count=await q.CountAsync(token);var rows=await q.OrderBy(e=>e.AppliedVersion).Skip((page-1)*pageSize).Take(pageSize).Select(e=>new ManagementAssignmentHistory(e.Id,e.Action,e.Reason,e.AppliedVersion,e.CreatedAt,e.CreatedBy)).ToArrayAsync(token);await tx.CommitAsync(token);return new(rows,page,pageSize,count);
    }
    public Task<ManagementAssignmentMutation?> CreateAsync(Guid tenant,Guid property,CreateManagementAssignmentRequest r,CancellationToken token)
    {var reason=Reason(r.OperationId,r.Reason);Role(r.Role);var candidate=new ManagementAssignment(tenant,property,r.PersonId,r.Role,r.StartDate,r.EndDate);return Write(tenant,property,null,r.OperationId,"CREATED",new{r.PersonId,r.Role,r.StartDate,r.EndDate,reason},reason,0,candidate,null,token);}
    public Task<ManagementAssignmentMutation?> EndAsync(Guid tenant,Guid property,Guid id,EndManagementAssignmentRequest r,CancellationToken token)
    {var reason=Reason(r.OperationId,r.Reason);if(r.ExpectedVersion<1)throw new ArgumentException("Version required.");return Write(tenant,property,id,r.OperationId,"ENDED",new{r.ExpectedVersion,r.EndDate,reason},reason,r.ExpectedVersion,null,r.EndDate,token);}
    public Task<ManagementAssignmentMutation?> CancelAsync(Guid tenant,Guid property,Guid id,CancelManagementAssignmentRequest r,CancellationToken token)
    {var reason=Reason(r.OperationId,r.Reason);if(r.ExpectedVersion<1)throw new ArgumentException("Version required.");return Write(tenant,property,id,r.OperationId,"CANCELLED",new{r.ExpectedVersion,reason},reason,r.ExpectedVersion,null,null,token);}
    private async Task<ManagementAssignmentMutation?> Write(Guid tenant,Guid property,Guid? id,Guid operation,string action,object payload,string reason,long expected,ManagementAssignment? candidate,DateOnly? end,CancellationToken token)
    {
        if(!await Allowed(tenant,property,true,token))return null;
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{tenant,property,id,action,payload}))));
        await using var tx=await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE",token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",token);
        if(!await Allowed(tenant,property,true,token))return null;
        var receipt=await db.Set<ManagementAssignmentOperation>().IgnoreQueryFilters().SingleOrDefaultAsync(o=>o.TenantId==tenant && o.ActorIssuer==actor.Issuer && o.ActorSubject==actor.UserId && o.OperationId==operation,token);
        if(receipt!=null)
        {
            if(receipt.IsDeleted || !receipt.IsEnabled || receipt.Fingerprint!=hash)Conflict();
            var replay=await Rows(tenant,property).AsNoTracking().SingleOrDefaultAsync(a=>a.Id==receipt.AssignmentId,token);if(replay==null)Conflict();Role(replay!.Role);return new(Row(replay),true,receipt.AppliedVersion);
        }
        ManagementAssignment row;
        if(candidate!=null)
        {
            row=candidate;
            if(!await ManagementAccess.EligiblePeople(db,tenant).AnyAsync(p=>p.Id==row.PersonId,token))return null;
            if(await Rows(tenant,property).AnyAsync(a=>a.PersonId==row.PersonId && a.Role==row.Role && a.CancelledAt==null && (row.EndDate==null || a.StartDate<=row.EndDate) && (a.EndDate==null || a.EndDate>=row.StartDate),token))Conflict();
            db.Add(row);
        }
        else
        {
            row=(await Rows(tenant,property).SingleOrDefaultAsync(a=>a.Id==id,token))!;if(row==null)return null;Role(row.Role);
            if(row.Version!=expected || row.CancelledAt!=null || !row.IsEnabled)Conflict();
            if(action=="ENDED" && (row.EndDate!=null || end<row.StartDate))Conflict();
            if(action=="CANCELLED" && row.EndDate<Today)Conflict();
            await PreserveAdministrator(tenant,property,row,action=="ENDED"?end:null,token);
            if(action=="ENDED")row.End(end!.Value,reason);else row.Cancel(clock.GetUtcNow(),reason);
        }
        try{await db.SaveChangesAsync(token);}
        catch(DbUpdateException e) when(e.InnerException is PostgresException {SqlState:PostgresErrorCodes.CheckViolation or PostgresErrorCodes.UniqueViolation}){Conflict();throw;}
        db.Add(new ManagementAssignmentEvent(tenant,property,row.Id,action,reason,row.Version));
        db.Add(new ManagementAssignmentOperation(tenant,property,row.Id,actor.Issuer!,actor.UserId!,operation,hash,row.Version));
        await db.SaveChangesAsync(token);await tx.CommitAsync(token);return new(Row(row),false,row.Version);
    }
    private async Task PreserveAdministrator(Guid tenant,Guid property,ManagementAssignment row,DateOnly? end,CancellationToken token)
    {
        if(access.Mode(tenant)!="ENFORCED" || row.Role!="SITE_ADMIN")return;
        var start=end is null?Today:end==DateOnly.MaxValue?DateOnly.MaxValue:end.Value.AddDays(1);
        if(start<Today)start=Today;if(start<row.StartDate)start=row.StartDate;
        if(row.EndDate<start || end==DateOnly.MaxValue)return;
        var now=clock.GetUtcNow();var persons=ManagementAccess.EligiblePeople(db,tenant);
        var others=await Rows(tenant,property).AsNoTracking().Where(a=>a.Id!=row.Id && a.Role=="SITE_ADMIN" && a.IsEnabled && a.CancelledAt==null && (a.EndDate==null || a.EndDate>=start) &&
            persons.Any(p=>p.Id==a.PersonId) &&
            db.Set<CentralPersonAccountLink>().Any(l=>l.TenantId==tenant && l.PersonId==a.PersonId && l.IsEnabled && l.EndedAt==null && l.LinkedAt<=now &&
                db.Set<PersonUserAccount>().Any(u=>u.Id==l.UserAccountId && u.IsEnabled && u.Issuer==actor.Issuer &&
                    db.Set<PersonTenantMembership>().Any(m=>m.TenantId==tenant && m.UserAccountId==u.Id && m.IsEnabled) &&
                    db.PropertyMemberships.Any(m=>m.PropertyId==property && m.UserId==u.Subject && m.IsEnabled))))
            .OrderBy(a=>a.StartDate).ThenBy(a=>a.Id).ToArrayAsync(token);
        // Verify the replacement account's actual realm capability before counting its period.
        // Assigning a SITE_ADMIN duty never creates a Keycloak role.
        var accountIds=await (from link in db.Set<CentralPersonAccountLink>()
            join user in db.Set<PersonUserAccount>() on link.UserAccountId equals user.Id
            where link.TenantId==tenant && link.IsEnabled && link.EndedAt==null && link.LinkedAt<=now && user.IsEnabled && user.Issuer==actor.Issuer
                && others.Select(a=>a.PersonId).Contains(link.PersonId)
            select new{link.PersonId,user.Subject}).ToArrayAsync(token);
        var accounts=accountIds.ToDictionary(a=>a.PersonId,a=>a.Subject);
        var checkedAccounts=new Dictionary<string,bool>();
        var cursor=start;var requiredEnd=row.EndDate??DateOnly.MaxValue;
        foreach(var a in others)
        {
            if(a.StartDate>cursor)break;
            var through=a.EndDate??DateOnly.MaxValue;
            if(through<cursor || !accounts.TryGetValue(a.PersonId,out var subject))continue;
            if(!checkedAccounts.TryGetValue(subject,out var verified))
            {
                var user=await identity.GetAsync(subject,token);
                var roles=user is {Enabled:true}?await identity.RealmRolesAsync(subject,token):[];
                verified=roles.Contains("SITE_ADMIN") || roles.Contains("SUPER_ADMIN");checkedAccounts.Add(subject,verified);
            }
            if(!verified)continue;
            if(through>=requiredEnd)return;
            cursor=through.AddDays(1);
        }
        Conflict();
    }
}
