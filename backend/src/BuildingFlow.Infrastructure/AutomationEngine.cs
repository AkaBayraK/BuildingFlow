using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

internal sealed record AutomationCandidate(Guid? UnitId, string? UnitNumber, Guid? ChargeId, Guid? PaymentId, int? Offset, DateOnly? Period, DateOnly? DueDate,
    decimal Balance, IReadOnlyDictionary<string, string> Values, IReadOnlyList<AutomationRecipient> Recipients, string? SourceCode = null, Guid? SourceId = null, DateOnly? EventDate = null);

// Id is the dedup identity (property Person or, for management accounts, the central person); UserId is the account that reads the in-app inbox.
internal sealed record AutomationRecipient(Guid Id, Guid? PersonId, string? UserId, string? PhoneNumber, string? Email)
{
    public static AutomationRecipient From(Person p) => new(p.Id, p.Id, p.LinkedUserId, p.PhoneNumber, p.Email);
}

// Evaluates one definition against current debt; the caller holds the Property lock, so payments and reminders never interleave.
public sealed class AutomationEngine(BuildingFlowDbContext db, ISmsQueue sms, IMessageOutbox outbox, AutomationChannelOptions channels, TimeProvider clock, AutomationSourceReader sources)
{
    public const int MaxCandidates = 2000, PaymentLookbackHours = 72;
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly OccupancyType[] Debtors = [OccupancyType.OWNER, OccupancyType.TENANT];

    internal async Task<(int Evaluated, int Dispatched, int Skipped)> DispatchAsync(AutomationDefinition definition, AutomationExecution execution,
        DateOnly today, bool quiet, CancellationToken token)
    {
        var candidates = await CandidatesAsync(definition, today, token);
        var channels = Channels(definition, quiet);
        var planned = candidates.SelectMany(c => c.Recipients.SelectMany(p => channels.Select(ch => (c, p, ch, key: Key(definition, c, p.Id, ch))))).ToArray();
        var keys = planned.Select(x => x.key).ToArray();
        var existing = keys.Length == 0 ? new HashSet<string>() : (await db.Set<NotificationDispatch>().IgnoreQueryFilters().Where(d => keys.Contains(d.DedupKey)).Select(d => d.DedupKey).ToListAsync(token)).ToHashSet();
        var unreached = candidates.Where(c => c.Recipients.Count == 0).SelectMany(c => channels.Select(ch => (c, ch, key: Key(definition, c, null, ch)))).ToArray();
        var unreachedKeys = unreached.Select(x => x.key).ToArray();
        var known = unreachedKeys.Length == 0 ? new HashSet<string>() : (await db.Set<NotificationDispatch>().IgnoreQueryFilters().Where(d => unreachedKeys.Contains(d.DedupKey)).Select(d => d.DedupKey).ToListAsync(token)).ToHashSet();
        var property = await db.Properties.AsNoTracking().SingleAsync(p => p.Id == definition.PropertyId, token);
        int dispatched = 0, skipped = 0;
        foreach (var (candidate, channel, key) in unreached.Where(x => !known.Contains(x.key)))
        {
            var row = New(definition, execution, candidate, null, channel, key); row.Skip("NO_RECIPIENT"); skipped++;
        }
        foreach (var (candidate, person, channel, key) in planned.Where(x => !existing.Contains(x.key)))
        {
            var row = New(definition, execution, candidate, person.Id, channel, key);
            var values = new Dictionary<string, string>(candidate.Values) { ["propertyName"] = property.Name, ["today"] = today.ToString("dd.MM.yyyy", Turkish) };
            if (await SendAsync(definition, execution, row, person, values, token)) dispatched++; else skipped++;
        }
        return (candidates.Count, dispatched, skipped);
    }

    internal async Task<IReadOnlyList<AutomationCandidate>> CandidatesAsync(AutomationDefinition definition, DateOnly today, CancellationToken token) =>
        definition.Type switch {
            AutomationType.PAYMENT_RECEIVED => await PaymentCandidatesAsync(definition, today, token),
            AutomationType.CUSTOM => await CustomCandidatesAsync(definition, today, token),
            _ => await ChargeCandidatesAsync(definition, today, token)
        };

    private async Task<IReadOnlyList<AutomationCandidate>> CustomCandidatesAsync(AutomationDefinition d, DateOnly today, CancellationToken token)
    {
        var type = await db.Set<AutomationTypeDefinition>().AsNoTracking().SingleOrDefaultAsync(t => t.Id == d.CustomTypeId, token);
        if (type is null || type.Status != AutomationTypeStatus.PUBLISHED || type.PropertyId is { } owner && owner != d.PropertyId) return [];
        return await sources.CandidatesAsync(d, type, today, d.ActivatedAt ?? clock.GetUtcNow(), token);
    }

