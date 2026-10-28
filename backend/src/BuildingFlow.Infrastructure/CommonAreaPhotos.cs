using System.Data;
using System.Security.Cryptography;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class CommonAreaStore
{
    private async Task<bool> PhotoAccess(Guid property, Guid area, ActiveContextQuery? context, CancellationToken token)
    {
        if (context is null) {
            if (!actor.IsAuthenticated || !new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" }.Any(actor.IsInRole)) throw new UnauthorizedAccessException();
            return await Allowed(property, token) && await db.Set<CommonArea>().AnyAsync(x => x.PropertyId == property && x.Id == area, token);
        }
        if (context.PropertyId != property || context.Capacity is not ("OWNER" or "TENANT" or "RESIDENT")) return false;
        if (await portfolio.ResolveAsync(context, token) is null) return false;
        var unit = await db.Units.AsNoTracking().SingleOrDefaultAsync(u => u.Id == context.UnitId && u.PropertyId == property && u.IsEnabled, token);
        return unit is not null && await db.Properties.AnyAsync(p => p.Id == property && p.IsEnabled, token) &&
            await db.Set<CommonArea>().AnyAsync(x => x.PropertyId == property && x.Id == area && x.IsEnabled &&
                (x.BuildingId == null || db.Buildings.Any(b => b.PropertyId == property && b.Id == x.BuildingId && b.IsEnabled)) &&
                (x.Visibility == CommonAreaVisibility.PROPERTY || x.BuildingId == unit.BuildingId), token);
    }
    private IQueryable<CommonAreaPhoto> PhotoRows(Guid property, Guid area) => db.Set<CommonAreaPhoto>().Where(p => p.PropertyId == property && p.CommonAreaId == area && p.IsEnabled &&
        db.StoredDocuments.Any(d => d.PropertyId == property && d.Id == p.DocumentId && d.IsEnabled && (d.ContentType == "image/jpeg" || d.ContentType == "image/png")));
    private async Task<CommonAreaPhotoResponse> PhotoResult(CommonAreaPhoto p, CancellationToken token)
    {
        var d = await db.StoredDocuments.AsNoTracking().SingleAsync(d => d.PropertyId == p.PropertyId && d.Id == p.DocumentId, token);
        return new(p.Id, p.PropertyId, p.CommonAreaId, d.FileName, d.ContentType, d.Size, d.Sha256, p.Version);
    }
    public async Task<IReadOnlyList<CommonAreaPhotoResponse>?> Photos(Guid property, Guid area, ActiveContextQuery? context, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await PhotoAccess(property, area, context, token)) return null;
        var photos = await PhotoRows(property, area).AsNoTracking().OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).ToArrayAsync(token);
        var result = new List<CommonAreaPhotoResponse>(); foreach (var p in photos) result.Add(await PhotoResult(p, token));
        await tx.CommitAsync(token); return result;
    }
    public async Task<CommonAreaPhotoResponse?> Link(Guid property, Guid area, CommonAreaPhotoRequest request, CancellationToken token)
    {
        if (request.DocumentId == Guid.Empty || request.AreaVersion < 1 || !request.RightsConfirmed) CommonAreaService.Invalid("photo");
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, token); if (parent is null || !await PhotoAccess(property, area, null, token)) return null;
        var current = await db.Set<CommonArea>().SingleAsync(x => x.PropertyId == property && x.Id == area, token);
        var prior = await db.Set<CommonAreaPhoto>().IgnoreQueryFilters().SingleOrDefaultAsync(p => p.PropertyId == property && p.CommonAreaId == area && p.DocumentId == request.DocumentId, token);
        if (prior is not null) { if (prior.IsDeleted || !await db.StoredDocuments.AnyAsync(d => d.Id == prior.DocumentId && d.PropertyId == property && d.IsEnabled, token)) Conflict(); return await PhotoResult(prior, token); }
        Expected(request.AreaVersion, current.Version);
        var doc = await db.StoredDocuments.SingleOrDefaultAsync(d => d.PropertyId == property && d.Id == request.DocumentId && d.IsEnabled && d.AnnouncementId == null && d.Audience == ContentAudience.MANAGEMENT && (d.ContentType == "image/jpeg" || d.ContentType == "image/png"), token);
        if (doc is null || await db.Set<CommonAreaPhoto>().CountAsync(p => p.PropertyId == property && p.CommonAreaId == area, token) >= 10) CommonAreaService.Invalid("photo");
        db.Entry(doc!).Property(d => d.IsEnabled).IsModified = true; // Concurrent metadata deletion loses by version.
        var photo = new CommonAreaPhoto(property, area, request.DocumentId, true); db.Add(photo);
        db.Entry(current).Property(x => x.IsEnabled).IsModified = true; db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return await PhotoResult(photo, token);
    }
    public async Task<bool> Unlink(Guid property, Guid area, Guid photo, long version, CancellationToken token)
    {
        if (version < 1) CommonAreaService.Invalid("version");
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, token); if (parent is null || !await PhotoAccess(property, area, null, token)) return false;
        var p = await db.Set<CommonAreaPhoto>().SingleOrDefaultAsync(x => x.PropertyId == property && x.CommonAreaId == area && x.Id == photo, token);
        if (p is null) return false; Expected(version, p.Version); db.Remove(p);
        var current = await db.Set<CommonArea>().SingleAsync(x => x.PropertyId == property && x.Id == area, token);
        db.Entry(current).Property(x => x.IsEnabled).IsModified = true; db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
    }
    public async Task<DocumentDownload?> DownloadPhoto(Guid property, Guid area, Guid photo, ActiveContextQuery? context, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await PhotoAccess(property, area, context, token)) return null;
        var p = await PhotoRows(property, area).AsNoTracking().SingleOrDefaultAsync(p => p.Id == photo, token); if (p is null) return null;
        var d = await db.StoredDocuments.AsNoTracking().SingleAsync(d => d.PropertyId == property && d.Id == p.DocumentId, token);
        using var source = await storage.DownloadAsync(d.ObjectKey, token); var bytes = new MemoryStream(); var buffer = new byte[81920]; int read;
        try {
            while ((read = await source.ReadAsync(buffer, token)) > 0) { if (bytes.Length + read > StoredDocument.MaxBytes) throw new StorageUnavailableException(); await bytes.WriteAsync(buffer.AsMemory(0, read), token); }
            if (bytes.Length != d.Size || !string.Equals(Convert.ToHexString(SHA256.HashData(bytes.ToArray())), d.Sha256, StringComparison.OrdinalIgnoreCase)) throw new StorageUnavailableException();
            bytes.Position = 0; await tx.CommitAsync(token); return new(new(d.Id, property, d.RequestId, null, "MANAGEMENT", d.FileName, d.ContentType, d.Size, d.Sha256, d.Version, d.CreatedAt), bytes);
        } catch { bytes.Dispose(); throw; }
    }
}
