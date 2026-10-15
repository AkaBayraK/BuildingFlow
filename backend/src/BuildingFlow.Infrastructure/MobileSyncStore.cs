using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MobileSyncStore(BuildingFlowDbContext db, IMobileSessionStore sessions, ICurrentUserService actor, IFileStorage storage, TimeProvider clock) : IMobileSyncStore {
    private static readonly string[] Management=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"];
    private static string Key(Guid property,Guid? unit,string role)=>$"{property}:{(unit.HasValue?unit.Value.ToString():"-")}:{role}";
    private string User()=>actor.IsAuthenticated && !string.IsNullOrWhiteSpace(actor.UserId)?actor.UserId:throw new UnauthorizedAccessException();
    private static HierarchyConflictException Conflict()=>new(ErrorCatalog.MobileSyncConflict.Create());
    private async Task Authorize(Guid property,Guid? unit,string role,CancellationToken token) {
        var user=User();
        var session=await sessions.ReadAsync(user,actor.Username,Management.Where(actor.IsInRole).ToArray(),token);
        if(session.HasMore || role=="ACCOUNTANT" || !session.Contexts.Any(c=>c.PropertyId==property && c.UnitId==unit && c.Role==role)) throw new UnauthorizedAccessException();
    }
    private static MobileRequestDto Dto(MobileRequest r,MobileRequestPhotoDto[] photos)=>new(r.Id,r.PropertyId,r.UnitId,r.ContextRole,r.IsDeleted?null:r.Description,r.Version,r.IsDeleted,r.ModifiedAt,r.IsDeleted?null:r.Category,r.IsDeleted?null:r.Priority,r.IsDeleted?[]:photos);
    private Task<MobileRequestPhotoDto[]> Photos(Guid request,CancellationToken token)=>db.Set<MobileRequestPhoto>().AsNoTracking().Where(p=>p.RequestId==request && p.State=="CURRENT" && p.IsEnabled).OrderBy(p=>p.CreatedAt).ThenBy(p=>p.Id).Select(p=>new MobileRequestPhotoDto(p.Id,p.Sha256,p.Size)).ToArrayAsync(token);
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    // Commands without category/priority keep the pre-phase-12 fingerprint so queued legacy replays stay idempotent.
    private static string Fingerprint(MobileSyncCommand c)=>Hash(c.Category==null && c.Priority==null?JsonSerializer.Serialize(new {c.OperationId,c.RecordId,c.PropertyId,c.UnitId,c.Role,c.Action,c.ExpectedVersion,c.Description}):JsonSerializer.Serialize(c));
    public async Task<MobileSyncResult> PushAsync(MobileSyncCommand c,CancellationToken token) {
        var edit=c.Action!="DELETE";
        if(c.OperationId==Guid.Empty || c.RecordId==Guid.Empty || c.PropertyId==Guid.Empty || c.UnitId==Guid.Empty || c.ExpectedVersion<0 || c.Action is not ("CREATE" or "UPDATE" or "DELETE") || (edit && (string.IsNullOrWhiteSpace(c.Description) || c.Description.Trim().Length>2000)) || (c.Action=="CREATE" && c.ExpectedVersion!=0) || (c.Action!="CREATE" && c.ExpectedVersion<1) || (!edit && (c.Description!=null || c.Category!=null || c.Priority!=null)) || (c.Category!=null && !MobileRequest.Categories.Contains(c.Category)) || (c.Priority!=null && !MobileRequest.Priorities.Contains(c.Priority))) throw new ArgumentException("Invalid mobile command.");
        var user=User();
        var canonical=c with {Description=c.Description?.Trim()};
        var fingerprint=Fingerprint(canonical);
        await using var tx=await db.Database.BeginTransactionAsync(token);
        // Serialize identical operation IDs and record IDs, including concurrent replay attempts.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({user+":"+c.OperationId},0))",token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"mobile-record:"+c.RecordId},0))",token);
        await Authorize(c.PropertyId,c.UnitId,c.Role,token);
        var receipt=await db.Set<MobileOperationReceipt>().IgnoreQueryFilters().SingleOrDefaultAsync(r=>r.OwnerUserId==user && r.OperationId==c.OperationId,token);
        if(receipt!=null) {
            if(receipt.IsDeleted || receipt.Fingerprint!=fingerprint) throw Conflict();
            return new(c.OperationId,JsonSerializer.Deserialize<MobileRequestDto>(receipt.Result)!,true);
        }
        var record=await db.Set<MobileRequest>().IgnoreQueryFilters().SingleOrDefaultAsync(r=>r.Id==c.RecordId,token);
        if(c.Action=="CREATE") {
            if(record!=null) throw Conflict();
            record=new(c.RecordId,c.PropertyId,c.UnitId,c.Role,user,canonical.Description!,c.Category??"OTHER",c.Priority??"NORMAL"); db.Add(record);
        } else {
            if(record==null || record.OwnerUserId!=user || record.PropertyId!=c.PropertyId || record.UnitId!=c.UnitId || record.ContextRole!=c.Role) throw new UnauthorizedAccessException();
            if(record.IsDeleted || !record.IsEnabled || record.Version!=c.ExpectedVersion) throw Conflict();
            if(c.Action=="DELETE") {
                db.Remove(record);
                foreach(var photo in await db.Set<MobileRequestPhoto>().Where(p=>p.RequestId==record.Id && p.State=="CURRENT").ToArrayAsync(token)) photo.Retire();
            } else record.Edit(canonical.Description!,c.Category??record.Category,c.Priority??record.Priority);
        }
        await db.SaveChangesAsync(token);
        var result=Dto(record,await Photos(record.Id,token));
        db.Add(new MobileOperationReceipt(user,c.OperationId,fingerprint,JsonSerializer.Serialize(result)));
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(c.OperationId,result,false);
    }
    public async Task<MobileRequestSnapshot> PullAsync(Guid property,Guid? unit,string role,CancellationToken token) {
        var user=User(); await Authorize(property,unit,role,token);
        var rows=await db.Set<MobileRequest>().IgnoreQueryFilters().AsNoTracking().Where(r=>r.OwnerUserId==user && r.PropertyId==property && r.UnitId==unit && r.ContextRole==role).OrderBy(r=>r.Id).Take(1001).ToArrayAsync(token);
        var ids=rows.Take(1000).Where(r=>!r.IsDeleted).Select(r=>r.Id).ToArray();
        var photos=(await db.Set<MobileRequestPhoto>().AsNoTracking().Where(p=>ids.Contains(p.RequestId) && p.State=="CURRENT" && p.IsEnabled).OrderBy(p=>p.CreatedAt).ThenBy(p=>p.Id).ToArrayAsync(token)).ToLookup(p=>p.RequestId,p=>new MobileRequestPhotoDto(p.Id,p.Sha256,p.Size));
        return new(user,Key(property,unit,role),rows.Take(1000).Select(r=>Dto(r,photos[r.Id].ToArray())).ToArray(),rows.Length>1000);
    }
    private async Task<MobileRequest> OwnRequest(Guid request,MobileRequestPhotoCommand c,string user,CancellationToken token) {
        var r=await db.Set<MobileRequest>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Id==request,token);
        if(r==null || r.OwnerUserId!=user || r.PropertyId!=c.PropertyId || r.UnitId!=c.UnitId || r.ContextRole!=c.Role) throw new UnauthorizedAccessException();
        if(r.IsDeleted || !r.IsEnabled) throw Conflict();
        return r;
    }
    public async Task<MobileRequestPhotoResult> AttachPhotoAsync(Guid request,MobileRequestPhotoCommand c,CancellationToken token) {
        if(request==Guid.Empty || c.OperationId==Guid.Empty || c.PropertyId==Guid.Empty || c.UnitId==Guid.Empty || string.IsNullOrEmpty(c.Role) || c.PhotoBase64==null || c.PhotoBase64.Length>350000) throw new ArgumentException("Invalid photo command.");
        byte[] bytes;
        try {bytes=ProfilePhoto.Normalize(Convert.FromBase64String(c.PhotoBase64));}catch(FormatException){throw new ArgumentException("Invalid image encoding.");}
        var user=User(); await Authorize(c.PropertyId,c.UnitId,c.Role,token);
        var hash=Convert.ToHexString(SHA256.HashData(bytes));
        var fingerprint=Hash("request-photo:"+request+":"+JsonSerializer.Serialize(c with {PhotoBase64=hash}));
        // Durable reservation before upload: failed or uncertain storage writes are reclaimed by maintenance.
        var staged=await db.Set<MobileRequestPhoto>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token);
        if(staged==null) {
            var owner=await OwnRequest(request,c,user,token);
            // Early limit check avoids reserving objects that can never become current; the locked check below still decides races.
            if(await db.Set<MobileRequestPhoto>().CountAsync(x=>x.RequestId==owner.Id && x.State=="CURRENT",token)>=MobileRequest.MaxPhotos) throw new ArgumentException("Photo limit reached.");
            staged=new(owner.Id,owner.PropertyId,user,c.OperationId,fingerprint,hash,bytes.Length); db.Add(staged);
            try {await db.SaveChangesAsync(token);}
            catch(DbUpdateException e) when(e.InnerException is Npgsql.PostgresException pg && pg.SqlState=="23505") {
                db.Entry(staged).State=EntityState.Detached;
                staged=await db.Set<MobileRequestPhoto>().IgnoreQueryFilters().SingleAsync(x=>x.OwnerUserId==user && x.OperationId==c.OperationId,token);
            }
            db.ChangeTracker.Clear();
        }
        if(staged.Fingerprint!=fingerprint || staged.RequestId!=request || staged.IsDeleted || !staged.IsEnabled) throw Conflict();
        await using var tx=await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({user+":"+c.OperationId},0))",token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"mobile-record:"+request},0))",token);
        await Authorize(c.PropertyId,c.UnitId,c.Role,token);
        var receipt=await db.Set<MobileOperationReceipt>().IgnoreQueryFilters().SingleOrDefaultAsync(r=>r.OwnerUserId==user && r.OperationId==c.OperationId,token);
        if(receipt!=null) {
            if(receipt.IsDeleted || receipt.Fingerprint!=fingerprint) throw Conflict();
            return JsonSerializer.Deserialize<MobileRequestPhotoResult>(receipt.Result)! with {Replayed=true};
        }
        var record=await OwnRequest(request,c,user,token);
        var photo=await db.Set<MobileRequestPhoto>().SingleAsync(x=>x.Id==staged.Id,token);
        if(photo.State!="STAGED") throw Conflict();
        if(await db.Set<MobileRequestPhoto>().CountAsync(x=>x.RequestId==record.Id && x.State=="CURRENT",token)>=MobileRequest.MaxPhotos) throw new ArgumentException("Photo limit reached.");
        using(var stream=new MemoryStream(bytes)) await storage.UploadAsync(photo.ObjectKey,stream,"image/jpeg",token);
        photo.Current();
        await db.SaveChangesAsync(token);
        var result=new MobileRequestPhotoResult(c.OperationId,request,new(photo.Id,photo.Sha256,photo.Size),false);
        db.Add(new MobileOperationReceipt(user,c.OperationId,fingerprint,JsonSerializer.Serialize(result)));
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return result;
    }
    public async Task<byte[]?> PhotoAsync(Guid request,Guid id,Guid property,Guid? unit,string role,CancellationToken token) {
        var user=User(); await Authorize(property,unit,role,token);
        var photo=await db.Set<MobileRequestPhoto>().AsNoTracking().SingleOrDefaultAsync(p=>p.Id==id && p.RequestId==request && p.OwnerUserId==user && p.State=="CURRENT" && p.IsEnabled && db.Set<MobileRequest>().Any(r=>r.Id==request && r.OwnerUserId==user && r.PropertyId==property && r.UnitId==unit && r.ContextRole==role && r.IsEnabled),token);
        if(photo==null) return null;
        await using var stream=await storage.DownloadAsync(photo.ObjectKey,token);
        using var output=new MemoryStream(); var buffer=new byte[8192]; int read;
        while((read=await stream.ReadAsync(buffer,token))>0){if(output.Length+read>ProfilePhoto.MaxBytes)throw new StorageUnavailableException();await output.WriteAsync(buffer.AsMemory(0,read),token);}
        var bytes=output.ToArray();
        if(bytes.Length!=photo.Size || Convert.ToHexString(SHA256.HashData(bytes))!=photo.Sha256) throw new StorageUnavailableException();
        return bytes;
    }
    public async Task MaintainPhotosAsync(CancellationToken token) {
        var expiry=clock.GetUtcNow().AddMinutes(-30);
        var candidates=await db.Set<MobileRequestPhoto>().Where(x=>x.State=="RETIRED" || (x.State=="STAGED" && x.CreatedAt<expiry)).OrderBy(x=>x.CreatedAt).Take(10).ToArrayAsync(token);
        foreach(var photo in candidates) {
            await using var tx=await db.Database.BeginTransactionAsync(token);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"mobile-record:"+photo.RequestId},0))",token);
            await db.Entry(photo).ReloadAsync(token);
            if(photo.State is not ("STAGED" or "RETIRED") || (photo.State=="STAGED" && photo.CreatedAt>=expiry)) continue;
            await storage.DeleteAsync(photo.ObjectKey,token); photo.Purged(); await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        }
    }
}
