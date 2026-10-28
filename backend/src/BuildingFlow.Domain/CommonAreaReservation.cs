namespace BuildingFlow.Domain;

public enum CommonAreaReservationStatus { CONFIRMED, CANCELLED }

// Manager-created booking in Europe/Istanbul local time; cancellation keeps the row as history.
public sealed class CommonAreaReservation : BaseEntity
{
    public const int SlotMinutes = 15;
    private CommonAreaReservation() { }
    public CommonAreaReservation(Guid propertyId, Guid commonAreaId, Guid requestId, string fingerprint, Guid? unitId,
        DateOnly date, TimeOnly startsAt, TimeOnly endsAt, int partySize, string? note)
    {
        if (propertyId == Guid.Empty || commonAreaId == Guid.Empty || requestId == Guid.Empty || fingerprint.Length != 64 || !fingerprint.All(Uri.IsHexDigit))
            throw new ArgumentException("Invalid identity.");
        PropertyId = propertyId; CommonAreaId = commonAreaId; RequestId = requestId; Fingerprint = fingerprint;
        Status = CommonAreaReservationStatus.CONFIRMED;
        Edit(unitId, date, startsAt, endsAt, partySize, note);
    }
    public Guid PropertyId { get; private set; }
    public Guid CommonAreaId { get; private set; }
    public Guid RequestId { get; private set; }
    public string Fingerprint { get; private set; } = null!;
    public Guid? UnitId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartsAt { get; private set; }
    public TimeOnly EndsAt { get; private set; }
    public int PartySize { get; private set; }
    public string? Note { get; private set; }
    public CommonAreaReservationStatus Status { get; private set; }
    public string? CancelReason { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public bool Overlaps(TimeOnly start, TimeOnly end) => StartsAt < end && start < EndsAt;
    public void Edit(Guid? unitId, DateOnly date, TimeOnly startsAt, TimeOnly endsAt, int partySize, string? note)
    {
        if (Status != CommonAreaReservationStatus.CONFIRMED) throw new InvalidOperationException("Cancelled reservation is immutable.");
        if (unitId == Guid.Empty || startsAt >= endsAt || !OnSlot(startsAt) || !OnSlot(endsAt) || partySize is < 1 or > 10000)
            throw new ArgumentException("Invalid reservation.");
        UnitId = unitId; Date = date; StartsAt = startsAt; EndsAt = endsAt; PartySize = partySize;
        Note = string.IsNullOrWhiteSpace(note) ? null : Required(note, nameof(note), 500);
    }
    public void Cancel(string reason, DateTimeOffset at)
    {
        if (Status != CommonAreaReservationStatus.CONFIRMED) throw new InvalidOperationException("Reservation already cancelled.");
        CancelReason = Required(reason, nameof(reason), 500); CancelledAt = at; Status = CommonAreaReservationStatus.CANCELLED;
    }
    public static bool OnSlot(TimeOnly value) => value.Ticks % (TimeSpan.TicksPerMinute * SlotMinutes) == 0;
}
