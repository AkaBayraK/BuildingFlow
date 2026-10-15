namespace BuildingFlow.Domain;
public sealed class Unit : BaseEntity
{
    private Unit() { }
    internal Unit(Guid propertyId,Guid? buildingId,string number,int floor,decimal area,UnitType type=UnitType.APARTMENT,UnitFeatures? details=null)
    {
        if(propertyId==Guid.Empty || buildingId==Guid.Empty)throw new ArgumentException("Valid parent identity is required.");
        PropertyId=propertyId;BuildingId=buildingId;Update(number,floor,area,true,type,details);
    }
    public Guid PropertyId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public string Number { get; private set; }=null!;
    public int Floor { get; private set; }
    public decimal GrossSquareMeters { get; private set; }
    public UnitType Type { get; private set; }
    public decimal? NetSquareMeters { get; private set; }
    public RoomLayout? RoomLayout { get; private set; }
    public FloorPosition? FloorPosition { get; private set; }
    public bool? IsMezzanine { get; private set; }
    public int? BathroomCount { get; private set; }
    public KitchenType? KitchenType { get; private set; }
    public bool? HasBalcony { get; private set; }
    public HeatingType? HeatingType { get; private set; }
    public UnitFeatures Details => new(NetSquareMeters, RoomLayout, FloorPosition, IsMezzanine, BathroomCount, KitchenType, HasBalcony, HeatingType);
    internal void Update(string number,int floor,decimal area,bool enabled,UnitType type=UnitType.APARTMENT,UnitFeatures? details=null)
    {
        number=Required(number,nameof(number),30);
        if(area<=0 || area>99999999.99m || decimal.Round(area,2)!=area)throw new ArgumentOutOfRangeException(nameof(area));
        if(!Enum.IsDefined(type))throw new ArgumentException("Invalid unit type.",nameof(type));
        var next=details ?? Details;next.Validate(floor,area);
        Number=number;Floor=floor;GrossSquareMeters=area;Type=type;SetEnabled(enabled);
        (NetSquareMeters,RoomLayout,FloorPosition,IsMezzanine)=(next.NetSquareMeters,next.RoomLayout,next.FloorPosition,next.IsMezzanine);
        (BathroomCount,KitchenType,HasBalcony,HeatingType)=(next.BathroomCount,next.Kitchen,next.HasBalcony,next.Heating);
    }
}
