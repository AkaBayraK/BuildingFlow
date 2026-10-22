using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class PersonPortfolioApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private const string Issuer="http://localhost:8080/realms/buildingflow";
    private static readonly Dictionary<string,string?> Flags=new(){{"PersonRefactor:Enabled","true"},{"PersonRefactor:AssignmentsEnabled","true"},{"PersonRefactor:UnitRelationWritesEnabled","true"}};
    private readonly List<WebApplicationFactory<Program>> children=[];
    private static DateOnly Today=>DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private HttpClient Client(string user,string role="MANAGER",bool writes=true)
    {
        var flags=new Dictionary<string,string?>(Flags){["PersonRefactor:UnitRelationWritesEnabled"]=writes?"true":"false"};
        var child=factory.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(flags)));children.Add(child);var c=child.CreateClient();using var signed=TestToken.Client(factory,user,role);c.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return c;
    }
    private static async Task<T> Ok<T>(HttpResponseMessage response){Assert.True(response.StatusCode==HttpStatusCode.OK,$"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");return (await response.Content.ReadFromJsonAsync<T>())!;}
    private static async Task Coded(HttpResponseMessage response,string code){Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);Assert.Contains(code,await response.Content.ReadAsStringAsync());}
    private sealed record World(Guid Tenant,string User,Guid Account,Guid Site,Guid BlockA,Guid BlockB,Guid D5,Guid D12,Guid Apartment,Guid D3,Guid D7,Guid Ahmet,Guid Mehmet,string Token);
    private async Task<World> Seed()
    {
        var token="Tok"+Guid.NewGuid().ToString("N")[..10];var t=new PersonTenant("Portfolio "+token);var user=Guid.NewGuid().ToString();var account=new PersonUserAccount(Issuer,user);
        var site=new Property("Örnek Sitesi "+token,"City","District","Address",PropertyType.SITE);var a=site.AddBuilding("A Blok");var b=site.AddBuilding("B Blok");var d5=a.AddUnit("D5",1,100);var d12=b.AddUnit("D12",3,100);
        var apartment=new Property("Örnek Apartmanı "+token,"City","District","Address",PropertyType.APARTMENT);var d3=apartment.AddUnit("D3",1,90);var d7=apartment.AddUnit("D7",2,90);
        var ahmet=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Ahmet",token,null,null,null,null);var mehmet=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","10987654321","Mehmet",token,null,null,null,null);
        await factory.WithDb(async db=>{db.AddRange(t,account,site,a,b,d5,d12,apartment,d3,d7,ahmet,mehmet,new PersonTenantMembership(t.Id,account.Id),new PropertyMembership(site.Id,user),new PropertyMembership(apartment.Id,user),new PersonPropertyScope(t.Id,site.Id),new PersonPropertyScope(t.Id,apartment.Id));await db.SaveChangesAsync();});
        return new(t.Id,user,account.Id,site.Id,a.Id,b.Id,d5.Id,d12.Id,apartment.Id,d3.Id,d7.Id,ahmet.Id,mehmet.Id,token);
    }
    private async Task Relations(World w)
    {
        var start=Today.AddYears(-1);await factory.WithDb(async db=>
        {
            var rented=new LeaseContract(w.Tenant,w.Site,w.D5,start);var own=new LeaseContract(w.Tenant,w.Site,w.D12,start);var old=new LeaseContract(w.Tenant,w.Apartment,w.D7,start.AddYears(-2),start.AddDays(-1));var second=new LeaseContract(w.Tenant,w.Apartment,w.D7,start);
            VerifiableRecord[] rows=[rented,own,old,second,new UnitOwnership(w.Tenant,w.Site,w.D5,w.Ahmet,start),new UnitTenancy(rented,w.Mehmet,start),new UnitResidency(w.Tenant,w.Site,w.D5,w.Mehmet,start),
                new UnitTenancy(own,w.Ahmet,start),new UnitResidency(w.Tenant,w.Site,w.D12,w.Ahmet,start),new UnitOwnership(w.Tenant,w.Apartment,w.D3,w.Ahmet,start),new UnitResidency(w.Tenant,w.Apartment,w.D3,w.Ahmet,start),
                new UnitTenancy(old,w.Ahmet,start.AddYears(-2),start.AddDays(-1)),new UnitTenancy(second,w.Ahmet,start)];
            foreach(var row in rows)row.Verify(w.Account,DateTimeOffset.UtcNow);db.AddRange(rows);await db.SaveChangesAsync();
        });
    }
    private static string Units(World w)=>$"/api/v2/tenants/{w.Tenant}/properties/{w.Apartment}/units/{w.D3}/lease-contracts";
    private static (MobileProfile Profile,MobileProfilePhoto Photo) Photo(string user)
    {
        var photo=new MobileProfilePhoto(user,Guid.NewGuid(),Guid.NewGuid().ToString("N"),new string('A',64),10);photo.Current();var profile=new MobileProfile(user);profile.SetPhoto(photo.Id);return (profile,photo);
    }
    [PostgresFact]public async Task DirectoryShowsLinkedOrScopedLegacyAccountPhotoAndPhotoEndpointKeepsPersonScope()
    {
        var w=await Seed();var linked=Guid.NewGuid().ToString();var legacyUser=Guid.NewGuid().ToString();var hiddenUser=Guid.NewGuid().ToString();
        var account=new PersonUserAccount(Issuer,linked);var hiddenArea=new Property("Gizli "+w.Token,"City","District","Address",PropertyType.APARTMENT);
        var zeynep=new CentralPerson(w.Tenant,"NATURAL","DOMESTIC","TR",TestRelations.Tc(9100),"Zeynep",w.Token,null,null,null,null);
        var visible=new Person(w.Site,"Mehmet",w.Token,"5550000101","m@example.test");visible.SetProfile("NATURAL","FOREIGN","US","PHOTO101",null,null,null,legacyUser);var hidden=new Person(hiddenArea.Id,"Zeynep",w.Token,"5550000102","z@example.test");hidden.SetProfile("NATURAL","FOREIGN","US","PHOTO102",null,null,null,hiddenUser);
        var a=Photo(linked);var b=Photo(legacyUser);var h=Photo(hiddenUser);
        await factory.WithDb(async db=>{db.AddRange(account,new PersonTenantMembership(w.Tenant,account.Id),hiddenArea,new PersonPropertyScope(w.Tenant,hiddenArea.Id),zeynep,visible,hidden,a.Profile,a.Photo,b.Profile,b.Photo,h.Profile,h.Photo);await db.SaveChangesAsync();
            db.AddRange(new CentralPersonAccountLink(w.Tenant,account.Id,w.Ahmet,DateTimeOffset.UtcNow),new LegacyPersonMap(w.Tenant,visible,w.Mehmet,"{}"),new LegacyPersonMap(w.Tenant,hidden,zeynep.Id,"{}"));await db.SaveChangesAsync();});
        using var c=Client(w.User);var rows=(await Ok<PagedRows<PersonDirectoryRow>>(await c.GetAsync($"/api/v2/people?tenantId={w.Tenant}&pageSize=50"))).Items.ToDictionary(r=>r.Id);
        Assert.Equal(a.Photo.Id,rows[w.Ahmet].PhotoId);Assert.Equal(b.Photo.Id,rows[w.Mehmet].PhotoId);Assert.Null(rows[zeynep.Id].PhotoId);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v2/tenants/{w.Tenant}/people/{zeynep.Id}/photo")).StatusCode);
        var other=await Seed();using var stranger=Client(other.User);Assert.Equal(HttpStatusCode.NotFound,(await stranger.GetAsync($"/api/v2/tenants/{w.Tenant}/people/{w.Ahmet}/photo")).StatusCode);
        foreach(var role in new[]{"OWNER","TENANT","RESIDENT","ACCOUNTANT"}){using var denied=Client(w.User,role);Assert.Equal(HttpStatusCode.Forbidden,(await denied.GetAsync($"/api/v2/tenants/{w.Tenant}/people/{w.Ahmet}/photo")).StatusCode);}
    }
    [PostgresFact]public async Task PortfolioGroupsPersonPerUnitAcrossAreasAndNeverLeaksOtherPeople()
    {
        var w=await Seed();await Relations(w);using var c=Client(w.User);
        var response=await c.GetAsync($"/api/v2/tenants/{w.Tenant}/people/{w.Ahmet}/portfolio");var view=await Ok<PersonPortfolioView>(response);var json=await response.Content.ReadAsStringAsync();
        Assert.Equal(Today,view.AsOf);Assert.Equal(2,view.Properties.Count);var units=view.Properties.SelectMany(p=>p.Buildings).SelectMany(b=>b.Units).ToDictionary(u=>u.UnitId);
        Assert.Equal(PersonPortfolio.RentedOut,units[w.D5].Status);Assert.NotNull(units[w.D5].ActiveLease);Assert.Equal(PersonPortfolio.TenantResident,units[w.D12].Status);
        Assert.Equal(PersonPortfolio.OwnerOccupied,units[w.D3].Status);Assert.Equal(PersonPortfolio.TenantNotResident,units[w.D7].Status);Assert.Single(units[w.D7].Relations);
        var site=view.Properties.Single(p=>p.PropertyId==w.Site);Assert.Equal(new Guid?[]{w.BlockA,w.BlockB},site.Buildings.Select(b=>b.BuildingId));Assert.Null(Assert.Single(view.Properties.Single(p=>p.PropertyId==w.Apartment).Buildings).BuildingId);
        Assert.DoesNotContain(w.Mehmet.ToString(),json);Assert.DoesNotContain("Mehmet",json);Assert.DoesNotContain("12345678901",json);
        var other=await Seed();using var stranger=Client(other.User);Assert.Equal(HttpStatusCode.NotFound,(await stranger.GetAsync($"/api/v2/tenants/{w.Tenant}/people/{w.Ahmet}/portfolio")).StatusCode);
        foreach(var role in new[]{"OWNER","TENANT","RESIDENT","ACCOUNTANT"}){using var denied=Client(w.User,role);Assert.Equal(HttpStatusCode.Forbidden,(await denied.GetAsync($"/api/v2/tenants/{w.Tenant}/people/{w.Ahmet}/portfolio")).StatusCode);}
    }
    [PostgresFact]public async Task DirectoryNeedsNoAreaIsScopedPerTenantMaskedAndDeduplicated()
    {
        var w=await Seed();var other=await Seed();await Relations(w);using var manager=Client(w.User);using var admin=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");
        var mine=await Ok<PagedRows<PersonDirectoryRow>>(await manager.GetAsync($"/api/v2/people?search={w.Token}"));Assert.Equal(2,mine.TotalCount);Assert.All(mine.Items,r=>Assert.Equal(w.Tenant,r.TenantId));
        Assert.Empty((await Ok<PagedRows<PersonDirectoryRow>>(await manager.GetAsync($"/api/v2/people?search={other.Token}"))).Items);
        var response=await admin.GetAsync($"/api/v2/people?search={other.Token}&pageSize=1&sortBy=createdAt&sortDirection=desc");var all=await Ok<PagedRows<PersonDirectoryRow>>(response);Assert.Equal(2,all.TotalCount);Assert.Single(all.Items);
        var json=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("10987654321",json);Assert.DoesNotContain("12345678901",json);Assert.Matches("^\\*{7}\\d{4}$",all.Items[0].MaskedIdentity);
        var scoped=await Ok<PagedRows<PersonDirectoryRow>>(await manager.GetAsync($"/api/v2/people?propertyId={w.Site}&search={w.Token}"));Assert.Equal(2,scoped.TotalCount);Assert.Equal(scoped.Items.Count,scoped.Items.Select(r=>r.Id).Distinct().Count());
        var owners=await Ok<PagedRows<PersonDirectoryRow>>(await manager.GetAsync($"/api/v2/people?propertyId={w.Apartment}&relation=OWNER&search={w.Token}"));Assert.Equal(w.Ahmet,Assert.Single(owners.Items).Id);
        Assert.Equal(w.Ahmet,Assert.Single((await Ok<PagedRows<PersonDirectoryRow>>(await manager.GetAsync($"/api/v2/people?relation=RESIDENT&tenantId={w.Tenant}&search=Ahmet {w.Token}"))).Items).Id);
        Assert.Empty((await Ok<PagedRows<PersonDirectoryRow>>(await manager.GetAsync($"/api/v2/people?propertyId={other.Site}"))).Items);
        Assert.Empty((await Ok<PagedRows<PersonDirectoryRow>>(await manager.GetAsync($"/api/v2/people?tenantId={other.Tenant}"))).Items);
        foreach(var url in new[]{"/api/v2/people?pageSize=101","/api/v2/people?sortBy=identity","/api/v2/people?relation=MANAGER","/api/v2/people?search="+new string('a',201)})Assert.Equal(HttpStatusCode.BadRequest,(await manager.GetAsync(url)).StatusCode);
        foreach(var role in new[]{"OWNER","TENANT","RESIDENT","ACCOUNTANT"}){using var denied=Client(w.User,role);Assert.Equal(HttpStatusCode.Forbidden,(await denied.GetAsync("/api/v2/people")).StatusCode);}
    }
    [PostgresFact]public async Task DependentChoicesFollowAreaBuildingUnitAndRejectForeignBuildings()
    {
        var w=await Seed();var other=await Seed();using var c=Client(w.User);var root=$"/api/v2/tenants/{w.Tenant}/properties";
        Assert.Equal(["A Blok","B Blok"],(await Ok<PagedRows<PersonBuildingChoice>>(await c.GetAsync($"{root}/{w.Site}/person-building-choices"))).Items.Select(b=>b.Name));
        var block=await Ok<PagedRows<PersonUnitChoice>>(await c.GetAsync($"{root}/{w.Site}/person-unit-choices?buildingId={w.BlockB}"));Assert.Equal(w.D12,Assert.Single(block.Items).Id);Assert.Equal(w.BlockB,block.Items[0].BuildingId);
        Assert.Empty((await Ok<PagedRows<PersonBuildingChoice>>(await c.GetAsync($"{root}/{w.Apartment}/person-building-choices"))).Items);
        Assert.All((await Ok<PagedRows<PersonUnitChoice>>(await c.GetAsync($"{root}/{w.Apartment}/person-unit-choices"))).Items,u=>Assert.Null(u.BuildingId));
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"{root}/{w.Site}/person-unit-choices?buildingId={other.BlockA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v2/tenants/{other.Tenant}/properties/{other.Site}/person-building-choices")).StatusCode);
    }
    [PostgresFact]public async Task LeaseLifecycleKeepsOneContractPerPeriodWithJointTenantsReplayAndVersionChecks()
    {
        var w=await Seed();using var c=Client(w.User);var start=Today;var path=Units(w);
        var draft=new CreateLeaseContractRequest(Guid.NewGuid(),start,start.AddMonths(12).AddDays(-1),"KS-2026/3",[new(w.Ahmet,start,start.AddMonths(12).AddDays(-1),true),new(w.Mehmet,start,start.AddMonths(6),false)]);
        var created=await Ok<LeaseContractMutation>(await c.PostAsJsonAsync(path,draft));Assert.Equal("VERIFIED",created.Record.VerificationStatus);Assert.Equal(2,created.Record.Parties.Count);Assert.All(created.Record.Parties,p=>Assert.Equal("VERIFIED",p.VerificationStatus));Assert.Equal("ACTIVE",created.Record.Status);
        var replay=await Ok<LeaseContractMutation>(await c.PostAsJsonAsync(path,draft));Assert.True(replay.Replayed);Assert.Equal(created.Record.Id,replay.Record.Id);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(path,draft with{ContractReference="other"})).StatusCode);
        await Coded(await c.PostAsJsonAsync(path,draft with{OperationId=Guid.NewGuid(),StartDate=start.AddMonths(3),EndDate=null,Parties=[new(w.Mehmet,start.AddMonths(7),null,false)]}),"BF_LEASE_OVERLAP");
        await Coded(await c.PostAsJsonAsync(path,draft with{OperationId=Guid.NewGuid(),StartDate=start.AddYears(2),EndDate=start.AddYears(3),Parties=[new(w.Mehmet,start.AddYears(2),null,false)]}),"BF_LEASE_PARTY_OUTSIDE");
        var contract=$"{path}/{created.Record.Id}";
        await Coded(await c.PostAsJsonAsync(contract+"/parties",new AddLeasePartyRequest(Guid.NewGuid(),created.Record.Version,new(w.Mehmet,start.AddMonths(7),null,false))),"BF_LEASE_PARTY_OUTSIDE");
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(contract+"/parties",new AddLeasePartyRequest(Guid.NewGuid(),created.Record.Version+5,new(w.Mehmet,start.AddMonths(7),start.AddMonths(8),false)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(contract+"/parties",new AddLeasePartyRequest(Guid.NewGuid(),created.Record.Version,new(w.Mehmet,start.AddMonths(5),start.AddMonths(8),false)))).StatusCode);
        var joined=await Ok<LeaseContractMutation>(await c.PostAsJsonAsync(contract+"/parties",new AddLeasePartyRequest(Guid.NewGuid(),created.Record.Version,new(w.Mehmet,start.AddMonths(7),start.AddMonths(8),false))));Assert.Equal(3,joined.Record.Parties.Count);Assert.True(joined.Record.Version>created.Record.Version);
        var free=await Ok<PersonUnitRelationMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{w.Tenant}/people/{w.Mehmet}/unit-relations",new CreatePersonUnitRelationRequest(Guid.NewGuid(),w.Apartment,w.D3,"TENANT",start.AddMonths(9),start.AddMonths(10),false)));Assert.Null(free.Record.LeaseContractId);Assert.Equal("VERIFIED",free.Record.VerificationStatus);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(contract+"/attach",new AttachLeasePartyRequest(Guid.NewGuid(),joined.Record.Version,free.Record.Id,free.Record.Version+1))).StatusCode);
        var attached=await Ok<LeaseContractMutation>(await c.PostAsJsonAsync(contract+"/attach",new AttachLeasePartyRequest(Guid.NewGuid(),joined.Record.Version,free.Record.Id,free.Record.Version)));Assert.Equal(4,attached.Record.Parties.Count);
        var endDate=start.AddMonths(12).AddDays(-1);Assert.Equal(endDate,attached.Record.EndDate);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(contract+"/end",new EndLeaseContractRequest(Guid.NewGuid(),attached.Record.Version,endDate))).StatusCode);
        var renewal=await Ok<LeaseContractMutation>(await c.PostAsJsonAsync(path,new CreateLeaseContractRequest(Guid.NewGuid(),endDate.AddDays(1),null,null,[new(w.Ahmet,endDate.AddDays(1),null,true)])));Assert.Equal("UPCOMING",renewal.Record.Status);
        var next=$"{path}/{renewal.Record.Id}";var late=await Ok<LeaseContractMutation>(await c.PostAsJsonAsync(next+"/parties",new AddLeasePartyRequest(Guid.NewGuid(),renewal.Record.Version,new(w.Mehmet,endDate.AddDays(40),endDate.AddDays(50),false))));
        await Coded(await c.PostAsJsonAsync(next+"/end",new EndLeaseContractRequest(Guid.NewGuid(),late.Record.Version,endDate.AddDays(30))),"BF_LEASE_PARTY_OUTSIDE");
        var ended=await Ok<LeaseContractMutation>(await c.PostAsJsonAsync(next+"/end",new EndLeaseContractRequest(Guid.NewGuid(),late.Record.Version,endDate.AddDays(60))));
        Assert.Equal(endDate.AddDays(60),ended.Record.EndDate);Assert.Equal(endDate.AddDays(60),ended.Record.Parties.Single(p=>p.PersonId==w.Ahmet).EndDate);Assert.Equal(endDate.AddDays(50),ended.Record.Parties.Single(p=>p.PersonId==w.Mehmet).EndDate);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(next+"/end",new EndLeaseContractRequest(Guid.NewGuid(),ended.Record.Version,endDate.AddDays(70)))).StatusCode);
        var list=await Ok<PagedRows<LeaseContractRow>>(await c.GetAsync(path+"?pageSize=1"));Assert.Equal(2,list.TotalCount);Assert.Equal(renewal.Record.Id,Assert.Single(list.Items).Id);
        Assert.Equal(created.Record.Id,(await Ok<LeaseContractRow>(await c.GetAsync(contract))).Id);
    }
    [PostgresFact]public async Task LeaseWritesRejectRacesForeignScopesRolesClosedGateAndLegacyAreas()
    {
        var w=await Seed();var other=await Seed();using var c=Client(w.User);var start=Today;var path=Units(w);
        CreateLeaseContractRequest Draft(Guid person)=>new(Guid.NewGuid(),start,null,null,[new(person,start,null,false)]);
        var race=await Task.WhenAll(c.PostAsJsonAsync(path,Draft(w.Ahmet)),c.PostAsJsonAsync(path,Draft(w.Mehmet)));Assert.Single(race,r=>r.StatusCode==HttpStatusCode.OK);var lost=Assert.Single(race,r=>r.StatusCode!=HttpStatusCode.OK);await Coded(lost,"BF_LEASE_OVERLAP");
        await factory.WithDb(async db=>Assert.Single(await db.Set<LeaseContract>().Where(l=>l.UnitId==w.D3).ToArrayAsync()));
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync($"/api/v2/tenants/{w.Tenant}/properties/{w.Apartment}/units/{other.D3}/lease-contracts",Draft(w.Ahmet))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/v2/tenants/{w.Tenant}/properties/{w.Apartment}/units/{w.D7}/lease-contracts",Draft(other.Ahmet))).StatusCode);
        using var stranger=Client(other.User);Assert.Equal(HttpStatusCode.NotFound,(await stranger.PostAsJsonAsync($"/api/v2/tenants/{w.Tenant}/properties/{w.Apartment}/units/{w.D7}/lease-contracts",Draft(w.Ahmet))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await stranger.GetAsync(path)).StatusCode);
        foreach(var role in new[]{"OWNER","TENANT","RESIDENT","ACCOUNTANT"}){using var denied=Client(w.User,role);Assert.Equal(HttpStatusCode.Forbidden,(await denied.PostAsJsonAsync(path,Draft(w.Ahmet))).StatusCode);}
        using var closed=Client(w.User,writes:false);Assert.Equal(HttpStatusCode.NotFound,(await closed.PostAsJsonAsync($"/api/v2/tenants/{w.Tenant}/properties/{w.Apartment}/units/{w.D7}/lease-contracts",Draft(w.Ahmet))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(path,new CreateLeaseContractRequest(Guid.NewGuid(),start,null,new string('R',101),[new(w.Ahmet,start,null,false)]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(path,new CreateLeaseContractRequest(Guid.NewGuid(),start,null,null,[]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(path,new{operationId=Guid.NewGuid(),startDate=start,parties=new[]{new{personId=w.Ahmet,startDate=start,isPrimaryContact=false}},tenantId=Guid.NewGuid()})).StatusCode);
        await factory.WithDb(async db=>{db.Add(new Person(w.Site,"Legacy","Person",null,null));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/v2/tenants/{w.Tenant}/properties/{w.Site}/units/{w.D5}/lease-contracts",Draft(w.Ahmet))).StatusCode);
    }
    [PostgresFact]public async Task DirectoryPortfolioAndLeaseListUseConstantQueryCounts()
    {
        var small=await Seed();var large=await Seed();await Relations(large);
        await factory.WithDb(async db=>{for(var i=0;i<14;i++)db.Add(new CentralPerson(large.Tenant,"NATURAL","DOMESTIC","TR","12345678901","Extra"+i,large.Token,null,null,null,null));
            var lease=new LeaseContract(small.Tenant,small.Apartment,small.D3,new(2020,1,1),new(2020,12,31));db.AddRange(lease,new UnitTenancy(lease,small.Ahmet,new(2020,1,1),new(2020,12,31)),new UnitOwnership(small.Tenant,small.Apartment,small.D3,small.Ahmet,new(2020,1,1)));
            for(var y=2021;y<2026;y++){var next=new LeaseContract(large.Tenant,large.Apartment,large.D3,new(y,1,1),new(y,12,31));db.AddRange(next,new UnitTenancy(next,large.Ahmet,new(y,1,1),new(y,6,30)),new UnitTenancy(next,large.Mehmet,new(y,1,1),new(y,12,31)));}
            await db.SaveChangesAsync();});
        async Task<int> Count(World w,Func<PersonCenterStore,LeaseContractStore,Task> act)
        {
            var counter=new Counter();var count=0;await factory.WithDb(async source=>
            {
                var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(source.Database.GetConnectionString()).AddInterceptors(counter).Options;await using var db=new BuildingFlowDbContext(options);
                var actor=new Actor(w.User);var config=new ConfigurationBuilder().AddInMemoryCollection(Flags).Build();var access=new ManagementAccess(db,actor,TimeProvider.System,config);
                var people=new CentralPeopleStore(db,actor,TimeProvider.System,access);await act(new PersonCenterStore(db,access,people,actor,TimeProvider.System,config),new LeaseContractStore(db,access,actor,TimeProvider.System,config));count=counter.Commands;
            });return count;
        }
        Assert.Equal(await Count(small,(s,_)=>s.DirectoryAsync(new(Search:small.Token,PageSize:100),default)),await Count(large,(s,_)=>s.DirectoryAsync(new(Search:large.Token,PageSize:100),default)));
        Assert.Equal(await Count(small,(s,_)=>s.PortfolioAsync(small.Tenant,small.Ahmet,default)),await Count(large,(s,_)=>s.PortfolioAsync(large.Tenant,large.Ahmet,default)));
        Assert.Equal(await Count(small,(_,l)=>l.ListAsync(small.Tenant,small.Apartment,small.D3,1,20,default)),await Count(large,(_,l)=>l.ListAsync(large.Tenant,large.Apartment,large.D3,1,20,default)));
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
    private sealed class Counter:DbCommandInterceptor
    {
        public int Commands;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default){Interlocked.Increment(ref Commands);return base.ReaderExecutingAsync(command,eventData,result,cancellationToken);}
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<object> result,CancellationToken cancellationToken=default){Interlocked.Increment(ref Commands);return base.ScalarExecutingAsync(command,eventData,result,cancellationToken);}
    }
    private sealed class Actor(string user):ICurrentUserService{public string? Issuer=>PersonPortfolioApiTests.Issuer;public string? UserId=>user;public string? Username=>user;public bool IsAuthenticated=>true;public bool IsInRole(string role)=>role=="MANAGER";}
}
