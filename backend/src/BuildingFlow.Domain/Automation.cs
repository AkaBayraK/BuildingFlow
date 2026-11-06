namespace BuildingFlow.Domain;

public enum AutomationType { DUES_DUE_REMINDER, DUES_OVERDUE_REMINDER, PAYMENT_RECEIVED, CUSTOM }
public enum AutomationTypeStatus { DRAFT, PUBLISHED, ARCHIVED }
public enum AutomationTriggerKind { DATE_OFFSET, EVENT, CALENDAR }
public enum AutomationStatus { ACTIVE, PAUSED }
public enum AutomationTrigger { SCHEDULE, EVENT, MANUAL }
public enum AutomationExecutionStatus { SUCCEEDED, FAILED, DEAD_LETTER }
public enum NotificationChannel { IN_APP, SMS, EMAIL }
public enum NotificationDispatchStatus { DELIVERED, QUEUED, SKIPPED, FAILED, CANCELLED }

public sealed record AutomationSettings(string Name, DateOnly? StartsOn, DateOnly? EndsOn, int SendHour, int QuietStartHour, int QuietEndHour,
    IReadOnlyList<int> OffsetDays, bool InApp, bool Sms, bool Email, string TitleTemplate, string BodyTemplate);

public sealed class AutomationDefinition : BaseEntity
{
    public const int MaxOffsets = 10, MaxOffset = 60, MaxTitle = 200, MaxBody = 2000;
    private AutomationDefinition() { }
    public AutomationDefinition(Guid propertyId, AutomationType type, AutomationSettings settings, AutomationTypeDefinition? custom = null)
    {
        if (propertyId == Guid.Empty || !Enum.IsDefined(type) || (type == AutomationType.CUSTOM) != (custom is not null)) throw new ArgumentException("Invalid automation identity.");
        if (custom is not null && (custom.PropertyId is { } owner && owner != propertyId || custom.Status != AutomationTypeStatus.PUBLISHED)) throw new ArgumentException("Automation type is not available.");
        PropertyId = propertyId; Type = type; Status = AutomationStatus.PAUSED; RuleVersion = 1; CustomTypeId = custom?.Id; CustomTrigger = custom?.TriggerKind;
        Apply(settings);
    }
    public Guid? CustomTypeId { get; private set; }
    public AutomationTriggerKind? CustomTrigger { get; private set; }
    public Guid PropertyId { get; private set; }
    public AutomationType Type { get; private set; }
    public AutomationStatus Status { get; private set; }
    public string Name { get; private set; } = null!;
    public DateOnly? StartsOn { get; private set; }
    public DateOnly? EndsOn { get; private set; }
    public int SendHour { get; private set; }
    public int QuietStartHour { get; private set; }
    public int QuietEndHour { get; private set; }
    public int[] OffsetDays { get; private set; } = [];
    public bool InAppEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }
    public string TitleTemplate { get; private set; } = null!;
    public string BodyTemplate { get; private set; } = null!;
    public int RuleVersion { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }

    public bool IsScheduled => Type == AutomationType.CUSTOM ? CustomTrigger != AutomationTriggerKind.EVENT : Type != AutomationType.PAYMENT_RECEIVED;
    public bool IsEventDriven => !IsScheduled;
    public bool IsQuiet(int hour) => InQuietWindow(QuietStartHour, QuietEndHour, hour);
    public static bool InQuietWindow(int start, int end, int hour) => start != end && (start < end ? hour >= start && hour < end : hour >= start || hour < end);
    public bool CoversDate(DateOnly day) => (StartsOn is null || StartsOn <= day) && (EndsOn is null || EndsOn >= day);

    public void Update(AutomationSettings settings) { Apply(settings); RuleVersion++; }
    public void Activate(DateTimeOffset at)
    {
        if (Status == AutomationStatus.ACTIVE) throw new InvalidOperationException("Automation is already active.");
        Status = AutomationStatus.ACTIVE; ActivatedAt = at; RuleVersion++;
    }
    public void Pause()
    {
        if (Status == AutomationStatus.PAUSED) throw new InvalidOperationException("Automation is already paused.");
        Status = AutomationStatus.PAUSED; RuleVersion++;
    }

    private void Apply(AutomationSettings s)
    {
        if (s.SendHour is < 0 or > 23 || s.QuietStartHour is < 0 or > 23 || s.QuietEndHour is < 0 or > 23) throw new ArgumentOutOfRangeException(nameof(s));
        if (s.StartsOn is { } start && s.EndsOn is { } end && end < start) throw new ArgumentException("End date precedes start date.");
        if (!s.InApp && !s.Sms && !s.Email) throw new ArgumentException("Select at least one channel.");
        var offsets = s.OffsetDays.Distinct().Order().ToArray();
        if (offsets.Length != s.OffsetDays.Count || offsets.Any(o => o is < -MaxOffset or > MaxOffset)) throw new ArgumentException("Invalid offsets.");
        var needsOffsets = Type == AutomationType.CUSTOM ? CustomTrigger == AutomationTriggerKind.DATE_OFFSET : Type != AutomationType.PAYMENT_RECEIVED;
        if (needsOffsets ? offsets.Length is < 1 or > MaxOffsets : offsets.Length != 0) throw new ArgumentException("Invalid offsets.");
        if (Type == AutomationType.DUES_DUE_REMINDER && offsets.Any(o => o > 0) || Type == AutomationType.DUES_OVERDUE_REMINDER && offsets.Any(o => o < 1))
            throw new ArgumentException("Offsets do not match the automation type.");
        Name = Required(s.Name, nameof(s.Name), 100); StartsOn = s.StartsOn; EndsOn = s.EndsOn;
        SendHour = s.SendHour; QuietStartHour = s.QuietStartHour; QuietEndHour = s.QuietEndHour;
        if (IsScheduled && IsQuiet(SendHour)) throw new ArgumentException("Send hour falls into quiet hours.");
        OffsetDays = offsets; InAppEnabled = s.InApp; SmsEnabled = s.Sms; EmailEnabled = s.Email;
        TitleTemplate = Required(s.TitleTemplate, nameof(s.TitleTemplate), MaxTitle); BodyTemplate = Required(s.BodyTemplate, nameof(s.BodyTemplate), MaxBody);
    }
}

