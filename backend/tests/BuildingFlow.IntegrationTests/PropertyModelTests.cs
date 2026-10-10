using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class PropertyModelTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private HttpClient Client(string? user=null,string role="SUPER_ADMIN")=>TestToken.Client(factory,user,role);
    private async Task<(Property Property,string Actor)> Apartment()
    {
        var actor=Guid.NewGuid().ToString();var property=new Property("ABC Apartmanı","İstanbul","Kadıköy","Test",PropertyType.APARTMENT,8);
        await factory.WithDb(async db=>{db.Add(property);db.Add(new PropertyMembership(property.Id,actor));await db.SaveChangesAsync();});return(property,actor);
    }
    [PostgresTheory][InlineData("APARTMENT")][InlineData("SITE")][InlineData("RESIDENCE")][InlineData("MIXED")]
    public async Task FourTypesCreateWithAccurateMetadataAndCounts(string type)
    {
        using var client=Client(Guid.NewGuid().ToString());var response=await client.PostAsJsonAsync("/api/v1/properties",new CreatePropertyRequest("Test","City","District","Address",type,type=="APARTMENT"?8:null));
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);var property=(await response.Content.ReadFromJsonAsync<PropertyResponse>())!;
        Assert.Equal(type,property.Type);Assert.Equal(type=="APARTMENT"?8:null,property.FloorCount);Assert.Equal(0,property.BuildingCount);Assert.Equal(0,property.UnitCount);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/v1/properties/{property.Id}/"+(type=="APARTMENT"?"buildings":"units"),type=="APARTMENT"?(object)new CreateBuildingRequest("Artificial"):new CreateUnitRequest("1",1,100))).StatusCode);
    }
    [PostgresFact]
    public async Task ApartmentDirectUnitCrudNeverCreatesArtificialBuilding()
    {
        var x=await Apartment();using var client=Client(x.Actor,"SITE_ADMIN");var url=$"/api/v1/properties/{x.Property.Id}/units";
        var response=await client.PostAsJsonAsync(url,new {number=" 1 ",floor=-1,grossSquareMeters=120.50m,type="APARTMENT",buildingId=Guid.NewGuid(),propertyId=Guid.NewGuid(),version=999,createdBy="attacker"});
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);var unit=(await response.Content.ReadFromJsonAsync<UnitResponse>())!;
        Assert.Null(unit.BuildingId);Assert.Equal(x.Property.Id,unit.PropertyId);Assert.Equal(url+"/"+unit.Id,response.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(url,new CreateUnitRequest("1",2,100))).StatusCode);
        var list=(await client.GetFromJsonAsync<PagedRows<UnitResponse>>(url+"?sortBy=floor"))!;Assert.Equal(unit.Id,Assert.Single(list.Items).Id);
        var property=(await client.GetFromJsonAsync<PropertyResponse>($"/api/v1/properties/{x.Property.Id}"))!;Assert.Equal(0,property.BuildingCount);Assert.Equal(1,property.UnitCount);Assert.Equal(8,property.FloorCount);
        var edit=await client.PutAsJsonAsync(response.Headers.Location,new UpdateUnitRequest("2",2,130.25m,false,1));Assert.Equal(HttpStatusCode.OK,edit.StatusCode);unit=(await edit.Content.ReadFromJsonAsync<UnitResponse>())!;
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(response.Headers.Location,new UpdateUnitRequest("Stale",3,100,true,1))).StatusCode);
        using var global=Client(x.Actor);Assert.Equal(HttpStatusCode.Conflict,(await global.DeleteAsync($"/api/v1/properties/{x.Property.Id}?version={property.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(url+"/"+unit.Id+"?version="+unit.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(url+"/"+unit.Id)).StatusCode);
        await factory.WithDb(async db=>{Assert.False(await db.Buildings.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==x.Property.Id));var saved=await db.Units.IgnoreQueryFilters().SingleAsync(value=>value.Id==unit.Id);Assert.True(saved.IsDeleted);Assert.Null(saved.BuildingId);Assert.Equal(x.Actor,saved.CreatedBy);Assert.Equal(x.Actor,saved.ModifiedBy);});
    }
    [PostgresTheory][InlineData("OFFICE")][InlineData("SHOP")][InlineData("VILLA")][InlineData("WAREHOUSE")][InlineData("WORKSHOP")][InlineData("APARTMENT")]
    public async Task MixedBuildingSupportsSharedUnitTypes(string type)
    {
        var property=new Property("Mixed","City","District","Address",PropertyType.MIXED);var building=property.AddBuilding("Tower",10);var actor=Guid.NewGuid().ToString();
        await factory.WithDb(async db=>{db.Add(property);await db.SaveChangesAsync();});using var client=Client(actor);
        var response=await client.PostAsJsonAsync($"/api/v1/properties/{property.Id}/buildings/{building.Id}/units",new CreateUnitRequest("1",1,100,type));Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var unit=(await response.Content.ReadFromJsonAsync<UnitResponse>())!;Assert.Equal(type,unit.Type);Assert.Equal(property.Id,unit.PropertyId);Assert.Equal(building.Id,unit.BuildingId);
    }
    [PostgresFact]
    public async Task ApartmentRejectsNonApartmentUnitAndPopulatedTypeChange()
    {
        var x=await Apartment();using var client=Client(x.Actor);var root=$"/api/v1/properties/{x.Property.Id}";
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(root+"/units",new CreateUnitRequest("1",1,100,"OFFICE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await client.PostAsJsonAsync(root+"/units",new CreateUnitRequest("1",1,100))).StatusCode);
        var before=(await client.GetFromJsonAsync<PropertyResponse>(root))!;
        var response=await client.PutAsJsonAsync(root,new{name="Renamed",city="City",district="District",address="Address",isEnabled=true,version=before.Version,floorCount=(int?)null,type="SITE"});Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);
        var property=(await client.GetFromJsonAsync<PropertyResponse>(root))!;Assert.Equal("APARTMENT",property.Type);Assert.Equal(8,property.FloorCount);Assert.Equal(before.Name,property.Name);Assert.Equal(before.Version,property.Version);
    }
    [PostgresFact]
    public async Task DirectRoutesEnforceAuthenticationRoleAndMembershipScope()
    {
        var x=await Apartment();var root=$"/api/v1/properties/{x.Property.Id}/units";using var anonymous=Client();
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(root)).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PostAsJsonAsync(root,new{})).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(root+"/"+Guid.NewGuid())).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PutAsJsonAsync(root+"/"+Guid.NewGuid(),new{})).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.DeleteAsync(root+"/"+Guid.NewGuid()+"?version=1")).StatusCode);
        using var resident=Client(x.Actor,"RESIDENT");Assert.Equal(HttpStatusCode.OK,(await resident.GetAsync(root)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await resident.PostAsJsonAsync(root,new CreateUnitRequest("1",1,100))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await resident.PutAsJsonAsync(root+"/"+Guid.NewGuid(),new{})).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await resident.DeleteAsync(root+"/"+Guid.NewGuid()+"?version=1")).StatusCode);
        using var outsider=Client(Guid.NewGuid().ToString(),"MANAGER");Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(root+"?readAll=true")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await outsider.PostAsJsonAsync(root,new CreateUnitRequest("1",1,100))).StatusCode);
    }
    [PostgresTheory][InlineData("unknown",null)][InlineData("APARTMENT",0)][InlineData("SITE",8)][InlineData("1",null)]
    public async Task InvalidTypeOrFloorMetadataDoesNotCreateProperty(string type,int? floors)
    {
        using var client=Client(Guid.NewGuid().ToString());Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/v1/properties",new CreatePropertyRequest("Test","City","District","Address",type,floors))).StatusCode);
    }
    [PostgresFact]
    public async Task DatabaseEnforcesDirectUnitNumberUniquenessWithoutLoadedCollection()
    {
        var x=await Apartment();
        await factory.WithDb(async db=>{var property=await db.Properties.SingleAsync(value=>value.Id==x.Property.Id);property.AddUnit("A1",1,100);await db.SaveChangesAsync();});
        await factory.WithDb(async db=>{
            // No Units Include: the PostgreSQL partial unique index is the final guard.
            var property=await db.Properties.SingleAsync(value=>value.Id==x.Property.Id);property.AddUnit("a1",2,120);
            var error=await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());var postgres=Assert.IsType<PostgresException>(error.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation,postgres.SqlState);Assert.Equal("IX_units_property_id_number",postgres.ConstraintName);
        });
        await factory.WithDb(async db=>Assert.Equal(1,await db.Units.CountAsync(value=>value.PropertyId==x.Property.Id)));
    }
    [PostgresFact]
    public async Task DatabaseCompositeForeignKeyRejectsBuildingFromAnotherProperty()
    {
        var one=new Property("One","City","District","Address");var two=new Property("Two","City","District","Address");var building=one.AddBuilding("A");var unit=building.AddUnit("1",1,100);
        await factory.WithDb(async db=>{db.AddRange(one,two);await db.SaveChangesAsync();});
        // property_id is now part of the occupancy principal key; EF correctly
        // forbids editing it, so raw SQL exercises the actual database FK here.
        await factory.WithDb(async db=>{var error=await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE units SET property_id={two.Id} WHERE id={unit.Id}"));Assert.Equal(PostgresErrorCodes.ForeignKeyViolation,error.SqlState);});
        await factory.WithDb(async db=>Assert.Equal(one.Id,(await db.Units.SingleAsync(value=>value.Id==unit.Id)).PropertyId));
    }
}
