using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class SmsStore(BuildingFlowDbContext db, ISmsProtector protector, ISmsQueue queue) : ISmsStore
{
    private IQueryable<Property> Properties(AccessScope access) => db.Properties.Where(p => access.ReadAll ||
        db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == access.UserId && m.IsEnabled));
    private Task<bool> Visible(Guid property, AccessScope access, CancellationToken token) => Properties(access).AnyAsync(p => p.Id == property, token);
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());
    private static RequestValidationException Invalid(string field, ErrorDefinition? error = null) => new(new() { [field] = [(error ?? ErrorCatalog.SmsInvalid).Create()] });

    public async Task<SmsOverview?> OverviewAsync(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var account = await db.SmsAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property, token);
        return new(property, account is null ? null : Map(account), SmsTypes.All);
    }

    public async Task<SmsAccountResponse?> SaveAccountAsync(Guid property, SaveSmsAccountRequest request, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var account = await db.SmsAccounts.SingleOrDefaultAsync(x => x.PropertyId == property, token);
        var provider = Enum.Parse<SmsProvider>(request.Provider!);
        try
        {
            if (account is null)
            {
                if (request.Version != 0) throw Conflict(ErrorCatalog.RecordChanged);
                account = new SmsAccount(property, request.Name!, provider, request.SenderHeader!, request.LowCreditThreshold);
                db.SmsAccounts.Add(account);
            }
            else if (account.Version != request.Version) throw Conflict(ErrorCatalog.RecordChanged);
            account.Update(request.Name!, provider, request.SenderHeader!, request.Username, request.ApiUrl, request.LowCreditThreshold, request.IsEnabled);
        }
        catch (ArgumentException) { throw Invalid("senderHeader"); }
        if (!string.IsNullOrWhiteSpace(request.Secret)) account.SetSecret(protector.Protect(request.Secret.Trim()));
        db.Entry(account).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return Map(account);
    }

    public async Task<SmsCreditTransactionResponse?> AddCreditAsync(Guid property, SmsCreditRequest request, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var account = await db.SmsAccounts.FromSql($"SELECT * FROM sms_accounts WHERE property_id = {property} AND is_deleted = false FOR UPDATE").IgnoreQueryFilters().SingleOrDefaultAsync(token)
            ?? throw Conflict(ErrorCatalog.SmsAccountRequired);
        SmsCreditTransaction entry;
        try { entry = new SmsCreditTransaction(account, Enum.Parse<SmsCreditTransactionType>(request.Type!), request.Quantity, request.UnitPrice, request.Currency, request.ReferenceNo, request.Description, null); }
        catch (InvalidOperationException) { throw Conflict(ErrorCatalog.SmsCreditInsufficient); }
        catch (ArgumentException) { throw Invalid("quantity"); }
        db.SmsCreditTransactions.Add(entry);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return Map(entry);
    }

    public async Task<PagedRows<SmsCreditTransactionResponse>?> CreditsAsync(Guid property, int page, int pageSize, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var query = db.SmsCreditTransactions.AsNoTracking().Where(x => x.PropertyId == property);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(Map).ToArray(), page, pageSize, await query.CountAsync(token));
    }

    public async Task<IReadOnlyList<SmsTemplateResponse>?> TemplatesAsync(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var saved = await db.SmsTemplates.AsNoTracking().Where(x => x.PropertyId == property).ToListAsync(token);
        return SmsTypes.All.Select(type => saved.SingleOrDefault(x => x.TypeCode == type.Code) is { } template ? Map(template, type)
            : new SmsTemplateResponse(null, type.Code, type.Code, type.DefaultContent, type.Purpose.ToString(), true, true, SmsText.Segments(type.DefaultContent), 0)).ToArray();
    }

    public async Task<SmsTemplateResponse?> SaveTemplateAsync(Guid property, Guid? templateId, SmsTemplateRequest request, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var type = SmsTypes.Find(request.TypeCode)!;
        SmsTemplate template;
        if (templateId is null)
        {
            if (await db.SmsTemplates.AnyAsync(x => x.PropertyId == property && x.TypeCode == type.Code, token)) throw Conflict(ErrorCatalog.SmsTemplateConflict);
            template = new SmsTemplate(property, type.Code, request.Name!, request.Content!);
            template.SetEnabled(request.IsEnabled);
            db.SmsTemplates.Add(template);
        }
        else
        {
            var found = await db.SmsTemplates.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == templateId, token);
            if (found is null) return null;
            if (found.TypeCode != type.Code) throw Invalid("typeCode");
            if (found.Version != request.Version) throw Conflict(ErrorCatalog.RecordChanged);
            found.Update(request.Name!, request.Content!, request.IsEnabled);
            db.Entry(found).Property(x => x.IsEnabled).IsModified = true;
            template = found;
        }
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { throw Conflict(ErrorCatalog.SmsTemplateConflict); }
        return Map(template, type);
    }

    public async Task<bool> DeleteTemplateAsync(Guid property, Guid templateId, long version, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return false;
        var template = await db.SmsTemplates.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == templateId, token);
        if (template is null) return false;
        if (template.Version != version) throw Conflict(ErrorCatalog.RecordChanged);
        db.Remove(template); await db.SaveChangesAsync(token);
        return true;
    }

    public async Task<PagedRows<SmsMessageResponse>?> MessagesAsync(Guid property, int page, int pageSize, string? status, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var query = db.SmsMessages.AsNoTracking().Where(x => x.PropertyId == property);
        if (status is not null) { var value = Enum.Parse<SmsMessageStatus>(status); query = query.Where(x => x.Status == value); }
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(Map).ToArray(), page, pageSize, await query.CountAsync(token));
    }

    public async Task<SmsMessageResponse?> SendAsync(Guid property, SendSmsRequest request, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var previous = await db.SmsMessages.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property && x.RequestId == request.RequestId, token);
        if (previous is not null) return Map(previous);
        var phone = request.PhoneNumber;
        if (request.PersonId is { } personId)
        {
            var person = await db.People.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == personId, token);
            if (person is null) return null;
            phone = person.PhoneNumber;
        }
        var account = await db.SmsAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property, token);
        if (account is not { IsEnabled: true }) throw Conflict(ErrorCatalog.SmsAccountRequired);
        if (account.CreditBalance < SmsText.Segments(request.Body!)) throw Conflict(ErrorCatalog.SmsCreditInsufficient);
        var result = await queue.EnqueueAsync(property, SmsTypes.Manual, phone, new Dictionary<string, string> { ["message"] = request.Body! }, request.PersonId, request.RequestId, token);
        if (result.MessageId is null) throw result.ErrorCode == SmsQueue.PhoneInvalid ? Invalid("phoneNumber", ErrorCatalog.SmsPhoneInvalid) : Invalid("body");
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            return Map(await db.SmsMessages.AsNoTracking().SingleAsync(x => x.PropertyId == property && x.RequestId == request.RequestId, token));
        }
        return Map(await db.SmsMessages.AsNoTracking().SingleAsync(x => x.Id == result.MessageId, token));
    }

    private static SmsAccountResponse Map(SmsAccount x) => new(x.Id, x.PropertyId, x.Name, x.Provider.ToString(), x.SenderHeader, x.Username, x.ProtectedSecret is not null,
        x.ApiUrl, x.CreditBalance, x.LowCreditThreshold, x.CreditBalance <= x.LowCreditThreshold, x.IsEnabled, x.Version);
    private static SmsCreditTransactionResponse Map(SmsCreditTransaction x) => new(x.Id, x.Type.ToString(), x.Quantity, x.BalanceAfter, x.UnitPrice, x.Currency,
        x.ReferenceNo, x.Description, x.MessageId, x.CreatedAt, x.CreatedBy);
    private static SmsTemplateResponse Map(SmsTemplate x, SmsTypeDefinition type) => new(x.Id, x.TypeCode, x.Name, x.Content, type.Purpose.ToString(), x.IsEnabled, false,
        SmsText.Segments(x.Content), x.Version);
    private static SmsMessageResponse Map(SmsMessage x) => new(x.Id, x.TypeCode, x.Purpose.ToString(), x.PersonId, x.PhoneMasked, x.MaskedBody, x.Segments, x.CreditCost,
        x.Status.ToString(), x.Provider?.ToString(), x.ProviderMessageId, x.ErrorCode, x.Attempts, x.CreatedAt, x.SentAt, x.CreatedBy);
}
