using System.Security.Cryptography;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

internal sealed record SmsPayload(string PhoneNumber, string Body);

public sealed class SmsQueue(BuildingFlowDbContext db, ISmsProtector protector, TimeProvider clock, ISmsPolicyCatalog? policies = null) : ISmsQueue
{
    public const string Queued = "QUEUED", Failed = "FAILED", Skipped = "SKIPPED";
    public const string NoAccount = "NO_ACCOUNT", InsufficientCredit = "INSUFFICIENT_CREDIT", PhoneInvalid = "PHONE_INVALID", TemplateDisabled = "TEMPLATE_DISABLED", TooLong = "TOO_LONG",
        CommercialConsentRequired = "COMMERCIAL_CONSENT_REQUIRED";

    public async Task<SmsQueueResult> EnqueueAsync(Guid property, string typeCode, string? phoneNumber, IReadOnlyDictionary<string, string> parameters,
        Guid? personId, Guid? requestId, CancellationToken token)
    {
        var type = SmsTypes.Find(typeCode) ?? throw new ArgumentException("Unknown SMS type.", nameof(typeCode));
        // Commercial approval and the legal footer arrive in BF-SMS 03/04; until then a commercial type must never reach a provider.
        if ((policies ?? SmsPolicyCatalog.Default).Get(type.Category).RequiresCommercialConsent) return new(null, Skipped, CommercialConsentRequired);
        var template = await db.SmsTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property && x.TypeCode == type.Code, token);
        if (template is { IsEnabled: false }) return new(null, Skipped, TemplateDisabled);
        var phone = SmsText.NormalizePhone(phoneNumber);
        if (phone is null) return new(null, Failed, PhoneInvalid);
        var content = template?.Content ?? type.DefaultContent;
        var body = SmsText.Render(content, parameters);
        var segments = SmsText.Segments(body);
        if (segments is < 1 or > SmsText.MaxSegments) return new(null, Failed, TooLong);
        var account = await db.SmsAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property, token);
        var now = clock.GetUtcNow();
        var message = new SmsMessage(property, account?.Id, personId, requestId, type.Code, type.Purpose, SmsText.MaskPhone(phone), SmsText.Mask(content, type, parameters), segments, Trim(now));
        if (account is null || !account.IsEnabled) message.Reject(NoAccount);
        else if (account.CreditBalance < segments) message.Reject(InsufficientCredit);
        else message.Protect(protector.Protect(JsonSerializer.Serialize(new SmsPayload(phone, body))));
        db.SmsMessages.Add(message);
        return new(message.Id, message.Status.ToString(), message.ErrorCode);
    }
    internal static DateTimeOffset Trim(DateTimeOffset value) => value.AddTicks(-(value.Ticks % 10));
}

public sealed class MockSmsProvider : ISmsProvider
{
    public SmsProvider Kind => SmsProvider.MOCK;
    public Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken token) =>
        Task.FromResult(new SmsSendResult(true, "MOCK-" + Guid.NewGuid().ToString("N"), null, false));
}

// One message per transaction: the account row stays locked while the provider call runs so balance checks and debits cannot race.
public sealed class SmsDispatcher(BuildingFlowDbContext db, ISmsProtector protector, IEnumerable<ISmsProvider> providers, TimeProvider clock)
{
    public const int BatchSize = 20;
    public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE", PayloadUnreadable = "PAYLOAD_UNREADABLE", ProviderError = "PROVIDER_ERROR";

    public async Task<int> DispatchPendingAsync(CancellationToken token)
    {
        var processed = 0;
        while (processed < BatchSize && await DispatchNextAsync(token)) processed++;
        return processed;
    }

    private async Task<bool> DispatchNextAsync(CancellationToken token)
    {
        var now = SmsQueue.Trim(clock.GetUtcNow());
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var message = await db.SmsMessages.FromSql($"""
            SELECT * FROM sms_messages WHERE status = 'QUEUED' AND is_deleted = false AND next_attempt_at <= {now}
            ORDER BY next_attempt_at, created_at LIMIT 1 FOR UPDATE SKIP LOCKED
            """).IgnoreQueryFilters().SingleOrDefaultAsync(token);
        if (message is null) return false;
        var account = message.AccountId is { } accountId
            ? await db.SmsAccounts.FromSql($"SELECT * FROM sms_accounts WHERE id = {accountId} AND is_deleted = false FOR UPDATE").IgnoreQueryFilters().SingleOrDefaultAsync(token)
            : null;
        var provider = account is null ? null : providers.FirstOrDefault(x => x.Kind == account.Provider);
        if (account is null || !account.IsEnabled) message.Reject(SmsQueue.NoAccount);
        else if (provider is null) message.Reject(ProviderUnavailable);
        else if (account.CreditBalance < message.CreditCost) message.Reject(SmsQueue.InsufficientCredit);
        else await SendAsync(message, account, provider, now, token);
        await db.SaveChangesAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        db.ChangeTracker.Clear();
        return true;
    }

    private async Task SendAsync(SmsMessage message, SmsAccount account, ISmsProvider provider, DateTimeOffset now, CancellationToken token)
    {
        SmsPayload payload;
        try { payload = JsonSerializer.Deserialize<SmsPayload>(protector.Unprotect(message.ProtectedPayload!)) ?? throw new CryptographicException(); }
        catch (Exception error) when (error is CryptographicException or FormatException or JsonException) { message.Reject(PayloadUnreadable); return; }
        SmsSendResult result;
        try { result = await provider.SendAsync(new(account, payload.PhoneNumber, payload.Body), token); }
        catch (Exception error) when (error is HttpRequestException or TimeoutException) { result = new(false, null, ProviderError, false); }
        if (result.Success && !string.IsNullOrWhiteSpace(result.ProviderMessageId))
        {
            db.SmsCreditTransactions.Add(new SmsCreditTransaction(account, SmsCreditTransactionType.CONSUME, message.CreditCost, null, null, null, null, message.Id));
            message.MarkSent(provider.Kind, result.ProviderMessageId, now);
        }
        else message.RecordFailure(result.ErrorCode ?? ProviderError, now.Add(OutboxRelay.Backoff(message.Attempts + 4)), result.Permanent);
    }
}
