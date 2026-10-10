using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class PropertyTypeChangeTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private HttpClient Client(string? actor=null,string role="SUPER_ADMIN")=>TestToken.Client(factory,actor,role);
    private async Task<(Property Property,string Actor)> Seed(PropertyType type=PropertyType.SITE)
    {
        var actor=Guid.NewGuid().ToString();
        var property=new Property("Original","City","District","Address",type,type==PropertyType.APARTMENT?8:null);
        await factory.WithDb(async db=>{db.Add(property);db.Add(new PropertyMembership(property.Id,actor));await db.SaveChangesAsync();});
        return(property,actor);
    }
    private static string Url(Property property)=>$"/api/v1/properties/{property.Id}";
    private static UpdatePropertyRequest Edit(Property property,string? type,long? version=null,int? floors=null)=>new("Updated","City","District","Address",true,version ?? property.Version,floors,type);

    [PostgresTheory]
    [InlineData(PropertyType.SITE,"APARTMENT",8)]
    [InlineData(PropertyType.APARTMENT,"SITE",null)]
    [InlineData(PropertyType.SITE,"RESIDENCE",null)]
    [InlineData(PropertyType.RESIDENCE,"MIXED",null)]
    public async Task EmptyPropertyCanChangeTypePreservingIdentityAuditAndMembership(PropertyType initial,string next,int? floors)
    {
        var x=await Seed(initial);using var client=Client(x.Actor);
        var before=(await client.GetFromJsonAsync<PropertyResponse>(Url(x.Property)))!;Assert.True(before.CanChangeType);
        var response=await client.PutAsJsonAsync(Url(x.Property),Edit(x.Property,next,floors:floors));Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var updated=(await response.Content.ReadFromJsonAsync<PropertyResponse>())!;
        Assert.Equal(next,updated.Type);Assert.Equal(floors,updated.FloorCount);Assert.Equal(before.Version+1,updated.Version);Assert.Equal(before.Id,updated.Id);Assert.Equal(before.CreatedAt,updated.CreatedAt);Assert.True(updated.CanChangeType);Assert.Equal(0,updated.BuildingCount);Assert.Equal(0,updated.UnitCount);
        var list=(await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?search=Updated&pageSize=100"))!;Assert.True(list.Items.Single(value=>value.Id==updated.Id).CanChangeType);
        await factory.WithDb(async db=>{var saved=await db.Properties.SingleAsync(value=>value.Id==updated.Id);Assert.Equal(x.Property.CreatedBy,saved.CreatedBy);Assert.Equal(x.Actor,saved.ModifiedBy);Assert.Single(await db.PropertyMemberships.Where(value=>value.PropertyId==updated.Id && value.UserId==x.Actor).ToListAsync());Assert.False(await db.Buildings.AnyAsync(value=>value.PropertyId==updated.Id));Assert.False(await db.Units.AnyAsync(value=>value.PropertyId==updated.Id));});
        var childResponse=next=="APARTMENT"?await client.PostAsJsonAsync(Url(x.Property)+"/units",new CreateUnitRequest("1",1,100)):await client.PostAsJsonAsync(Url(x.Property)+"/buildings",new CreateBuildingRequest("A"));
        Assert.Equal(HttpStatusCode.Created,childResponse.StatusCode);
    }

    [PostgresTheory]
    [InlineData(false,"active")][InlineData(false,"disabled")][InlineData(false,"deleted")]
    [InlineData(true,"active")][InlineData(true,"disabled")][InlineData(true,"deleted")]
    public async Task AnyBuildingOrUnitHistoryLocksTypeWithoutPartialUpdates(bool directUnit,string state)
    {
        var x=await Seed(directUnit?PropertyType.APARTMENT:PropertyType.SITE);
        await factory.WithDb(async db=>{
            var property=await db.Properties.SingleAsync(value=>value.Id==x.Property.Id);
            BaseEntity child=directUnit?property.AddUnit("1",1,100):property.AddBuilding("A");db.Add(child);
            if(state=="disabled")child.SetEnabled(false);await db.SaveChangesAsync();
            if(state=="deleted"){db.Remove(child);await db.SaveChangesAsync();}
        });
        using var client=Client(x.Actor);var before=(await client.GetFromJsonAsync<PropertyResponse>(Url(x.Property)))!;
        Assert.False(before.CanChangeType);if(state=="deleted"){Assert.Equal(0,before.BuildingCount);Assert.Equal(0,before.UnitCount);}
        var list=(await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?search=Original&pageSize=100"))!;Assert.False(list.Items.Single(value=>value.Id==before.Id).CanChangeType);
        var response=await client.PutAsJsonAsync(Url(x.Property),Edit(x.Property,"MIXED",before.Version));Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);
        Assert.Contains("geçmişi",await response.Content.ReadAsStringAsync());
        var after=(await client.GetFromJsonAsync<PropertyResponse>(Url(x.Property)))!;Assert.Equal(before,after);
    }

    [PostgresTheory][InlineData("")][InlineData("unknown")][InlineData("1")][InlineData("apartment")]
    public async Task InvalidUpdateTypeReturnsFieldErrorAndDoesNotMutate(string type)
    {
        var x=await Seed();using var client=Client(x.Actor);var response=await client.PutAsJsonAsync(Url(x.Property),Edit(x.Property,type));
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Contains("\"type\"",await response.Content.ReadAsStringAsync());
        await factory.WithDb(async db=>{var saved=await db.Properties.SingleAsync(value=>value.Id==x.Property.Id);Assert.Equal(PropertyType.SITE,saved.Type);Assert.Equal("Original",saved.Name);Assert.Equal(x.Property.Version,saved.Version);});
    }
    [PostgresTheory][InlineData("SITE_ADMIN")][InlineData("MANAGER")][InlineData("RESIDENT")]
    public async Task TypeChangeStillRequiresSuperAdmin(string role)
    {
        var x=await Seed();using var client=Client(x.Actor,role);Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync(Url(x.Property),Edit(x.Property,"APARTMENT"))).StatusCode);
    }
    [PostgresFact]
    public async Task TypeChangeRequiresAuthenticationAndCurrentVersion()
    {
        var x=await Seed();using var anonymous=Client();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PutAsJsonAsync(Url(x.Property),Edit(x.Property,"APARTMENT"))).StatusCode);
        using var client=Client(x.Actor);Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(Url(x.Property),Edit(x.Property,"MIXED"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(Url(x.Property),Edit(x.Property,"APARTMENT",floors:8))).StatusCode);
        await factory.WithDb(async db=>Assert.Equal(PropertyType.MIXED,(await db.Properties.SingleAsync(value=>value.Id==x.Property.Id)).Type));
    }
    [PostgresFact]
    public async Task SameTypeAndOmittedTypeStillAllowOrdinaryEditsWithChildren()
    {
        var x=await Seed();using var client=Client(x.Actor);Assert.Equal(HttpStatusCode.Created,(await client.PostAsJsonAsync(Url(x.Property)+"/buildings",new CreateBuildingRequest("A"))).StatusCode);
        var current=(await client.GetFromJsonAsync<PropertyResponse>(Url(x.Property)))!;
        foreach(var type in new string?[]{"SITE",null}){var response=await client.PutAsJsonAsync(Url(x.Property),Edit(x.Property,type,current.Version));Assert.Equal(HttpStatusCode.OK,response.StatusCode);current=(await response.Content.ReadFromJsonAsync<PropertyResponse>())!;Assert.Equal("SITE",current.Type);Assert.False(current.CanChangeType);Assert.Equal(1,current.BuildingCount);}
    }
    [PostgresFact]
    public async Task ChildCommittedDuringStaleTypeEditPreventsConversion()
    {
        var x=await Seed();using var scope=factory.Services.CreateScope();var stale=scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>();
        await stale.Properties.Include(value=>value.Buildings).Include(value=>value.Units).SingleAsync(value=>value.Id==x.Property.Id);
        using var client=Client(x.Actor);Assert.Equal(HttpStatusCode.Created,(await client.PostAsJsonAsync(Url(x.Property)+"/buildings",new CreateBuildingRequest("Concurrent"))).StatusCode);
        var store=new HierarchyStore(stale);await Assert.ThrowsAsync<HierarchyConflictException>(()=>store.UpdatePropertyAsync(x.Property.Id,Edit(x.Property,"APARTMENT",floors:8),new(x.Actor,true),default));
        await factory.WithDb(async db=>{var saved=await db.Properties.SingleAsync(value=>value.Id==x.Property.Id);Assert.Equal(PropertyType.SITE,saved.Type);Assert.Equal("Original",saved.Name);Assert.Equal(1,await db.Buildings.CountAsync(value=>value.PropertyId==saved.Id));});
    }
    [PostgresFact]
    public async Task TypeChangeCommittedDuringStaleChildInsertRollsBackChild()
    {
        var x=await Seed(PropertyType.APARTMENT);using var scope=factory.Services.CreateScope();var stale=scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>();
        await stale.Properties.Include(value=>value.Buildings).Include(value=>value.Units).SingleAsync(value=>value.Id==x.Property.Id);
        using var client=Client(x.Actor);Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(Url(x.Property),Edit(x.Property,"SITE"))).StatusCode);
        var store=new HierarchyStore(stale);await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>store.CreateUnitAsync(x.Property.Id,null,new CreateUnitRequest("Race",1,100),new(x.Actor,true),default));
        await factory.WithDb(async db=>{var saved=await db.Properties.SingleAsync(value=>value.Id==x.Property.Id);Assert.Equal(PropertyType.SITE,saved.Type);Assert.Equal(x.Property.Version+1,saved.Version);Assert.False(await db.Units.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==saved.Id));});
    }
}
