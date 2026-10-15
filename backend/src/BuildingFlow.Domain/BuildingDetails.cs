namespace BuildingFlow.Domain;

public enum HeatingType { NONE, STOVE, GAS_STOVE, FLOOR_HEATER, CENTRAL, CENTRAL_METERED, COMBI, UNDERFLOOR, AIR_CONDITIONING, OTHER }
public enum ParkingType { OPEN, CLOSED, OPEN_AND_CLOSED, NONE }
public enum RoomLayout { STUDIO, R1_1, R1_5_1, R2_0, R2_1, R2_5_1, R3_1, R3_5_1, R4_1, R4_5_1, R5_1, R5_2, R6_PLUS }
public enum KitchenType { OPEN, CLOSED }
public enum FloorPosition { BELOW_ENTRANCE, BASEMENT, GROUND, GARDEN, ENTRANCE, HIGH_ENTRANCE, REGULAR, ROOF }

public sealed record BuildingFeatures(int? ConstructionYear = null, HeatingType? Heating = null, bool? HasElevator = null, ParkingType? Parking = null)
{
    public const int MinConstructionYear = 1800;
    public static readonly BuildingFeatures None = new();
    public bool IsEmpty => this == None;
    internal void Validate()
    {
        if (ConstructionYear is < MinConstructionYear or > 9999) throw new ArgumentOutOfRangeException(nameof(ConstructionYear));
        if (Heating is { } heating && !Enum.IsDefined(heating)) throw new ArgumentException("Invalid heating type.", nameof(Heating));
        if (Parking is { } parking && !Enum.IsDefined(parking)) throw new ArgumentException("Invalid parking type.", nameof(Parking));
    }
}

public sealed record UnitFeatures(decimal? NetSquareMeters = null, RoomLayout? RoomLayout = null, FloorPosition? FloorPosition = null, bool? IsMezzanine = null,
    int? BathroomCount = null, KitchenType? Kitchen = null, bool? HasBalcony = null, HeatingType? Heating = null)
{
    public const int MaxBathroomCount = 6;
    public static readonly UnitFeatures None = new();
    public static bool FloorMatches(FloorPosition position, int floor) => position switch
    {
        Domain.FloorPosition.BELOW_ENTRANCE or Domain.FloorPosition.BASEMENT => floor < 0,
        Domain.FloorPosition.GROUND or Domain.FloorPosition.GARDEN or Domain.FloorPosition.ENTRANCE or Domain.FloorPosition.HIGH_ENTRANCE => floor == 0,
        _ => floor >= 1
    };
    internal void Validate(int floor, decimal grossSquareMeters)
    {
        if (NetSquareMeters is { } net && (net <= 0 || net > grossSquareMeters || decimal.Round(net, 2) != net)) throw new ArgumentOutOfRangeException(nameof(NetSquareMeters));
        if (RoomLayout is { } layout && !Enum.IsDefined(layout)) throw new ArgumentException("Invalid room layout.", nameof(RoomLayout));
        if (FloorPosition is { } position && (!Enum.IsDefined(position) || !FloorMatches(position, floor))) throw new ArgumentException("Floor position does not match the floor number.", nameof(FloorPosition));
        if (BathroomCount is < 0 or > MaxBathroomCount) throw new ArgumentOutOfRangeException(nameof(BathroomCount));
        if (Kitchen is { } kitchen && !Enum.IsDefined(kitchen)) throw new ArgumentException("Invalid kitchen type.", nameof(Kitchen));
        if (Heating is { } heating && !Enum.IsDefined(heating)) throw new ArgumentException("Invalid heating type.", nameof(Heating));
    }
}
