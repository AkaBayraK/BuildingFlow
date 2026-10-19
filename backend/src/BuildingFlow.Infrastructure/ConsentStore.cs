using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class ConsentStore(BuildingFlowDbContext db) : IConsentStore
{
    // Initializer form (not a positional record) so EF can push filters on State down into SQL.
    private sealed class Row { public UserConsent Consent { get; init; } = null!; public ConsentType Type { get; init; } = null!; public string State { get; init; } = null!; }
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());
    private static string Pattern(string search) => "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    public async Task<IReadOnlyList<ConsentTypeResponse>> TypesAsync(CancellationToken token)
    {
        var types = await db.ConsentTypes.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(token);
        var texts = await db.ConsentTexts.AsNoTracking().GroupBy(x => x.ConsentTypeId)
            .Select(g => new { g.Key, Count = g.Count(), Current = g.OrderByDescending(x => x.TextVersion).First() }).ToListAsync(token);
        return types.Select(type => texts.SingleOrDefault(x => x.Key == type.Id) is { } t ? Map(type, t.Count, t.Current) : Map(type, 0, null)).ToArray();
    }

    public async Task<ConsentTypeResponse> CreateTypeAsync(CreateConsentTypeRequest request, CancellationToken token)
    {
        if (await db.ConsentTypes.IgnoreQueryFilters().AnyAsync(x => x.Code == request.Code, token)) throw Conflict(ErrorCatalog.ConsentTypeConflict);
        var type = new ConsentType(request.Code!, request.NameTr!, request.NameEn!, Enum.Parse<ConsentKind>(request.Kind!), request.SortOrder);
        db.ConsentTypes.Add(type);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { throw Conflict(ErrorCatalog.ConsentTypeConflict); }
        return Map(type, 0, null);
    }

    public async Task<ConsentTypeResponse?> UpdateTypeAsync(Guid typeId, UpdateConsentTypeRequest request, CancellationToken token)
    {
        var type = await db.ConsentTypes.SingleOrDefaultAsync(x => x.Id == typeId, token);
        if (type is null) return null;
        if (type.Version != request.Version) throw Conflict(ErrorCatalog.RecordChanged);
        if (type.IsSystem && !request.IsEnabled) throw Conflict(ErrorCatalog.ConsentSystemType);
        type.Rename(request.NameTr!, request.NameEn!, request.SortOrder);
        type.SetEnabled(request.IsEnabled);
        db.Entry(type).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token);
        var count = await db.ConsentTexts.CountAsync(x => x.ConsentTypeId == type.Id, token);
        var current = await db.ConsentTexts.AsNoTracking().Where(x => x.ConsentTypeId == type.Id).OrderByDescending(x => x.TextVersion).FirstOrDefaultAsync(token);
        return Map(type, count, current);
    }

    public async Task<bool> DeleteTypeAsync(Guid typeId, long version, CancellationToken token)
    {
        var type = await db.ConsentTypes.SingleOrDefaultAsync(x => x.Id == typeId, token);
        if (type is null) return false;
        if (type.IsSystem) throw Conflict(ErrorCatalog.ConsentSystemType);
        if (type.Version != version) throw Conflict(ErrorCatalog.RecordChanged);
        if (await db.ConsentTexts.IgnoreQueryFilters().AnyAsync(x => x.ConsentTypeId == type.Id, token)) throw Conflict(ErrorCatalog.ConsentTypeInUse);
        db.Remove(type);
        await db.SaveChangesAsync(token);
        return true;
    }

    public async Task<PagedRows<ConsentTextResponse>?> TextsAsync(Guid typeId, int page, int pageSize, CancellationToken token)
    {
        var type = await db.ConsentTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == typeId, token);
        if (type is null) return null;
        var query = db.ConsentTexts.AsNoTracking().Where(x => x.ConsentTypeId == typeId);
        var rows = await query.OrderByDescending(x => x.TextVersion).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(x => Map(x, type.Code)).ToArray(), page, pageSize, await query.CountAsync(token));
    }

    public async Task<ConsentTextResponse?> TextAsync(Guid textId, CancellationToken token)
    {
        var row = await (from text in db.ConsentTexts.AsNoTracking() join type in db.ConsentTypes on text.ConsentTypeId equals type.Id where text.Id == textId select new { text, type.Code }).SingleOrDefaultAsync(token);
        return row is null ? null : Map(row.text, row.Code);
    }

    public async Task<ConsentTextResponse?> PublishAsync(Guid typeId, PublishConsentTextRequest request, string body, string fingerprint, CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            // The type row lock serializes version numbers; the unique index is the final guard.
            var type = await db.ConsentTypes.FromSql($"SELECT * FROM consent_types WHERE id = {typeId} AND is_deleted = false FOR UPDATE").SingleOrDefaultAsync(token);
            if (type is null) return null;
            var previous = await db.ConsentTexts.AsNoTracking().SingleOrDefaultAsync(x => x.ConsentTypeId == typeId && x.OperationId == request.OperationId, token);
            if (previous is not null) return previous.Fingerprint == fingerprint ? Map(previous, type.Code) : throw Conflict(ErrorCatalog.ConsentOperationConflict);
            if (!type.IsEnabled) throw Conflict(ErrorCatalog.ConsentTypeDisabled);
            var next = (await db.ConsentTexts.IgnoreQueryFilters().Where(x => x.ConsentTypeId == typeId).MaxAsync(x => (int?)x.TextVersion, token) ?? 0) + 1;
            var text = new ConsentText(type, next, request.Title!, body, ConsentEvidence.Sha256(body), request.RequiresReconsent, request.OperationId, fingerprint);
            db.ConsentTexts.Add(text);
            try
            {
                await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
                return Map(text, type.Code);
            }
            catch (DbUpdateException error) when (attempt == 0 && error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(token); db.ChangeTracker.Clear();
            }
        }
    }

    private IQueryable<string> VisibleOwners(AccessScope access, Guid? property)
    {
        var memberships = db.PropertyMemberships.AsNoTracking().Where(m => m.IsEnabled && db.Properties.Any(p => p.Id == m.PropertyId));
        if (property is { } value) memberships = memberships.Where(m => m.PropertyId == value);
        if (!access.ReadAll)
        {
            var managed = db.PropertyMemberships.Where(m => m.UserId == access.UserId && m.IsEnabled).Select(m => m.PropertyId);
            memberships = memberships.Where(m => managed.Contains(m.PropertyId));
        }
        return memberships.Select(m => m.UserId);
    }

    private IQueryable<Row> Rows(AccessScope access, Guid? property)
    {
        var consents = db.UserConsents.AsNoTracking();
        if (!access.ReadAll || property is not null) { var owners = VisibleOwners(access, property); consents = consents.Where(c => owners.Contains(c.OwnerUserId)); }
        return from c in consents
               join t in db.ConsentTypes.IgnoreQueryFilters() on c.ConsentTypeId equals t.Id
               select new Row { Consent = c, Type = t, State =
                   c.RevokedAt != null ? (c.RevokeReason == ConsentRevokeReason.WITHDRAWN ? ConsentStates.Withdrawn : ConsentStates.Superseded)
                   : db.ConsentTexts.Any(x => x.ConsentTypeId == c.ConsentTypeId && x.TextVersion > c.TextVersion && x.RequiresReconsent) ? ConsentStates.Outdated
                   : ConsentStates.Active };
    }

    public async Task<PagedRows<ConsentRecordResponse>?> RecordsAsync(ConsentRecordQuery query, AccessScope access, CancellationToken token)
    {
        if (query.PropertyId is { } property && !await db.Properties.AnyAsync(p => p.Id == property && (access.ReadAll ||
                db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == access.UserId && m.IsEnabled)), token)) return null;
        var rows = Rows(access, query.PropertyId);
        if (query.TypeId is { } type) rows = rows.Where(x => x.Consent.ConsentTypeId == type);
        if (query.State is { } state) rows = rows.Where(x => x.State == state);
        if (query.UserId is { } owner) rows = rows.Where(x => x.Consent.OwnerUserId == owner);
        if (query.Search is { } search) { var pattern = Pattern(search); rows = rows.Where(x => EF.Functions.ILike(x.Consent.ApproverName, pattern, "\\")); }
        var total = await rows.CountAsync(token);
        var values = await rows.OrderByDescending(x => x.Consent.ApprovedAt).ThenByDescending(x => x.Consent.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        return new(values.Select(Map).ToArray(), query.Page, query.PageSize, total);
    }

    public async Task<ConsentDocumentResponse?> DocumentAsync(Guid record, AccessScope access, CancellationToken token)
    {
        var row = await Rows(access, null).SingleOrDefaultAsync(x => x.Consent.Id == record, token);
        if (row is null) return null;
        var text = await db.ConsentTexts.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == row.Consent.ConsentTextId, token);
        var bodyIntact = text.BodySha256 == row.Consent.BodySha256 && ConsentEvidence.Sha256(text.Body) == text.BodySha256;
        var evidenceIntact = ConsentEvidence.Compute(row.Consent, row.Type.Code) == row.Consent.EvidenceSha256;
        return new(Map(row), text.Title, text.Body, row.Consent.BodySha256, row.Consent.EvidenceSha256, bodyIntact, evidenceIntact);
    }

    public async Task<UserConsentHistory?> HistoryAsync(string userId, AccessScope access, CancellationToken token)
    {
        if (!access.ReadAll && !await VisibleOwners(access, null).AnyAsync(owner => owner == userId, token)) return null;
        var records = (await Rows(access, null).Where(x => x.Consent.OwnerUserId == userId)
            .OrderByDescending(x => x.Consent.ApprovedAt).ThenByDescending(x => x.Consent.Id).Take(500).ToListAsync(token)).Select(Map).ToArray();
        var types = await db.ConsentTypes.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(token);
        var versions = await db.ConsentTexts.AsNoTracking().GroupBy(x => x.ConsentTypeId).Select(g => new { g.Key, Max = g.Max(x => x.TextVersion) }).ToDictionaryAsync(x => x.Key, x => x.Max, token);
        var summary = types.Select(type =>
        {
            var own = records.Where(r => r.TypeId == type.Id).ToArray();
            var active = own.FirstOrDefault(r => r.RevokedAt is null);
            var state = active?.State ?? (own.Any(r => r.State == ConsentStates.Withdrawn) ? ConsentStates.Withdrawn : ConsentStates.None);
            return new ConsentTypeState(type.Id, type.Code, type.NameTr, type.NameEn, type.Kind.ToString(), state, active?.TextVersion, active?.ApprovedAt,
                versions.TryGetValue(type.Id, out var max) ? max : null);
        }).ToArray();
        return new(userId, summary, records);
    }

    private static ConsentTypeResponse Map(ConsentType x, int count, ConsentText? current) => new(x.Id, x.Code, x.NameTr, x.NameEn, x.Kind.ToString(), x.IsSystem,
        x.Feature?.ToString(), x.SortOrder, x.IsEnabled, x.Version, count,
        current is null ? null : new(current.Id, current.TextVersion, current.Title, current.RequiresReconsent, current.CreatedAt, current.CreatedBy));
    private static ConsentTextResponse Map(ConsentText x, string code) => new(x.Id, x.ConsentTypeId, code, x.TextVersion, x.Title, x.Body, x.BodySha256,
        x.RequiresReconsent, x.CreatedAt, x.CreatedBy);
    private static ConsentRecordResponse Map(Row x) => new(x.Consent.Id, x.Consent.OwnerUserId, x.Consent.ApproverName, x.Type.Id, x.Type.Code, x.Type.NameTr,
        x.Type.NameEn, x.Type.Kind.ToString(), x.Consent.ConsentTextId, x.Consent.TextVersion, x.Consent.ApprovedAt, x.Consent.Channel.ToString(), x.Consent.AppVersion,
        x.Consent.RevokedAt, x.Consent.RevokeReason?.ToString(), x.State);
}
