using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record BuildingFeatureInput(int? ConstructionYear, string? HeatingType, bool? HasElevator, string? ParkingType)
{
    public bool IsEmpty => ConstructionYear is null && string.IsNullOrWhiteSpace(HeatingType) && HasElevator is null && string.IsNullOrWhiteSpace(ParkingType);
}
public sealed record UnitFeatureInput(decimal? NetSquareMeters, string? RoomLayout, string? FloorPosition, bool? IsMezzanine, int? BathroomCount, string? KitchenType, bool? HasBalcony, string? HeatingType);

// Request enums are case-sensitive names; numeric aliases are rejected like PropertyType.
public static class PropertyDetails
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    public static int CurrentYear(TimeProvider clock) => TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone).Year;

    public static void ValidateNeighborhood(Dictionary<string, ApiErrorMessage[]> errors, string? neighborhood)
    {
        if (neighborhood?.Trim().Length > 100) errors["neighborhood"] = [ErrorCatalog.FieldTooLong.Create(("field", "neighborhood"), ("max", 100))];
    }

    public static void Validate(Dictionary<string, ApiErrorMessage[]> errors, BuildingFeatureInput input, int currentYear)
    {
        if (input.ConstructionYear is { } year && (year < BuildingFeatures.MinConstructionYear || year > currentYear)) errors["constructionYear"] = [ErrorCatalog.ConstructionYearInvalid.Create()];
        if (!IsValid<HeatingType>(input.HeatingType)) errors["heatingType"] = [ErrorCatalog.HeatingTypeInvalid.Create()];
        if (!IsValid<ParkingType>(input.ParkingType)) errors["parkingType"] = [ErrorCatalog.ParkingTypeInvalid.Create()];
    }

    public static void Validate(Dictionary<string, ApiErrorMessage[]> errors, UnitFeatureInput input, int? floor, decimal? grossSquareMeters)
    {
        if (input.NetSquareMeters is { } net && (net <= 0 || net > 99999999.99m || decimal.Round(net, 2) != net || grossSquareMeters is null || net > grossSquareMeters))
            errors["netSquareMeters"] = [ErrorCatalog.NetAreaInvalid.Create()];
        if (!IsValid<RoomLayout>(input.RoomLayout)) errors["roomLayout"] = [ErrorCatalog.RoomLayoutInvalid.Create()];
        if (!IsValid<FloorPosition>(input.FloorPosition) || (Parse<FloorPosition>(input.FloorPosition) is { } position && floor is { } value && !UnitFeatures.FloorMatches(position, value)))
            errors["floorPosition"] = [ErrorCatalog.FloorPositionInvalid.Create()];
        if (input.BathroomCount is < 0 or > UnitFeatures.MaxBathroomCount) errors["bathroomCount"] = [ErrorCatalog.BathroomCountInvalid.Create()];
        if (!IsValid<KitchenType>(input.KitchenType)) errors["kitchenType"] = [ErrorCatalog.KitchenTypeInvalid.Create()];
        if (!IsValid<HeatingType>(input.HeatingType)) errors["heatingType"] = [ErrorCatalog.HeatingTypeInvalid.Create()];
    }

    public static void ValidateFloorWithinBuilding(int floor, int? floorCount)
    {
        if (floorCount is { } max && floor > max)
            throw new RequestValidationException(new() { ["floor"] = [ErrorCatalog.FloorExceedsFloorCount.Create()] });
    }

    public static BuildingFeatures ToFeatures(this BuildingFeatureInput input) =>
        new(input.ConstructionYear, Parse<HeatingType>(input.HeatingType), input.HasElevator, Parse<ParkingType>(input.ParkingType));

    public static UnitFeatures ToFeatures(this UnitFeatureInput input) =>
        new(input.NetSquareMeters, Parse<RoomLayout>(input.RoomLayout), Parse<FloorPosition>(input.FloorPosition), input.IsMezzanine,
            input.BathroomCount, Parse<KitchenType>(input.KitchenType), input.HasBalcony, Parse<HeatingType>(input.HeatingType));

    private static bool IsValid<T>(string? value) where T : struct, Enum =>
        string.IsNullOrWhiteSpace(value) || (Enum.TryParse<T>(value, out var parsed) && Enum.IsDefined(parsed) && parsed.ToString() == value);

    private static T? Parse<T>(string? value) where T : struct, Enum =>
        IsValid<T>(value) && !string.IsNullOrWhiteSpace(value) ? Enum.Parse<T>(value) : null;
}
