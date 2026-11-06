using System.Diagnostics.CodeAnalysis;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record AutomationTypeInfo(string Type, string DefaultName, int[] DefaultOffsets, int MinOffset, int MaxOffset, string[] Variables,
    string DefaultTitle, string DefaultBody, string SmsTypeCode, Guid? CustomTypeId = null, string? Icon = null, string? Trigger = null, string? SourceCode = null);
public sealed record AutomationRequest(string? Type, string? Name, DateOnly? StartsOn, DateOnly? EndsOn, int SendHour, int QuietStartHour, int QuietEndHour,
    int[]? OffsetDays, bool InApp, bool Sms, bool Email, string? TitleTemplate, string? BodyTemplate, string? Reason, long Version = 0, Guid? CustomTypeId = null);
public sealed record AutomationStatusRequest(string? Reason, long Version);
public sealed record AutomationExecutionResponse(Guid Id, Guid DefinitionId, string Type, DateOnly BusinessDate, string Trigger, string Status, int Attempts,
    DateTimeOffset StartedAt, DateTimeOffset? FinishedAt, DateTimeOffset? NextAttemptAt, int Evaluated, int Dispatched, int Skipped, string? ErrorCode,
    int RuleVersion, Guid CorrelationId);
public sealed record AutomationResponse(Guid Id, Guid PropertyId, string Type, string Status, string Name, DateOnly? StartsOn, DateOnly? EndsOn,
    int SendHour, int QuietStartHour, int QuietEndHour, int[] OffsetDays, bool InApp, bool Sms, bool Email, string TitleTemplate, string BodyTemplate,
    int RuleVersion, DateTimeOffset? ActivatedAt, DateTimeOffset? NextRunAt, AutomationExecutionResponse? LastExecution, long Version,
    Guid? CustomTypeId = null, string? CustomTypeName = null, string? Icon = null);
public sealed record AutomationRevisionResponse(Guid Id, int RuleVersion, string Action, string Reason, string Snapshot, DateTimeOffset CreatedAt, string CreatedBy);
public sealed record NotificationDispatchResponse(Guid Id, Guid DefinitionId, string Type, string Channel, string Status, string? ReasonCode, string? UnitNumber,
    DateOnly? Period, int? OffsetDays, decimal? Amount, string? RecipientMasked, string? Title, DateTimeOffset CreatedAt, Guid CorrelationId, int RuleVersion,
    string? SmsStatus, int? SmsAttempts, string? SmsErrorCode);
public sealed record AutomationChannelCount(string Channel, int Delivered, int Queued, int Skipped, int Failed, int Cancelled);
public sealed record AutomationHealth(int OutboxPending, int OutboxRetrying, int SmsQueued, int SmsFailedToday, int ExecutionsFailed, int ExecutionsDeadLetter,
    DateTimeOffset? LastExecutionAt, bool EmailChannelAvailable, bool SmsAccountEnabled, bool PushAvailable);
public sealed record AutomationOverview(Guid PropertyId, DateOnly Today, IReadOnlyList<AutomationTypeInfo> Types, IReadOnlyList<AutomationResponse> Definitions,
    int ActiveCount, int PausedCount, int ExecutionsToday, IReadOnlyList<AutomationChannelCount> Channels, AutomationHealth Health);
public sealed record AutomationPreviewItem(string UnitNumber, DateOnly? Period, decimal Balance, DateOnly? DueDate, int? OffsetDays, int Recipients,
    string[] Channels, string Title, string Body);
public sealed record AutomationPreview(Guid DefinitionId, DateOnly Date, int Total, bool Truncated, IReadOnlyList<AutomationPreviewItem> Items);
public sealed record AutomationDispatchFilter(int Page, int PageSize, Guid? DefinitionId, string? Channel, string? Status);
public sealed record AutomationChannelOptions(bool EmailEnabled);

