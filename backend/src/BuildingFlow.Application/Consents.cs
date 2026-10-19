using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record ConsentTextSummary(Guid Id, int TextVersion, string Title, bool RequiresReconsent, DateTimeOffset PublishedAt, string PublishedBy);
public sealed record ConsentTypeResponse(Guid Id, string Code, string NameTr, string NameEn, string Kind, bool IsSystem, string? Feature, int SortOrder,
    bool IsEnabled, long Version, int TextCount, ConsentTextSummary? CurrentText);
public sealed record ConsentTextResponse(Guid Id, Guid ConsentTypeId, string TypeCode, int TextVersion, string Title, string Body, string BodySha256,
    bool RequiresReconsent, DateTimeOffset PublishedAt, string PublishedBy);
public sealed record CreateConsentTypeRequest(string? Code, string? NameTr, string? NameEn, string? Kind, int SortOrder = 100);
public sealed record UpdateConsentTypeRequest(string? NameTr, string? NameEn, int SortOrder, bool IsEnabled, long Version);
public sealed record PublishConsentTextRequest(Guid OperationId, string? Title, string? Body, bool RequiresReconsent = true);
public sealed record ConsentRecordQuery(int Page = 1, int PageSize = 20, Guid? TypeId = null, string? State = null, Guid? PropertyId = null, string? Search = null, string? UserId = null);
public sealed record ConsentRecordResponse(Guid Id, string OwnerUserId, string ApproverName, Guid TypeId, string TypeCode, string TypeNameTr, string TypeNameEn,
    string Kind, Guid TextId, int TextVersion, DateTimeOffset ApprovedAt, string Channel, string? AppVersion, DateTimeOffset? RevokedAt, string? RevokeReason, string State);
public sealed record ConsentDocumentResponse(ConsentRecordResponse Record, string Title, string Body, string BodySha256, string EvidenceSha256,
    bool BodyIntact, bool EvidenceIntact);
public sealed record ConsentTypeState(Guid TypeId, string Code, string NameTr, string NameEn, string Kind, string State, int? ApprovedVersion, DateTimeOffset? ApprovedAt, int? CurrentVersion);
public sealed record UserConsentHistory(string UserId, IReadOnlyList<ConsentTypeState> Summary, IReadOnlyList<ConsentRecordResponse> Records);

public static class ConsentStates
{
    // Row states (history) and the per-type summary state share one vocabulary.
    public const string None = "NONE", Active = "ACTIVE", Outdated = "OUTDATED", Withdrawn = "WITHDRAWN", Superseded = "SUPERSEDED";
    public static readonly IReadOnlySet<string> RowStates = new HashSet<string> { Active, Outdated, Withdrawn, Superseded };
}

public interface IConsentStore
{
    Task<IReadOnlyList<ConsentTypeResponse>> TypesAsync(CancellationToken token);
    Task<ConsentTypeResponse> CreateTypeAsync(CreateConsentTypeRequest request, CancellationToken token);
    Task<ConsentTypeResponse?> UpdateTypeAsync(Guid type, UpdateConsentTypeRequest request, CancellationToken token);
    Task<bool> DeleteTypeAsync(Guid type, long version, CancellationToken token);
    Task<PagedRows<ConsentTextResponse>?> TextsAsync(Guid type, int page, int pageSize, CancellationToken token);
    Task<ConsentTextResponse?> TextAsync(Guid text, CancellationToken token);
    Task<ConsentTextResponse?> PublishAsync(Guid type, PublishConsentTextRequest request, string body, string fingerprint, CancellationToken token);
    Task<PagedRows<ConsentRecordResponse>?> RecordsAsync(ConsentRecordQuery query, AccessScope access, CancellationToken token);
    Task<ConsentDocumentResponse?> DocumentAsync(Guid record, AccessScope access, CancellationToken token);
    Task<UserConsentHistory?> HistoryAsync(string userId, AccessScope access, CancellationToken token);
}

public static class ConsentEvidence
{
    public static string Sha256(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string TextFingerprint(string title, string body, bool requiresReconsent) => Sha256(string.Join('\n', "text-v1", title, requiresReconsent ? "1" : "0", body));

    // The hash binds who approved which exact text, when, and through which request; any later edit of the row or text breaks it.
    public static string Compute(UserConsent consent, string typeCode) => Sha256(string.Join('\n',
        "consent-v1", consent.Id.ToString("D"), consent.OwnerUserId, typeCode, consent.ConsentTextId.ToString("D"),
        consent.TextVersion.ToString(CultureInfo.InvariantCulture), consent.BodySha256, consent.ApproverName,
        consent.ApprovedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture),
        consent.Channel.ToString(), consent.AppVersion ?? "", consent.OperationId.ToString("D")));
}

