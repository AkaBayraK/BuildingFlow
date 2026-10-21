using BuildingFlow.Domain;
namespace BuildingFlow.Application;
// Category is optional for older clients: null keeps the current category on edit and means GENERAL on create.
public sealed record AnnouncementRequest(string? Title, string? Content, string? Audience, long Version = 0, string? Category = null);
public sealed record AnnouncementResponse(Guid Id, Guid PropertyId, string Title, string Content, string Audience, string Status, long Version, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt, string Category = "GENERAL");
public sealed record DocumentResponse(Guid Id, Guid PropertyId, Guid RequestId, Guid? AnnouncementId, string Audience, string FileName, string ContentType, long Size, string Sha256, long Version, DateTimeOffset CreatedAt, bool Replayed = false);
public sealed record ContentQuery(int Page = 1, int PageSize = 10, string? Search = null, string? Status = null, Guid? AnnouncementId = null, string? Category = null);
public sealed record ContentCategoryCount(string Category, int Count);
// CategoryCounts is set for announcement lists only and ignores the category filter itself.
public sealed record ContentPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, IReadOnlyList<ContentCategoryCount>? CategoryCounts = null);
public sealed record DocumentUpload(Guid RequestId, Guid? AnnouncementId, long AnnouncementVersion, ContentAudience Audience, string FileName, string ContentType, byte[] Bytes);
public sealed record DocumentDownload(DocumentResponse Metadata, Stream Content);
public sealed class StorageUnavailableException : Exception { }
public sealed class DocumentSizeException : Exception { }
public interface ICommunicationStore
{
    Task<AnnouncementResponse?> SaveAsync(Guid property, Guid? id, AnnouncementRequest request, ContentAudience audience, AnnouncementCategory? category, AccessScope access, CancellationToken token);
    Task<AnnouncementResponse?> ChangeStatusAsync(Guid property, Guid id, long version, bool publish, AccessScope access, CancellationToken token);
    Task<ContentPage<AnnouncementResponse>?> AnnouncementsAsync(Guid property, ContentQuery query, AccessScope access, bool manager, CancellationToken token);
    Task<AnnouncementResponse?> AnnouncementAsync(Guid property, Guid id, AccessScope access, bool manager, CancellationToken token);
    Task<DocumentResponse?> UploadAsync(Guid property, DocumentUpload command, AccessScope access, CancellationToken token);
    Task<ContentPage<DocumentResponse>?> DocumentsAsync(Guid property, ContentQuery query, AccessScope access, bool manager, CancellationToken token);
    Task<DocumentDownload?> DownloadAsync(Guid property, Guid id, AccessScope access, bool manager, CancellationToken token);
    Task<bool> DeleteAsync(Guid property, Guid id, long version, AccessScope access, CancellationToken token);
}
public sealed class CommunicationService(ICommunicationStore store, ICurrentUserService user)
{
    public bool IsManager => new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" }.Any(user.IsInRole);
    private AccessScope Access(bool write = false)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || (write && !IsManager)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    public static void Invalid(string field, ErrorDefinition definition) => throw new RequestValidationException(new() { [field] = [definition.Create()] });
    public static ContentAudience Audience(string? value)
    {
        if (!Enum.TryParse<ContentAudience>(value, out var audience) || !Enum.IsDefined(audience) || value != audience.ToString()) Invalid("audience", ErrorCatalog.ContentInvalid);
        return audience;
    }
    public static AnnouncementCategory? Category(string? value)
    {
        if (value is null) return null;
        if (!Enum.GetNames<AnnouncementCategory>().Contains(value)) Invalid("category", ErrorCatalog.ContentInvalid);
        return Enum.Parse<AnnouncementCategory>(value);
    }
    public static void Version(long version) { if (version < 1) Invalid("version", ErrorCatalog.VersionRequired); }
    private static void Query(ContentQuery query)
    {
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100) Invalid("page", ErrorCatalog.InvalidPage);
        if (query.Search?.Length > 200) Invalid("search", ErrorCatalog.ErrorsSearchBounds);
        if (query.Status is not (null or "" or "DRAFT" or "PUBLISHED" or "ARCHIVED")) Invalid("status", ErrorCatalog.ContentInvalid);
        if (query.AnnouncementId == Guid.Empty) Invalid("announcementId", ErrorCatalog.ContentInvalid);
        if (!string.IsNullOrEmpty(query.Category)) Category(query.Category);
    }
    public Task<AnnouncementResponse?> Save(Guid property, Guid? id, AnnouncementRequest request, CancellationToken token)
    {
        var access = Access(true); if (id.HasValue) Version(request.Version);
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200 || string.IsNullOrWhiteSpace(request.Content) || request.Content.Trim().Length > 10000) Invalid("content", ErrorCatalog.ContentInvalid);
        return store.SaveAsync(property, id, request, Audience(request.Audience), Category(request.Category), access, token);
    }
    public Task<AnnouncementResponse?> ChangeStatus(Guid property, Guid id, long version, bool publish, CancellationToken token)
    { var access = Access(true); Version(version); return store.ChangeStatusAsync(property, id, version, publish, access, token); }
    public Task<ContentPage<AnnouncementResponse>?> Announcements(Guid property, ContentQuery query, CancellationToken token)
    { var access = Access(); Query(query); return store.AnnouncementsAsync(property, query, access, IsManager, token); }
    public Task<AnnouncementResponse?> Announcement(Guid property, Guid id, CancellationToken token) => store.AnnouncementAsync(property, id, Access(), IsManager, token);
    public Task<ContentPage<DocumentResponse>?> Documents(Guid property, ContentQuery query, CancellationToken token)
    { var access = Access(); Query(query); return store.DocumentsAsync(property, query, access, IsManager, token); }
    public Task<DocumentDownload?> Download(Guid property, Guid id, CancellationToken token) => store.DownloadAsync(property, id, Access(), IsManager, token);
    public Task<bool> Delete(Guid property, Guid id, long version, CancellationToken token)
    { var access = Access(true); Version(version); return store.DeleteAsync(property, id, version, access, token); }
    public Task<DocumentResponse?> Upload(Guid property, DocumentUpload command, CancellationToken token)
    {
        var access = Access(true);
        if (command.RequestId == Guid.Empty) Invalid("requestId", ErrorCatalog.DuesErrorRequestId);
        if (command.AnnouncementId.HasValue) Version(command.AnnouncementVersion);
        ValidateFile(command.FileName, command.ContentType, command.Bytes);
        return store.UploadAsync(property, command, access, token);
    }
    public static void ValidateFile(string name, string type, byte[] bytes)
    {
        if (bytes.Length > StoredDocument.MaxBytes) throw new DocumentSizeException();
        if (bytes.Length == 0 || string.IsNullOrWhiteSpace(name) || name.Length > 200 || name.Any(char.IsControl) || name.Contains('/') || name.Contains('\\')) Invalid("file", ErrorCatalog.DocumentInvalid);
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var valid = type switch {
            "application/pdf" => extension == ".pdf" && bytes.AsSpan().StartsWith("%PDF-"u8),
            "image/jpeg" => extension is ".jpg" or ".jpeg" && bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 }),
            "image/png" => extension == ".png" && bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            _ => false
        };
        if (!valid) Invalid("file", ErrorCatalog.DocumentInvalid);
    }
}
