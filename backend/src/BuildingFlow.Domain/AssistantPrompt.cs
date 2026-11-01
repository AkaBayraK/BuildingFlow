namespace BuildingFlow.Domain;

// AI Asistan FAZ 1: a ready question bound to exactly one read-only tool. The tool, not the text, decides what is read.
public sealed class AssistantPrompt : BaseEntity
{
    public const int CodeLength = 40, KeyLength = 40, TitleLength = 120, TextLength = 500, MaxSortOrder = 999;
    private AssistantPrompt() { }
    public AssistantPrompt(string code, string category, string toolKey, string titleTr, string titleEn, string textTr, string textEn, int sortOrder)
    {
        Code = Required(code, nameof(code), CodeLength);
        Set(category, toolKey, titleTr, titleEn, textTr, textEn, sortOrder, true);
    }
    public string Code { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string ToolKey { get; private set; } = null!;
    public string TitleTr { get; private set; } = null!;
    public string TitleEn { get; private set; } = null!;
    public string TextTr { get; private set; } = null!;
    public string TextEn { get; private set; } = null!;
    public int SortOrder { get; private set; }

    public void Set(string category, string toolKey, string titleTr, string titleEn, string textTr, string textEn, int sortOrder, bool enabled)
    {
        if (sortOrder is < 0 or > MaxSortOrder) throw new ArgumentOutOfRangeException(nameof(sortOrder));
        Category = Required(category, nameof(category), KeyLength);
        ToolKey = Required(toolKey, nameof(toolKey), KeyLength);
        TitleTr = Required(titleTr, nameof(titleTr), TitleLength);
        TitleEn = Required(titleEn, nameof(titleEn), TitleLength);
        TextTr = Required(textTr, nameof(textTr), TextLength);
        TextEn = Required(textEn, nameof(textEn), TextLength);
        SortOrder = sortOrder;
        SetEnabled(enabled);
    }
}

// One assistant call: who, when, which tool, how long and which source answered. The question and answer are never stored.
public sealed class AssistantAuditEntry : BaseEntity
{
    public const int UserLength = 200, KeyLength = 40;
    private AssistantAuditEntry() { }
    public AssistantAuditEntry(string userId, Guid propertyId, DateOnly businessDay, string requestKind, string? promptCode)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property required.", nameof(propertyId));
        UserId = Required(userId, nameof(userId), UserLength);
        PropertyId = propertyId;
        BusinessDay = businessDay;
        RequestKind = Required(requestKind, nameof(requestKind), KeyLength);
        PromptCode = promptCode is null ? null : Required(promptCode, nameof(promptCode), KeyLength);
        Outcome = "STARTED";
    }
    public string UserId { get; private set; } = null!;
    public Guid PropertyId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public string RequestKind { get; private set; } = null!;
    public string? PromptCode { get; private set; }
    public string? ToolKey { get; private set; }
    public string? Source { get; private set; }
    public string Outcome { get; private set; } = null!;
    public int? DurationMs { get; private set; }

    public void Finish(string? toolKey, string? source, string outcome, int durationMs)
    {
        if (Outcome != "STARTED") throw new InvalidOperationException("Assistant audit entry already finished.");
        ToolKey = toolKey is null ? null : Required(toolKey, nameof(toolKey), KeyLength);
        Source = source is null ? null : Required(source, nameof(source), KeyLength);
        Outcome = Required(outcome, nameof(outcome), KeyLength);
        DurationMs = Math.Max(0, durationMs);
    }
}