// Immutable history of every rule change; the snapshot is the serialized settings at that version.
public sealed class AutomationRevision : BaseEntity
{
    private AutomationRevision() { }
    public AutomationRevision(AutomationDefinition definition, string action, string reason, string snapshot)
    {
        DefinitionId = definition.Id; PropertyId = definition.PropertyId; RuleVersion = definition.RuleVersion;
        Action = Required(action, nameof(action), 20); Reason = Required(reason, nameof(reason), 500); Snapshot = Required(snapshot, nameof(snapshot), 8000);
    }
    public Guid DefinitionId { get; private set; }
    public Guid PropertyId { get; private set; }
    public int RuleVersion { get; private set; }
    public string Action { get; private set; } = null!;
    public string Reason { get; private set; } = null!;
    public string Snapshot { get; private set; } = null!;
}

public sealed class AutomationExecution : BaseEntity
{
    public const int MaxAttempts = 5;
    private AutomationExecution() { }
    public AutomationExecution(AutomationDefinition definition, DateOnly businessDate, AutomationTrigger trigger, Guid correlationId, DateTimeOffset startedAt)
    {
        if (correlationId == Guid.Empty || !Enum.IsDefined(trigger)) throw new ArgumentException("Invalid execution identity.");
        DefinitionId = definition.Id; PropertyId = definition.PropertyId; RuleVersion = definition.RuleVersion; BusinessDate = businessDate;
        Trigger = trigger; CorrelationId = correlationId; StartedAt = startedAt; Status = AutomationExecutionStatus.SUCCEEDED;
    }
    public Guid DefinitionId { get; private set; }
    public Guid PropertyId { get; private set; }
    public int RuleVersion { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public AutomationTrigger Trigger { get; private set; }
    public Guid CorrelationId { get; private set; }
    public AutomationExecutionStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public int Evaluated { get; private set; }
    public int Dispatched { get; private set; }
    public int Skipped { get; private set; }
    public string? ErrorCode { get; private set; }

    public void Succeed(int evaluated, int dispatched, int skipped, DateTimeOffset at)
    {
        Attempts++; Status = AutomationExecutionStatus.SUCCEEDED; Evaluated = evaluated; Dispatched = dispatched; Skipped = skipped;
        FinishedAt = at; NextAttemptAt = null; ErrorCode = null;
    }
    public void Fail(string errorCode, DateTimeOffset at, TimeSpan backoff)
    {
        Attempts++; ErrorCode = Required(errorCode, nameof(errorCode), 50); FinishedAt = at;
        Status = Attempts >= MaxAttempts ? AutomationExecutionStatus.DEAD_LETTER : AutomationExecutionStatus.FAILED;
        NextAttemptAt = Status == AutomationExecutionStatus.FAILED ? at.Add(backoff) : null;
    }
    public void Restart(int ruleVersion, DateTimeOffset at) { RuleVersion = ruleVersion; StartedAt = at; }
}

public sealed class NotificationDispatch : BaseEntity
{
    private NotificationDispatch() { }
    public NotificationDispatch(AutomationExecution execution, AutomationType type, string dedupKey, NotificationChannel channel, Guid? unitId)
    {
        if (dedupKey.Length != 64 || dedupKey.Any(c => !Uri.IsHexDigit(c)) || unitId == Guid.Empty || !Enum.IsDefined(channel)) throw new ArgumentException("Invalid dispatch identity.");
        PropertyId = execution.PropertyId; DefinitionId = execution.DefinitionId; ExecutionId = execution.Id; RuleVersion = execution.RuleVersion;
        CorrelationId = execution.CorrelationId; Type = type; DedupKey = dedupKey; Channel = channel; UnitId = unitId;
    }
    public Guid PropertyId { get; private set; }
    public Guid DefinitionId { get; private set; }
    public Guid ExecutionId { get; private set; }
    public int RuleVersion { get; private set; }
    public Guid CorrelationId { get; private set; }
    public AutomationType Type { get; private set; }
    public string DedupKey { get; private set; } = null!;
    public NotificationChannel Channel { get; private set; }
    public NotificationDispatchStatus Status { get; private set; }
    public string? ReasonCode { get; private set; }
    public Guid? UnitId { get; private set; }
    public Guid? ChargeId { get; private set; }
    public string? SourceCode { get; private set; }
    public Guid? SourceId { get; private set; }
    public Guid? PaymentId { get; private set; }
    public Guid? PersonId { get; private set; }
    public int? OffsetDays { get; private set; }
    public DateOnly? Period { get; private set; }
    public decimal? Amount { get; private set; }
    public string? RecipientMasked { get; private set; }
    public string? Title { get; private set; }
    public Guid? PersonMessageId { get; private set; }
    public Guid? SmsMessageId { get; private set; }
    public Guid? OutboxMessageId { get; private set; }

