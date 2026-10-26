using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class SmsConsentStore(BuildingFlowDbContext db, ISmsPolicyResolver resolver, ICommercialConsentStore consents) : ISmsConsentStore
{
    private const long PlatformSenderLock = 0x5B5_C0DE;
    private Task<bool> Visible(Guid property, AccessScope access, CancellationToken token) => db.Properties.AnyAsync(p => p.Id == property && (access.ReadAll ||
        db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == access.UserId && m.IsEnabled)), token);
    private static RequestValidationException Invalid(string field, ErrorDefinition? error = null) => new(new() { [field] = [(error ?? ErrorCatalog.SmsInvalid).Create()] });
    private Task<SmsLegalSender?> Own(Guid? property, CancellationToken token) => db.SmsLegalSenders.SingleOrDefaultAsync(x => x.PropertyId == property, token);
    // Same choice as the send path: the Property's enabled sender, otherwise the platform company.
    private Task<SmsLegalSender?> Effective(Guid property, CancellationToken token) => db.SmsLegalSenders.AsNoTracking()
        .Where(x => x.IsEnabled && (x.PropertyId == property || x.PropertyId == null)).OrderBy(x => x.PropertyId == null).FirstOrDefaultAsync(token);

    public async Task<LegalSenderView?> LegalSenderAsync(Guid? property, AccessScope access, CancellationToken token)
    {
        if (property is { } id && !await Visible(id, access, token)) return null;
        var sender = await db.SmsLegalSenders.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property, token);
        return new(sender is null ? null : Map(sender));
    }

    public async Task<LegalSenderResponse?> SaveLegalSenderAsync(Guid? property, SaveLegalSenderRequest r, AccessScope access, CancellationToken token)
    {
        if (property is { } id && !await Visible(id, access, token)) return null;
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (property is { } locked) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {locked} FOR UPDATE", token);
        else await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({PlatformSenderLock})", token);
        var sender = await Own(property, token);
        if ((sender?.Version ?? 0) != r.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if (sender is null) db.SmsLegalSenders.Add(sender = new SmsLegalSender(property, r.Title!, r.MersisNumber, r.ContactInformation!, r.OptOutKeyword!, r.OptOutShortCode!,
            r.OptOutEnabled, r.IysIntegrationEnabled, r.IysBrandCode));
        else sender.Update(r.Title!, r.MersisNumber, r.ContactInformation!, r.OptOutKeyword!, r.OptOutShortCode!, r.OptOutEnabled, r.IysIntegrationEnabled, r.IysBrandCode);
        sender.SetEnabled(r.IsEnabled);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return Map(sender);
    }

    public async Task<SmsPreviewResponse?> PreviewAsync(Guid property, SmsTypeDefinition type, string body, string? phone, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var policy = await resolver.ResolveAsync(property, type, phone, token);
        var text = body + policy.Footer;
        var length = SmsText.Measure(text);
        return new(type.Code, type.Category.ToString(), text, policy.Footer, length.Encoding, length.Characters, length.Segments,
            policy.Allowed && length.Segments <= SmsText.MaxSegments, policy.Allowed && length.Segments > SmsText.MaxSegments ? SmsQueue.TooLong : policy.ErrorCode, policy.LegalSenderId);
    }

    public async Task<PagedRows<CommercialConsentState>?> ConsentsAsync(Guid property, int page, int pageSize, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var sender = await Effective(property, token);
        if (sender is null) return new([], page, pageSize, 0);
        var query = db.CommercialConsentRecords.AsNoTracking().Where(x => x.SenderId == sender.Id).OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(CommercialConsentStore.Map).ToArray(), page, pageSize, await query.CountAsync(token));
    }

    public async Task<CommercialConsentState?> OptOutAsync(Guid property, Guid operationId, string phone, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var sender = await Effective(property, token) ?? throw Invalid("sender");
        return await consents.RecordAsync(new(sender.Id, phone, false, CommercialConsentStatus.REJECTED, CommercialConsentSource.ADMIN, operationId, access.UserId), token);
    }

    public async Task<CommunicationPreferences> PreferencesAsync(string userId, CancellationToken token)
    {
        var phone = await VerifiedPhone(userId, token);
        var items = new List<CommunicationPreferenceItem>();
        foreach (var sender in await Senders(userId, token))
        {
            var current = phone is null ? null : await consents.CurrentAsync(sender.Id, phone, token);
            items.Add(Item(sender, current));
        }
        return new(phone is null ? null : SmsText.MaskPhone(phone), CommercialConsentTexts.Current, items);
    }

    public async Task<CommunicationPreferenceItem?> SetPreferenceAsync(string userId, Guid senderId, CommunicationPreferenceCommand c, CancellationToken token)
    {
        var sender = (await Senders(userId, token)).SingleOrDefault(x => x.Id == senderId);
        if (sender is null) return null;
        var phone = await VerifiedPhone(userId, token) ?? throw new HierarchyConflictException(ErrorCatalog.SmsPhoneNotVerified.Create());
        var source = c.Channel == "MOBILE" ? CommercialConsentSource.MOBILE : CommercialConsentSource.WEB;
        var state = await consents.RecordAsync(c.Approved
            ? new(sender.Id, phone, true, CommercialConsentStatus.APPROVED, source, c.OperationId, userId, null, c.TextVersion, c.TextSha256)
            : new(sender.Id, phone, true, CommercialConsentStatus.REJECTED, source, c.OperationId, userId), token);
        return Item(sender, state);
    }

    public async Task<PagedRows<CommercialConsentState>?> PreferenceHistoryAsync(string userId, Guid senderId, int page, int pageSize, CancellationToken token)
    {
        if (!(await Senders(userId, token)).Any(x => x.Id == senderId)) return null;
        var phone = await VerifiedPhone(userId, token);
        return phone is null ? new([], page, pageSize, 0) : await consents.HistoryAsync(senderId, phone, page, pageSize, token);
    }

    // The platform sender plus the senders of Properties where the account holds an active membership.
    private async Task<List<SmsLegalSender>> Senders(string userId, CancellationToken token) => await db.SmsLegalSenders.AsNoTracking()
        .Where(x => x.IsEnabled && (x.PropertyId == null || db.PropertyMemberships.Any(m => m.PropertyId == x.PropertyId && m.UserId == userId && m.IsEnabled)))
        .OrderBy(x => x.PropertyId != null).ThenBy(x => x.Title).ToListAsync(token);
    private async Task<string?> VerifiedPhone(string userId, CancellationToken token) => SmsText.NormalizePhone(await db.Set<AccountPhoneVerification>().AsNoTracking()
        .Where(x => x.OwnerUserId == userId && x.VerifiedKey != null).Select(x => x.VerifiedKey).SingleOrDefaultAsync(token));
    private static CommunicationPreferenceItem Item(SmsLegalSender sender, CommercialConsentState? current) => new(sender.Id, sender.PropertyId is null ? "PLATFORM" : "PROPERTY",
        sender.PropertyId, sender.Title, current?.Status ?? "NONE", current?.OccurredAt, current?.TextVersion);
    private static LegalSenderResponse Map(SmsLegalSender x) => new(x.Id, x.PropertyId, x.Title, x.MersisNumber, x.ContactInformation, x.OptOutKeyword, x.OptOutShortCode,
        x.OptOutEnabled, x.IysIntegrationEnabled, x.IysBrandCode, x.IsEnabled, x.Version);
}
