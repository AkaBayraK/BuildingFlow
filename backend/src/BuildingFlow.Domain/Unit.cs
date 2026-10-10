namespace BuildingFlow.Domain;
public sealed class Unit : BaseEntity
{
    private Unit() { }
    internal Unit(Guid propertyId,Guid? buildingId,string number,int floor,decimal area,UnitType type=UnitType.APARTMENT)
    {
        if(propertyId==Guid.Empty || buildingId==Guid.Empty)throw new ArgumentException("Valid parent identity is required.");
        PropertyId=propertyId;BuildingId=buildingId;Update(number,floor,area,true,type);
    }
    public Guid PropertyId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public string Number { get; private set; }=null!;
    public int Floor { get; private set; }
    public decimal GrossSquareMeters { get; private set; }
    public UnitType Type { get; private set; }
    internal void Update(string number,int floor,decimal area,bool enabled,UnitType type=UnitType.APARTMENT)
    {
        number=Required(number,nameof(number),30);
        if(area<=0 || area>99999999.99m || decimal.Round(area,2)!=area)throw new ArgumentOutOfRangeException(nameof(area));
        if(!Enum.IsDefined(type))throw new ArgumentException("Invalid unit type.",nameof(type));
        Number=number;Floor=floor;GrossSquareMeters=area;Type=type;SetEnabled(enabled);
    }
}
