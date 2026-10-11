using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class PropertyDetailsApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private HttpClient Client(string? user=null,string role="SUPER_ADMIN")=>TestToken.Client(factory,user,role);
    private async Task<(Property Property,string Actor)> Seed(PropertyType type,int? floors=null)
    {
        var actor=Guid.NewGuid().ToString();var property=new Property("Details "+actor,"İstanbul","Kadıköy","Address",type,floors);
        await factory.WithDb(async db=>{db.Add(property);db.Add(new PropertyMembership(property.Id,actor));await db.SaveChangesAsync();});return(property,actor);
    }
    private static async Task<JsonElement> Problem(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        using var body=JsonDocument.Parse(await response.Content.ReadAsStringAsync());return body.RootElement.GetProperty("fieldIssues").Clone();
    }
    private static string Code(JsonElement issues,string field)=>issues.GetProperty(field)[0].GetProperty("code").GetString()!;

    [PostgresFact]
    public async Task ApartmentCreateUpdateAndListCarryBuildingFeatures()
    {
        var actor=Guid.NewGuid().ToString();using var client=Client(actor);
        var response=await client.PostAsJsonAsync("/api/v1/properties",new CreatePropertyRequest("Moda "+actor,"İstanbul","Kadıköy","Street 1","APARTMENT",6," Moda ",2010,"CENTRAL_METERED",true,"CLOSED"));
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);var created=(await response.Content.ReadFromJsonAsync<PropertyResponse>())!;
        Assert.Equal(("Moda",2010,"CENTRAL_METERED",true,"CLOSED"),(created.Neighborhood,created.ConstructionYear,created.HeatingType,created.HasElevator,created.ParkingType));
        var list=(await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?search="+Uri.EscapeDataString(actor)))!;var row=Assert.Single(list.Items);
        Assert.Equal(("Moda",2010,"CENTRAL_METERED",true,"CLOSED"),(row.Neighborhood,row.ConstructionYear,row.HeatingType,row.HasElevator,row.ParkingType));
        var edit=await client.PutAsJsonAsync($"/api/v1/properties/{created.Id}",new UpdatePropertyRequest(created.Name,"İstanbul","Kadıköy","Street 1",true,created.Version,6,null,null,DateTime.UtcNow.Year,"COMBI",false,"NONE"));
        Assert.Equal(HttpStatusCode.OK,edit.StatusCode);var updated=(await edit.Content.ReadFromJsonAsync<PropertyResponse>())!;
        Assert.Equal((null,DateTime.UtcNow.Year,"COMBI",false,"NONE"),(updated.Neighborhood,updated.ConstructionYear,updated.HeatingType,updated.HasElevator,updated.ParkingType));
        await factory.WithDb(async db=>{var saved=await db.Properties.AsNoTracking().SingleAsync(value=>value.Id==created.Id);Assert.Equal(HeatingType.COMBI,saved.HeatingType);Assert.Equal(actor,saved.ModifiedBy);});
    }

    [PostgresFact]
    public async Task InvalidBuildingFeaturesAreRejectedWithFieldCodes()
    {
        var actor=Guid.NewGuid().ToString();using var client=Client(actor);
        var issues=await Problem(await client.PostAsJsonAsync("/api/v1/properties",new {name="Bad",city="City",district="District",address="Address",type="APARTMENT",constructionYear=DateTime.UtcNow.Year+2,heatingType="combi",parkingType="1",neighborhood=new string('x',101)}));
        Assert.Equal("BF_CONSTRUCTION_YEAR_INVALID",Code(issues,"constructionYear"));Assert.Equal("BF_HEATING_TYPE_INVALID",Code(issues,"heatingType"));
        Assert.Equal("BF_PARKING_TYPE_INVALID",Code(issues,"parkingType"));Assert.Equal("BF_FIELD_TOO_LONG",Code(issues,"neighborhood"));
        issues=await Problem(await client.PostAsJsonAsync("/api/v1/properties",new CreatePropertyRequest("Site","City","District","Address","SITE",null,null,null,"COMBI")));
        Assert.Equal("BF_BUILDING_FEATURES_APARTMENT_ONLY",Code(issues,"buildingFeatures"));
        var x=await Seed(PropertyType.APARTMENT,5);
        issues=await Problem(await client.PutAsJsonAsync($"/api/v1/properties/{x.Property.Id}",new UpdatePropertyRequest(x.Property.Name,"City","District","Address",true,x.Property.Version,null,"SITE",null,null,null,true)));
        Assert.Equal("BF_BUILDING_FEATURES_APARTMENT_ONLY",Code(issues,"buildingFeatures"));
        var site=await Seed(PropertyType.SITE);
        issues=await Problem(await client.PutAsJsonAsync($"/api/v1/properties/{site.Property.Id}",new UpdatePropertyRequest(site.Property.Name,"City","District","Address",true,site.Property.Version,null,null,null,1990)));
        Assert.Equal("BF_BUILDING_FEATURES_APARTMENT_ONLY",Code(issues,"buildingFeatures"));
        await factory.WithDb(async db=>Assert.Null((await db.Properties.AsNoTracking().SingleAsync(value=>value.Id==site.Property.Id)).ConstructionYear));
    }

    [PostgresTheory][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task SiteBuildingsCarryFeaturesAndUnitsCarryDetails(string role)
    {
        var x=await Seed(PropertyType.SITE);using var client=Client(x.Actor,role);var root=$"/api/v1/properties/{x.Property.Id}/buildings";
        var response=await client.PostAsJsonAsync(root,new CreateBuildingRequest("A",4,1998,"FLOOR_HEATER",false,"OPEN"));Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var building=(await response.Content.ReadFromJsonAsync<BuildingResponse>())!;Assert.Equal((1998,"FLOOR_HEATER",false,"OPEN"),(building.ConstructionYear,building.HeatingType,building.HasElevator,building.ParkingType));
        var listed=Assert.Single((await client.GetFromJsonAsync<PagedRows<BuildingResponse>>(root))!.Items);Assert.Equal("FLOOR_HEATER",listed.HeatingType);
        var units=$"{root}/{building.Id}/units";
        response=await client.PostAsJsonAsync(units,new CreateUnitRequest("1",0,110.5m,"APARTMENT",95.25m,"R2_1","HIGH_ENTRANCE",true,1,"OPEN",true,"COMBI"));
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);var unit=(await response.Content.ReadFromJsonAsync<UnitResponse>())!;
        Assert.Equal((95.25m,"R2_1","HIGH_ENTRANCE",true,1,"OPEN",true,"COMBI"),(unit.NetSquareMeters,unit.RoomLayout,unit.FloorPosition,unit.IsMezzanine,unit.BathroomCount,unit.KitchenType,unit.HasBalcony,unit.HeatingType));
        var row=Assert.Single((await client.GetFromJsonAsync<PagedRows<UnitResponse>>(units))!.Items);Assert.Equal("R2_1",row.RoomLayout);Assert.Equal(95.25m,row.NetSquareMeters);
        var edit=await client.PutAsJsonAsync($"{units}/{unit.Id}",new UpdateUnitRequest("1",4,100m,true,unit.Version,"APARTMENT",null,"R6_PLUS","ROOF",null,6,"CLOSED",false));
        Assert.Equal(HttpStatusCode.OK,edit.StatusCode);unit=(await edit.Content.ReadFromJsonAsync<UnitResponse>())!;
        Assert.Equal((null,"R6_PLUS","ROOF",null,6,"CLOSED",false,null),(unit.NetSquareMeters,unit.RoomLayout,unit.FloorPosition,unit.IsMezzanine,unit.BathroomCount,unit.KitchenType,unit.HasBalcony,unit.HeatingType));
        var issues=await Problem(await client.PutAsJsonAsync($"{units}/{unit.Id}",new UpdateUnitRequest("1",5,100m,true,unit.Version)));
        Assert.Equal("BF_FLOOR_EXCEEDS_FLOOR_COUNT",Code(issues,"floor"));
    }

    [PostgresFact]
    public async Task InvalidUnitDetailsAreRejectedBeforePersistence()
    {
        var x=await Seed(PropertyType.APARTMENT,3);using var client=Client(x.Actor,"MANAGER");var url=$"/api/v1/properties/{x.Property.Id}/units";
        var issues=await Problem(await client.PostAsJsonAsync(url,new {number="1",floor=1,grossSquareMeters=100m,type="APARTMENT",netSquareMeters=100.5m,roomLayout="3+1",floorPosition="GROUND",bathroomCount=7,kitchenType="AMERICAN",heatingType="Combi"}));
        foreach(var (field,code) in new[]{("netSquareMeters","BF_NET_AREA_INVALID"),("roomLayout","BF_ROOM_LAYOUT_INVALID"),("floorPosition","BF_FLOOR_POSITION_INVALID"),("bathroomCount","BF_BATHROOM_COUNT_INVALID"),("kitchenType","BF_KITCHEN_TYPE_INVALID"),("heatingType","BF_HEATING_TYPE_INVALID")})
            Assert.Equal(code,Code(issues,field));
        issues=await Problem(await client.PostAsJsonAsync(url,new {number="1",floor=1,type="APARTMENT",netSquareMeters=50m}));Assert.Equal("BF_NET_AREA_INVALID",Code(issues,"netSquareMeters"));
        issues=await Problem(await client.PostAsJsonAsync(url,new CreateUnitRequest("1",4,100m)));Assert.Equal("BF_FLOOR_EXCEEDS_FLOOR_COUNT",Code(issues,"floor"));
        Assert.Equal(HttpStatusCode.Created,(await client.PostAsJsonAsync(url,new CreateUnitRequest("1",3,100m,"APARTMENT",100m,"STUDIO","ROOF"))).StatusCode);
        await factory.WithDb(async db=>Assert.Equal(1,await db.Units.CountAsync(value=>value.PropertyId==x.Property.Id)));
    }

    [PostgresFact]
    public async Task DatabaseConstraintsGuardDetailInvariants()
    {
        var x=await Seed(PropertyType.SITE);Unit? unit=null;
        await factory.WithDb(async db=>{var site=await db.Properties.Include(value=>value.Buildings).SingleAsync(value=>value.Id==x.Property.Id);var building=site.AddBuilding("A");db.Buildings.Add(building);unit=building.AddUnit("1",2,100m);db.Units.Add(unit);await db.SaveChangesAsync();});
        foreach(var sql in new[]{
            $"UPDATE units SET net_square_meters = 100.01 WHERE id = '{unit!.Id}'",$"UPDATE units SET floor_position = 'GROUND' WHERE id = '{unit.Id}'",
            $"UPDATE units SET bathroom_count = 7 WHERE id = '{unit.Id}'",$"UPDATE units SET room_layout = '3+1' WHERE id = '{unit.Id}'",
            $"UPDATE properties SET heating_type = 'COMBI' WHERE id = '{x.Property.Id}'",$"UPDATE buildings SET parking_type = 'GARAGE' WHERE property_id = '{x.Property.Id}'"})
            await factory.WithDb(async db=>await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync(sql)));
    }
}
