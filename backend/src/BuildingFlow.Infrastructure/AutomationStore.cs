using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Infrastructure;

public sealed class AutomationStore(BuildingFlowDbContext db, AutomationEngine engine, AutomationChannelOptions channels, TimeProvider clock,
    ILogger<AutomationStore> logger) : IAutomationStore, IAutomationScheduler
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    private DbSet<AutomationDefinition> Definitions => db.Set<AutomationDefinition>();
    private DbSet<AutomationExecution> Executions => db.Set<AutomationExecution>();
    private DbSet<NotificationDispatch> Dispatches => db.Set<NotificationDispatch>();

    private IQueryable<Property> Properties(AccessScope a) => db.Properties.Where(p => !p.IsDeleted &&
        (a.ReadAll || db.PropertyMemberships.Any(m => m.IsEnabled && m.PropertyId == p.Id && m.UserId == a.UserId)));
    private Task<Property?> Visible(Guid property, AccessScope a, CancellationToken token) => Properties(a).AsNoTracking().SingleOrDefaultAsync(p => p.Id == property, token);

    public async Task<AutomationOverview?> OverviewAsync(Guid property, AccessScope access, CancellationToken token)
    {
        if (await Visible(property, access, token) is null) return null;
        var now = clock.GetUtcNow(); var local = TimeZoneInfo.ConvertTime(now, Zone); var today = DateOnly.FromDateTime(local.DateTime);
        var dayStart = new DateTimeOffset(local.Date, local.Offset).ToUniversalTime();
        var definitions = await Definitions.AsNoTracking().Where(d => d.PropertyId == property).OrderBy(d => d.Type).ToListAsync(token);
        var responses = new List<AutomationResponse>();
        foreach (var d in definitions) responses.Add(await ResponseAsync(d, token));
        var counts = await Dispatches.AsNoTracking().Where(d => d.PropertyId == property && d.CreatedAt >= dayStart)
            .GroupBy(d => new { d.Channel, d.Status }).Select(g => new { g.Key.Channel, g.Key.Status, Count = g.Count() }).ToListAsync(token);
        var byChannel = Enum.GetValues<NotificationChannel>().Select(ch => new AutomationChannelCount(ch.ToString(),
            Count(counts.Where(c => c.Channel == ch).Select(c => (c.Status, c.Count)), NotificationDispatchStatus.DELIVERED),
            Count(counts.Where(c => c.Channel == ch).Select(c => (c.Status, c.Count)), NotificationDispatchStatus.QUEUED),
            Count(counts.Where(c => c.Channel == ch).Select(c => (c.Status, c.Count)), NotificationDispatchStatus.SKIPPED),
            Count(counts.Where(c => c.Channel == ch).Select(c => (c.Status, c.Count)), NotificationDispatchStatus.FAILED),
            Count(counts.Where(c => c.Channel == ch).Select(c => (c.Status, c.Count)), NotificationDispatchStatus.CANCELLED))).ToArray();
        var week = now.AddDays(-7);
        var health = new AutomationHealth(
            await db.OutboxMessages.CountAsync(m => m.PropertyId == property && m.Status == OutboxStatus.PENDING, token),
            await db.OutboxMessages.CountAsync(m => m.PropertyId == property && m.Status == OutboxStatus.PENDING && m.Attempts > 0, token),
            await db.SmsMessages.CountAsync(m => m.PropertyId == property && m.Status == SmsMessageStatus.QUEUED, token),
            await db.SmsMessages.CountAsync(m => m.PropertyId == property && m.Status == SmsMessageStatus.FAILED && m.CreatedAt >= dayStart, token),
            await Executions.CountAsync(e => e.PropertyId == property && e.Status == AutomationExecutionStatus.FAILED && e.StartedAt >= week, token),
            await Executions.CountAsync(e => e.PropertyId == property && e.Status == AutomationExecutionStatus.DEAD_LETTER && e.StartedAt >= week, token),
            await Executions.Where(e => e.PropertyId == property).MaxAsync(e => (DateTimeOffset?)e.FinishedAt, token),
            channels.EmailEnabled,
            await db.SmsAccounts.AnyAsync(a => a.PropertyId == property && a.IsEnabled, token), false);
        return new(property, today, AutomationCatalog.All, responses, definitions.Count(d => d.Status == AutomationStatus.ACTIVE),
            definitions.Count(d => d.Status == AutomationStatus.PAUSED), await Executions.CountAsync(e => e.PropertyId == property && e.StartedAt >= dayStart, token), byChannel, health);
    }
    private static int Count(IEnumerable<(NotificationDispatchStatus Status, int Count)> rows, NotificationDispatchStatus status) => rows.Where(r => r.Status == status).Sum(r => r.Count);

    public async Task<AutomationResponse?> CreateAsync(Guid property, AutomationType type, AutomationSettings settings, string reason, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (!await LockAsync(property, access, token)) return null;
        if (await Definitions.IgnoreQueryFilters().AnyAsync(d => d.PropertyId == property && d.Type == type && !d.IsDeleted, token)) throw Conflict();
        var definition = new AutomationDefinition(property, type, settings);
        Definitions.Add(definition); Revision(definition, "CREATED", reason);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return await ResponseAsync(definition, token);
    }

    public async Task<AutomationResponse?> UpdateAsync(Guid property, Guid id, AutomationSettings settings, string reason, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (!await LockAsync(property, access, token)) return null;
        var definition = await Definitions.SingleOrDefaultAsync(d => d.Id == id && d.PropertyId == property, token);
        if (definition is null) return null;
        if (definition.Version != version) throw Conflict();
        try { definition.Update(settings); } catch (ArgumentException) { throw Invalid(); }
        Revision(definition, "UPDATED", reason);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return await ResponseAsync(definition, token);
    }

    public async Task<AutomationResponse?> SetActiveAsync(Guid property, Guid id, bool active, string reason, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (!await LockAsync(property, access, token)) return null;
        var definition = await Definitions.SingleOrDefaultAsync(d => d.Id == id && d.PropertyId == property, token);
        if (definition is null) return null;
        if (definition.Version != version || (definition.Status == AutomationStatus.ACTIVE) == active) throw Conflict();
        if (active) definition.Activate(clock.GetUtcNow()); else definition.Pause();
        Revision(definition, active ? "ACTIVATED" : "PAUSED", reason);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return await ResponseAsync(definition, token);
    }

    public async Task<AutomationExecutionResponse?> RunAsync(Guid property, Guid id, AccessScope access, CancellationToken token)
    {
        if (await Visible(property, access, token) is not { IsEnabled: true }) return null;
        var definition = await Definitions.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id && d.PropertyId == property, token);
        if (definition is null) return null;
        if (definition.Status != AutomationStatus.ACTIVE) throw Conflict();
        var (today, hour) = LocalNow();
        var quiet = definition.IsQuiet(hour);
        if (quiet && definition.Type != AutomationType.PAYMENT_RECEIVED && (definition.SmsEnabled || definition.EmailEnabled))
            throw new HierarchyConflictException(ErrorCatalog.AutomationQuietHours.Create());
        var execution = await ProcessAsync(id, today, AutomationTrigger.MANUAL, quiet, token);
        return execution is null ? null : Response(execution, definition.Type);
    }

    public async Task<AutomationPreview?> PreviewAsync(Guid property, Guid id, DateOnly? date, AccessScope access, CancellationToken token)
    {
        if (await Visible(property, access, token) is not { } parent) return null;
        var definition = await Definitions.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id && d.PropertyId == property, token);
        if (definition is null) return null;
        var day = date ?? LocalNow().Today;
        var candidates = await engine.CandidatesAsync(definition, day, token);
        var channels = AutomationEngine.Channels(definition, false).Select(c => c.ToString()).ToArray();
        var items = candidates.Take(100).Select(c => {
            var (title, body) = AutomationEngine.Render(definition, new Dictionary<string, string>(c.Values) { ["propertyName"] = parent.Name });
            return new AutomationPreviewItem(c.UnitNumber, c.Period, c.Balance, c.DueDate, c.Offset, c.Recipients.Count, channels, title, body);
        }).ToArray();
        return new(id, day, candidates.Count, candidates.Count > items.Length, items);
    }

    public async Task<IReadOnlyList<AutomationRevisionResponse>?> RevisionsAsync(Guid property, Guid id, AccessScope access, CancellationToken token)
    {
        if (await Visible(property, access, token) is null || !await Definitions.AnyAsync(d => d.Id == id && d.PropertyId == property, token)) return null;
        return await db.Set<AutomationRevision>().AsNoTracking().Where(r => r.DefinitionId == id && r.PropertyId == property).OrderByDescending(r => r.RuleVersion)
            .Take(100).Select(r => new AutomationRevisionResponse(r.Id, r.RuleVersion, r.Action, r.Reason, r.Snapshot, r.CreatedAt, r.CreatedBy)).ToListAsync(token);
    }

    public async Task<PagedRows<AutomationExecutionResponse>?> ExecutionsAsync(Guid property, int page, int pageSize, Guid? definition, AccessScope access, CancellationToken token)
    {
        if (await Visible(property, access, token) is null) return null;
        var query = Executions.AsNoTracking().Where(e => e.PropertyId == property && (definition == null || e.DefinitionId == definition));
        var types = await Definitions.AsNoTracking().Where(d => d.PropertyId == property).ToDictionaryAsync(d => d.Id, d => d.Type, token);
        var rows = await query.OrderByDescending(e => e.StartedAt).ThenBy(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(e => Response(e, types[e.DefinitionId])).ToArray(), page, pageSize, await query.CountAsync(token));
    }

    public async Task<PagedRows<NotificationDispatchResponse>?> DispatchesAsync(Guid property, AutomationDispatchFilter f, AccessScope access, CancellationToken token)
    {
        if (await Visible(property, access, token) is null) return null;
        var channel = f.Channel is null ? (NotificationChannel?)null : Enum.Parse<NotificationChannel>(f.Channel);
        var status = f.Status is null ? (NotificationDispatchStatus?)null : Enum.Parse<NotificationDispatchStatus>(f.Status);
        var query = Dispatches.AsNoTracking().Where(d => d.PropertyId == property && (f.DefinitionId == null || d.DefinitionId == f.DefinitionId) &&
            (channel == null || d.Channel == channel) && (status == null || d.Status == status));
        var rows = await query.OrderByDescending(d => d.CreatedAt).ThenBy(d => d.Id).Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)
            .Select(d => new {
                Row = d, Unit = db.Units.Where(u => u.Id == d.UnitId).Select(u => u.Number).FirstOrDefault(),
                Sms = db.SmsMessages.Where(m => m.Id == d.SmsMessageId).Select(m => new { m.Status, m.Attempts, m.ErrorCode }).FirstOrDefault()
            }).ToListAsync(token);
        var items = rows.Select(x => new NotificationDispatchResponse(x.Row.Id, x.Row.DefinitionId, x.Row.Type.ToString(), x.Row.Channel.ToString(), x.Row.Status.ToString(),
            x.Row.ReasonCode, x.Unit, x.Row.Period, x.Row.OffsetDays, x.Row.Amount, x.Row.RecipientMasked, x.Row.Title, x.Row.CreatedAt, x.Row.CorrelationId,
            x.Row.RuleVersion, x.Sms?.Status.ToString(), x.Sms?.Attempts, x.Sms?.ErrorCode)).ToArray();
        return new(items, f.Page, f.PageSize, await query.CountAsync(token));
    }

    public async Task<int> RunDueAsync(CancellationToken token)
    {
        var (today, hour) = LocalNow(); var now = clock.GetUtcNow(); var total = 0;
        var definitions = await Definitions.AsNoTracking().Where(d => d.Status == AutomationStatus.ACTIVE && d.IsEnabled &&
            db.Properties.Any(p => p.Id == d.PropertyId && p.IsEnabled)).OrderBy(d => d.PropertyId).ThenBy(d => d.Type).ToListAsync(token);
        foreach (var definition in definitions)
        {
            if (!definition.CoversDate(today)) continue;
            var quiet = definition.IsQuiet(hour);
            var trigger = definition.IsScheduled ? AutomationTrigger.SCHEDULE : AutomationTrigger.EVENT;
            if (definition.IsScheduled && (hour < definition.SendHour || quiet)) continue;
            var blocker = await Executions.AsNoTracking().Where(e => e.DefinitionId == definition.Id && e.BusinessDate == today && e.Trigger == trigger)
                .OrderByDescending(e => e.StartedAt).FirstOrDefaultAsync(token);
            if (blocker is not null && (blocker.Status == AutomationExecutionStatus.DEAD_LETTER || blocker.Status == AutomationExecutionStatus.FAILED && blocker.NextAttemptAt > now ||
                definition.IsScheduled && blocker.Status == AutomationExecutionStatus.SUCCEEDED)) continue;
            try { total += (await ProcessAsync(definition.Id, today, trigger, quiet, token))?.Dispatched ?? 0; }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception error) { await FailAsync(definition.Id, today, trigger, error, token); }
        }
        return total;
    }

    // One Property-locked transaction per run: dispatch rows, in-app messages, SMS queue rows and e-mail outbox rows commit together or not at all.
    private async Task<AutomationExecution?> ProcessAsync(Guid id, DateOnly today, AutomationTrigger trigger, bool quiet, CancellationToken token)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var propertyId = await Definitions.AsNoTracking().Where(d => d.Id == id).Select(d => d.PropertyId).SingleAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {propertyId} FOR UPDATE", token);
        var definition = await Definitions.SingleAsync(d => d.Id == id, token);
        if (definition.Status != AutomationStatus.ACTIVE) return null;
        var now = clock.GetUtcNow();
        AutomationExecution? execution = null;
        if (trigger != AutomationTrigger.MANUAL)
        {
            execution = await Executions.Where(e => e.DefinitionId == id && e.BusinessDate == today && e.Trigger == trigger)
                .OrderByDescending(e => e.StartedAt).FirstOrDefaultAsync(token);
            if (execution is { Status: AutomationExecutionStatus.DEAD_LETTER } || trigger == AutomationTrigger.SCHEDULE && execution is { Status: AutomationExecutionStatus.SUCCEEDED }) return null;
            if (trigger == AutomationTrigger.EVENT && execution is { Status: AutomationExecutionStatus.SUCCEEDED }) execution = null;
        }
        var fresh = execution is null;
        if (execution is null) { execution = new AutomationExecution(definition, today, trigger, Guid.NewGuid(), now); Executions.Add(execution); }
        else execution.Restart(definition.RuleVersion, now);
        var (evaluated, dispatched, skipped) = await engine.DispatchAsync(definition, execution, today, quiet, token);
        if (trigger == AutomationTrigger.EVENT && fresh && dispatched + skipped == 0) return execution;
        execution.Succeed(evaluated, dispatched, skipped, clock.GetUtcNow());
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        if (dispatched + skipped > 0) logger.LogInformation("BuildingFlow.Automation {Type} run {CorrelationId} dispatched {Dispatched}, skipped {Skipped}.",
            definition.Type, execution.CorrelationId, dispatched, skipped);
        return execution;
    }

    private async Task FailAsync(Guid id, DateOnly today, AutomationTrigger trigger, Exception error, CancellationToken token)
    {
        db.ChangeTracker.Clear();
        logger.LogWarning("BuildingFlow.Automation run failed: {ErrorType}.", error.GetType().Name);
        var definition = await Definitions.AsNoTracking().SingleAsync(d => d.Id == id, token);
        var execution = await Executions.Where(e => e.DefinitionId == id && e.BusinessDate == today && e.Trigger == trigger && e.Status != AutomationExecutionStatus.SUCCEEDED)
            .OrderByDescending(e => e.StartedAt).FirstOrDefaultAsync(token);
        if (execution is null) { execution = new AutomationExecution(definition, today, trigger, Guid.NewGuid(), clock.GetUtcNow()); Executions.Add(execution); }
        var code = error is DbUpdateException ? "DATABASE_ERROR" : error.GetType().Name.Length > 50 ? "UNEXPECTED_ERROR" : error.GetType().Name;
        execution.Fail(code, clock.GetUtcNow(), OutboxRelay.Backoff(execution.Attempts + 5));
        await db.SaveChangesAsync(token);
    }

    private async Task<bool> LockAsync(Guid property, AccessScope access, CancellationToken token)
    {
        var parent = await Visible(property, access, token);
        if (parent is null) return false;
        if (!parent.IsEnabled) throw Conflict();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        return true;
    }

    private void Revision(AutomationDefinition d, string action, string reason) => db.Add(new AutomationRevision(d, action, reason, JsonSerializer.Serialize(new {
        d.Name, Status = d.Status.ToString(), d.StartsOn, d.EndsOn, d.SendHour, d.QuietStartHour, d.QuietEndHour, d.OffsetDays,
        InApp = d.InAppEnabled, Sms = d.SmsEnabled, Email = d.EmailEnabled, d.TitleTemplate, d.BodyTemplate })));

    private (DateOnly Today, int Hour) LocalNow()
    {
        var local = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone);
        return (DateOnly.FromDateTime(local.DateTime), local.Hour);
    }

    private DateTimeOffset? NextRun(AutomationDefinition d, AutomationExecution? last)
    {
        if (d.Status != AutomationStatus.ACTIVE || !d.IsScheduled) return null;
        var (today, hour) = LocalNow();
        var doneToday = last is { Trigger: AutomationTrigger.SCHEDULE, Status: AutomationExecutionStatus.SUCCEEDED or AutomationExecutionStatus.DEAD_LETTER } && last.BusinessDate == today;
        if (last is { Trigger: AutomationTrigger.SCHEDULE, Status: AutomationExecutionStatus.FAILED, NextAttemptAt: { } retry } && last.BusinessDate == today) return retry;
        var day = doneToday || hour >= d.SendHour && d.IsQuiet(hour) ? today.AddDays(1) : today;
        for (var i = 0; i < 400 && !d.CoversDate(day); i++) { if (d.EndsOn is { } end && day > end) return null; day = day.AddDays(1); }
        var at = day.ToDateTime(new TimeOnly(d.SendHour, 0));
        return new DateTimeOffset(at, Zone.GetUtcOffset(at)).ToUniversalTime();
    }

    private async Task<AutomationResponse> ResponseAsync(AutomationDefinition d, CancellationToken token)
    {
        var last = await Executions.AsNoTracking().Where(e => e.DefinitionId == d.Id).OrderByDescending(e => e.StartedAt).FirstOrDefaultAsync(token);
        var lastSchedule = await Executions.AsNoTracking().Where(e => e.DefinitionId == d.Id && e.Trigger == AutomationTrigger.SCHEDULE).OrderByDescending(e => e.StartedAt).FirstOrDefaultAsync(token);
        return new(d.Id, d.PropertyId, d.Type.ToString(), d.Status.ToString(), d.Name, d.StartsOn, d.EndsOn, d.SendHour, d.QuietStartHour, d.QuietEndHour, d.OffsetDays,
            d.InAppEnabled, d.SmsEnabled, d.EmailEnabled, d.TitleTemplate, d.BodyTemplate, d.RuleVersion, d.ActivatedAt, NextRun(d, lastSchedule),
            last is null ? null : Response(last, d.Type), d.Version);
    }

    private static AutomationExecutionResponse Response(AutomationExecution e, AutomationType type) => new(e.Id, e.DefinitionId, type.ToString(), e.BusinessDate,
        e.Trigger.ToString(), e.Status.ToString(), e.Attempts, e.StartedAt, e.FinishedAt, e.NextAttemptAt, e.Evaluated, e.Dispatched, e.Skipped, e.ErrorCode, e.RuleVersion, e.CorrelationId);
    private static HierarchyConflictException Conflict() => new(ErrorCatalog.AutomationConflict.Create());
    private static RequestValidationException Invalid() => new(new() { ["settings"] = [ErrorCatalog.AutomationInvalid.Create()] });
}
