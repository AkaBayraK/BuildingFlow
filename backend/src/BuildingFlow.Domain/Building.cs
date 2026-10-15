namespace BuildingFlow.Domain;
public sealed class Building : BaseEntity
{
    private readonly List<Unit> _units=[];
    private Building() { }
    internal Building(Guid propertyId,string name,int? floorCount=null,BuildingFeatures? features=null)
    {
        if(propertyId==Guid.Empty)throw new ArgumentException("Property ID is required.",nameof(propertyId));
        PropertyId=propertyId;Update(name,true,floorCount,features);
    }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; }=null!;
    public int? FloorCount { get; private set; }
    public int? ConstructionYear { get; private set; }
    public HeatingType? HeatingType { get; private set; }
    public bool? HasElevator { get; private set; }
    public ParkingType? ParkingType { get; private set; }
    public BuildingFeatures Features => new(ConstructionYear, HeatingType, HasElevator, ParkingType);
    public IReadOnlyCollection<Unit> Units=>_units.AsReadOnly();
    internal void Update(string name,bool enabled,int? floorCount=null,BuildingFeatures? features=null)
    {
        name=Required(name,nameof(name),100);if(floorCount is <=0)throw new ArgumentOutOfRangeException(nameof(floorCount));
        var nextFeatures=features ?? Features;nextFeatures.Validate();
        Name=name;FloorCount=floorCount;SetEnabled(enabled);
        (ConstructionYear,HeatingType,HasElevator,ParkingType)=(nextFeatures.ConstructionYear,nextFeatures.Heating,nextFeatures.HasElevator,nextFeatures.Parking);
    }
    public Unit AddUnit(string number,int floor,decimal area,UnitType type=UnitType.APARTMENT,UnitFeatures? details=null)
    {
        number=Required(number,nameof(number),30);
        if(_units.Any(value=>!value.IsDeleted && string.Equals(value.Number,number,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Unit number already exists.",nameof(number));
        var unit=new Unit(PropertyId,Id,number,floor,area,type,details);_units.Add(unit);return unit;
    }
    public void UpdateUnit(Guid id,string number,int floor,decimal area,bool enabled,UnitType type=UnitType.APARTMENT,UnitFeatures? details=null)
    {
        number=Required(number,nameof(number),30);var unit=_units.Single(value=>value.Id==id && !value.IsDeleted);
        if(_units.Any(value=>value.Id!=id && !value.IsDeleted && string.Equals(value.Number,number,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Unit number already exists.",nameof(number));
        unit.Update(number,floor,area,enabled,type,details);
    }
}
