namespace BuildingFlow.Domain;

public enum ParkingSpaceType { STANDARD, ACCESSIBLE, VISITOR, EV_CHARGING, MOTORCYCLE }
public enum VehicleType { CAR, MOTORCYCLE, VAN, OTHER }
public enum VehicleStatus { PENDING, APPROVED, REJECTED, PASSIVE }
public enum GuestVehicleStatus { PLANNED, ARRIVED, DEPARTED, CANCELLED }

public static class VehiclePlate
{
    public const int MaxLength = 12;
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        var plate = new string(value.Where(c => c is not (' ' or '-' or '.')).ToArray()).ToUpperInvariant();
        if (plate.Length is < 2 or > MaxLength || !plate.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9'))
            throw new ArgumentException("Invalid plate.", nameof(value));
        return plate;
    }
}

public sealed class ParkingPolicy : BaseEntity
{
    public const int MaxQuota = 20, MaxGuestHours = 168, MaxRetentionDays = 365, DefaultRetentionDays = 30;
    private ParkingPolicy() { }
    public ParkingPolicy(Guid propertyId, int maxVehiclesPerUnit, int maxVehiclesPerPerson, int maxSpacesPerUnit,
        int guestMaxHours, int retentionDays = DefaultRetentionDays)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property required.");
        PropertyId = propertyId; Edit(maxVehiclesPerUnit, maxVehiclesPerPerson, maxSpacesPerUnit, guestMaxHours, retentionDays);
    }
    public Guid PropertyId { get; private set; }
    public int MaxVehiclesPerUnit { get; private set; }
    public int MaxVehiclesPerPerson { get; private set; }
    public int MaxSpacesPerUnit { get; private set; }
    public int GuestMaxHours { get; private set; }
    public int RetentionDays { get; private set; }
    public void Edit(int maxVehiclesPerUnit, int maxVehiclesPerPerson, int maxSpacesPerUnit, int guestMaxHours, int retentionDays)
    {
        if (maxVehiclesPerUnit is < 1 or > MaxQuota || maxVehiclesPerPerson is < 1 or > MaxQuota || maxSpacesPerUnit is < 0 or > MaxQuota)
            throw new ArgumentOutOfRangeException(nameof(maxVehiclesPerUnit), "Invalid parking quota.");
        if (guestMaxHours is < 1 or > MaxGuestHours) throw new ArgumentOutOfRangeException(nameof(guestMaxHours));
        if (retentionDays is < 1 or > MaxRetentionDays) throw new ArgumentOutOfRangeException(nameof(retentionDays));
        (MaxVehiclesPerUnit, MaxVehiclesPerPerson, MaxSpacesPerUnit, GuestMaxHours, RetentionDays) =
            (maxVehiclesPerUnit, maxVehiclesPerPerson, maxSpacesPerUnit, guestMaxHours, retentionDays);
    }
    public bool AllowsVehicle(int activeVehiclesOfUnit, int activeVehiclesOfPerson) =>
        activeVehiclesOfUnit < MaxVehiclesPerUnit && activeVehiclesOfPerson < MaxVehiclesPerPerson;
    public bool AllowsSpace(int activeSpacesOfUnit) => activeSpacesOfUnit < MaxSpacesPerUnit;
    public bool AllowsGuestWindow(DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        endsAt > startsAt && endsAt - startsAt <= TimeSpan.FromHours(GuestMaxHours);
}

public sealed class ParkingFacility : BaseEntity
{
    public const int NameLength = 100;
    private ParkingFacility() { }
    public ParkingFacility(Guid propertyId, Guid? buildingId, string name)
    {
        if (propertyId == Guid.Empty || buildingId == Guid.Empty) throw new ArgumentException("Facility scope required.");
        PropertyId = propertyId; Edit(buildingId, name);
    }
    public Guid PropertyId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsShared => BuildingId is null;
    public void Edit(Guid? buildingId, string name)
    {
        if (buildingId == Guid.Empty) throw new ArgumentException("Invalid building.", nameof(buildingId));
        BuildingId = buildingId; Name = Required(name, nameof(name), NameLength);
    }
}

