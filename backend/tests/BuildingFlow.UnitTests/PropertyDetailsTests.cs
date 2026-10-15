using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class PropertyDetailsTests
{
    private static readonly BuildingFeatures Features = new(2015, HeatingType.COMBI, true, ParkingType.OPEN_AND_CLOSED);

    [Fact] public void ApartmentPropertyKeepsBuildingFeaturesAndNeighborhood()
    {
        var property=new Property("A","İstanbul","Kadıköy","Address",PropertyType.APARTMENT,5,Features);property.SetNeighborhood(" Moda ");
        Assert.Equal(Features,property.Features);Assert.Equal("Moda",property.Neighborhood);
        property.SetNeighborhood(" ");Assert.Null(property.Neighborhood);
        Assert.Throws<ArgumentException>(()=>property.SetNeighborhood(new string('x',101)));
    }
    [Fact] public void BuildingFeaturesBelongToBuildingsForMultiBuildingTypes()
    {
        Assert.Throws<ArgumentException>(()=>new Property("S","City","District","Address",PropertyType.SITE,null,Features));
        var site=new Property("S","City","District","Address");var building=site.AddBuilding("A",8,Features);Assert.Equal(Features,building.Features);
        site.UpdateBuilding(building.Id,"A",true,8,BuildingFeatures.None);Assert.True(building.Features.IsEmpty);
        var apartment=new Property("A","City","District","Address",PropertyType.APARTMENT,5,Features);
        Assert.Throws<ArgumentException>(()=>apartment.UpdateDetails("A","City","District","Address",true,null,PropertyType.SITE));
        apartment.UpdateDetails("A","City","District","Address",true,null,PropertyType.SITE,BuildingFeatures.None);Assert.Equal(PropertyType.SITE,apartment.Type);
    }
    [Theory][InlineData(1799)][InlineData(10000)] public void ConstructionYearOutsideDomainRangeIsRejected(int year) =>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new Property("A","City","District","Address",PropertyType.APARTMENT,5,new(year)));

    [Fact] public void UnitDetailsRequireNetAreaWithinGrossAndConsistentFloorPosition()
    {
        var property=new Property("A","City","District","Address",PropertyType.APARTMENT,5);
        var details=new UnitFeatures(95.5m,RoomLayout.R3_1,FloorPosition.REGULAR,false,2,KitchenType.OPEN,true,HeatingType.COMBI);
        var unit=property.AddUnit("1",2,120m,UnitType.APARTMENT,details);Assert.Equal(details,unit.Details);
        Assert.Throws<ArgumentOutOfRangeException>(()=>property.AddUnit("2",1,100m,UnitType.APARTMENT,new(100.01m)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>property.AddUnit("3",1,100m,UnitType.APARTMENT,new(50.555m)));
        Assert.Throws<ArgumentException>(()=>property.AddUnit("4",1,100m,UnitType.APARTMENT,new(FloorPosition:FloorPosition.GROUND)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>property.AddUnit("5",1,100m,UnitType.APARTMENT,new(BathroomCount:7)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>property.UpdateUnit(unit.Id,"1",2,90m,true));
        Assert.Equal(120m,unit.GrossSquareMeters);
        property.UpdateUnit(unit.Id,"1",2,90m,true,UnitType.APARTMENT,details with {NetSquareMeters=80m});Assert.Equal(80m,unit.NetSquareMeters);Assert.Equal(RoomLayout.R3_1,unit.RoomLayout);
        property.UpdateUnit(unit.Id,"1",2,90m,true);Assert.Equal(80m,unit.NetSquareMeters);
    }
    [Theory]
    [InlineData(FloorPosition.BELOW_ENTRANCE,-2,true)][InlineData(FloorPosition.BASEMENT,0,false)][InlineData(FloorPosition.GARDEN,0,true)]
    [InlineData(FloorPosition.HIGH_ENTRANCE,1,false)][InlineData(FloorPosition.REGULAR,0,false)][InlineData(FloorPosition.ROOF,6,true)]
    public void FloorPositionMatchesFloorNumber(FloorPosition position,int floor,bool expected) => Assert.Equal(expected,UnitFeatures.FloorMatches(position,floor));

    [Fact] public void RequestValidationReturnsFieldCodes()
    {
        Dictionary<string,ApiErrorMessage[]> errors=[];
        PropertyDetails.Validate(errors,new BuildingFeatureInput(2031,"combi",null,"GARAGE"),2026);
        PropertyDetails.Validate(errors,new UnitFeatureInput(130m,"3+1","GROUND",null,7,"AMERICAN",null,"1"),2,120m);
        Assert.Equal("BF_CONSTRUCTION_YEAR_INVALID",errors["constructionYear"][0].Code);Assert.Equal("BF_HEATING_TYPE_INVALID",errors["heatingType"][0].Code);
        Assert.Equal("BF_PARKING_TYPE_INVALID",errors["parkingType"][0].Code);Assert.Equal("BF_NET_AREA_INVALID",errors["netSquareMeters"][0].Code);
        Assert.Equal("BF_ROOM_LAYOUT_INVALID",errors["roomLayout"][0].Code);Assert.Equal("BF_FLOOR_POSITION_INVALID",errors["floorPosition"][0].Code);
        Assert.Equal("BF_BATHROOM_COUNT_INVALID",errors["bathroomCount"][0].Code);Assert.Equal("BF_KITCHEN_TYPE_INVALID",errors["kitchenType"][0].Code);
        errors.Clear();PropertyDetails.Validate(errors,new UnitFeatureInput(50m,null,null,null,null,null,null,null),1,null);Assert.Equal("BF_NET_AREA_INVALID",errors["netSquareMeters"][0].Code);
        errors.Clear();PropertyDetails.Validate(errors,new BuildingFeatureInput(2026,"CENTRAL_METERED",false,"NONE"),2026);
        PropertyDetails.Validate(errors,new UnitFeatureInput(120m,"R6_PLUS","ROOF",true,6,"CLOSED",false,"UNDERFLOOR"),5,120m);Assert.Empty(errors);
        var exception=Assert.Throws<RequestValidationException>(()=>PropertyDetails.ValidateFloorWithinBuilding(6,5));Assert.Equal("BF_FLOOR_EXCEEDS_FLOOR_COUNT",exception.Issues["floor"][0].Code);
        PropertyDetails.ValidateFloorWithinBuilding(5,5);PropertyDetails.ValidateFloorWithinBuilding(40,null);
    }
    [Fact] public void BusinessYearUsesIstanbulTime() =>
        Assert.Equal(2027,PropertyDetails.CurrentYear(new FixedClock(new DateTimeOffset(2026,12,31,21,30,0,TimeSpan.Zero))));
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
