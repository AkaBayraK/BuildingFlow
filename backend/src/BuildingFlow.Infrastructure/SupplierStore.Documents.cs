using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private async Task<bool> AreaAllowed(Guid tenant, Guid supplier, Guid property, CancellationToken token) =>
        await Allowed(tenant, token) && await PropertyScopes(tenant).AnyAsync(s => s.PropertyId == property, token) &&
        (await management.EvaluateAsync(tenant,property,"MANAGER",token)).Allowed &&
        await db.Set<SupplierManagementArea>().AnyAsync(a => a.TenantId == tenant && a.SupplierId == supplier && a.PropertyId == property && a.IsEnabled, token);
    private async Task<(Supplier Supplier, Property Property)?> LockArea(Guid tenant, Guid supplier, Guid property, bool active, CancellationToken token)
    {
        if (!await AreaAllowed(tenant,supplier,property,token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE",token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",token);
        if (!await AreaAllowed(tenant,supplier,property,token)) return null;
        var row = await Rows(tenant).SingleOrDefaultAsync(s => s.Id == supplier,token); if (row is null) return null;
        if (active && row.Status != SupplierStatus.ACTIVE) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        var parent = await db.Properties.SingleAsync(p => p.Id == property,token); return (row,parent);
    }
    private static void Expected(long expected,long current) { if(expected != current) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    private void TouchArea((Supplier Supplier,Property Property) scope)
    { db.Entry(scope.Supplier).Property(x=>x.IsEnabled).IsModified=true; db.Entry(scope.Property).Property(x=>x.IsEnabled).IsModified=true; }
    private IQueryable<SupplierDocument> Files(Guid tenant,Guid supplier) => db.Set<SupplierDocument>().Where(d=>d.TenantId==tenant && d.SupplierId==supplier);
    private static SupplierFileResponse FileResponse(SupplierDocument d,StoredDocument f,bool current,string propertyName) => new(d.Id,d.TenantId,d.SupplierId,d.PropertyId,f.Id,f.FileName,f.ContentType,f.Size,f.Sha256,d.ExpiresOn,d.PreviousDocumentId,d.RevisionNumber,current,d.Version,d.CreatedAt,propertyName);
    public async Task<PagedRows<SupplierFileResponse>?> Documents(Guid tenant,Guid supplier,int page,int pageSize,CancellationToken token)
    {
        Page(page,pageSize); await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        if(!await Allowed(tenant,token) || !await Rows(tenant).AnyAsync(s=>s.Id==supplier,token))return null;
        var scopes=PropertyScopes(tenant);
        var rows=from d in Files(tenant,supplier).AsNoTracking() join f in db.StoredDocuments on new{d.PropertyId,Id=d.DocumentId} equals new{f.PropertyId,f.Id}
                 join property in db.Properties on d.PropertyId equals property.Id
                 where d.IsEnabled && f.IsEnabled && f.IsSupplierOnly && scopes.Any(s=>s.PropertyId==d.PropertyId)
                 select new{Link=d,File=f,PropertyName=property.Name,Current=!db.Set<SupplierDocument>().IgnoreQueryFilters().Any(next=>next.TenantId==tenant && next.SupplierId==supplier && next.PropertyId==d.PropertyId && next.PreviousDocumentId==d.Id)};
        var total=await rows.CountAsync(token);var values=await rows.OrderByDescending(x=>x.Link.CreatedAt).ThenBy(x=>x.Link.Id).Skip((page-1)*pageSize).Take(pageSize).ToArrayAsync(token);
        await tx.CommitAsync(token);return new(values.Select(x=>FileResponse(x.Link,x.File,x.Current,x.PropertyName)).ToArray(),page,pageSize,total);
    }
    public async Task<SupplierFileMutation?> Upload(Guid tenant,Guid supplier,SupplierFileUpload c,CancellationToken token)
    {
        if(c.OperationId==Guid.Empty || c.SupplierVersion<1 || c.PropertyId==Guid.Empty || c.PreviousDocumentId==Guid.Empty || c.ExpiresOn is {Year:<2000 or >2100})InvalidSupplier("file");
        CommunicationService.ValidateFile(c.FileName,c.ContentType,c.Bytes);
        await using var tx=await db.Database.BeginTransactionAsync(token);var scope=await LockArea(tenant,supplier,c.PropertyId,true,token);if(scope is null)return null;
        var hash=Convert.ToHexString(SHA256.HashData(c.Bytes));var fingerprint=Hash(new{action="SUPPLIER_FILE",tenant,supplier,c.PropertyId,c.SupplierVersion,c.FileName,c.ContentType,hash,c.ExpiresOn,c.PreviousDocumentId});
        var old=await db.StoredDocuments.IgnoreQueryFilters().SingleOrDefaultAsync(f=>f.PropertyId==c.PropertyId && f.RequestId==c.OperationId,token);
        if(old is not null)
        {
            var link=await Files(tenant,supplier).SingleOrDefaultAsync(d=>d.PropertyId==c.PropertyId && d.DocumentId==old.Id && d.IsEnabled,token);
            if(old.IsDeleted || !old.IsEnabled || !old.IsSupplierOnly || old.RequestFingerprint!=fingerprint || link is null)throw new HierarchyConflictException(ErrorCatalog.DocumentRequestConflict.Create());
            var current=!await Files(tenant,supplier).AnyAsync(d=>d.PreviousDocumentId==link.Id,token);await tx.CommitAsync(token);return new(FileResponse(link,old,current,scope.Value.Property.Name),Response(scope.Value.Supplier),true);
        }
        Expected(c.SupplierVersion,scope.Value.Supplier.Version);SupplierDocument? previous=null;
        if(c.PreviousDocumentId.HasValue)
        {
            previous=await Files(tenant,supplier).SingleOrDefaultAsync(d=>d.Id==c.PreviousDocumentId && d.PropertyId==c.PropertyId && d.IsEnabled,token);if(previous is null)return null;
            if(await Files(tenant,supplier).AnyAsync(d=>d.PreviousDocumentId==previous.Id,token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        }
        var file=new StoredDocument(c.PropertyId,c.OperationId,fingerprint,null,ContentAudience.MANAGEMENT,c.FileName,c.ContentType,c.Bytes.LongLength,hash,true);
        var value=new SupplierDocument(tenant,supplier,c.PropertyId,file.Id,c.ExpiresOn,previous?.Id,(previous?.RevisionNumber??0)+1);var saved=false;
        try
        {
            using var content=new MemoryStream(c.Bytes,writable:false);await storage.UploadAsync(file.ObjectKey,content,c.ContentType,token);
            db.AddRange(file,value);TouchArea(scope.Value);await db.SaveChangesAsync(token);saved=true;await tx.CommitAsync(token);
            return new(FileResponse(value,file,true,scope.Value.Property.Name),Response(scope.Value.Supplier),false);
        }
        catch
        {
            if(saved){logger.LogWarning("Supplier document commit outcome requires storage maintenance.");throw;}
            using var cleanup=new CancellationTokenSource(TimeSpan.FromSeconds(5));try{await storage.DeleteAsync(file.ObjectKey,cleanup.Token);}catch{logger.LogWarning("Supplier document cleanup requires storage maintenance.");}throw;
        }
    }
    public async Task<SupplierFileDownload?> Download(Guid tenant,Guid supplier,Guid document,CancellationToken token)
    {
        var link=await Files(tenant,supplier).AsNoTracking().SingleOrDefaultAsync(d=>d.Id==document && d.IsEnabled,token);
        if(link is null || !await AreaAllowed(tenant,supplier,link.PropertyId,token))return null;
        var file=await db.StoredDocuments.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==link.DocumentId && f.PropertyId==link.PropertyId && f.IsSupplierOnly && f.IsEnabled,token);if(file is null)return null;
        var stream=await storage.DownloadAsync(file.ObjectKey,token);
        if(stream.Length!=file.Size || Convert.ToHexString(await SHA256.HashDataAsync(stream,token))!=file.Sha256){await stream.DisposeAsync();throw new StorageUnavailableException();}
        var propertyName=await db.Properties.Where(p=>p.Id==link.PropertyId).Select(p=>p.Name).SingleOrDefaultAsync(token);
        if(propertyName is null){await stream.DisposeAsync();return null;}
        stream.Position=0;return new(FileResponse(link,file,!await Files(tenant,supplier).AnyAsync(d=>d.PreviousDocumentId==link.Id,token),propertyName),stream);
    }
    public async Task<SupplierMutation?> RemoveDocument(Guid tenant,Guid supplier,Guid document,SupplierFileRemove c,CancellationToken token)
    {
        if(c.OperationId==Guid.Empty || c.SupplierVersion<1 || c.Version<1)InvalidSupplier("version");
        if(!await Allowed(tenant,token))return null;
        var link=await db.Set<SupplierDocument>().IgnoreQueryFilters().SingleOrDefaultAsync(d=>d.TenantId==tenant && d.SupplierId==supplier && d.Id==document,token);if(link is null)return null;
        await using var tx=await db.Database.BeginTransactionAsync(token);var scope=await LockArea(tenant,supplier,link.PropertyId,false,token);if(scope is null)return null;
        var fingerprint=Hash(new{action="REMOVE_FILE",tenant,supplier,document,c.SupplierVersion,c.Version});
        var prior=await db.Set<SupplierOperation>().SingleOrDefaultAsync(o=>o.TenantId==tenant && o.ActorIssuer==actor.Issuer && o.ActorSubject==actor.UserId && o.OperationId==c.OperationId,token);
        if(prior is not null){if(prior.SupplierId!=supplier || prior.Fingerprint!=fingerprint)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());await tx.CommitAsync(token);return new(Response(scope.Value.Supplier),true,prior.AppliedVersion);}
        if(link.IsDeleted || !link.IsEnabled)return null;Expected(c.SupplierVersion,scope.Value.Supplier.Version);Expected(c.Version,link.Version);
        if(await db.Set<SupplierWorkOrderPhoto>().AnyAsync(x=>x.TenantId==tenant && x.SupplierId==supplier && x.PropertyId==link.PropertyId && x.DocumentId==link.Id,token) || link.PreviousDocumentId.HasValue || await Files(tenant,supplier).AnyAsync(d=>d.PreviousDocumentId==link.Id,token) || await db.Set<SupplierContract>().AnyAsync(x=>x.TenantId==tenant && x.SupplierId==supplier && x.PropertyId==link.PropertyId && x.DocumentId==link.Id,token))throw new HierarchyConflictException(ErrorCatalog.SupplierDocumentProtected.Create());
        var file=await db.StoredDocuments.SingleAsync(f=>f.PropertyId==link.PropertyId && f.Id==link.DocumentId && f.IsSupplierOnly,token);
        db.Remove(link);db.Remove(file);TouchArea(scope.Value);db.Add(new SupplierOperation(tenant,supplier,c.OperationId,actor.Issuer!,actor.UserId!,fingerprint,c.SupplierVersion+1));
        await db.SaveChangesAsync(token);await tx.CommitAsync(token);return new(Response(scope.Value.Supplier),false,c.SupplierVersion+1);
    }
}
