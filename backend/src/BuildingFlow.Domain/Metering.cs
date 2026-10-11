namespace BuildingFlow.Domain;

public enum MeterKind { WATER, ELECTRICITY, GAS }
public sealed class Meter : BaseEntity
{
    private Meter() { }
    public Meter(Guid propertyId, Guid? unitId, Guid requestId, string fingerprint, string name, MeterKind kind)
    {
        if (propertyId == Guid.Empty || unitId == Guid.Empty || requestId == Guid.Empty || !Enum.IsDefined(kind)) throw new ArgumentException("Invalid meter identity or kind.");
        FinanceIdentity.Validate(fingerprint);
        PropertyId = propertyId; UnitId = unitId; RequestId = requestId; RequestFingerprint = fingerprint; Name = Required(name, nameof(name), 100); Kind = kind;
    }
    public Guid PropertyId { get; private set; }
    public Guid? UnitId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public MeterKind Kind { get; private set; }
}
public sealed class MeterReading : BaseEntity
{
    private MeterReading() { }
    public MeterReading(Guid propertyId, Guid meterId, DateOnly date) { if(propertyId==Guid.Empty||meterId==Guid.Empty||date.Year<2000)throw new ArgumentException("Invalid reading scope or date."); PropertyId = propertyId; MeterId = meterId; ReadingDate = date; }
    public Guid PropertyId { get; private set; }
    public Guid MeterId { get; private set; }
    public DateOnly ReadingDate { get; private set; }
    public decimal Value { get; private set; }
    public int RevisionCount { get; private set; }
    public MeterReadingRevision Revise(Guid requestId, string fingerprint, decimal value, string reason)
    {
        if (value < 0 || value > 999999999.999m || decimal.Round(value, 3) != value) throw new ArgumentOutOfRangeException(nameof(value));
        if(requestId==Guid.Empty)throw new ArgumentException("A revision request identity is required.");
        FinanceIdentity.Validate(fingerprint);
        reason = Required(reason, nameof(reason), 500);
        Value = value; RevisionCount++;
        return new MeterReadingRevision(PropertyId, Id, requestId, fingerprint, value, RevisionCount, reason);
    }
}
public sealed class MeterReadingRevision : BaseEntity
{
    private MeterReadingRevision() { }
    internal MeterReadingRevision(Guid propertyId, Guid readingId, Guid requestId, string fingerprint, decimal value, int sequence, string reason)
    { PropertyId = propertyId; ReadingId = readingId; RequestId = requestId; RequestFingerprint = fingerprint; Value = value; Sequence = sequence; Reason = reason; }
    public Guid PropertyId { get; private set; }
    public Guid ReadingId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = null!;
    public decimal Value { get; private set; }
    public int Sequence { get; private set; }
    public string Reason { get; private set; } = null!;
}