public sealed class ParkingLevel : BaseEntity
{
    public const int NameLength = 50, MinOrder = -10, MaxOrder = 50;
    private ParkingLevel() { }
    public ParkingLevel(Guid propertyId, Guid facilityId, string name, int sortOrder)
    {
        if (propertyId == Guid.Empty || facilityId == Guid.Empty) throw new ArgumentException("Level scope required.");
        PropertyId = propertyId; FacilityId = facilityId; Edit(name, sortOrder);
    }
    public Guid PropertyId { get; private set; }
    public Guid FacilityId { get; private set; }
    public string Name { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public void Edit(string name, int sortOrder)
    {
        if (sortOrder is < MinOrder or > MaxOrder) throw new ArgumentOutOfRangeException(nameof(sortOrder));
        Name = Required(name, nameof(name), NameLength); SortOrder = sortOrder;
    }
}

public sealed class ParkingSpace : BaseEntity
{
    public const int CodeLength = 20;
    private ParkingSpace() { }
    public ParkingSpace(Guid propertyId, Guid facilityId, Guid? levelId, string code, ParkingSpaceType type)
    {
        if (propertyId == Guid.Empty || facilityId == Guid.Empty) throw new ArgumentException("Space scope required.");
        PropertyId = propertyId; FacilityId = facilityId; Edit(levelId, code, type);
    }
    public Guid PropertyId { get; private set; }
    public Guid FacilityId { get; private set; }
    public Guid? LevelId { get; private set; }
    public string Code { get; private set; } = null!;
    public ParkingSpaceType Type { get; private set; }
    public bool IsAllocatable => Type != ParkingSpaceType.VISITOR && IsEnabled && !IsDeleted;
    public void Edit(Guid? levelId, string code, ParkingSpaceType type)
    {
        if (levelId == Guid.Empty || !Enum.IsDefined(type)) throw new ArgumentException("Invalid space level or type.");
        LevelId = levelId; Code = Required(code, nameof(code), CodeLength).ToUpperInvariant(); Type = type;
    }
}

public sealed class Vehicle : BaseEntity
{
    public const int TextLength = 50, ReasonLength = 500;
    private Vehicle() { }
    public Vehicle(Guid propertyId, Guid personId, Guid unitId, string plate, VehicleType type, string? brand, string? model, string? color,
        Guid? requestId = null)
    {
        if (propertyId == Guid.Empty || personId == Guid.Empty || unitId == Guid.Empty || requestId == Guid.Empty) throw new ArgumentException("Vehicle scope required.");
        PropertyId = propertyId; RequestId = requestId ?? Guid.NewGuid(); PersonId = personId; UnitId = unitId; Plate = VehiclePlate.Normalize(plate);
        Status = VehicleStatus.PENDING; Edit(type, brand, model, color);
    }
    public Guid PropertyId { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid RequestId { get; private set; }
    public string Plate { get; private set; } = null!;
    public VehicleType Type { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? Color { get; private set; }
    public VehicleStatus Status { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTimeOffset? DeactivatedAt { get; private set; }
    public DateTimeOffset? PersonalDataRedactedAt { get; private set; }
    public bool Redact(DateTimeOffset now, int retentionDays)
    {
        if (retentionDays is < 1 or > ParkingPolicy.MaxRetentionDays) throw new ArgumentOutOfRangeException(nameof(retentionDays));
        var closed = Status == VehicleStatus.PASSIVE ? DeactivatedAt : Status == VehicleStatus.REJECTED ? ReviewedAt : null;
        if (PersonalDataRedactedAt is not null || closed is null || closed > now.AddDays(-retentionDays)) return false;
        Plate = "REDACTED"; Brand = Model = Color = null; ReviewReason = Status == VehicleStatus.REJECTED ? "REDACTED" : null;
        PersonalDataRedactedAt = now; return true;
    }
    public bool IsActive => Status is VehicleStatus.PENDING or VehicleStatus.APPROVED;
    public void Edit(VehicleType type, string? brand, string? model, string? color)
    {
        if (!IsActive) throw new InvalidOperationException("Closed vehicle cannot be edited.");
        if (!Enum.IsDefined(type)) throw new ArgumentException("Invalid vehicle type.", nameof(type));
        Type = type; Brand = Optional(brand, nameof(brand)); Model = Optional(model, nameof(model)); Color = Optional(color, nameof(color));
    }
    public void Approve(DateTimeOffset at) => Review(VehicleStatus.APPROVED, at, null);
    public void Reject(DateTimeOffset at, string reason) => Review(VehicleStatus.REJECTED, at, Required(reason, nameof(reason), ReasonLength));
    public void Deactivate(DateTimeOffset at)
    {
        if (!IsActive) throw new InvalidOperationException("Vehicle already closed.");
        Status = VehicleStatus.PASSIVE; DeactivatedAt = at;
    }
    private void Review(VehicleStatus status, DateTimeOffset at, string? reason)
    {
        if (Status != VehicleStatus.PENDING) throw new InvalidOperationException("Only pending vehicles can be reviewed.");
        Status = status; ReviewedAt = at; ReviewReason = reason;
    }
    private static string? Optional(string? value, string field) => string.IsNullOrWhiteSpace(value) ? null : Required(value, field, TextLength);
}

public sealed class ParkingAllocation : BaseEntity
{
    private ParkingAllocation() { }
    public ParkingAllocation(Guid propertyId, ParkingSpace space, Guid unitId, Guid? vehicleId, DateOnly startDate, DateOnly? endDate = null)
    {
        if (propertyId == Guid.Empty || unitId == Guid.Empty || vehicleId == Guid.Empty) throw new ArgumentException("Allocation scope required.");
        if (space.PropertyId != propertyId || !space.IsAllocatable) throw new ArgumentException("Space cannot be allocated.", nameof(space));
        if (startDate == DateOnly.MinValue || endDate < startDate) throw new ArgumentException("Invalid allocation dates.");
        PropertyId = propertyId; SpaceId = space.Id; UnitId = unitId; VehicleId = vehicleId; StartDate = startDate; EndDate = endDate;
    }
    public Guid PropertyId { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid? VehicleId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool Contains(DateOnly date) => StartDate <= date && (EndDate is null || EndDate >= date);
    public bool Overlaps(DateOnly start, DateOnly? end) => (end is null || StartDate <= end) && (EndDate is null || EndDate >= start);
    public void End(DateOnly endDate)
    {
        if (EndDate is not null) throw new InvalidOperationException("An ended allocation cannot be rewritten.");
        if (endDate < StartDate) throw new ArgumentException("End date precedes start date.");
        EndDate = endDate;
    }
}

public sealed class GuestVehicle : BaseEntity
{
    public const int NameLength = 100, NoteLength = 500;
    private GuestVehicle() { }
    public GuestVehicle(ParkingPolicy policy, Guid hostUnitId, string plate, string? guestName, DateTimeOffset startsAt, DateTimeOffset endsAt,
        Guid? spaceId, string? note, Guid? requestId = null)
    {
        if (hostUnitId == Guid.Empty || spaceId == Guid.Empty || requestId == Guid.Empty) throw new ArgumentException("Guest vehicle scope required.");
        RequestId = requestId ?? Guid.NewGuid();
        if (!policy.AllowsGuestWindow(startsAt, endsAt)) throw new ArgumentException("Guest window exceeds policy.");
        PropertyId = policy.PropertyId; HostUnitId = hostUnitId; Plate = VehiclePlate.Normalize(plate);
        GuestName = Optional(guestName, nameof(guestName), NameLength); Note = Optional(note, nameof(note), NoteLength);
        StartsAt = startsAt.ToUniversalTime(); EndsAt = endsAt.ToUniversalTime(); SpaceId = spaceId; Status = GuestVehicleStatus.PLANNED;
    }
    public Guid PropertyId { get; private set; }
    public Guid HostUnitId { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid? SpaceId { get; private set; }
    public string Plate { get; private set; } = null!;
    public string? GuestName { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public GuestVehicleStatus Status { get; private set; }
    public DateTimeOffset? ArrivedAt { get; private set; }
    public DateTimeOffset? DepartedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? PersonalDataRedactedAt { get; private set; }
    public bool Redact(DateTimeOffset now, int retentionDays)
    {
        if (retentionDays is < 1 or > ParkingPolicy.MaxRetentionDays) throw new ArgumentOutOfRangeException(nameof(retentionDays));
        var closed = Status == GuestVehicleStatus.DEPARTED ? DepartedAt : Status == GuestVehicleStatus.CANCELLED ? CancelledAt : null;
        if (PersonalDataRedactedAt is not null || closed is null || closed > now.AddDays(-retentionDays)) return false;
        Plate = "REDACTED"; GuestName = Note = null; PersonalDataRedactedAt = now; return true;
    }
    public bool Overlaps(DateTimeOffset start, DateTimeOffset end) =>
        Status is GuestVehicleStatus.PLANNED or GuestVehicleStatus.ARRIVED && StartsAt < end && start < EndsAt;
    public void MarkArrived(DateTimeOffset at)
    {
        if (Status != GuestVehicleStatus.PLANNED || at < StartsAt || at >= EndsAt) throw new InvalidOperationException("Guest vehicle is not expected.");
        Status = GuestVehicleStatus.ARRIVED; ArrivedAt = at;
    }
    public void MarkDeparted(DateTimeOffset at)
    {
        if (Status != GuestVehicleStatus.ARRIVED || at < ArrivedAt) throw new InvalidOperationException("Guest vehicle has not arrived.");
        Status = GuestVehicleStatus.DEPARTED; DepartedAt = at;
    }
    public void Cancel(DateTimeOffset at)
    {
        if (Status != GuestVehicleStatus.PLANNED) throw new InvalidOperationException("Only planned guest vehicles can be cancelled.");
        Status = GuestVehicleStatus.CANCELLED; CancelledAt = at;
    }
    private static string? Optional(string? value, string field, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : Required(value, field, maxLength);
}