public sealed class ConsentService(IConsentStore store, ICurrentUserService user)
{
    private static readonly string[] Readers = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER"];
    private AccessScope Access(bool admin = false)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !(admin ? user.IsInRole("SUPER_ADMIN") : Readers.Any(user.IsInRole))) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    [DoesNotReturn] private static void Invalid(string field, ErrorDefinition? error = null) => throw new RequestValidationException(new() { [field] = [(error ?? ErrorCatalog.ConsentInvalid).Create()] });
    private static void Page(int page, int size) { if (page is < 1 or > 1_000_000 || size is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] }); }
    private static void Names(string? tr, string? en, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(tr) || tr.Trim().Length > ConsentType.MaxNameLength) Invalid("nameTr");
        if (string.IsNullOrWhiteSpace(en) || en.Trim().Length > ConsentType.MaxNameLength) Invalid("nameEn");
        if (sortOrder is < 0 or > 10_000) Invalid("sortOrder");
    }

    public Task<IReadOnlyList<ConsentTypeResponse>> TypesAsync(CancellationToken token) { Access(); return store.TypesAsync(token); }

    public Task<ConsentTypeResponse> CreateTypeAsync(CreateConsentTypeRequest request, CancellationToken token)
    {
        Access(true);
        var code = request.Code?.Trim();
        if (code is null || code.Length is < 2 or > ConsentType.MaxCodeLength || !char.IsAsciiLetterUpper(code[0]) || code.Any(c => !char.IsAsciiLetterUpper(c) && !char.IsAsciiDigit(c) && c != '_')) Invalid("code");
        if (request.Kind is null || int.TryParse(request.Kind, out _) || !Enum.TryParse<ConsentKind>(request.Kind, false, out var kind) || !Enum.IsDefined(kind)) Invalid("kind");
        Names(request.NameTr, request.NameEn, request.SortOrder);
        return store.CreateTypeAsync(request with { Code = code, NameTr = request.NameTr!.Trim(), NameEn = request.NameEn!.Trim() }, token);
    }

    public Task<ConsentTypeResponse?> UpdateTypeAsync(Guid type, UpdateConsentTypeRequest request, CancellationToken token)
    {
        Access(true);
        Names(request.NameTr, request.NameEn, request.SortOrder);
        if (request.Version < 1) Invalid("version");
        return store.UpdateTypeAsync(type, request with { NameTr = request.NameTr!.Trim(), NameEn = request.NameEn!.Trim() }, token);
    }

    public Task<bool> DeleteTypeAsync(Guid type, long version, CancellationToken token)
    { Access(true); if (version < 1) Invalid("version"); return store.DeleteTypeAsync(type, version, token); }

    public Task<PagedRows<ConsentTextResponse>?> TextsAsync(Guid type, int page, int pageSize, CancellationToken token)
    { Access(); Page(page, pageSize); return store.TextsAsync(type, page, pageSize, token); }

    public Task<ConsentTextResponse?> TextAsync(Guid text, CancellationToken token) { Access(); return store.TextAsync(text, token); }

    public Task<ConsentTextResponse?> PublishAsync(Guid type, PublishConsentTextRequest request, CancellationToken token)
    {
        Access(true);
        if (request.OperationId == Guid.Empty) Invalid("operationId");
        var title = request.Title?.Trim();
        if (string.IsNullOrEmpty(title) || title.Length > ConsentText.MaxTitleLength || title.Any(char.IsControl)) Invalid("title");
        var parsed = ConsentMarkup.Parse(request.Body);
        if (parsed.Error is not null) Invalid("body", parsed.Error == ConsentMarkup.ErrorLink ? ErrorCatalog.ConsentLinkInvalid : ErrorCatalog.ConsentTextInvalid);
        var body = ConsentMarkup.Normalize(request.Body!).Trim('\n');
        return store.PublishAsync(type, request with { Title = title }, body, ConsentEvidence.TextFingerprint(title, body, request.RequiresReconsent), token);
    }

    public Task<PagedRows<ConsentRecordResponse>?> RecordsAsync(ConsentRecordQuery query, CancellationToken token)
    {
        var access = Access(); Page(query.Page, query.PageSize);
        if (query.State is not null && !ConsentStates.RowStates.Contains(query.State)) Invalid("state");
        if (query.Search is { } search && (search.Trim().Length is 0 or > 100)) Invalid("search");
        if (query.UserId is { } userId && (userId.Trim().Length is 0 or > 200)) Invalid("userId");
        return store.RecordsAsync(query with { Search = query.Search?.Trim(), UserId = query.UserId?.Trim() }, access, token);
    }

    public Task<ConsentDocumentResponse?> DocumentAsync(Guid record, CancellationToken token) => store.DocumentAsync(record, Access(), token);

    public Task<UserConsentHistory?> HistoryAsync(string userId, CancellationToken token)
    {
        var access = Access();
        if (string.IsNullOrWhiteSpace(userId) || userId.Length > 200) Invalid("userId");
        return store.HistoryAsync(userId, access, token);
    }
}