    internal static NotificationChannel[] Channels(AutomationDefinition d, bool quiet) =>
        [.. new[] { (d.InAppEnabled, NotificationChannel.IN_APP), (d.SmsEnabled && !quiet, NotificationChannel.SMS), (d.EmailEnabled && !quiet, NotificationChannel.EMAIL) }
            .Where(x => x.Item1).Select(x => x.Item2)];

    internal static (string Title, string Body) Render(AutomationDefinition d, IReadOnlyDictionary<string, string> values) =>
        (Clip(SmsText.Render(d.TitleTemplate, values), AutomationDefinition.MaxTitle), Clip(SmsText.Render(d.BodyTemplate, values), AutomationDefinition.MaxBody));

    private async Task<IReadOnlyList<AutomationCandidate>> ChargeCandidatesAsync(AutomationDefinition d, DateOnly today, CancellationToken token)
    {
        var dueDates = d.OffsetDays.Select(o => today.AddDays(-o)).ToArray();
        var charges = await db.DuesCharges.AsNoTracking()
            .Where(c => c.PropertyId == d.PropertyId && !c.IsCancelled && c.Amount > c.PaidAmount && dueDates.Contains(c.DueDate) &&
                db.Units.Any(u => u.Id == c.UnitId && u.PropertyId == c.PropertyId && u.IsEnabled))
            .OrderBy(c => c.DueDate).ThenBy(c => c.Id).Take(MaxCandidates).ToListAsync(token);
        if (charges.Count == 0) return [];
        var units = await Units(d.PropertyId, charges.Select(c => c.UnitId), token);
        var recipients = await RecipientsAsync(d.PropertyId, charges.Select(c => (c.Id, c.UnitId)).ToArray(), today, token);
        return charges.Select(c => {
            var offset = today.DayNumber - c.DueDate.DayNumber;
            var values = new Dictionary<string, string> {
                ["unitNumber"] = units[c.UnitId], ["periodLabel"] = Turkish.TextInfo.ToTitleCase(c.Period.ToString("MMMM yyyy", Turkish)),
                ["chargeLabel"] = c.Kind == ChargeKind.DUES ? "aidat" : "ek ödeme", ["amount"] = Money(c.Amount), ["balance"] = Money(c.Balance),
                ["dueDate"] = c.DueDate.ToString("dd.MM.yyyy", Turkish), ["daysLeft"] = (-offset).ToString(Turkish), ["daysOverdue"] = offset.ToString(Turkish),
                ["dueText"] = offset == 0 ? "bugün son ödeme günü" : $"son ödemeye {-offset} gün kaldı"
            };
            return new AutomationCandidate(c.UnitId, units[c.UnitId], c.Id, null, offset, c.Period, c.DueDate, c.Balance, values, recipients[c.Id]);
        }).ToArray();
    }

