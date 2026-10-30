using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Infrastructure;
public sealed class CommunicationStore(BuildingFlowDbContext db, IFileStorage storage, TimeProvider clock, ILogger<CommunicationStore> logger) : ICommunicationStore
{
    private IQueryable<Property> Properties(AccessScope scope) => db.Properties.Where(p => !p.IsDeleted && (scope.ReadAll || db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == scope.UserId && m.IsEnabled && !m.IsDeleted)));
    private IQueryable<Announcement> Announcements(Guid property, AccessScope scope, bool manager) => db.Announcements.AsNoTracking().Where(x => x.PropertyId == property && x.IsEnabled && Properties(scope).Any(p => p.Id == property) && (manager || (x.Status == AnnouncementStatus.PUBLISHED && x.Audience == ContentAudience.ALL_MEMBERS)));
    private IQueryable<StoredDocument> Documents(Guid property, AccessScope scope, bool manager) => db.StoredDocuments.AsNoTracking().Where(x => !x.IsSupplierOnly && x.PropertyId == property && x.IsEnabled && Properties(scope).Any(p => p.Id == property) && (x.AnnouncementId == null ? (manager || x.Audience == ContentAudience.ALL_MEMBERS) : Announcements(property, scope, manager).Any(a => a.Id == x.AnnouncementId)));
    private async Task<Property?> Lock(Guid property, AccessScope scope, CancellationToken token)
    {
        if (!await Properties(scope).AnyAsync(p => p.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var parent = await Properties(scope).SingleOrDefaultAsync(p => p.Id == property, token);
        if (parent is not null && !parent.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        return parent;
    }
    private void Touch(Property property) => db.Entry(property).Property(x => x.IsEnabled).IsModified = true;
    private void Touch(Announcement announcement) => db.Entry(announcement).Property(x => x.IsEnabled).IsModified = true;
    private static void Version(long expected, long actual) { if (expected != actual) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    private static void Draft(Announcement a) { if (a.Status != AnnouncementStatus.DRAFT) throw new HierarchyConflictException(ErrorCatalog.ContentState.Create()); }
    private static AnnouncementResponse Response(Announcement a) => new(a.Id, a.PropertyId, a.Title, a.Content, a.Audience.ToString(), a.Status.ToString(), a.Version, a.CreatedAt, a.PublishedAt, a.Category.ToString());
    private static DocumentResponse Response(StoredDocument d, bool replayed = false) => new(d.Id, d.PropertyId, d.RequestId, d.AnnouncementId, d.Audience.ToString(), d.FileName, d.ContentType, d.Size, d.Sha256, d.Version, d.CreatedAt, replayed);
    private static string Pattern(string search) => "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    public async Task<AnnouncementResponse?> SaveAsync(Guid property, Guid? id, AnnouncementRequest request, ContentAudience audience, AnnouncementCategory? category, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        Announcement value;
        if (id.HasValue) {
            value = (await db.Announcements.SingleOrDefaultAsync(a => a.PropertyId == property && a.Id == id && a.IsEnabled, token))!;
            if (value is null) return null; Version(request.Version, value.Version); Draft(value);
            await DefinitionPolicy.EnsureSelectableAsync(db, property, "ANNOUNCEMENT_CATEGORY", (category ?? value.Category).ToString(), value.Category.ToString(), "category", token);
            value.Edit(request.Title!, request.Content!, audience, category ?? value.Category); Touch(value);
        } else {
            await DefinitionPolicy.EnsureSelectableAsync(db, property, "ANNOUNCEMENT_CATEGORY", (category ?? AnnouncementCategory.GENERAL).ToString(), null, "category", token);
            value = new(property, request.Title!, request.Content!, audience, category ?? AnnouncementCategory.GENERAL); db.Add(value);
        }
        Touch(parent); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return Response(value);
    }
    public async Task<AnnouncementResponse?> ChangeStatusAsync(Guid property, Guid id, long version, bool publish, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token); var parent = await Lock(property, access, token); if (parent is null) return null;
        var value = await db.Announcements.SingleOrDefaultAsync(a => a.PropertyId == property && a.Id == id && a.IsEnabled, token); if (value is null) return null;
        Version(version, value.Version);
        if (publish) { Draft(value); value.Publish(clock.GetUtcNow()); }
        else { if (value.Status != AnnouncementStatus.PUBLISHED) throw new HierarchyConflictException(ErrorCatalog.ContentState.Create()); value.Archive(); }
        Touch(parent); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return Response(value);
    }
    public async Task<ContentPage<AnnouncementResponse>?> AnnouncementsAsync(Guid property, ContentQuery query, AccessScope access, bool manager, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        var rows = Announcements(property, access, manager);
        if (!string.IsNullOrWhiteSpace(query.Status)) { var status = Enum.Parse<AnnouncementStatus>(query.Status); rows = rows.Where(x => x.Status == status); }
        if (!string.IsNullOrWhiteSpace(query.Search)) { var pattern = Pattern(query.Search); rows = rows.Where(x => EF.Functions.ILike(x.Title, pattern, "\\") || EF.Functions.ILike(x.Content, pattern, "\\")); }
        var grouped = await rows.GroupBy(x => x.Category).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(token);
        var counts = Enum.GetValues<AnnouncementCategory>().Select(c => new ContentCategoryCount(c.ToString(), grouped.SingleOrDefault(g => g.Key == c)?.Count ?? 0)).ToArray();
        if (!string.IsNullOrEmpty(query.Category)) { var category = Enum.Parse<AnnouncementCategory>(query.Category); rows = rows.Where(x => x.Category == category); }
        var total = await rows.CountAsync(token); var values = await rows.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        await tx.CommitAsync(token); return new(values.Select(Response).ToArray(), query.Page, query.PageSize, total, counts);
    }
    public async Task<AnnouncementResponse?> AnnouncementAsync(Guid property, Guid id, AccessScope access, bool manager, CancellationToken token)
    { var value = await Announcements(property, access, manager).SingleOrDefaultAsync(x => x.Id == id, token); return value is null ? null : Response(value); }
    public async Task<DocumentResponse?> UploadAsync(Guid property, DocumentUpload command, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token); var parent = await Lock(property, access, token); if (parent is null) return null;
        var hash = Convert.ToHexString(SHA256.HashData(command.Bytes));
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { hash, command.FileName, command.ContentType, command.AnnouncementId, command.AnnouncementVersion, command.Audience }))));
        // Replay includes deleted metadata but never relaxes the independently verified parent/active membership scope.
        var old = await db.StoredDocuments.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property && x.RequestId == command.RequestId, token);
        if (old is not null) {
            if (old.IsDeleted || !old.IsEnabled || old.RequestFingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.DocumentRequestConflict.Create());
            await tx.CommitAsync(token); return Response(old, true);
        }
        Announcement? announcement = null;
        if (command.AnnouncementId.HasValue) {
            announcement = await db.Announcements.SingleOrDefaultAsync(a => a.PropertyId == property && a.Id == command.AnnouncementId && a.IsEnabled, token);
            if (announcement is null) return null; Version(command.AnnouncementVersion, announcement.Version); Draft(announcement);
            if (await db.StoredDocuments.CountAsync(x => x.PropertyId == property && x.AnnouncementId == announcement.Id, token) >= 10) throw new HierarchyConflictException(ErrorCatalog.DocumentLimit.Create());
        }
        var value = new StoredDocument(property, command.RequestId, fingerprint, command.AnnouncementId, command.Audience, command.FileName, command.ContentType, command.Bytes.LongLength, hash);
        var saved = false;
        try {
            using var content = new MemoryStream(command.Bytes, writable: false);
            await storage.UploadAsync(value.ObjectKey, content, value.ContentType, token);
            db.Add(value); Touch(parent); if (announcement is not null) Touch(announcement);
            await db.SaveChangesAsync(token); saved = true; await tx.CommitAsync(token); return Response(value);
        } catch {
            // Only this attempt's unique, server-generated object can be removed; replay objects are never touched.
            if (saved) { logger.LogWarning("Upload transaction outcome requires storage maintenance."); throw; }
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { await storage.DeleteAsync(value.ObjectKey, cleanup.Token); } catch { logger.LogWarning("Failed upload cleanup requires storage maintenance."); }
            throw;
        }
    }
    public async Task<ContentPage<DocumentResponse>?> DocumentsAsync(Guid property, ContentQuery query, AccessScope access, bool manager, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        if (query.AnnouncementId.HasValue && !await Announcements(property, access, manager).AnyAsync(a => a.Id == query.AnnouncementId, token)) return null;
        var rows = Documents(property, access, manager);
        if (query.AnnouncementId.HasValue) rows = rows.Where(x => x.AnnouncementId == query.AnnouncementId);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var pattern = Pattern(query.Search); rows = rows.Where(x => EF.Functions.ILike(x.FileName, pattern, "\\")); }
        var total = await rows.CountAsync(token); var values = await rows.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        await tx.CommitAsync(token); return new(values.Select(x => Response(x)).ToArray(), query.Page, query.PageSize, total);
    }
    public async Task<DocumentDownload?> DownloadAsync(Guid property, Guid id, AccessScope access, bool manager, CancellationToken token)
    {
        var value = await Documents(property, access, manager).SingleOrDefaultAsync(x => x.Id == id, token); if (value is null) return null;
        var stream = await storage.DownloadAsync(value.ObjectKey, token);
        if (stream.Length != value.Size || Convert.ToHexString(await SHA256.HashDataAsync(stream, token)) != value.Sha256) { await stream.DisposeAsync(); throw new StorageUnavailableException(); }
        stream.Position = 0; return new(Response(value), stream);
    }
    public async Task<bool> DeleteAsync(Guid property, Guid id, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token); var parent = await Lock(property, access, token); if (parent is null) return false;
        var value = await db.StoredDocuments.SingleOrDefaultAsync(x => !x.IsSupplierOnly && x.PropertyId == property && x.Id == id, token); if (value is null) return false;
        Version(version, value.Version);
        if (value.AnnouncementId.HasValue) {
            var announcement = await db.Announcements.SingleAsync(x => x.PropertyId == property && x.Id == value.AnnouncementId, token);
            Draft(announcement); Touch(announcement);
        }
        db.Remove(value); Touch(parent); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
    }
}
