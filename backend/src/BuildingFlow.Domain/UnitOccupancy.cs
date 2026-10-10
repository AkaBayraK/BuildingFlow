namespace BuildingFlow.Domain;

public enum OccupancyType { OWNER, TENANT, RESIDENT }

public sealed class UnitOccupancy : BaseEntity
{
    private UnitOccupancy() { }
    public UnitOccupancy(Guid propertyId, Guid unitId, Guid personId, OccupancyType type,
        DateOnly startDate, DateOnly? endDate = null, bool isPrimaryContact = false)
    {
        if (propertyId == Guid.Empty || unitId == Guid.Empty || personId == Guid.Empty)
            throw new ArgumentException("Property, unit and person identities are required.");
        if (!Enum.IsDefined(type)) throw new ArgumentException("Invalid occupancy type.");
        if (startDate == DateOnly.MinValue || (endDate is not null && endDate < startDate))
            throw new ArgumentException("Invalid occupancy dates.");
        PropertyId = propertyId; UnitId = unitId; PersonId = personId; Type = type;
        StartDate = startDate; EndDate = endDate; IsPrimaryContact = isPrimaryContact;
    }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid PersonId { get; private set; }
    public OccupancyType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsPrimaryContact { get; private set; }
    public bool Contains(DateOnly date) => StartDate <= date && (EndDate is null || EndDate >= date);
    public bool Overlaps(DateOnly start, DateOnly? end) => (end is null || StartDate <= end) && (EndDate is null || EndDate >= start);
    public void End(DateOnly endDate)
    {
        if (EndDate is not null) throw new InvalidOperationException("An ended relationship cannot be rewritten.");
        if (endDate < StartDate) throw new ArgumentException("End date precedes start date.");
        EndDate = endDate;
    }
}
