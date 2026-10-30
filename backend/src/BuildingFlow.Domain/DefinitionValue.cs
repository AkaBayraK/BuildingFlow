namespace BuildingFlow.Domain;

// TNM 02: system-wide presentation override for one code of a code-owned list. The code itself is never stored
// elsewhere as a foreign key; records keep their own code and stay readable when a value is disabled.
public sealed class DefinitionValue : BaseEntity
{
    public const int MaxKeyLength = 40;
    public const int MaxNameLength = 100;
    public const int MaxSortOrder = 999;
    private DefinitionValue() { }
    public DefinitionValue(string listKey, string code)
    {
        ListKey = Required(listKey, nameof(listKey), MaxKeyLength);
        Code = Required(code, nameof(code), MaxKeyLength);
    }
    public string ListKey { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string? NameTr { get; private set; }
    public string? NameEn { get; private set; }
    public string? IconKey { get; private set; }
    public string? Tone { get; private set; }
    public int SortOrder { get; private set; }

    public void Set(string? nameTr, string? nameEn, string? iconKey, string? tone, int sortOrder, bool enabled)
    {
        if (sortOrder is < 0 or > MaxSortOrder) throw new ArgumentOutOfRangeException(nameof(sortOrder));
        NameTr = Optional(nameTr, nameof(nameTr), MaxNameLength);
        NameEn = Optional(nameEn, nameof(nameEn), MaxNameLength);
        IconKey = Optional(iconKey, nameof(iconKey), MaxKeyLength);
        Tone = Optional(tone, nameof(tone), MaxKeyLength);
        SortOrder = sortOrder;
        SetEnabled(enabled);
    }

    private static string? Optional(string? value, string field, int max) => string.IsNullOrWhiteSpace(value) ? null : Required(value, field, max);
}

// TNM 03: per managed area visibility and order of a system value. Hidden values are not offered for new records in
// that area; records that already carry the code keep it.
public sealed class PropertyDefinitionSetting : BaseEntity
{
    private PropertyDefinitionSetting() { }
    public PropertyDefinitionSetting(Guid propertyId, string listKey, string code)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property required.", nameof(propertyId));
        PropertyId = propertyId;
        ListKey = Required(listKey, nameof(listKey), DefinitionValue.MaxKeyLength);
        Code = Required(code, nameof(code), DefinitionValue.MaxKeyLength);
    }
    public Guid PropertyId { get; private set; }
    public string ListKey { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public bool IsHidden { get; private set; }
    public int? SortOrder { get; private set; }
    public void Set(bool hidden, int? sortOrder)
    {
        if (sortOrder is < 0 or > DefinitionValue.MaxSortOrder) throw new ArgumentOutOfRangeException(nameof(sortOrder));
        IsHidden = hidden; SortOrder = sortOrder;
    }
}
