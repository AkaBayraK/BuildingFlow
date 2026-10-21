using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class MobileConsentStore(BuildingFlowDbContext db, IMobileSessionStore sessions, ICurrentUserService actor, TimeProvider clock) : IMobileConsentStore
{
    private static readonly string[] Management = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT"];
    private string User() => actor.IsAuthenticated && !string.IsNullOrWhiteSpace(actor.UserId) ? actor.UserId : throw new UnauthorizedAccessException();
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());
    private static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.ConsentInvalid.Create()] });

    // Consent belongs to the account itself, so accounts without a person link may use it (same rule as the mobile profile).
    private async Task Authorize(CancellationToken token)
    {
        var session = await sessions.ReadAsync(User(), actor.Username, Management.Where(actor.IsInRole).ToArray(), token);
        if (session.HasMore) throw new UnauthorizedAccessException();
    }

    private sealed record Current(ConsentType Type, ConsentText? Text, UserConsent? Active, bool Withdrawn);

    private async Task<Current[]> LoadAsync(string user, string? code, CancellationToken token)
    {
        var types = await db.ConsentTypes.AsNoTracking().Where(t => t.IsEnabled && (code == null || t.Code == code))
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Code).ToListAsync(token);
        var ids = types.Select(t => t.Id).ToArray();
        var texts = await db.ConsentTexts.AsNoTracking().Where(x => ids.Contains(x.ConsentTypeId))
            .GroupBy(x => x.ConsentTypeId).Select(g => g.OrderByDescending(x => x.TextVersion).First()).ToListAsync(token);
        var active = await db.UserConsents.AsNoTracking().Where(c => c.OwnerUserId == user && c.RevokedAt == null && ids.Contains(c.ConsentTypeId)).ToListAsync(token);
        var withdrawn = await db.UserConsents.AsNoTracking().Where(c => c.OwnerUserId == user && c.RevokeReason == ConsentRevokeReason.WITHDRAWN && ids.Contains(c.ConsentTypeId))
            .Select(c => c.ConsentTypeId).Distinct().ToListAsync(token);
        return types.Select(t => new Current(t, texts.SingleOrDefault(x => x.ConsentTypeId == t.Id), active.SingleOrDefault(c => c.ConsentTypeId == t.Id), withdrawn.Contains(t.Id))).ToArray();
    }

    private async Task<bool> OutdatedAsync(UserConsent consent, CancellationToken token) =>
        await db.ConsentTexts.AnyAsync(x => x.ConsentTypeId == consent.ConsentTypeId && x.TextVersion > consent.TextVersion && x.RequiresReconsent, token);

    private async Task<MobileConsentState> StateAsync(Current c, CancellationToken token)
    {
        var state = c.Active is { } active ? (await OutdatedAsync(active, token) ? ConsentStates.Outdated : ConsentStates.Active)
            : c.Withdrawn ? ConsentStates.Withdrawn : ConsentStates.None;
        return new(c.Type.Code, c.Type.NameTr, c.Type.NameEn, c.Type.Kind.ToString(), c.Type.Feature?.ToString(), c.Type.SortOrder, state,
            c.Active?.TextVersion, c.Active?.ApprovedAt, c.Text?.Id, c.Text?.TextVersion);
    }

    public async Task<MobileConsentList> ListAsync(CancellationToken token)
    {
        await Authorize(token);
        var user = User();
        var items = new List<MobileConsentItem>();
        foreach (var c in await LoadAsync(user, null, token))
            items.Add(new(await StateAsync(c, token), c.Text is { } t ? new(t.Id, t.TextVersion, t.Title, t.Body, t.BodySha256, t.RequiresReconsent, t.CreatedAt) : null));
        return new(user, items.ToArray());
    }

    public async Task<MobileConsentResult> CommandAsync(MobileConsentCommand c, CancellationToken token, ConsentChannel channel = ConsentChannel.MOBILE)
    {
        await Authorize(token);
        var user = User();
        if (c.OperationId == Guid.Empty) Invalid("operationId");
        if (c.Action is not (MobileConsentActions.Approve or MobileConsentActions.Withdraw)) Invalid("action");
        var code = c.TypeCode?.Trim();
        if (string.IsNullOrEmpty(code) || code.Length > ConsentType.MaxCodeLength) Invalid("typeCode");
        var approve = c.Action == MobileConsentActions.Approve;
        if (approve && (c.TextId is null || c.TextId == Guid.Empty)) Invalid("textId");
        if (approve && (c.BodySha256 is not { Length: 64 } hash || hash.Any(x => !char.IsAsciiHexDigitLower(x) && !char.IsAsciiDigit(x)))) Invalid("bodySha256");
        if (!approve && (c.TextId is not null || c.BodySha256 is not null)) Invalid("textId");
        var appVersion = string.IsNullOrWhiteSpace(c.AppVersion) ? null : c.AppVersion.Trim();
        if (appVersion is { Length: > 40 } || appVersion?.Any(char.IsControl) == true) Invalid("appVersion");
        var canonical = new { c.OperationId, c.Action, TypeCode = code, c.TextId, c.BodySha256, AppVersion = appVersion };
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("consent-v1:" + JsonSerializer.Serialize(canonical))));

        await using var tx = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({user + ":" + c.OperationId},0))", token);
        var receipt = await db.Set<MobileOperationReceipt>().IgnoreQueryFilters().SingleOrDefaultAsync(r => r.OwnerUserId == user && r.OperationId == c.OperationId, token);
        if (receipt != null)
        {
            if (receipt.IsDeleted || receipt.Fingerprint != fingerprint) throw Conflict(ErrorCatalog.ConsentOperationConflict);
            return new(c.OperationId, JsonSerializer.Deserialize<MobileConsentState>(receipt.Result)!, true);
        }
        var type = await db.ConsentTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Code == code, token);
        if (type is null) Invalid("typeCode");
        if (!type!.IsEnabled) throw Conflict(ErrorCatalog.ConsentTypeDisabled);
        // Same order as profile writes (profile lock, then consent lock) so a withdrawal cannot deadlock an upload.
        if (!approve && type.Feature == ConsentFeature.PROFILE_PHOTO)
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"profile:" + user},0))", token);
        await ConsentGate.LockAsync(db, user, token);

        var now = clock.GetUtcNow();
        var active = await db.UserConsents.SingleOrDefaultAsync(x => x.OwnerUserId == user && x.ConsentTypeId == type.Id && x.RevokedAt == null, token);
        if (approve)
        {
            var current = await db.ConsentTexts.AsNoTracking().Where(x => x.ConsentTypeId == type.Id).OrderByDescending(x => x.TextVersion).FirstOrDefaultAsync(token);
            // Only the latest text can be approved, and only with the body the person actually read.
            if (current is null || current.Id != c.TextId || current.BodySha256 != c.BodySha256) throw Conflict(ErrorCatalog.ConsentTextOutdated);
            if (active?.ConsentTextId != current.Id)
            {
                if (active is not null) { active.Revoke(ConsentRevokeReason.SUPERSEDED, now, c.OperationId); await db.SaveChangesAsync(token); }
                var consent = new UserConsent(user, await ApproverAsync(user, token), type, current, now, channel, appVersion, c.OperationId, fingerprint);
                consent.Seal(ConsentEvidence.Compute(consent, type.Code));
                db.UserConsents.Add(consent);
                await db.SaveChangesAsync(token);
            }
        }
        else
        {
            if (type.Kind == ConsentKind.INFORMATION) throw Conflict(ErrorCatalog.ConsentNotWithdrawable);
            if (active is not null)
            {
                active.Revoke(ConsentRevokeReason.WITHDRAWN, now, c.OperationId);
                if (type.Feature == ConsentFeature.PROFILE_PHOTO) await RemoveProfilePhotoAsync(user, token);
                // REQUEST_PHOTO rows are not touched: the centre keeps them, ConsentGate.VisibleToOwner hides them from the owner.
                await db.SaveChangesAsync(token);
            }
        }

        var state = await StateAsync((await LoadAsync(user, code, token)).Single(), token);
        db.Add(new MobileOperationReceipt(user, c.OperationId, fingerprint, JsonSerializer.Serialize(state)));
        await db.SaveChangesAsync(token);
        await tx.CommitAsync(token);
        return new(c.OperationId, state, false);
    }

    private async Task RemoveProfilePhotoAsync(string user, CancellationToken token)
    {
        var profile = await db.Set<MobileProfile>().SingleOrDefaultAsync(x => x.OwnerUserId == user, token);
        if (profile?.PhotoId is not Guid photoId) return;
        // RETIRED objects are purged from storage by ProfilePhotoMaintenance; the profile version moves so stale edits conflict.
        var photo = await db.Set<MobileProfilePhoto>().SingleOrDefaultAsync(x => x.Id == photoId && x.OwnerUserId == user, token);
        photo?.Retire();
        profile.SetPhoto(null);
        db.Entry(profile).State = EntityState.Modified;
    }

    private async Task<string> ApproverAsync(string user, CancellationToken token)
    {
        var profile = await db.Set<MobileProfile>().AsNoTracking().Where(x => x.OwnerUserId == user).Select(x => new { x.FirstName, x.LastName }).SingleOrDefaultAsync(token);
        var name = profile is null ? "" : $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name[..Math.Min(name.Length, UserConsent.MaxApproverLength)]
            : !string.IsNullOrWhiteSpace(actor.Username) ? actor.Username.Trim()[..Math.Min(actor.Username.Trim().Length, UserConsent.MaxApproverLength)] : user;
    }
}
