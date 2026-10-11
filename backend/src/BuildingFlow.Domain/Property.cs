namespace BuildingFlow.Domain;

public enum PropertyType { APARTMENT, SITE, RESIDENCE, MIXED }
public enum UnitType { APARTMENT, OFFICE, SHOP, VILLA, WAREHOUSE, WORKSHOP }

public sealed class Property : BaseEntity
{
    private readonly List<Building> _buildings = [];
    private readonly List<Unit> _units = [];
    private Property() { }
    public Property(string name, string city, string district, string address, PropertyType type = PropertyType.SITE, int? floorCount = null, BuildingFeatures? features = null)
    {
        if(!Enum.IsDefined(type))throw new ArgumentException("Invalid property type.",nameof(type));
        Type=type; UpdateDetails(name,city,district,address,true,floorCount,null,features);
    }
    public string Name { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string District { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public PropertyType Type { get; private set; }
    public int? FloorCount { get; private set; }
    public string? Neighborhood { get; private set; }
    public int? ConstructionYear { get; private set; }
    public HeatingType? HeatingType { get; private set; }
    public bool? HasElevator { get; private set; }
    public ParkingType? ParkingType { get; private set; }
    public BuildingFeatures Features => new(ConstructionYear, HeatingType, HasElevator, ParkingType);
    public IReadOnlyCollection<Building> Buildings => _buildings.AsReadOnly();
    public IReadOnlyCollection<Unit> Units => _units.AsReadOnly();
    public void UpdateDetails(string name,string city,string district,string address,bool enabled,int? floorCount=null,PropertyType? type=null,BuildingFeatures? features=null)
    {
        var nextType=type ?? Type;
        if(!Enum.IsDefined(nextType))throw new ArgumentException("Invalid property type.",nameof(type));
        if(nextType!=Type && (_buildings.Count>0 || _units.Count>0))throw new InvalidOperationException("A property with building or unit history cannot change type.");
        var values=(Required(name,nameof(name),200),Required(city,nameof(city),100),Required(district,nameof(district),100),Required(address,nameof(address),500));
        if(floorCount is <=0 || (nextType!=PropertyType.APARTMENT && floorCount is not null))throw new ArgumentException("Floor count is positive metadata for a standalone apartment.",nameof(floorCount));
        var nextFeatures=features ?? Features;nextFeatures.Validate();
        if(nextType!=PropertyType.APARTMENT && !nextFeatures.IsEmpty)throw new ArgumentException("Building features belong to buildings unless the property is a standalone apartment.",nameof(features));
        (Name,City,District,Address)=values;Type=nextType;FloorCount=floorCount;SetEnabled(enabled);
        (ConstructionYear,HeatingType,HasElevator,ParkingType)=(nextFeatures.ConstructionYear,nextFeatures.Heating,nextFeatures.HasElevator,nextFeatures.Parking);
    }
    public void SetNeighborhood(string? neighborhood)
    {
        neighborhood=string.IsNullOrWhiteSpace(neighborhood)?null:neighborhood.Trim();
        if(neighborhood?.Length>100)throw new ArgumentException("Neighborhood is too long.",nameof(neighborhood));
        Neighborhood=neighborhood;
    }
    public Building AddBuilding(string name,int? floorCount=null,BuildingFeatures? features=null)
    {
        if(Type==PropertyType.APARTMENT)throw new InvalidOperationException("Standalone apartments contain units directly.");
        name=Required(name,nameof(name),100);
        if(_buildings.Any(value=>!value.IsDeleted && string.Equals(value.Name,name,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Building name already exists.",nameof(name));
        var building=new Building(Id,name,floorCount,features);_buildings.Add(building);return building;
    }
    public void UpdateBuilding(Guid id,string name,bool enabled,int? floorCount=null,BuildingFeatures? features=null)
    {
        name=Required(name,nameof(name),100);var building=_buildings.Single(value=>value.Id==id && !value.IsDeleted);
        if(_buildings.Any(value=>value.Id!=id && !value.IsDeleted && string.Equals(value.Name,name,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Building name already exists.",nameof(name));
        building.Update(name,enabled,floorCount,features);
    }
    public Unit AddUnit(string number,int floor,decimal area,UnitType type=UnitType.APARTMENT,UnitFeatures? details=null)
    {
        RequireDirectUnits(type); number=Required(number,nameof(number),30);
        if(_units.Any(value=>!value.IsDeleted && value.BuildingId is null && string.Equals(value.Number,number,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Unit number already exists.",nameof(number));
        var unit=new Unit(Id,null,number,floor,area,type,details);_units.Add(unit);return unit;
    }
    public void UpdateUnit(Guid id,string number,int floor,decimal area,bool enabled,UnitType type=UnitType.APARTMENT,UnitFeatures? details=null)
    {
        RequireDirectUnits(type);number=Required(number,nameof(number),30);var unit=_units.Single(value=>value.Id==id && value.BuildingId is null && !value.IsDeleted);
        if(_units.Any(value=>value.Id!=id && value.BuildingId is null && !value.IsDeleted && string.Equals(value.Number,number,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Unit number already exists.",nameof(number));
        unit.Update(number,floor,area,enabled,type,details);
    }
    private void RequireDirectUnits(UnitType type)
    { if(Type!=PropertyType.APARTMENT || type!=UnitType.APARTMENT)throw new InvalidOperationException("Only apartment units belong directly to a standalone apartment property."); }
}