public interface IAutomationStore
{
    Task<AutomationOverview?> OverviewAsync(Guid property, AccessScope access, CancellationToken token);
    Task<AutomationResponse?> CreateAsync(Guid property, AutomationType type, Guid? customType, AutomationSettings settings, string reason, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<AutomationTypeResponse>?> TypesAsync(Guid property, AccessScope access, bool canEdit, bool platformAdmin, CancellationToken token);
    Task<AutomationTypeResponse?> CreateTypeAsync(Guid property, AutomationTypeSettings settings, bool platform, string reason, AccessScope access, CancellationToken token);
    Task<AutomationTypeResponse?> UpdateTypeAsync(Guid property, Guid id, AutomationTypeSettings settings, string reason, long version, AccessScope access, bool platformAdmin, CancellationToken token);
    Task<AutomationTypeResponse?> SetTypeStatusAsync(Guid property, Guid id, AutomationTypeStatus status, string reason, long version, AccessScope access, bool platformAdmin, CancellationToken token);
    Task<AutomationTypePreview?> PreviewTypeAsync(Guid property, Guid id, DateOnly? date, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<AutomationRevisionResponse>?> TypeRevisionsAsync(Guid property, Guid id, AccessScope access, CancellationToken token);
    Task<AutomationResponse?> UpdateAsync(Guid property, Guid id, AutomationSettings settings, string reason, long version, AccessScope access, CancellationToken token);
    Task<AutomationResponse?> SetActiveAsync(Guid property, Guid id, bool active, string reason, long version, AccessScope access, CancellationToken token);
    Task<AutomationExecutionResponse?> RunAsync(Guid property, Guid id, AccessScope access, CancellationToken token);
    Task<AutomationPreview?> PreviewAsync(Guid property, Guid id, DateOnly? date, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<AutomationRevisionResponse>?> RevisionsAsync(Guid property, Guid id, AccessScope access, CancellationToken token);
    Task<PagedRows<AutomationExecutionResponse>?> ExecutionsAsync(Guid property, int page, int pageSize, Guid? definition, AccessScope access, CancellationToken token);
    Task<PagedRows<NotificationDispatchResponse>?> DispatchesAsync(Guid property, AutomationDispatchFilter filter, AccessScope access, CancellationToken token);
}
// Called by the Worker without a user session; property isolation comes from each definition's own PropertyId.
public interface IAutomationScheduler { Task<int> RunDueAsync(CancellationToken token); }

public static class AutomationCatalog
{
    private static readonly string[] Dues = ["propertyName", "unitNumber", "periodLabel", "chargeLabel", "amount", "balance", "dueDate"];
    public static IReadOnlyList<AutomationTypeInfo> All { get; } = Array.AsReadOnly(new[]
    {
        new AutomationTypeInfo(nameof(AutomationType.DUES_DUE_REMINDER), "Aidat vade hatırlatma", [-3, -2, -1, 0], -AutomationDefinition.MaxOffset, 0,
            [.. Dues, "daysLeft", "dueText"], "{{periodLabel}} {{chargeLabel}}: {{dueText}}",
            "{{propertyName}}, {{unitNumber}} no.lu bağımsız bölümünüzün {{periodLabel}} {{chargeLabel}} borcundan kalan {{balance}} için son ödeme tarihi {{dueDate}}. Ödeme detayını BuildingFlow üzerinden inceleyebilirsiniz.",
            SmsTypes.DuesReminder),
        new AutomationTypeInfo(nameof(AutomationType.DUES_OVERDUE_REMINDER), "Aidat gecikme uyarısı", [1, 7, 15], 1, AutomationDefinition.MaxOffset,
            [.. Dues, "daysOverdue"], "Ödeme gecikmesi bildirimi",
            "{{propertyName}}, {{unitNumber}} no.lu bağımsız bölümünüzün {{periodLabel}} dönemine ait {{balance}} tutarındaki {{chargeLabel}} borcunun vadesi {{dueDate}} tarihinde, {{daysOverdue}} gün önce geçmiştir. Ödeme detayınızı inceleyiniz.",
            SmsTypes.DuesOverdue),
        new AutomationTypeInfo(nameof(AutomationType.PAYMENT_RECEIVED), "Ödeme alındı bildirimi", [], 0, 0,
            ["propertyName", "unitNumber", "paidAmount", "paidDate", "unitBalance"], "Ödemeniz alındı",
            "{{propertyName}}, {{unitNumber}} no.lu bağımsız bölümünüz için {{paidDate}} tarihli {{paidAmount}} tutarındaki tahsilat kaydedildi. Güncel kalan borç: {{unitBalance}}.",
            SmsTypes.PaymentReceived)
    });
    public static AutomationTypeInfo Get(AutomationType type) => All.Single(x => x.Type == type.ToString());
    public static AutomationTypeInfo Custom(AutomationTypeDefinition t)
    {
        var source = AutomationSources.Find(t.SourceCode)!;
        return new(nameof(AutomationType.CUSTOM), t.Name, t.DefaultOffsets, -AutomationDefinition.MaxOffset, AutomationDefinition.MaxOffset, source.Variables,
            t.DefaultTitle, t.DefaultBody, SmsTypes.AutomationNotice, t.Id, t.Icon, t.TriggerKind.ToString(), t.SourceCode);
    }
    public static bool TemplateFits(AutomationType type, string text) => SmsText.Placeholders(text).All(Get(type).Variables.Contains);
}

public sealed class AutomationService(IAutomationStore store, ICurrentUserService user)
{
    // Recipients and contact data stay with the management roles; ACCOUNTANT has no person-data access (people.md).
    private AccessScope Access()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" }.Any(user.IsInRole))
            throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    private (AccessScope Access, bool PlatformAdmin) TypeEditor()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !new[] { "SUPER_ADMIN", "SITE_ADMIN" }.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return (new(user.UserId, user.IsInRole("SUPER_ADMIN")), user.IsInRole("SUPER_ADMIN"));
    }
    [DoesNotReturn] private static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.AutomationInvalid.Create()] });
    private static void Page(int page, int size) { if (page is < 1 or > 1000000 || size is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] }); }
    private static string Reason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500 || reason.Any(char.IsControl)) Invalid("reason");
        return reason.Trim();
    }
    private static AutomationType Type(string? value)
    {
        if (value is not null && !int.TryParse(value, out _) && Enum.TryParse<AutomationType>(value, false, out var type) && Enum.IsDefined(type)) return type;
        Invalid("type"); return default;
    }
    private static AutomationSettings Settings(AutomationType type, AutomationRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 100 || r.Name.Any(char.IsControl)) Invalid("name");
        if (r.SendHour is < 0 or > 23 || r.QuietStartHour is < 0 or > 23 || r.QuietEndHour is < 0 or > 23) Invalid("sendHour");
        if (r.StartsOn is { } s && r.EndsOn is { } e && e < s) Invalid("endsOn");
        if (!r.InApp && !r.Sms && !r.Email) Invalid("channels");
        var custom = type == AutomationType.CUSTOM;
        if (custom != (r.CustomTypeId is { } typeId && typeId != Guid.Empty)) Invalid("customTypeId");
        var info = custom ? null : AutomationCatalog.Get(type);
        var offsets = r.OffsetDays ?? [];
        if (offsets.Distinct().Count() != offsets.Length || offsets.Length > AutomationDefinition.MaxOffsets ||
            (info is null ? offsets.Any(o => o is < -AutomationDefinition.MaxOffset or > AutomationDefinition.MaxOffset)
                : type == AutomationType.PAYMENT_RECEIVED ? offsets.Length != 0 : offsets.Length < 1 || offsets.Any(o => o < info.MinOffset || o > info.MaxOffset))) Invalid("offsetDays");
        if (string.IsNullOrWhiteSpace(r.TitleTemplate) || r.TitleTemplate.Trim().Length > AutomationDefinition.MaxTitle || r.TitleTemplate.Any(char.IsControl) ||
            !custom && !AutomationCatalog.TemplateFits(type, r.TitleTemplate)) Invalid("titleTemplate");
        if (string.IsNullOrWhiteSpace(r.BodyTemplate) || r.BodyTemplate.Trim().Length > AutomationDefinition.MaxBody ||
            r.BodyTemplate.Any(c => char.IsControl(c) && c != '\n') || !custom && !AutomationCatalog.TemplateFits(type, r.BodyTemplate)) Invalid("bodyTemplate");
        var settings = new AutomationSettings(r.Name.Trim(), r.StartsOn, r.EndsOn, r.SendHour, r.QuietStartHour, r.QuietEndHour, offsets, r.InApp, r.Sms, r.Email,
            r.TitleTemplate.Trim(), r.BodyTemplate.Trim());
        if (type is not (AutomationType.PAYMENT_RECEIVED or AutomationType.CUSTOM) && AutomationDefinition.InQuietWindow(r.QuietStartHour, r.QuietEndHour, r.SendHour)) Invalid("sendHour");
        return settings;
    }
    private static AutomationDefinition Probe(AutomationType type, AutomationSettings settings) => new(Guid.NewGuid(), type, settings);

    public Task<AutomationOverview?> OverviewAsync(Guid property, CancellationToken token) => store.OverviewAsync(property, Access(), token);
    public Task<AutomationResponse?> CreateAsync(Guid property, AutomationRequest request, CancellationToken token)
    {
        var access = Access(); var type = Type(request.Type); var settings = Validated(type, request);
        return store.CreateAsync(property, type, request.CustomTypeId, settings, Reason(request.Reason), access, token);
    }
    public Task<AutomationResponse?> UpdateAsync(Guid property, Guid id, AutomationRequest request, CancellationToken token)
    {
        var access = Access(); var type = Type(request.Type); var settings = Validated(type, request);
        if (request.Version < 1) Invalid("version");
        return store.UpdateAsync(property, id, settings, Reason(request.Reason), request.Version, access, token);
    }
    public Task<AutomationResponse?> SetActiveAsync(Guid property, Guid id, bool active, AutomationStatusRequest request, CancellationToken token)
    {
        var access = Access(); if (request.Version < 1) Invalid("version");
        return store.SetActiveAsync(property, id, active, Reason(request.Reason), request.Version, access, token);
    }
    public Task<AutomationExecutionResponse?> RunAsync(Guid property, Guid id, CancellationToken token) => store.RunAsync(property, id, Access(), token);
    public Task<AutomationPreview?> PreviewAsync(Guid property, Guid id, DateOnly? date, CancellationToken token)
    {
        var access = Access(); if (date is { Year: < 2000 or > 2100 }) Invalid("date");
        return store.PreviewAsync(property, id, date, access, token);
    }
    public Task<IReadOnlyList<AutomationRevisionResponse>?> RevisionsAsync(Guid property, Guid id, CancellationToken token) => store.RevisionsAsync(property, id, Access(), token);
    public Task<PagedRows<AutomationExecutionResponse>?> ExecutionsAsync(Guid property, int page, int pageSize, Guid? definition, CancellationToken token)
    { var access = Access(); Page(page, pageSize); return store.ExecutionsAsync(property, page, pageSize, definition, access, token); }
    public Task<PagedRows<NotificationDispatchResponse>?> DispatchesAsync(Guid property, AutomationDispatchFilter filter, CancellationToken token)
    {
        var access = Access(); Page(filter.Page, filter.PageSize);
        if (filter.Channel is not null && (int.TryParse(filter.Channel, out _) || !Enum.TryParse<NotificationChannel>(filter.Channel, false, out _))) Invalid("channel");
        if (filter.Status is not null && (int.TryParse(filter.Status, out _) || !Enum.TryParse<NotificationDispatchStatus>(filter.Status, false, out _))) Invalid("status");
        return store.DispatchesAsync(property, filter, access, token);
    }

    private static AutomationSettings Validated(AutomationType type, AutomationRequest request)
    {
        var settings = Settings(type, request);
        if (type != AutomationType.CUSTOM) try { Probe(type, settings); } catch (ArgumentException) { Invalid("offsetDays"); }
        return settings;
    }

    public Task<IReadOnlyList<AutomationTypeResponse>?> TypesAsync(Guid property, CancellationToken token)
    {
        var access = Access();
        return store.TypesAsync(property, access, new[] { "SUPER_ADMIN", "SITE_ADMIN" }.Any(user.IsInRole), user.IsInRole("SUPER_ADMIN"), token);
    }
    public Task<AutomationTypeResponse?> CreateTypeAsync(Guid property, AutomationTypeRequest request, CancellationToken token)
    {
        var (access, platformAdmin) = TypeEditor(); if (request.Platform && !platformAdmin) throw new UnauthorizedAccessException();
        return store.CreateTypeAsync(property, TypeSettings(request), request.Platform, Reason(request.Reason), access, token);
    }
    public Task<AutomationTypeResponse?> UpdateTypeAsync(Guid property, Guid id, AutomationTypeRequest request, CancellationToken token)
    {
        var (access, platformAdmin) = TypeEditor(); if (request.Version < 1) Invalid("version");
        return store.UpdateTypeAsync(property, id, TypeSettings(request), Reason(request.Reason), request.Version, access, platformAdmin, token);
    }
    public Task<AutomationTypeResponse?> SetTypeStatusAsync(Guid property, Guid id, AutomationTypeStatus status, AutomationTypeStatusRequest request, CancellationToken token)
    {
        var (access, platformAdmin) = TypeEditor(); if (request.Version < 1) Invalid("version");
        return store.SetTypeStatusAsync(property, id, status, Reason(request.Reason), request.Version, access, platformAdmin, token);
    }
    public Task<AutomationTypePreview?> PreviewTypeAsync(Guid property, Guid id, DateOnly? date, CancellationToken token)
    {
        var access = Access(); if (date is { Year: < 2000 or > 2100 }) Invalid("date");
        return store.PreviewTypeAsync(property, id, date, access, token);
    }
    public Task<IReadOnlyList<AutomationRevisionResponse>?> TypeRevisionsAsync(Guid property, Guid id, CancellationToken token) => store.TypeRevisionsAsync(property, id, Access(), token);
    public static IReadOnlyList<AutomationSourceInfo> Sources() => AutomationSources.All;

    private static AutomationTypeSettings TypeSettings(AutomationTypeRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 100 || r.Name.Any(char.IsControl)) Invalid("name");
        if (r.Trigger is null || int.TryParse(r.Trigger, out _) || !Enum.TryParse<AutomationTriggerKind>(r.Trigger, false, out var trigger) || !Enum.IsDefined(trigger)) { Invalid("trigger"); return null!; }
        if (string.IsNullOrWhiteSpace(r.DefaultTitle) || r.DefaultTitle.Trim().Length > AutomationDefinition.MaxTitle || r.DefaultTitle.Any(char.IsControl)) Invalid("defaultTitle");
        if (string.IsNullOrWhiteSpace(r.DefaultBody) || r.DefaultBody.Trim().Length > AutomationDefinition.MaxBody || r.DefaultBody.Any(c => char.IsControl(c) && c != '\n')) Invalid("defaultBody");
        var conditions = (r.Conditions ?? []).Select(c => c with { Values = c.Values ?? [] }).ToArray();
        var settings = new AutomationTypeSettings(r.Name.Trim(), r.Icon ?? "", r.SourceCode ?? "", conditions, trigger, r.DateField, r.DefaultOffsets ?? [], r.DayOfMonth, r.DayOfWeek,
            r.RecipientScopes ?? [], r.DefaultTitle.Trim(), r.DefaultBody.Trim());
        if (AutomationSources.Problem(settings) is { } field) Invalid(field);
        try { _ = new AutomationTypeDefinition(null, "probe", settings); } catch (ArgumentException) { Invalid("trigger"); }
        return settings;
    }
}