    public NotificationDispatch About(Guid? chargeId, Guid? paymentId, Guid? personId, int? offset, DateOnly? period, decimal? amount)
    {
        ChargeId = chargeId; PaymentId = paymentId; PersonId = personId; OffsetDays = offset; Period = period; Amount = amount; return this;
    }
    public NotificationDispatch FromSource(string code, Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Invalid source.");
        SourceCode = Required(code, nameof(code), 40); SourceId = id; return this;
    }
    public void Delivered(Guid personMessageId, string title, string recipientMasked)
    { Status = NotificationDispatchStatus.DELIVERED; PersonMessageId = personMessageId; Title = Required(title, nameof(title), 200); RecipientMasked = Mask(recipientMasked); }
    public void QueuedSms(Guid smsMessageId, string recipientMasked)
    { Status = NotificationDispatchStatus.QUEUED; SmsMessageId = smsMessageId; RecipientMasked = Mask(recipientMasked); }
    public void QueuedEmail(Guid outboxMessageId, string title, string recipientMasked)
    { Status = NotificationDispatchStatus.QUEUED; OutboxMessageId = outboxMessageId; Title = Required(title, nameof(title), 200); RecipientMasked = Mask(recipientMasked); }
    public void Skip(string reason, string? recipientMasked = null)
    { Status = NotificationDispatchStatus.SKIPPED; ReasonCode = Required(reason, nameof(reason), 50); RecipientMasked = recipientMasked is null ? null : Mask(recipientMasked); }
    public void Failed(string reason, Guid? smsMessageId, string? recipientMasked)
    { Status = NotificationDispatchStatus.FAILED; ReasonCode = Required(reason, nameof(reason), 50); SmsMessageId = smsMessageId; RecipientMasked = recipientMasked is null ? null : Mask(recipientMasked); }
    public void Cancel(string reason)
    {
        if (Status != NotificationDispatchStatus.QUEUED) throw new InvalidOperationException("Only queued dispatches can be cancelled.");
        Status = NotificationDispatchStatus.CANCELLED; ReasonCode = Required(reason, nameof(reason), 50);
    }
    private static string Mask(string value) => Required(value, nameof(value), 100);
}

public sealed record AutomationCondition(string Field, string Operator, string[] Values);
public sealed record AutomationTypeSettings(string Name, string Icon, string SourceCode, AutomationCondition[] Conditions, AutomationTriggerKind Trigger,
    string? DateField, int[] DefaultOffsets, int? DayOfMonth, int? DayOfWeek, string[] RecipientScopes, string DefaultTitle, string DefaultBody);

// A user-assembled automation kind. It only combines code-owned sources, fields, operators and recipient scopes; it never carries a query.
public sealed class AutomationTypeDefinition : BaseEntity
{
    private AutomationTypeDefinition() { }
    public AutomationTypeDefinition(Guid? propertyId, string code, AutomationTypeSettings settings)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Invalid property.");
        PropertyId = propertyId; Code = Required(code, nameof(code), 60); Status = AutomationTypeStatus.DRAFT; TypeVersion = 1; Apply(settings);
    }
    public Guid? PropertyId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Icon { get; private set; } = null!;
    public string SourceCode { get; private set; } = null!;
    public string Conditions { get; private set; } = "[]";
    public AutomationTriggerKind TriggerKind { get; private set; }
    public string? DateField { get; private set; }
    public int[] DefaultOffsets { get; private set; } = [];
    public int? DayOfMonth { get; private set; }
    public int? DayOfWeek { get; private set; }
    public string[] RecipientScopes { get; private set; } = [];
    public string DefaultTitle { get; private set; } = null!;
    public string DefaultBody { get; private set; } = null!;
    public AutomationTypeStatus Status { get; private set; }
    public int TypeVersion { get; private set; }