    private async Task<IReadOnlyList<AutomationCandidate>> PaymentCandidatesAsync(AutomationDefinition d, DateOnly today, CancellationToken token)
    {
        var activated = d.ActivatedAt ?? clock.GetUtcNow();
        var since = clock.GetUtcNow().AddHours(-PaymentLookbackHours);
        if (activated > since) since = activated;
        var payments = await db.Payments.AsNoTracking().Where(p => p.PropertyId == d.PropertyId && !p.IsCancelled && p.CreatedAt >= since)
            .OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).Take(MaxCandidates).ToListAsync(token);
        if (payments.Count == 0) return [];
        var units = await Units(d.PropertyId, payments.Select(p => p.UnitId), token);
        var ids = payments.Select(p => p.Id).ToArray();
        var allocations = await db.PaymentAllocations.AsNoTracking().Where(a => ids.Contains(a.PaymentId)).Select(a => new { a.PaymentId, a.ChargeId }).Distinct().ToListAsync(token);
        var pairs = allocations.Select(a => (a.ChargeId, payments.Single(p => p.Id == a.PaymentId).UnitId)).Distinct().ToArray();
        var chargeRecipients = await RecipientsAsync(d.PropertyId, pairs, today, token);
        var unitIds = payments.Select(p => p.UnitId).Distinct().ToArray();
        var balances = await db.DuesCharges.AsNoTracking().Where(c => c.PropertyId == d.PropertyId && unitIds.Contains(c.UnitId) && !c.IsCancelled)
            .GroupBy(c => c.UnitId).Select(g => new { g.Key, Balance = g.Sum(c => c.Amount - c.PaidAmount) }).ToDictionaryAsync(x => x.Key, x => x.Balance, token);
        var fallback = await PrimaryContactsAsync(d.PropertyId, unitIds, today, token);
        return payments.Select(p => {
            var people = allocations.Where(a => a.PaymentId == p.Id).SelectMany(a => chargeRecipients[a.ChargeId]).DistinctBy(x => x.Id).ToList();
            if (people.Count == 0 && fallback.TryGetValue(p.UnitId, out var primary)) people.Add(AutomationRecipient.From(primary));
            var values = new Dictionary<string, string> {
                ["unitNumber"] = units[p.UnitId], ["paidAmount"] = Money(p.Amount), ["paidDate"] = p.PaidDate.ToString("dd.MM.yyyy", Turkish),
                ["unitBalance"] = Money(balances.GetValueOrDefault(p.UnitId))
            };
            return new AutomationCandidate(p.UnitId, units[p.UnitId], null, p.Id, null, null, null, p.Amount, values, people);
        }).ToArray();
    }

    private async Task<Dictionary<Guid, string>> Units(Guid property, IEnumerable<Guid> ids, CancellationToken token)
    {
        var values = ids.Distinct().ToArray();
        return await db.Units.AsNoTracking().Where(u => u.PropertyId == property && values.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Number, token);
    }

    // Debtor snapshot first (06.1); legacy or unit-level debt falls back to the unit's current primary contact. Either way the person
    // must still hold a current relationship to that unit, so a tenant who moved out stops receiving reminders.
    private async Task<Dictionary<Guid, IReadOnlyList<AutomationRecipient>>> RecipientsAsync(Guid property, (Guid ChargeId, Guid UnitId)[] charges, DateOnly today, CancellationToken token)
    {
        var chargeIds = charges.Select(c => c.ChargeId).Distinct().ToArray();
        var unitIds = charges.Select(c => c.UnitId).Distinct().ToArray();
        var debtors = await db.DuesChargeParties.AsNoTracking().Where(p => p.PropertyId == property && chargeIds.Contains(p.ChargeId) && p.IsDebtor && Debtors.Contains(p.Type))
            .Select(p => new { p.ChargeId, p.PersonId, p.UnitId }).ToListAsync(token);
        var current = await db.UnitOccupancies.AsNoTracking().Where(o => o.PropertyId == property && unitIds.Contains(o.UnitId) && o.IsEnabled &&
                o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
            .Select(o => new { o.UnitId, o.PersonId, o.IsPrimaryContact }).ToListAsync(token);
        var personIds = debtors.Select(d => d.PersonId).Concat(current.Select(c => c.PersonId)).Distinct().ToArray();
        var people = await db.People.AsNoTracking().Where(p => p.PropertyId == property && personIds.Contains(p.Id) && p.IsEnabled).ToDictionaryAsync(p => p.Id, token);
        var result = new Dictionary<Guid, IReadOnlyList<AutomationRecipient>>();
        foreach (var (chargeId, unitId) in charges.DistinctBy(c => c.ChargeId))
        {
            var debtor = debtors.FirstOrDefault(d => d.ChargeId == chargeId);
            var personId = debtor?.PersonId ?? current.Where(c => c.UnitId == unitId && c.IsPrimaryContact).Select(c => (Guid?)c.PersonId).FirstOrDefault();
            var linked = personId is { } id && current.Any(c => c.UnitId == unitId && c.PersonId == id) && people.TryGetValue(id, out var person) ? new[] { AutomationRecipient.From(person) } : Array.Empty<AutomationRecipient>();
            result[chargeId] = linked;
        }
        return result;
    }

    private async Task<Dictionary<Guid, Person>> PrimaryContactsAsync(Guid property, Guid[] units, DateOnly today, CancellationToken token)
    {
        var rows = await db.UnitOccupancies.AsNoTracking().Where(o => o.PropertyId == property && units.Contains(o.UnitId) && o.IsEnabled && o.IsPrimaryContact &&
            o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)).Select(o => new { o.UnitId, o.PersonId }).ToListAsync(token);
        var ids = rows.Select(r => r.PersonId).Distinct().ToArray();
        var people = await db.People.AsNoTracking().Where(p => p.PropertyId == property && ids.Contains(p.Id) && p.IsEnabled).ToDictionaryAsync(p => p.Id, token);
        return rows.Where(r => people.ContainsKey(r.PersonId)).DistinctBy(r => r.UnitId).ToDictionary(r => r.UnitId, r => people[r.PersonId]);
    }

    private NotificationDispatch New(AutomationDefinition d, AutomationExecution e, AutomationCandidate c, Guid? person, NotificationChannel channel, string key)
    {
        var row = new NotificationDispatch(e, d.Type, key, channel, c.UnitId).About(c.ChargeId, c.PaymentId, person, c.Offset, c.Period, c.Balance);
        if (c.SourceCode is { } code && c.SourceId is { } id) row.FromSource(code, id);
        db.Add(row); return row;
    }

    private async Task<bool> SendAsync(AutomationDefinition d, AutomationExecution e, NotificationDispatch row, AutomationRecipient person, Dictionary<string, string> values, CancellationToken token)
    {
        var (title, body) = Render(d, values);
        if (d.Type == AutomationType.CUSTOM) values["message"] = body;
        switch (row.Channel)
        {
            case NotificationChannel.IN_APP:
                if (person.UserId is not { } user || !await db.PropertyMemberships.AnyAsync(m => m.PropertyId == d.PropertyId && m.UserId == user && m.IsEnabled, token))
                { row.Skip("NO_APP_ACCOUNT"); return false; }
                var message = new PersonMessage(d.PropertyId, person.PersonId, row.Id, row.DedupKey, user, title, body);
                db.PersonMessages.Add(message); row.Delivered(message.Id, title, "Uygulama hesabı"); return true;
            case NotificationChannel.SMS:
                var phone = SmsText.NormalizePhone(person.PhoneNumber);
                if (phone is null) { row.Skip(string.IsNullOrWhiteSpace(person.PhoneNumber) ? "NO_PHONE" : SmsQueue.PhoneInvalid); return false; }
                var smsType = d.Type == AutomationType.CUSTOM ? SmsTypes.AutomationNotice : AutomationCatalog.Get(d.Type).SmsTypeCode;
                var result = await sms.EnqueueAsync(d.PropertyId, smsType, phone, values, person.PersonId, row.Id, token);
                if (result.MessageId is { } smsId && result.Status == SmsQueue.Queued) { row.QueuedSms(smsId, SmsText.MaskPhone(phone)); return true; }
                if (result.MessageId is { } failedId) row.Failed(result.ErrorCode ?? SmsQueue.Failed, failedId, SmsText.MaskPhone(phone));
                else row.Skip(result.ErrorCode ?? SmsQueue.Skipped, SmsText.MaskPhone(phone));
                return false;
            default:
                if (!channels.EmailEnabled) { row.Skip("CHANNEL_UNAVAILABLE"); return false; }
                if (string.IsNullOrWhiteSpace(person.Email)) { row.Skip("NO_EMAIL"); return false; }
                var command = new EmailRequested(Guid.NewGuid(), d.PropertyId, e.CorrelationId, clock.GetUtcNow().ToUniversalTime(), "tr", person.Email.Trim(), title, body);
                try { outbox.Enqueue(command); }
                catch (InvalidMessageException) { row.Skip("EMAIL_INVALID"); return false; }
                row.QueuedEmail(command.MessageId, title, MaskEmail(person.Email.Trim())); return true;
        }
    }

    private static string Key(AutomationDefinition d, AutomationCandidate c, Guid? person, NotificationChannel channel) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{d.PropertyId}|{d.Type}|{c.ChargeId}|{c.PaymentId}|{c.Offset}|{channel}|{person}" +
            (d.Type == AutomationType.CUSTOM ? $"|{d.CustomTypeId}|{c.SourceCode}|{c.SourceId}|{c.EventDate:yyyy-MM-dd}" : "")))).ToLowerInvariant();
    public static string Money(decimal value) => value.ToString("N2", Turkish) + " TL";
    private static string Clip(string value, int max) => value.Length <= max ? value : value[..max];
    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" + email[Math.Max(at, 0)..] : email[0] + new string('*', Math.Min(at - 1, 6)) + email[at..];
    }
}

// A reminder SMS still waiting in the queue is withdrawn when its debt was settled, cancelled or the rule was paused meanwhile.
public sealed class AutomationSmsGuard(BuildingFlowDbContext db) : ISmsSendGuard
{
    public async Task<string?> BlockReasonAsync(Guid smsMessageId, CancellationToken token)
    {
        var dispatch = await db.Set<NotificationDispatch>().SingleOrDefaultAsync(d => d.SmsMessageId == smsMessageId && d.Status == NotificationDispatchStatus.QUEUED, token);
        if (dispatch is null) return null;
        var paused = await db.Set<AutomationDefinition>().AnyAsync(d => d.Id == dispatch.DefinitionId && d.Status != AutomationStatus.ACTIVE, token);
        string? reason = paused ? "AUTOMATION_PAUSED" : null;
        if (reason is null && dispatch.ChargeId is { } chargeId)
        {
            var charge = await db.DuesCharges.AsNoTracking().SingleOrDefaultAsync(c => c.Id == chargeId, token);
            reason = charge is null || charge.IsCancelled ? "CHARGE_CANCELLED" : charge.Balance == 0 ? "CHARGE_PAID" : null;
        }
        if (reason is not null) dispatch.Cancel(reason);
        return reason;
    }
}
