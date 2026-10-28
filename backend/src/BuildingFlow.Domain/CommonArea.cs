namespace BuildingFlow.Domain;

public enum CommonAreaType { POOL, FITNESS, MEETING_ROOM, PLAYGROUND, BARBECUE, SPORTS_COURT, EVENT_HALL, OTHER }
public enum CommonAreaVisibility { PROPERTY, BUILDING }
public enum CommonAreaUsage { OPEN_USE, RESERVATION, APPROVAL }

// Definition only. Availability, booking rights and collection are separate workflows.
public sealed class CommonArea : BaseEntity
{
    private CommonArea() { }
    public CommonArea(Guid propertyId, Guid requestId, string fingerprint, string name, CommonAreaType type,
        Guid? buildingId, CommonAreaVisibility visibility, CommonAreaUsage usage, int capacity,
        string? description, string? rules, TimeOnly? opensAt, TimeOnly? closesAt, decimal feeAmount)
    {
        if (propertyId == Guid.Empty || requestId == Guid.Empty || fingerprint.Length != 64 || !fingerprint.All(Uri.IsHexDigit))
            throw new ArgumentException("Invalid identity.");
        PropertyId = propertyId; RequestId = requestId; Fingerprint = fingerprint;
        Edit(name, type, buildingId, visibility, usage, capacity, description, rules, opensAt, closesAt, feeAmount, true);
    }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public string Fingerprint { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public CommonAreaType Type { get; private set; }
    public Guid? BuildingId { get; private set; }
    public CommonAreaVisibility Visibility { get; private set; }
    public CommonAreaUsage Usage { get; private set; }
    public int Capacity { get; private set; }
    public string? Description { get; private set; }
    public string? Rules { get; private set; }
    public TimeOnly? OpensAt { get; private set; }
    public TimeOnly? ClosesAt { get; private set; }
    public decimal FeeAmount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public void Edit(string name, CommonAreaType type, Guid? buildingId, CommonAreaVisibility visibility,
        CommonAreaUsage usage, int capacity, string? description, string? rules, TimeOnly? opensAt, TimeOnly? closesAt,
        decimal feeAmount, bool enabled)
    {
        if (!Enum.IsDefined(type) || !Enum.IsDefined(visibility) || !Enum.IsDefined(usage) || buildingId == Guid.Empty ||
            visibility == CommonAreaVisibility.BUILDING && buildingId is null || capacity is < 1 or > 10000 ||
            opensAt.HasValue != closesAt.HasValue || opensAt.HasValue && (opensAt >= closesAt || opensAt.Value.Ticks % TimeSpan.TicksPerMinute != 0 || closesAt!.Value.Ticks % TimeSpan.TicksPerMinute != 0) ||
            feeAmount is < 0 or > 99999999.99m || decimal.Round(feeAmount, 2) != feeAmount)
            throw new ArgumentException("Invalid common area definition.");
        var nextName = Required(name, nameof(name), 150);
        var nextDescription = Optional(description, 2000); var nextRules = Optional(rules, 8000);
        Name = nextName; Description = nextDescription; Rules = nextRules; Type = type; BuildingId = buildingId;
        Visibility = visibility; Usage = usage; Capacity = capacity; OpensAt = opensAt; ClosesAt = closesAt;
        FeeAmount = feeAmount; SetEnabled(enabled);
    }
    private static string? Optional(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : Required(value, nameof(value), max);
}
