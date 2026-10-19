using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class CentralPeopleStore(BuildingFlowDbContext db,ICurrentUserService actor,TimeProvider clock,IManagementAccess management):ICentralPeopleStore {
 private void Identity(){if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || string.IsNullOrWhiteSpace(actor.Issuer))throw new UnauthorizedAccessException();}
 private IQueryable<PersonUserAccount> Account()=>db.Set<PersonUserAccount>().Where(a=>a.IsEnabled && a.Issuer==actor.Issuer && a.Subject==actor.UserId);
 private async Task<bool> Allowed(Guid tenant,bool manage,CancellationToken token){
  Identity();if(manage && !new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(actor.IsInRole))throw new UnauthorizedAccessException();
  if(!await db.Set<PersonTenant>().AnyAsync(t=>t.Id==tenant && t.IsEnabled,token))return false;
  if(manage && actor.IsInRole("SUPER_ADMIN"))return true;
  return await db.Set<PersonTenantMembership>().AnyAsync(m=>m.TenantId==tenant && m.IsEnabled && Account().Any(a=>a.Id==m.UserAccountId),token) && (!manage || await management.TenantPeopleAllowedAsync(tenant,token));
 }
 private static CentralPersonProfile Profile(CentralPerson p)=>new(p.Kind,p.Citizenship,p.CountryCode,p.IdentityNumber,p.FirstName,p.LastName,p.BirthDate,p.Gender,p.PhoneNumber,p.Email);
 private static CentralPersonDetail Detail(CentralPerson p)=>new(p.Id,p.TenantId,Profile(p),p.IsEnabled,p.Version,p.CreatedAt);
 private IQueryable<CentralPerson> Persons(Guid tenant)=>db.Set<CentralPerson>().Where(p=>p.TenantId==tenant);
 public async Task<PagedRows<CentralPersonSummary>?> ListAsync(Guid tenant,PeopleQuery q,CancellationToken token){
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);if(!await Allowed(tenant,true,token))return null;
  var rows=Persons(tenant).AsNoTracking();if(!string.IsNullOrWhiteSpace(q.Search)){var pattern="%"+q.Search.Trim().Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_")+"%";rows=rows.Where(p=>EF.Functions.ILike(p.FirstName+" "+p.LastName,pattern,"\\") || EF.Functions.ILike(p.PhoneNumber??"",pattern,"\\") || EF.Functions.ILike(p.Email??"",pattern,"\\"));}
  var count=await rows.CountAsync(token);var desc=q.SortDirection=="desc";var sorted=q.SortBy=="createdAt"?(desc?rows.OrderByDescending(p=>p.CreatedAt):rows.OrderBy(p=>p.CreatedAt)):(desc?rows.OrderByDescending(p=>p.LastName).ThenByDescending(p=>p.FirstName):rows.OrderBy(p=>p.LastName).ThenBy(p=>p.FirstName));
  var items=await sorted.ThenBy(p=>p.Id).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).Select(p=>new CentralPersonSummary(p.Id,p.TenantId,(p.FirstName+" "+p.LastName).Trim(),p.Kind,p.IsEnabled,p.Version,p.CreatedAt)).ToArrayAsync(token);await tx.CommitAsync(token);return new(items,q.Page,q.PageSize,count);
 }
 public async Task<CentralPersonDetail?> GetAsync(Guid tenant,Guid person,CancellationToken token){if(!await Allowed(tenant,true,token))return null;var p=await Persons(tenant).AsNoTracking().SingleOrDefaultAsync(p=>p.Id==person,token);return p==null?null:Detail(p);}
 public async Task<SelfPerson?> SelfAsync(Guid tenant,CancellationToken token){
  if(!await Allowed(tenant,false,token))return null;
  var person=await (from link in db.Set<CentralPersonAccountLink>().AsNoTracking() join p in Persons(tenant) on new{link.TenantId,Id=link.PersonId} equals new{p.TenantId,p.Id} where link.TenantId==tenant && link.IsEnabled && link.LinkedAt<=clock.GetUtcNow() && link.EndedAt==null && p.IsEnabled && p.Kind=="NATURAL" && Account().Any(a=>a.Id==link.UserAccountId) select p).SingleOrDefaultAsync(token);
  if(person==null)return null;var id=person.IdentityNumber;return new(person.Id,tenant,(person.FirstName+" "+person.LastName).Trim(),person.Kind,new string('*',Math.Max(0,id.Length-4))+id[^Math.Min(4,id.Length)..]);
 }
 public async Task<CentralPersonMutation?> WriteAsync(Guid tenant,Guid? person,Guid operation,long expectedVersion,CentralPersonProfile profile,bool enabled,CancellationToken token){
  if(!await Allowed(tenant,true,token))return null;
  var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{tenant,person,expectedVersion,profile,enabled}))));
  await using var tx=await db.Database.BeginTransactionAsync(token);
  // Tenant lock gives writes, link changes and future mapping a consistent parent lock order.
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE",token);
  if(!await Allowed(tenant,true,token))return null;
  var prior=await db.Set<CentralPersonOperation>().IgnoreQueryFilters().SingleOrDefaultAsync(r=>r.TenantId==tenant && r.ActorIssuer==actor.Issuer && r.ActorSubject==actor.UserId && r.OperationId==operation,token);
  if(prior!=null){if(prior.IsDeleted || !prior.IsEnabled || prior.Fingerprint!=fingerprint)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());var current=await Persons(tenant).AsNoTracking().SingleOrDefaultAsync(p=>p.Id==prior.PersonId,token);if(current==null)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());return new(Detail(current),true,prior.AppliedVersion);}
  CentralPerson record;
  if(person is Guid id){record=(await Persons(tenant).SingleOrDefaultAsync(p=>p.Id==id,token))!;if(record==null)return null;if(record.Version!=expectedVersion)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
   if((!enabled || profile.Kind!="NATURAL") && await db.Set<CentralPersonAccountLink>().AnyAsync(l=>l.TenantId==tenant && l.PersonId==id && l.IsEnabled && l.EndedAt==null,token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
   if((!enabled || profile.Kind!="NATURAL") && await db.Set<ManagementAssignment>().AnyAsync(a=>a.TenantId==tenant && a.PersonId==id && a.IsEnabled && a.CancelledAt==null && (a.EndDate==null || a.EndDate>=BusinessDate.Today(clock)),token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
   if(await db.Set<LegacyPersonMap>().AnyAsync(m=>m.TenantId==tenant && m.PersonId==id,token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
   if(!enabled && await PersonCenterStore.HasCurrentNative(db,null,null,id,BusinessDate.Today(clock),token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
   if(profile.Kind!="NATURAL" && await db.Set<UnitResidency>().AnyAsync(r=>r.TenantId==tenant && r.PersonId==id && r.IsEnabled && (r.EndDate==null || r.EndDate>=BusinessDate.Today(clock)),token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
   record.Edit(profile.Kind!,profile.Citizenship!,profile.CountryCode!,profile.IdentityNumber!,profile.FirstName!,profile.LastName,profile.BirthDate,profile.Gender,profile.PhoneNumber,profile.Email,enabled);
  }else{record=new(tenant,profile.Kind!,profile.Citizenship!,profile.CountryCode!,profile.IdentityNumber!,profile.FirstName!,profile.LastName,profile.BirthDate,profile.Gender,profile.PhoneNumber,profile.Email);db.Add(record);}
  if(person!=null)db.Entry(record).Property(p=>p.IsEnabled).IsModified=true;
  await db.SaveChangesAsync(token);db.Add(new CentralPersonOperation(tenant,actor.Issuer!,actor.UserId!,operation,fingerprint,record.Id,record.Version));await db.SaveChangesAsync(token);await tx.CommitAsync(token);return new(Detail(record),false,record.Version);
 }
}
