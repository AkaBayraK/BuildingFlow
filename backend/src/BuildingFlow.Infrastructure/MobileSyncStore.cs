using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MobileSyncStore(BuildingFlowDbContext db, IMobileSessionStore sessions, ICurrentUserService actor) : IMobileSyncStore {
    private static readonly string[] Management=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"];
    private static string Key(Guid property,Guid? unit,string role)=>$"{property}:{(unit.HasValue?unit.Value.ToString():"-")}:{role}";
    private string User()=>actor.IsAuthenticated && !string.IsNullOrWhiteSpace(actor.UserId)?actor.UserId:throw new UnauthorizedAccessException();
    private static HierarchyConflictException Conflict()=>new(ErrorCatalog.MobileSyncConflict.Create());
    private async Task Authorize(Guid property,Guid? unit,string role,CancellationToken token) {
        var user=User();
        var session=await sessions.ReadAsync(user,actor.Username,Management.Where(actor.IsInRole).ToArray(),token);
        if(session.HasMore || role=="ACCOUNTANT" || !session.Contexts.Any(c=>c.PropertyId==property && c.UnitId==unit && c.Role==role)) throw new UnauthorizedAccessException();
    }
    private static MobileRequestDto Dto(MobileRequest r)=>new(r.Id,r.PropertyId,r.UnitId,r.ContextRole,r.IsDeleted?null:r.Description,r.Version,r.IsDeleted,r.ModifiedAt);
    public async Task<MobileSyncResult> PushAsync(MobileSyncCommand c,CancellationToken token) {
        if(c.OperationId==Guid.Empty || c.RecordId==Guid.Empty || c.PropertyId==Guid.Empty || c.UnitId==Guid.Empty || c.ExpectedVersion<0 || c.Action is not ("CREATE" or "UPDATE" or "DELETE") || (c.Action!="DELETE" && (string.IsNullOrWhiteSpace(c.Description) || c.Description.Trim().Length>2000)) || (c.Action=="CREATE" && c.ExpectedVersion!=0) || (c.Action!="CREATE" && c.ExpectedVersion<1) || (c.Action=="DELETE" && c.Description!=null)) throw new ArgumentException("Invalid mobile command.");
        var user=User();
        var canonical=c with {Description=c.Description?.Trim()};
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical))));
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
            record=new(c.RecordId,c.PropertyId,c.UnitId,c.Role,user,canonical.Description!); db.Add(record);
        } else {
            if(record==null || record.OwnerUserId!=user || record.PropertyId!=c.PropertyId || record.UnitId!=c.UnitId || record.ContextRole!=c.Role) throw new UnauthorizedAccessException();
            if(record.IsDeleted || !record.IsEnabled || record.Version!=c.ExpectedVersion) throw Conflict();
            if(c.Action=="DELETE") db.Remove(record); else record.Edit(canonical.Description!);
        }
        await db.SaveChangesAsync(token);
        var result=Dto(record);
        db.Add(new MobileOperationReceipt(user,c.OperationId,fingerprint,JsonSerializer.Serialize(result)));
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(c.OperationId,result,false);
    }
    public async Task<MobileRequestSnapshot> PullAsync(Guid property,Guid? unit,string role,CancellationToken token) {
        var user=User(); await Authorize(property,unit,role,token);
        var rows=await db.Set<MobileRequest>().IgnoreQueryFilters().AsNoTracking().Where(r=>r.OwnerUserId==user && r.PropertyId==property && r.UnitId==unit && r.ContextRole==role).OrderBy(r=>r.Id).Take(1001).ToArrayAsync(token);
        return new(user,Key(property,unit,role),rows.Take(1000).Select(Dto).ToArray(),rows.Length>1000);
    }
}
