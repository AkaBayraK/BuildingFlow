namespace BuildingFlow.Domain;

// A maintenance closure is a time window, never a claim about measured occupancy.
public sealed class ParkingClosure : BaseEntity
{
    private ParkingClosure() { }
    public ParkingClosure(Guid propertyId, Guid spaceId, Guid requestId, DateTimeOffset startsAt, DateTimeOffset endsAt, string reason)
    {
        if (propertyId == Guid.Empty || spaceId == Guid.Empty || requestId == Guid.Empty || endsAt <= startsAt)
            throw new ArgumentException("Invalid closure.");
        PropertyId = propertyId; SpaceId = spaceId; RequestId = requestId;
        StartsAt = startsAt.ToUniversalTime(); EndsAt = endsAt.ToUniversalTime(); Reason = Required(reason, nameof(reason), 500);
    }
    public Guid PropertyId { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid RequestId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public bool Overlaps(DateTimeOffset start, DateTimeOffset end) => CancelledAt is null && StartsAt < end && start < EndsAt;
    public void Cancel(DateTimeOffset at, string reason)
    {
        if (CancelledAt is not null || at >= EndsAt) throw new InvalidOperationException("Closure cannot be cancelled.");
        CancellationReason = Required(reason, nameof(reason), 500); CancelledAt = at;
    }
}
