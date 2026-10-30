using System.Data;
using System.Security.Cryptography;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    public async Task<PagedRows<SupplierWorkOrderPhotoResponse>?> WorkOrderPhotos(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null;
        var order = await OrderRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
        if (order is null || !await AreaAllowed(tenant, supplier, order.PropertyId, token)) return null;
        var rows = from photo in db.Set<SupplierWorkOrderPhoto>().AsNoTracking()
                   join d in Files(tenant, supplier) on new { photo.TenantId, photo.SupplierId, photo.PropertyId, Id = photo.DocumentId } equals new { d.TenantId, d.SupplierId, d.PropertyId, d.Id }
                   join f in db.StoredDocuments on new { d.PropertyId, Id = d.DocumentId } equals new { f.PropertyId, f.Id }
                   join p in db.Properties on photo.PropertyId equals p.Id
                   where photo.TenantId == tenant && photo.SupplierId == supplier && photo.PropertyId == order.PropertyId && photo.WorkOrderId == id && d.IsEnabled && f.IsEnabled && f.IsSupplierOnly
                   select new { Photo = photo, Document = d, File = f, PropertyName = p.Name, Current = !db.Set<SupplierDocument>().Any(next => next.TenantId == tenant && next.SupplierId == supplier && next.PropertyId == order.PropertyId && next.PreviousDocumentId == d.Id) };
        var count = await rows.CountAsync(token); var items = await rows.OrderBy(x => x.Photo.CreatedAt).ThenBy(x => x.Photo.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items.Select(x => new SupplierWorkOrderPhotoResponse(x.Photo.Id, FileResponse(x.Document, x.File, x.Current, x.PropertyName), x.Photo.Caption, x.Photo.CreatedAt)).ToArray(), page, pageSize, count);
    }
    public async Task<SupplierWorkOrderMutation?> UploadWorkOrderPhoto(Guid tenant, Guid supplier, Guid id, SupplierWorkOrderUpload c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1 || c.ContentType is not ("image/jpeg" or "image/png")) InvalidSupplier("photo");
        CommunicationService.ValidateFile(c.FileName, c.ContentType, c.Bytes);
        string caption; try { caption = SupplierWorkOrder.WorkText(c.Caption, nameof(c.Caption), 1000); } catch (ArgumentException) { InvalidSupplier("caption"); throw; }
        await using var tx = await db.Database.BeginTransactionAsync(token); var scope = await LockOrder(tenant, supplier, id, token); if (scope is null) return null; var order = scope.Value.Order;
        var hash = Convert.ToHexString(SHA256.HashData(c.Bytes)); var fingerprint = Hash(new { action = "WORK_ORDER_PHOTO", tenant, supplier, id, c.Version, c.FileName, c.ContentType, hash, caption });
        var replay = await OrderReplay(tenant, supplier, c.OperationId, fingerprint, scope.Value.Supplier, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        Expected(c.Version, order.Version);
        if (!order.AllowsPhotos || await db.Set<SupplierWorkOrderPhoto>().CountAsync(x => x.TenantId == tenant && x.SupplierId == supplier && x.PropertyId == order.PropertyId && x.WorkOrderId == id, token) >= 10) throw new HierarchyConflictException(ErrorCatalog.SupplierWorkOrderState.Create());
        if (await db.StoredDocuments.IgnoreQueryFilters().AnyAsync(x => x.PropertyId == order.PropertyId && x.RequestId == c.OperationId, token)) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        var file = new StoredDocument(order.PropertyId, c.OperationId, fingerprint, null, ContentAudience.MANAGEMENT, c.FileName, c.ContentType, c.Bytes.LongLength, hash, true);
        var document = new SupplierDocument(tenant, supplier, order.PropertyId, file.Id, null); var photo = new SupplierWorkOrderPhoto(order, document, caption); var saved = false;
        try
        {
            using var content = new MemoryStream(c.Bytes, writable: false); await storage.UploadAsync(file.ObjectKey, content, c.ContentType, token);
            db.AddRange(file, document, photo); OrderRevision(order, c.OperationId, fingerprint, "PHOTO", c.Version + 1, new { PhotoId = photo.Id, DocumentId = document.Id, c.FileName, c.ContentType, Size = c.Bytes.LongLength, Sha256 = hash, Caption = caption });
            TouchArea((scope.Value.Supplier, scope.Value.Property)); await db.SaveChangesAsync(token); saved = true;
            var result = new SupplierWorkOrderMutation(await OrderResponse(order, token), Response(scope.Value.Supplier), false, order.Version); await tx.CommitAsync(token); return result;
        }
        catch
        {
            if (saved) { logger.LogWarning("Supplier work order photo commit outcome requires storage maintenance."); throw; }
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5)); try { await storage.DeleteAsync(file.ObjectKey, cleanup.Token); } catch { logger.LogWarning("Supplier work order photo cleanup requires storage maintenance."); } throw;
        }
    }
}
