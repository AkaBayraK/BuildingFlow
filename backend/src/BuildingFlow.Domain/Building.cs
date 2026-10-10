namespace BuildingFlow.Domain;
public sealed class Building : BaseEntity
{
    private readonly List<Unit> _units=[];
    private Building() { }
    internal Building(Guid propertyId,string name,int? floorCount=null)
    {
        if(propertyId==Guid.Empty)throw new ArgumentException("Property ID is required.",nameof(propertyId));
        PropertyId=propertyId;Update(name,true,floorCount);
    }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; }=null!;
    public int? FloorCount { get; private set; }
    public IReadOnlyCollection<Unit> Units=>_units.AsReadOnly();
    internal void Update(string name,bool enabled,int? floorCount=null)
    {
        name=Required(name,nameof(name),100);if(floorCount is <=0)throw new ArgumentOutOfRangeException(nameof(floorCount));
        Name=name;FloorCount=floorCount;SetEnabled(enabled);
    }
    public Unit AddUnit(string number,int floor,decimal area,UnitType type=UnitType.APARTMENT)
    {
        number=Required(number,nameof(number),30);
        if(_units.Any(value=>!value.IsDeleted && string.Equals(value.Number,number,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Unit number already exists.",nameof(number));
        var unit=new Unit(PropertyId,Id,number,floor,area,type);_units.Add(unit);return unit;
    }
    public void UpdateUnit(Guid id,string number,int floor,decimal area,bool enabled,UnitType type=UnitType.APARTMENT)
    {
        number=Required(number,nameof(number),30);var unit=_units.Single(value=>value.Id==id && !value.IsDeleted);
        if(_units.Any(value=>value.Id!=id && !value.IsDeleted && string.Equals(value.Number,number,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Unit number already exists.",nameof(number));
        unit.Update(number,floor,area,enabled,type);
    }
}