    public AutomationCondition[] ParsedConditions() => System.Text.Json.JsonSerializer.Deserialize<AutomationCondition[]>(Conditions) ?? [];
    public void Update(AutomationTypeSettings settings)
    {
        if (Status == AutomationTypeStatus.ARCHIVED) throw new InvalidOperationException("Archived types cannot change.");
        if (Status == AutomationTypeStatus.PUBLISHED && (settings.SourceCode != SourceCode || settings.Trigger != TriggerKind))
            throw new InvalidOperationException("A published type keeps its source and trigger.");
        Apply(settings); TypeVersion++;
    }
    public void Publish() { if (Status != AutomationTypeStatus.DRAFT) throw new InvalidOperationException("Only drafts are published."); Status = AutomationTypeStatus.PUBLISHED; TypeVersion++; }
    public void Archive() { if (Status == AutomationTypeStatus.ARCHIVED) throw new InvalidOperationException("Already archived."); Status = AutomationTypeStatus.ARCHIVED; TypeVersion++; }

    private void Apply(AutomationTypeSettings s)
    {
        if (!Enum.IsDefined(s.Trigger)) throw new ArgumentException("Invalid trigger.");
        if (s.Trigger == AutomationTriggerKind.DATE_OFFSET ? string.IsNullOrWhiteSpace(s.DateField) || s.DefaultOffsets.Length is < 1 or > AutomationDefinition.MaxOffsets : s.DefaultOffsets.Length != 0)
            throw new ArgumentException("Invalid offsets.");
        if (s.DefaultOffsets.Any(o => o is < -AutomationDefinition.MaxOffset or > AutomationDefinition.MaxOffset) || s.DefaultOffsets.Distinct().Count() != s.DefaultOffsets.Length) throw new ArgumentException("Invalid offsets.");
        if (s.Trigger == AutomationTriggerKind.CALENDAR ? (s.DayOfMonth is null) == (s.DayOfWeek is null) || s.DayOfMonth is < 1 or > 28 || s.DayOfWeek is < 1 or > 7
            : s.DayOfMonth is not null || s.DayOfWeek is not null) throw new ArgumentException("Invalid calendar.");
        if (s.RecipientScopes.Length is < 1 or > 6 || s.RecipientScopes.Distinct().Count() != s.RecipientScopes.Length) throw new ArgumentException("Invalid recipients.");
        if (s.Conditions.Length > 10) throw new ArgumentException("Too many conditions.");
        Name = Required(s.Name, nameof(s.Name), 100); Icon = Required(s.Icon, nameof(s.Icon), 40); SourceCode = Required(s.SourceCode, nameof(s.SourceCode), 40);
        Conditions = System.Text.Json.JsonSerializer.Serialize(s.Conditions); TriggerKind = s.Trigger;
        DateField = s.Trigger == AutomationTriggerKind.DATE_OFFSET ? Required(s.DateField!, nameof(s.DateField), 40) : null;
        DefaultOffsets = [.. s.DefaultOffsets.Order()]; DayOfMonth = s.DayOfMonth; DayOfWeek = s.DayOfWeek; RecipientScopes = [.. s.RecipientScopes];
        DefaultTitle = Required(s.DefaultTitle, nameof(s.DefaultTitle), AutomationDefinition.MaxTitle); DefaultBody = Required(s.DefaultBody, nameof(s.DefaultBody), AutomationDefinition.MaxBody);
    }
}

public sealed class AutomationTypeRevision : BaseEntity
{
    private AutomationTypeRevision() { }
    public AutomationTypeRevision(AutomationTypeDefinition type, string action, string reason, string snapshot)
    {
        TypeId = type.Id; PropertyId = type.PropertyId; TypeVersion = type.TypeVersion;
        Action = Required(action, nameof(action), 20); Reason = Required(reason, nameof(reason), 500); Snapshot = Required(snapshot, nameof(snapshot), 8000);
    }
    public Guid TypeId { get; private set; }
    public Guid? PropertyId { get; private set; }
    public int TypeVersion { get; private set; }
    public string Action { get; private set; } = null!;
    public string Reason { get; private set; } = null!;
    public string Snapshot { get; private set; } = null!;
}
