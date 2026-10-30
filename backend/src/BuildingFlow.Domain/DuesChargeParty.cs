namespace BuildingFlow.Domain;

public sealed class DuesChargeParty : BaseEntity
{
    private DuesChargeParty() { }
    public DuesChargeParty(DuesCharge charge, UnitOccupancy link, Person person, bool isDebtor)
    {
        if (charge.PropertyId != link.PropertyId || charge.UnitId != link.UnitId || person.PropertyId != link.PropertyId || person.Id != link.PersonId)
            throw new ArgumentException("Party scope does not match the charge.");
        if (link.StartDate > charge.Period || link.EndDate < charge.Period)
            throw new ArgumentException("The relationship does not cover the assessed period start.");
        if (isDebtor && link.Type == OccupancyType.RESIDENT) throw new ArgumentException("Select an owner or tenant as debtor.");
        PropertyId = charge.PropertyId; UnitId = charge.UnitId; ChargeId = charge.Id;
        PersonId = person.Id; OccupancyId = link.Id; PersonName = person.FirstName + " " + person.LastName;
        Type = link.Type; StartDate = link.StartDate; EndDate = link.EndDate; IsDebtor = isDebtor;
    }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid ChargeId { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid OccupancyId { get; private set; }
    public string PersonName { get; private set; } = null!;
    public OccupancyType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsDebtor { get; private set; }
}

public sealed class DuesCancellationReason : BaseEntity
{
    private DuesCancellationReason() { }
    public static readonly Guid OtherId = Guid.Parse("9c74d000-6001-4000-8000-000000000005");
    public static readonly Guid TenantMovedOutId = Guid.Parse("9c74d000-6001-4000-8000-000000000004");
    public const int MaxCodeLength = 50, MaxNameLength = 100;
    // TNM 04: administrators add reasons; charges keep their own code/name snapshot, so renaming never rewrites history.
    public DuesCancellationReason(string code, string name, bool requiresExplanation)
    {
        Code = Required(code, nameof(code), MaxCodeLength);
        Edit(name, requiresExplanation);
    }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool RequiresExplanation { get; private set; }
    public bool Locked => Id == OtherId || Id == TenantMovedOutId;
    public void Edit(string name, bool requiresExplanation)
    {
        Name = Required(name, nameof(name), MaxNameLength);
        RequiresExplanation = Locked ? Id == OtherId || RequiresExplanation : requiresExplanation;
    }
}
