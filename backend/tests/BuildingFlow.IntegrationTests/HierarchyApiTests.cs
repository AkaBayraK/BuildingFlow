using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class HierarchyApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private HttpClient Client(string? actor=null,string role="SUPER_ADMIN",bool subject=true)
    {
        var client=factory.CreateClient();if(actor is null)return client;
        List<Claim> claims=[];if(subject)claims.Add(new("sub",actor));
        if(role=="spoofed")claims.Add(new("buildingflow_role","SUPER_ADMIN"));
        else if(role=="client-role")claims.Add(new("resource_access","{\"buildingflow-web\":{\"roles\":[\"SUPER_ADMIN\"]}}",JsonClaimValueTypes.Json));
        else claims.Add(new("realm_access",JsonSerializer.Serialize(new {roles=new[]{role}}),JsonClaimValueTypes.Json));
        var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('s',64))){KeyId="sites-test-only"};
        var jwt=new JwtSecurityToken("http://localhost:8080/realms/buildingflow","buildingflow-api",claims,DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5),new SigningCredentials(key,SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization=new("Bearer",new JwtSecurityTokenHandler().WriteToken(jwt));return client;
    }
    private async Task<(string Actor,Property Property,Building Building,Unit Unit)> Seed()
    {
        var actor=Guid.NewGuid().ToString();var site=new Property("Hierarchy "+actor,"City","District","Private address");
        var block=site.AddBuilding("A");var apartment=block.AddUnit("1",-1,120.25m);
        await factory.WithDb(async db=>{db.Add(site);db.Add(new PropertyMembership(site.Id,actor));await db.SaveChangesAsync();});
        return(actor,site,block,apartment);
    }
    private static string SiteUrl(Property value)=>$"/api/v1/properties/{value.Id}";
    private static string BlockUrl(Property site,Building block)=>$"{SiteUrl(site)}/buildings/{block.Id}";
    private static string ApartmentUrl(Property site,Building block,Unit apartment)=>$"{BlockUrl(site,block)}/units/{apartment.Id}";
    private static UpdatePropertyRequest SiteEdit(Property value,long version,bool enabled=true)=>new(value.Name,value.City,value.District,value.Address,enabled,version);

    [PostgresFact]
    public async Task AnonymousCannotAccessAnyHierarchyRoute()
    {
        using var client=Client();var s=Guid.NewGuid();var b=Guid.NewGuid();var a=Guid.NewGuid();
        foreach(var path in new[]{$"/api/v1/properties/{s}/buildings",$"/api/v1/properties/{s}/buildings/{b}",$"/api/v1/properties/{s}/buildings/{b}/units",$"/api/v1/properties/{s}/buildings/{b}/units/{a}"})
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync(path)).StatusCode);
        foreach(var path in new[]{$"/api/v1/properties/{s}/buildings",$"/api/v1/properties/{s}/buildings/{b}/units"})
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync(path,new{})).StatusCode);
        foreach(var path in new[]{$"/api/v1/properties/{s}",$"/api/v1/properties/{s}/buildings/{b}",$"/api/v1/properties/{s}/buildings/{b}/units/{a}"}) {
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.PutAsJsonAsync(path,new{})).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.DeleteAsync(path+"?version=1")).StatusCode);
        }
    }
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task WritersCanCreateEditAndSoftDeleteWithTrustedAudit(string role)
    {
        var x=await Seed();using var client=Client(x.Actor,role);
        using var response=await client.PostAsJsonAsync(SiteUrl(x.Property)+"/buildings",new{name=" B ",id=Guid.NewGuid(),version=999,isDeleted=true,createdBy="attacker",siteId=Guid.NewGuid()});
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);var block=(await response.Content.ReadFromJsonAsync<BuildingResponse>())!;
        Assert.Equal("B",block.Name);Assert.Equal(1,block.Version);Assert.Equal(x.Property.Id,block.PropertyId);Assert.Equal(SiteUrl(x.Property)+"/buildings/"+block.Id,response.Headers.Location!.ToString());
        var url=response.Headers.Location.ToString();
        var apartmentResponse=await client.PostAsJsonAsync(url+"/units",new{number=" 2 ",floor=-2,grossSquareMeters=88.50m,blockId=x.Building.Id,createdBy="attacker"});
        Assert.Equal(HttpStatusCode.Created,apartmentResponse.StatusCode);var apartment=(await apartmentResponse.Content.ReadFromJsonAsync<UnitResponse>())!;
        Assert.Equal(block.Id,apartment.BuildingId);Assert.Equal(-2,apartment.Floor);Assert.Equal(88.5m,apartment.GrossSquareMeters);
        var edit=await client.PutAsJsonAsync(url+"/units/"+apartment.Id,new UpdateUnitRequest(" 2B ",3,90m,false,apartment.Version));
        Assert.Equal(HttpStatusCode.OK,edit.StatusCode);apartment=(await edit.Content.ReadFromJsonAsync<UnitResponse>())!;
        Assert.Equal("2B",apartment.Number);Assert.Equal(2,apartment.Version);Assert.False(apartment.IsEnabled);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(url+"/units/"+apartment.Id+"?version="+apartment.Version)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(url+"/units/"+apartment.Id)).StatusCode);
        block=(await client.GetFromJsonAsync<BuildingResponse>(url))!;Assert.Equal(0,block.UnitCount);
        var blockEdit=await client.PutAsJsonAsync(url,new UpdateBuildingRequest(" C ",false,block.Version));Assert.Equal(HttpStatusCode.OK,blockEdit.StatusCode);
        block=(await blockEdit.Content.ReadFromJsonAsync<BuildingResponse>())!;Assert.Equal("C",block.Name);Assert.False(block.IsEnabled);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(url+"?version="+block.Version)).StatusCode);
        await factory.WithDb(async db=>{
            var saved=await db.Units.IgnoreQueryFilters().SingleAsync(value=>value.Id==apartment.Id);
            Assert.True(saved.IsDeleted);Assert.Equal(x.Actor,saved.CreatedBy);Assert.Equal(x.Actor,saved.ModifiedBy);Assert.Equal(3,saved.Version);Assert.Equal(apartment.CreatedAt,saved.CreatedAt);
            Assert.True((await db.Buildings.IgnoreQueryFilters().SingleAsync(value=>value.Id==block.Id)).IsDeleted);
            Assert.True((await db.Properties.SingleAsync(value=>value.Id==x.Property.Id)).Version>x.Property.Version);
        });
    }
    [PostgresTheory][InlineData("RESIDENT")][InlineData("ACCOUNTANT")][InlineData("")][InlineData("spoofed")][InlineData("client-role")]
    public async Task ReadOnlyOrForgedRolesCannotWrite(string role)
    {
        var x=await Seed();using var client=Client(x.Actor,role);var b=BlockUrl(x.Property,x.Building);var a=ApartmentUrl(x.Property,x.Building,x.Unit);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(b)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(a)).StatusCode);
        foreach(var path in new[]{SiteUrl(x.Property)+"/buildings",b+"/units"})Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(path,new{})).StatusCode);
        foreach(var path in new[]{SiteUrl(x.Property),b,a}){
            Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync(path,new{})).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await client.DeleteAsync(path+"?version=1")).StatusCode);
        }
    }
    [PostgresTheory][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task SiteEditsRemainGlobalAdminOnly(string role)
    {
        var x=await Seed();using var client=Client(x.Actor,role);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync(SiteUrl(x.Property),SiteEdit(x.Property,1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.DeleteAsync(SiteUrl(x.Property)+"?version=1")).StatusCode);
    }
    [PostgresFact]
    public async Task CrossSiteAndWrongParentRoutesCannotReadOrMutate()
    {
        var x=await Seed();var other=await Seed();using var outsider=Client(other.Actor,"SITE_ADMIN");
        var b=BlockUrl(x.Property,x.Building);var a=ApartmentUrl(x.Property,x.Building,x.Unit);
        foreach(var path in new[]{SiteUrl(x.Property)+"/buildings",b,b+"/units",a})Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(path+"?readAll=true&userId="+x.Actor)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.PostAsJsonAsync(SiteUrl(x.Property)+"/buildings",new CreateBuildingRequest("X"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.PostAsJsonAsync(b+"/units",new CreateUnitRequest("X",1,100))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.PutAsJsonAsync(b,new UpdateBuildingRequest("X",true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.PutAsJsonAsync(a,new UpdateUnitRequest("X",1,100,true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.DeleteAsync(b+"?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.DeleteAsync(a+"?version=1")).StatusCode);
        using var global=Client(x.Actor);var wrong=BlockUrl(other.Property,x.Building);
        Assert.Equal(HttpStatusCode.NotFound,(await global.GetAsync(wrong)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await global.PostAsJsonAsync(wrong+"/units",new CreateUnitRequest("X",1,100))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await global.PutAsJsonAsync(wrong,new UpdateBuildingRequest("X",true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await global.DeleteAsync(wrong+"?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await global.GetAsync(BlockUrl(other.Property,other.Building)+"/units/"+x.Unit.Id)).StatusCode);
    }
    [PostgresFact]
    public async Task NoStableSubjectOrInactiveMembershipCannotAccessHierarchy()
    {
        var x=await Seed();using var noSubject=Client(x.Actor,"SUPER_ADMIN",false);
        Assert.Equal(HttpStatusCode.Forbidden,(await noSubject.GetAsync(BlockUrl(x.Property,x.Building))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await noSubject.PostAsJsonAsync(SiteUrl(x.Property)+"/buildings",new CreateBuildingRequest("B"))).StatusCode);
        await factory.WithDb(async db=>{var member=await db.PropertyMemberships.SingleAsync(value=>value.PropertyId==x.Property.Id);member.SetEnabled(false);await db.SaveChangesAsync();});
        using var memberClient=Client(x.Actor,"MANAGER");Assert.Equal(HttpStatusCode.NotFound,(await memberClient.GetAsync(BlockUrl(x.Property,x.Building))).StatusCode);
        await factory.WithDb(async db=>{var member=await db.PropertyMemberships.SingleAsync(value=>value.PropertyId==x.Property.Id);db.Remove(member);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.NotFound,(await memberClient.PostAsJsonAsync(BlockUrl(x.Property,x.Building)+"/units",new CreateUnitRequest("2",1,100))).StatusCode);
    }
    [PostgresFact]
    public async Task DuplicatesReturn409AndSoftDeletedNamesCanBeReused()
    {
        var x=await Seed();using var client=Client(x.Actor);var b=BlockUrl(x.Property,x.Building);var a=ApartmentUrl(x.Property,x.Building,x.Unit);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(SiteUrl(x.Property)+"/buildings",new CreateBuildingRequest(" a "))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(b+"/units",new CreateUnitRequest(" 1 ",1,100))).StatusCode);
        var response=await client.PostAsJsonAsync(SiteUrl(x.Property)+"/buildings",new CreateBuildingRequest("B"));var block=(await response.Content.ReadFromJsonAsync<BuildingResponse>())!;
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(response.Headers.Location,new UpdateBuildingRequest("A",true,block.Version))).StatusCode);
        var ar=await client.PostAsJsonAsync(b+"/units",new CreateUnitRequest("2",1,100));var apartment=(await ar.Content.ReadFromJsonAsync<UnitResponse>())!;
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(ar.Headers.Location,new UpdateUnitRequest("1",1,100,true,apartment.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(a+"?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await client.PostAsJsonAsync(b+"/units",new CreateUnitRequest("1",1,100))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(response.Headers.Location+"?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await client.PostAsJsonAsync(SiteUrl(x.Property)+"/buildings",new CreateBuildingRequest(" b "))).StatusCode);
    }
    [PostgresFact]
    public async Task OldVersionsAndNonEmptyParentsReturn409WithoutPartialChanges()
    {
        var x=await Seed();using var client=Client(x.Actor);var b=BlockUrl(x.Property,x.Building);var a=ApartmentUrl(x.Property,x.Building,x.Unit);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync(SiteUrl(x.Property)+"?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync(b+"?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(a,new UpdateUnitRequest("Edited",2,130,true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(a,new UpdateUnitRequest("Stale",3,150,true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync(a+"?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(b,new UpdateBuildingRequest("Stale",true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(SiteUrl(x.Property),SiteEdit(x.Property,1))).StatusCode);
        await factory.WithDb(async db=>{var saved=await db.Units.SingleAsync(value=>value.Id==x.Unit.Id);Assert.Equal("Edited",saved.Number);Assert.Equal(2,saved.Version);});
    }
    [PostgresFact]
    public async Task InactiveParentBlocksChildWritesButCanBeReenabled()
    {
        var x=await Seed();using var client=Client(x.Actor);var s=SiteUrl(x.Property);var b=BlockUrl(x.Property,x.Building);var a=ApartmentUrl(x.Property,x.Building,x.Unit);
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(s,SiteEdit(x.Property,1,false))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(b)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(s+"/buildings",new CreateBuildingRequest("B"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(b,new UpdateBuildingRequest("B",true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(b+"/units",new CreateUnitRequest("2",1,100))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync(a+"?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(s,SiteEdit(x.Property,2))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(b,new UpdateBuildingRequest("A",false,1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(b+"/units",new CreateUnitRequest("2",1,100))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(a,new UpdateUnitRequest("2",1,100,true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(b,new UpdateBuildingRequest("A",true,2))).StatusCode);
    }
    [PostgresFact]
    public async Task WholeHierarchyCanBeDeletedBottomUpPreservingAudit()
    {
        var x=await Seed();using var client=Client(x.Actor);var s=SiteUrl(x.Property);var b=BlockUrl(x.Property,x.Building);var a=ApartmentUrl(x.Property,x.Building,x.Unit);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(a+"?version=1")).StatusCode);
        var block=(await client.GetFromJsonAsync<BuildingResponse>(b))!;Assert.Equal(0,block.UnitCount);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(b+"?version="+block.Version)).StatusCode);
        var site=(await client.GetFromJsonAsync<PropertyResponse>(s))!;
        var edited=await client.PutAsJsonAsync(s,new UpdatePropertyRequest("Renamed","New city","New district","New address",true,site.Version));Assert.Equal(HttpStatusCode.OK,edited.StatusCode);
        site=(await edited.Content.ReadFromJsonAsync<PropertyResponse>())!;Assert.Equal("Renamed",site.Name);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(s+"?version="+site.Version)).StatusCode);
        foreach(var path in new[]{s,s+"/buildings",b,b+"/units",a})Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(path)).StatusCode);
        await factory.WithDb(async db=>{var saved=await db.Properties.IgnoreQueryFilters().SingleAsync(value=>value.Id==x.Property.Id);Assert.True(saved.IsDeleted);Assert.Equal(x.Property.CreatedAt,saved.CreatedAt);Assert.Equal(x.Actor,saved.ModifiedBy);});
    }
    [PostgresTheory][InlineData("block-blank")][InlineData("block-long")][InlineData("number-blank")][InlineData("number-long")][InlineData("floor-missing")][InlineData("area-zero")][InlineData("area-negative")][InlineData("area-large")][InlineData("area-precision")]
    public async Task InvalidCreateFieldsReturn400(string kind)
    {
        var x=await Seed();using var client=Client(x.Actor);HttpResponseMessage response;
        if(kind.StartsWith("block"))response=await client.PostAsJsonAsync(SiteUrl(x.Property)+"/buildings",new CreateBuildingRequest(kind=="block-blank"?" ":new string('x',101)));
        else response=await client.PostAsJsonAsync(BlockUrl(x.Property,x.Building)+"/units",new CreateUnitRequest(kind=="number-blank"?" ":kind=="number-long"?new string('x',31):"2",kind=="floor-missing"?null:1,kind switch{"area-zero"=>0,"area-negative"=>-1,"area-large"=>100000000,"area-precision"=>1.001m,_=>100}));
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);var problem=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Assert.True(problem.RootElement.GetProperty("errors").EnumerateObject().Any());
        await factory.WithDb(async db=>{Assert.Equal(1,await db.Buildings.CountAsync(value=>value.PropertyId==x.Property.Id));Assert.Equal(1,await db.Units.CountAsync(value=>value.BuildingId==x.Building.Id));});
    }
    [PostgresFact]
    public async Task UpdatesRequireAllFieldsAndDeleteRequiresVersion()
    {
        var x=await Seed();using var client=Client(x.Actor);
        foreach(var path in new[]{SiteUrl(x.Property),BlockUrl(x.Property,x.Building),ApartmentUrl(x.Property,x.Building,x.Unit)}){
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync(path,new{})).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.DeleteAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.DeleteAsync(path+"?version=0")).StatusCode);
        }
    }
    [PostgresFact]
    public async Task SearchSortPagesAndCountsRespectSoftDeleteAndParents()
    {
        var x=await Seed();using var client=Client(x.Actor,"RESIDENT");
        await factory.WithDb(async db=>{var site=await db.Properties.Include(value=>value.Buildings).ThenInclude(value=>value.Units).SingleAsync(value=>value.Id==x.Property.Id);site.AddBuilding("100%_B");var block=site.Buildings.Single(value=>value.Id==x.Building.Id);block.AddUnit("100%_2",2,200);block.AddUnit("3",3,300);await db.SaveChangesAsync();});
        var blocks=(await client.GetFromJsonAsync<PagedRows<BuildingResponse>>(SiteUrl(x.Property)+"/buildings?search=%25"))!;Assert.Equal("100%_B",Assert.Single(blocks.Items).Name);
        var b=BlockUrl(x.Property,x.Building);var page=(await client.GetFromJsonAsync<PagedRows<UnitResponse>>(b+"/units?pageSize=1&sortBy=floor&sortDirection=desc"))!;
        Assert.Equal(3,page.TotalCount);Assert.Equal("3",Assert.Single(page.Items).Number);
        var second=(await client.GetFromJsonAsync<PagedRows<UnitResponse>>(b+"/units?page=2&pageSize=1&sortBy=grossSquareMeters&sortDirection=desc"))!;Assert.Equal("100%_2",Assert.Single(second.Items).Number);
        var literal=(await client.GetFromJsonAsync<PagedRows<UnitResponse>>(b+"/units?search=%5F"))!;Assert.Equal("100%_2",Assert.Single(literal.Items).Number);
        using var admin=Client(x.Actor);await admin.DeleteAsync(ApartmentUrl(x.Property,x.Building,x.Unit)+"?version=1");
        Assert.Equal(2,(await client.GetFromJsonAsync<BuildingResponse>(b))!.UnitCount);
        Assert.Equal(2,(await client.GetFromJsonAsync<PagedRows<UnitResponse>>(b+"/units"))!.TotalCount);
    }
    [PostgresTheory][InlineData("page=0")][InlineData("pageSize=101")][InlineData("sortBy=siteId")][InlineData("sortDirection=sideways")][InlineData("sortBy=name")]
    public async Task ApartmentQueryWhitelistRejectsInvalidParameters(string query)
    {
        var x=await Seed();using var client=Client(x.Actor);Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(BlockUrl(x.Property,x.Building)+"/units?"+query)).StatusCode);
    }
    [PostgresFact]
    public async Task ConcurrentDuplicateCommandsCommitOnlyOneRow()
    {
        var x=await Seed();using var client=Client(x.Actor);var url=SiteUrl(x.Property)+"/buildings";
        var results=await Task.WhenAll(Enumerable.Range(0,2).Select(_=>client.PostAsJsonAsync(url,new CreateBuildingRequest("Race"))));
        Assert.Single(results,response=>response.StatusCode==HttpStatusCode.Created);Assert.Single(results,response=>response.StatusCode==HttpStatusCode.Conflict);
        await factory.WithDb(async db=>Assert.Equal(1,await db.Buildings.CountAsync(value=>value.PropertyId==x.Property.Id && value.Name=="Race")));
    }
    [PostgresFact]
    public async Task ParentConcurrencyLockRollsBackChildInsert()
    {
        var x=await Seed();using var staleScope=factory.Services.CreateScope();var stale=staleScope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>();
        await stale.Properties.Include(value=>value.Buildings).ThenInclude(value=>value.Units).SingleAsync(value=>value.Id==x.Property.Id);
        using var client=Client(x.Actor);Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(SiteUrl(x.Property),SiteEdit(x.Property,1,false))).StatusCode);
        var store=new HierarchyStore(stale);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>store.CreateUnitAsync(x.Property.Id,x.Building.Id,new CreateUnitRequest("Race",1,100),new AccessScope(x.Actor,true),default));
        await factory.WithDb(async db=>{Assert.False(await db.Units.AnyAsync(value=>value.BuildingId==x.Building.Id && value.Number=="Race"));Assert.False((await db.Properties.SingleAsync(value=>value.Id==x.Property.Id)).IsEnabled);Assert.Equal(1,(await db.Buildings.SingleAsync(value=>value.Id==x.Building.Id)).Version);});
    }
}
