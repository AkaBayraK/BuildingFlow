using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;
public sealed class MobileSyncStore(BuildingFlowDbContext db, IMobileSessionStore sessions, ICurrentUserService actor,IFileStorage storage,TimeProvider clock,ISessionContextStore contexts,IConfiguration config) : IMobileSyncStore {
    private static readonly string[] Management=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"];
    private static string Key(Guid property,Guid? unit,string role)=>$"{property}:{(unit.HasValue?unit.Value.ToString():"-")}:{role}";
    private string User()=>actor.IsAuthenticated && !string.IsNullOrWhiteSpace(actor.UserId)?actor.UserId:throw new UnauthorizedAccessException();
    private static HierarchyConflictException Conflict()=>new(ErrorCatalog.MobileSyncConflict.Create());
    private async Task<SessionContext?> Authorize(Guid property,Guid? unit,string role,CancellationToken token,string? kind=null,Guid? id=null) {
        if(config.GetValue<bool>("PersonRefactor:SessionContextsEnabled")){
            if(!config.GetValue<bool>("PersonRefactor:Enabled") || !config.GetValue<bool>("PersonRefactor:MobileAuthorizationEnabled") || !config.GetValue<bool>("PersonRefactor:MobileSyncEnabled") || id==null || id==Guid.Empty || kind is not ("PLATFORM" or "MANAGEMENT" or "OWNER" or "TENANT" or "RESIDENT") || role=="ACCOUNTANT")throw new UnauthorizedAccessException();
            var c=(await contexts.GetAsync(kind,id.Value,token))?.Context;
            if(c==null || c.PropertyId!=property || c.UnitId!=unit || c.Role!=role)throw new UnauthorizedAccessException();
            return c;
        }
        if(kind!=null || id!=null)throw new UnauthorizedAccessException();
        var user=User();
        var session=await sessions.ReadAsync(user,actor.Username,Management.Where(actor.IsInRole).ToArray(),token);
        if(session.HasMore || role=="ACCOUNTANT" || !session.Contexts.Any(c=>c.PropertyId==property && c.UnitId==unit && c.Role==role)) throw new UnauthorizedAccessException();
        return null;
    }
    private MobileRequestDto Dto(MobileRequest r)=>new(r.Id,r.PropertyId,r.UnitId,r.ContextRole,r.IsDeleted?null:r.Description,r.Version,r.IsDeleted,r.ModifiedAt,r.Category,r.Priority,r.IsDeleted||r.PhotoId==null||!ConsentGate.VisibleToOwner(db,db.Set<MobileRequestPhoto>().Where(p=>p.Id==r.PhotoId && p.State=="CURRENT")).Any()?null:r.PhotoId,r.IsDeleted||r.PhotoId==null?null:ConsentGate.VisibleToOwner(db,db.Set<MobileRequestPhoto>().Where(p=>p.Id==r.PhotoId && p.OwnerUserId==r.OwnerUserId && p.PropertyId==r.PropertyId && p.State=="CURRENT")).Select(p=>p.Sha256).SingleOrDefault(),r.ManagementStatus,r.ManagementResponse,r.HasManagementHistory);
    public async Task<MobileSyncResult> PushAsync(MobileSyncCommand c,CancellationToken token) {
        if(c.OperationId==Guid.Empty || c.RecordId==Guid.Empty || c.PropertyId==Guid.Empty || c.UnitId==Guid.Empty || c.ExpectedVersion<0 || c.Action is not ("CREATE" or "UPDATE" or "DELETE") || (c.Action!="DELETE" && (string.IsNullOrWhiteSpace(c.Description) || c.Description.Trim().Length>2000)) || (c.Action=="CREATE" && c.ExpectedVersion!=0) || (c.Action!="CREATE" && c.ExpectedVersion<1) || (c.Action=="DELETE" && c.Description!=null)) throw new ArgumentException("Invalid mobile command.");
        var user=User();
        if(c.Category!=null && !MobileRequest.Categories.Contains(c.Category) || c.Priority is not (null or "LOW" or "NORMAL" or "HIGH") || c.PhotoAction is not ("KEEP" or "REPLACE" or "REMOVE") || (c.PhotoAction!="REPLACE" && c.PhotoBase64!=null) || (c.Action=="DELETE" && (c.Category!=null || c.Priority!=null || c.PhotoAction!="KEEP")))throw new ArgumentException("Invalid request details.");
        var context=await Authorize(c.PropertyId,c.UnitId,c.Role,token,c.ContextKind,c.ContextId);
        byte[]? bytes=null;
        if(c.PhotoAction=="REPLACE"){
            if(c.PhotoBase64==null || c.PhotoBase64.Length>350000)throw new ArgumentException("Invalid request photo.");
            try{bytes=ProfilePhoto.Normalize(Convert.FromBase64String(c.PhotoBase64));}catch(FormatException){throw new ArgumentException("Invalid request photo encoding.");}
        }
        var sha=bytes==null?null:Convert.ToHexString(SHA256.HashData(bytes));
        var canonical=c with {Description=c.Description?.Trim(),PhotoBase64=sha};
        // Old queued commands retain their original fingerprint and receipt compatibility.
        var legacy=c.Category==null && c.Priority==null && c.PhotoAction=="KEEP" && c.ContextId==null && c.ContextKind==null;
        var serialized=legacy?JsonSerializer.Serialize(new{c.OperationId,c.RecordId,c.PropertyId,c.UnitId,c.Role,c.Action,c.ExpectedVersion,Description=canonical.Description}):(c.ContextId==null?"request-v2:"+JsonSerializer.Serialize(new{canonical.OperationId,canonical.RecordId,canonical.PropertyId,canonical.UnitId,canonical.Role,canonical.Action,canonical.ExpectedVersion,canonical.Description,canonical.Category,canonical.Priority,canonical.PhotoAction,canonical.PhotoBase64}):"request-v3:"+JsonSerializer.Serialize(canonical));
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized)));
        MobileRequestPhoto? staged=null;
        // T17.3: a new request photo needs an ACTIVE REQUEST_PHOTO consent; the request itself can always be saved without one.
        if(bytes!=null && !await db.Set<MobileOperationReceipt>().AnyAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token))await ConsentGate.RequireAsync(db,user,ConsentFeature.REQUEST_PHOTO,token);
        if(bytes!=null){
            staged=await db.Set<MobileRequestPhoto>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token);
            if(staged==null){
                staged=new(user,c.PropertyId,c.RecordId,c.OperationId,fingerprint,sha!,bytes.Length);db.Add(staged);
                try{await db.SaveChangesAsync(token);}catch(DbUpdateException e) when(e.InnerException is Npgsql.PostgresException {SqlState:"23505"}){db.Entry(staged).State=EntityState.Detached;staged=await db.Set<MobileRequestPhoto>().IgnoreQueryFilters().SingleAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token);}
            }
            if(staged.IsDeleted || !staged.IsEnabled || staged.Fingerprint!=fingerprint)throw Conflict();
        }
        await using var tx=await db.Database.BeginTransactionAsync(token);
        if(context!=null){
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={context.TenantId} FOR UPDATE",token);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={context.PropertyId} FOR UPDATE",token);
        }
        // Serialize identical operation IDs and record IDs, including concurrent replay attempts.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({user+":"+c.OperationId},0))",token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"mobile-record:"+c.RecordId},0))",token);
        await Authorize(c.PropertyId,c.UnitId,c.Role,token,c.ContextKind,c.ContextId);
        var receipt=await db.Set<MobileOperationReceipt>().IgnoreQueryFilters().SingleOrDefaultAsync(r=>r.OwnerUserId==user && r.OperationId==c.OperationId,token);
        if(receipt!=null) {
            if(receipt.IsDeleted || receipt.Fingerprint!=fingerprint) throw Conflict();
            return new(c.OperationId,JsonSerializer.Deserialize<MobileRequestDto>(receipt.Result)!,true);
        }
        if(bytes!=null){await ConsentGate.LockAsync(db,user,token);await ConsentGate.RequireAsync(db,user,ConsentFeature.REQUEST_PHOTO,token);}
        var record=await db.Set<MobileRequest>().IgnoreQueryFilters().SingleOrDefaultAsync(r=>r.Id==c.RecordId,token);
        if(c.Action=="CREATE") {
            if(record!=null) throw Conflict();
            record=new(c.RecordId,c.PropertyId,c.UnitId,c.Role,user,canonical.Description!); db.Add(record);
        } else {
            if(record==null || record.OwnerUserId!=user || record.PropertyId!=c.PropertyId || record.UnitId!=c.UnitId || record.ContextRole!=c.Role) throw new UnauthorizedAccessException();
            if(record.IsDeleted || !record.IsEnabled || record.Version!=c.ExpectedVersion || record.HasManagementHistory) throw Conflict();
            if(c.Action=="DELETE") db.Remove(record); else record.Edit(canonical.Description!);
        }
        var photoId=record.PhotoId;
        if(c.Action=="DELETE" || c.PhotoAction!="KEEP"){
            if(photoId is Guid old){var previous=await db.Set<MobileRequestPhoto>().SingleAsync(x=>x.Id==old && x.OwnerUserId==user && x.PropertyId==c.PropertyId,token);previous.Retire();}
            photoId=null;
            if(c.Action!="DELETE" && staged!=null && bytes!=null){await db.Entry(staged).ReloadAsync(token);using var content=new MemoryStream(bytes);await storage.UploadAsync(staged.ObjectKey,content,"image/jpeg",token);staged.Current();photoId=staged.Id;}
        }
        record.Details(c.Category??record.Category,c.Priority??record.Priority,photoId);
        await db.SaveChangesAsync(token);
        var result=Dto(record);
        db.Add(new MobileOperationReceipt(user,c.OperationId,fingerprint,JsonSerializer.Serialize(result)));
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(c.OperationId,result,false);
    }
    public async Task<MobileRequestSnapshot> PullAsync(Guid property,Guid? unit,string role,CancellationToken token,string? contextKind=null,Guid? contextId=null) {
        var user=User();var context=await Authorize(property,unit,role,token,contextKind,contextId);
        var rows=await db.Set<MobileRequest>().IgnoreQueryFilters().AsNoTracking().Where(r=>r.OwnerUserId==user && r.PropertyId==property && r.UnitId==unit && r.ContextRole==role).OrderBy(r=>r.Id).Take(1001).ToArrayAsync(token);
        var ids=rows.Select(r=>r.PhotoId).Where(x=>x!=null).ToArray();
        var hashes=await ConsentGate.VisibleToOwner(db,db.Set<MobileRequestPhoto>().AsNoTracking().Where(x=>ids.Contains(x.Id) && x.OwnerUserId==user && x.PropertyId==property && x.State=="CURRENT")).ToDictionaryAsync(x=>x.Id,x=>x.Sha256,token);
        return new(user,context==null?Key(property,unit,role):$"{context.TenantId}:{context.Kind}:{context.Id}",rows.Take(1000).Select(r=>new MobileRequestDto(r.Id,r.PropertyId,r.UnitId,r.ContextRole,r.IsDeleted?null:r.Description,r.Version,r.IsDeleted,r.ModifiedAt,r.Category,r.Priority,!r.IsDeleted && r.PhotoId is Guid visible && hashes.ContainsKey(visible)?visible:null,!r.IsDeleted && r.PhotoId is Guid id && hashes.TryGetValue(id,out var hash)?hash:null,r.ManagementStatus,r.ManagementResponse,r.HasManagementHistory)).ToArray(),rows.Length>1000);
    }
    public async Task<byte[]?> PhotoAsync(Guid request,CancellationToken token,string? contextKind=null,Guid? contextId=null){
        var user=User();var record=await db.Set<MobileRequest>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==request && x.IsEnabled,token);if(record==null)return null;
        if(record.OwnerUserId==user)await Authorize(record.PropertyId,record.UnitId,record.ContextRole,token,contextKind,contextId);
        else{
            if(config.GetValue<bool>("PersonRefactor:SessionContextsEnabled")){
                if(!await contexts.PropertyAllowedAsync(record.PropertyId,"MANAGER",token))throw new UnauthorizedAccessException();
            }else{
            var roles=Management.Where(actor.IsInRole).Where(r=>r!="ACCOUNTANT").ToArray();
            if(roles.Length==0 || !await db.Properties.AnyAsync(p=>p.Id==record.PropertyId && p.IsEnabled && (roles.Contains("SUPER_ADMIN") || db.PropertyMemberships.Any(m=>m.PropertyId==p.Id && m.UserId==user && m.IsEnabled)),token))throw new UnauthorizedAccessException();
            }
        }
        var photos=db.Set<MobileRequestPhoto>().AsNoTracking();
        // The owner loses access after withdrawing REQUEST_PHOTO consent; the centre keeps it (T17.4).
        if(record.OwnerUserId==user)photos=ConsentGate.VisibleToOwner(db,photos);
        var photo=await photos.SingleOrDefaultAsync(x=>x.Id==record.PhotoId && x.RequestId==record.Id && x.OwnerUserId==record.OwnerUserId && x.PropertyId==record.PropertyId && x.IsEnabled && x.State=="CURRENT",token);if(photo==null)return null;
        await using var stream=await storage.DownloadAsync(photo.ObjectKey,token);using var output=new MemoryStream();var buffer=new byte[8192];int count;
        while((count=await stream.ReadAsync(buffer,token))>0){if(output.Length+count>ProfilePhoto.MaxBytes)throw new StorageUnavailableException();await output.WriteAsync(buffer.AsMemory(0,count),token);}
        var bytes=output.ToArray();if(bytes.Length!=photo.Size || Convert.ToHexString(SHA256.HashData(bytes))!=photo.Sha256)throw new StorageUnavailableException();return bytes;
    }
    public async Task MaintainAsync(CancellationToken token){
        var expiry=clock.GetUtcNow().AddMinutes(-30);
        var photos=await db.Set<MobileRequestPhoto>().Where(x=>x.State=="RETIRED" || x.State=="STAGED" && x.CreatedAt<expiry).OrderBy(x=>x.CreatedAt).Take(5).ToArrayAsync(token);
        foreach(var photo in photos){await using var tx=await db.Database.BeginTransactionAsync(token);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"mobile-record:"+photo.RequestId},0))",token);
            await db.Entry(photo).ReloadAsync(token);
            if(photo.State is not ("STAGED" or "RETIRED") || photo.State=="STAGED" && photo.CreatedAt>=expiry || await db.Set<MobileRequest>().IgnoreQueryFilters().AnyAsync(x=>x.PhotoId==photo.Id,token))continue;
            await storage.DeleteAsync(photo.ObjectKey,token);photo.Purged();await db.SaveChangesAsync(token);await tx.CommitAsync(token);
        }
    }

}
