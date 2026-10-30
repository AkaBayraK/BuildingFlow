using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class RequestManagementStore(BuildingFlowDbContext db,ICurrentUserService actor):IRequestManagementStore {
 private async Task Authorize(Guid property,CancellationToken token) {
  if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || !new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(actor.IsInRole) ||
   !await db.Properties.AnyAsync(p=>p.Id==property && p.IsEnabled && (actor.IsInRole("SUPER_ADMIN") || db.PropertyMemberships.Any(m=>m.PropertyId==property && m.UserId==actor.UserId && m.IsEnabled)),token)) throw new UnauthorizedAccessException();
 }
 private IQueryable<MobileRequest> Rows(Guid property)=>db.Set<MobileRequest>().Where(r=>r.PropertyId==property && r.IsEnabled);
 private IQueryable<RequestRow> Project(IQueryable<MobileRequest> rows)=>rows.Select(r=>new RequestRow(r.Id,r.PropertyId,r.UnitId,db.Units.Where(u=>u.PropertyId==r.PropertyId && u.Id==r.UnitId).Select(u=>u.Number).FirstOrDefault(),r.Description,r.Category,r.Priority,r.ManagementStatus,r.ManagementResponse,r.HasManagementHistory,r.Version,r.CreatedAt,r.ModifiedAt,r.PhotoId,db.Set<MobileRequestPhoto>().Where(p=>p.Id==r.PhotoId && p.PropertyId==r.PropertyId && p.RequestId==r.Id && p.State=="CURRENT").Select(p=>p.Sha256).FirstOrDefault()));
 public async Task<RequestPage> ListAsync(Guid property,int page,int pageSize,string? search,string? status,string? category,string? priority,string sort,CancellationToken token) {
  await Authorize(property,token);
  if(page<1 || page>100000 || pageSize<1 || pageSize>100 || search?.Length>200 || status is not(null or "RECORDED" or "IN_PROGRESS" or "RESOLVED" or "REJECTED") || category!=null && !MobileRequest.Categories.Contains(category) || priority is not(null or "LOW" or "NORMAL" or "HIGH") || sort is not("newest" or "oldest" or "updated"))throw new ArgumentException("Invalid request filters.");
  var q=Rows(property).AsNoTracking();
  if(!string.IsNullOrWhiteSpace(search)){var s=search.Trim();q=q.Where(r=>r.Description.Contains(s));}
  if(status!=null)q=q.Where(r=>r.ManagementStatus==status);
  if(category!=null)q=q.Where(r=>r.Category==category);
  if(priority!=null)q=q.Where(r=>r.Priority==priority);
  var total=await q.CountAsync(token);
  q=sort=="oldest"?q.OrderBy(r=>r.CreatedAt).ThenBy(r=>r.Id):sort=="updated"?q.OrderByDescending(r=>r.ModifiedAt).ThenBy(r=>r.Id):q.OrderByDescending(r=>r.CreatedAt).ThenBy(r=>r.Id);
  return new(await Project(q.Skip((page-1)*pageSize).Take(pageSize)).ToArrayAsync(token),page,pageSize,total);
 }
 private async Task<RequestDetail?> Read(Guid property,Guid id,bool replayed,CancellationToken token) {
  var r=await Project(Rows(property).AsNoTracking().Where(r=>r.Id==id)).SingleOrDefaultAsync(token);
  if(r==null)return null;
  var h=await db.Set<RequestManagementEvent>().AsNoTracking().Where(e=>e.PropertyId==property && e.RequestId==id).OrderBy(e=>e.ExpectedVersion).ThenBy(e=>e.Id).Select(e=>new RequestHistory(e.Id,e.FromStatus,e.ToStatus,e.Response,e.CreatedAt)).ToArrayAsync(token);
  return new(r,h,replayed,await PhotoConsent(r,token));
 }
 private async Task<RequestPhotoConsent?> PhotoConsent(RequestRow r,CancellationToken token) {
  if(r.PhotoId is not Guid id || r.PhotoSha256==null)return null;
  var photo=await db.Set<MobileRequestPhoto>().AsNoTracking().Where(p=>p.Id==id && p.RequestId==r.Id).Select(p=>new{p.OwnerUserId,p.ModifiedAt}).SingleOrDefaultAsync(token);
  if(photo==null)return null;
  var rows=await (from c in db.UserConsents.AsNoTracking() join t in db.ConsentTypes.IgnoreQueryFilters() on c.ConsentTypeId equals t.Id
   where c.OwnerUserId==photo.OwnerUserId && t.Feature==ConsentFeature.REQUEST_PHOTO select c).ToListAsync(token);
  // Photos uploaded before İŞ17 have no consent row; the centre sees NONE rather than a guessed approval.
  var granted=rows.Where(c=>c.ApprovedAt<=photo.ModifiedAt && (c.RevokedAt==null || c.RevokedAt>=photo.ModifiedAt)).OrderByDescending(c=>c.ApprovedAt).FirstOrDefault();
  var withdrawn=rows.Where(c=>c.RevokeReason==ConsentRevokeReason.WITHDRAWN && c.RevokedAt>=photo.ModifiedAt).OrderBy(c=>c.RevokedAt).FirstOrDefault();
  return new(withdrawn!=null?"WITHDRAWN":granted!=null?"GRANTED":"NONE",granted?.Id,granted?.TextVersion,granted?.ApprovedAt,withdrawn?.RevokedAt);
 }
 public async Task<RequestDetail?> DetailAsync(Guid property,Guid id,CancellationToken token){
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
  await Authorize(property,token);var result=await Read(property,id,false,token);await tx.CommitAsync(token);return result;
 }
 public async Task<RequestDetail> CreateAsync(Guid property,CreateRequest c,CancellationToken token){
  await Authorize(property,token);
  var description=c.Description?.Trim();
  if(c.OperationId==Guid.Empty || string.IsNullOrEmpty(description) || description.Length>2000 || c.Category==null || !MobileRequest.Categories.Contains(c.Category) || c.Priority is not("LOW" or "NORMAL" or "HIGH"))throw new ArgumentException("Invalid request command.");
  await using var tx=await db.Database.BeginTransactionAsync(token);
  // The operation id is the record id, so a retried submit returns the first result instead of a duplicate.
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"mobile-record:"+c.OperationId},0))",token);
  await Authorize(property,token);
  var prior=await db.Set<MobileRequest>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(r=>r.Id==c.OperationId,token);
  if(prior!=null){
   if(prior.OwnerUserId!=actor.UserId || prior.PropertyId!=property || prior.UnitId!=c.UnitId || prior.Category!=c.Category || prior.Priority!=c.Priority || prior.Description!=description)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
   var replay=await Read(property,prior.Id,true,token)??throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());await tx.CommitAsync(token);return replay;
  }
  if(c.UnitId is Guid unit && !await db.Units.AnyAsync(u=>u.PropertyId==property && u.Id==unit && u.IsEnabled,token))throw new ArgumentException("Invalid request unit.");
  await DefinitionPolicy.EnsureSelectableAsync(db,property,"REQUEST_CATEGORY",c.Category,null,"category",token);
  await DefinitionPolicy.EnsureSelectableAsync(db,property,"REQUEST_PRIORITY",c.Priority,null,"priority",token);
  var record=new MobileRequest(c.OperationId,property,c.UnitId,"CENTER",actor.UserId!,description);record.Details(c.Category,c.Priority,null);
  db.Add(record);await db.SaveChangesAsync(token);
  var result=await Read(property,record.Id,false,token);await tx.CommitAsync(token);return result!;
 }
 public async Task<RequestDetail?> ManageAsync(Guid property,Guid id,ManageRequest c,CancellationToken token){
  await Authorize(property,token);
  if(c.OperationId==Guid.Empty || c.Version<1 || string.IsNullOrWhiteSpace(c.Response) || c.Response.Trim().Length>2000 || c.Status is not("RECORDED" or "IN_PROGRESS" or "RESOLVED" or "REJECTED"))throw new ArgumentException("Invalid management command.");
  var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{property,id,c.Version,c.Status,Response=c.Response.Trim()}))));
  await using var tx=await db.Database.BeginTransactionAsync(token);
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({actor.UserId+":request-management:"+c.OperationId},0))",token);
  // The same lock as mobile edits/deletes prevents a management event racing an offline command.
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"mobile-record:"+id},0))",token);
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",token);
  await Authorize(property,token);
  var prior=await db.Set<RequestManagementEvent>().IgnoreQueryFilters().SingleOrDefaultAsync(e=>e.ActorUserId==actor.UserId && e.OperationId==c.OperationId,token);
  if(prior!=null){if(prior.Fingerprint!=fingerprint)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());return await Read(property,id,true,token);}
  var record=await Rows(property).SingleOrDefaultAsync(r=>r.Id==id,token);if(record==null)return null;
  if(record.Version!=c.Version || !MobileRequest.CanTransition(record.ManagementStatus,c.Status))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
  var ev=new RequestManagementEvent(property,id,record.ManagementStatus,c.Status,c.Response.Trim(),actor.UserId!,c.OperationId,fingerprint,c.Version);
  record.Manage(c.Status,c.Response);db.Add(ev);await db.SaveChangesAsync(token);
  var result=await Read(property,id,false,token);await tx.CommitAsync(token);return result;
 }
}
