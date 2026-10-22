using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MyPortfolioTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private const string Issuer="http://localhost:8080/realms/buildingflow",Phone="+905551234567";
    private readonly List<WebApplicationFactory<Program>> children=[];
    private static DateOnly Today=>DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private HttpClient Client(string user,string role,bool contexts)
    {
        var flags=new Dictionary<string,string?>{{"PersonRefactor:Enabled","true"},{"PersonRefactor:AssignmentsEnabled","true"},{"PersonRefactor:SessionContextsEnabled",contexts?"true":"false"}};
        var child=factory.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(flags)));children.Add(child);var c=child.CreateClient();using var signed=TestToken.Client(factory,user,role);c.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return c;
    }
    private HttpClient Manager(World w)=>Client(w.Manager,"MANAGER",false);
    private HttpClient Me(string user,string role="TENANT")=>Client(user,role,true);
    private static async Task<T> Ok<T>(HttpResponseMessage r){Assert.True(r.StatusCode==HttpStatusCode.OK,$"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return(await r.Content.ReadFromJsonAsync<T>())!;}
    private static async Task Coded(HttpResponseMessage r,string code){Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);Assert.Contains(code,await r.Content.ReadAsStringAsync());}
    private sealed record World(Guid Tenant,string Manager,Guid ManagerAccount,Guid Site,Guid D5,Guid D12,Guid Apartment,Guid D3,Guid D7,Guid Ahmet,Guid Mehmet,string Last,Relations Rows);
    private sealed record Relations(Guid OwnerD5,Guid MehmetD5,Guid TenantD12,Guid ResidentD12,Guid OwnerD3,Guid ResidentD3,Guid TenantD7,Guid LeaseD7);
    private async Task<World> Seed()
    {
        var last="Yilmaz"+new string(Guid.NewGuid().ToString("N").Where(char.IsAsciiLetter).Take(6).ToArray());var t=new PersonTenant("Müşteri "+last);var manager=Guid.NewGuid().ToString();var account=new PersonUserAccount(Issuer,manager);
        var site=new Property("Örnek Sitesi","City","District","Address",PropertyType.SITE);var a=site.AddBuilding("A Blok");var b=site.AddBuilding("B Blok");var d5=a.AddUnit("D5",1,100);var d12=b.AddUnit("D12",3,100);
        var apartment=new Property("Örnek Apartmanı","City","District","Address",PropertyType.APARTMENT);var d3=apartment.AddUnit("D3",1,90);var d7=apartment.AddUnit("D7",2,90);
        var ahmet=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Ahmet",last,null,null,Phone,null);var mehmet=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","10987654321","Mehmet",last,null,null,"+905559876543",null);
        var start=Today.AddYears(-1);var rented=new LeaseContract(t.Id,site.Id,d5.Id,start);var own=new LeaseContract(t.Id,site.Id,d12.Id,start);var d7Lease=new LeaseContract(t.Id,apartment.Id,d7.Id,start);
        var ownerD5=new UnitOwnership(t.Id,site.Id,d5.Id,ahmet.Id,start);var mehmetD5=new UnitTenancy(rented,mehmet.Id,start);var tenantD12=new UnitTenancy(own,ahmet.Id,start);var residentD12=new UnitResidency(t.Id,site.Id,d12.Id,ahmet.Id,start);
        var ownerD3=new UnitOwnership(t.Id,apartment.Id,d3.Id,ahmet.Id,start);var residentD3=new UnitResidency(t.Id,apartment.Id,d3.Id,ahmet.Id,start);var tenantD7=new UnitTenancy(d7Lease,ahmet.Id,start);
        VerifiableRecord[] rows=[rented,own,d7Lease,ownerD5,mehmetD5,tenantD12,residentD12,ownerD3,residentD3,tenantD7];foreach(var row in rows)row.Verify(account.Id,DateTimeOffset.UtcNow);
        await factory.WithDb(async db=>{db.AddRange(t,account,site,a,b,d5,d12,apartment,d3,d7,ahmet,mehmet,new PersonTenantMembership(t.Id,account.Id),new PropertyMembership(site.Id,manager),new PropertyMembership(apartment.Id,manager),new PersonPropertyScope(t.Id,site.Id),new PersonPropertyScope(t.Id,apartment.Id));await db.SaveChangesAsync();db.AddRange(rows);await db.SaveChangesAsync();});
        return new(t.Id,manager,account.Id,site.Id,d5.Id,d12.Id,apartment.Id,d3.Id,d7.Id,ahmet.Id,mehmet.Id,last,new(ownerD5.Id,mehmetD5.Id,tenantD12.Id,residentD12.Id,ownerD3.Id,residentD3.Id,tenantD7.Id,d7Lease.Id));
    }
    private static string Person(World w,Guid person)=>$"/api/v2/tenants/{w.Tenant}/people/{person}";
    private async Task<string> Issue(World w,Guid? person=null){using var m=Manager(w);var r=await m.PostAsync(Person(w,person??w.Ahmet)+"/account-link-codes",null);Assert.Equal("no-store",r.Headers.CacheControl?.ToString());return (await Ok<IssuedPersonLinkCode>(r)).Code;}
    private LinkMyPersonRequest Request(World w,string code,string? first=null)=>new(code,first??"Ahmet",w.Last,Phone,null);
    private async Task<string> Linked(World w){var user=Guid.NewGuid().ToString();var code=await Issue(w);using var me=Me(user);await Ok<LinkedCentralPerson>(await me.PostAsJsonAsync("/api/v2/me/person-links",Request(w,code)));return user;}
    [PostgresFact]public async Task ManagerIssuedCodeLinksOnceAndOnlyWithMatchingIdentity()
    {
        var w=await Seed();var code=await Issue(w);Assert.Matches("^[1-9][0-9]{5}$",code);
        using var m=Manager(w);var status=await Ok<PersonAccountStatus>(await m.GetAsync(Person(w,w.Ahmet)+"/account-status"));Assert.False(status.IsLinked);Assert.Equal(code[^2..],status.CodeHint);Assert.NotNull(status.CodeExpiresAt);
        var user=Guid.NewGuid().ToString();using var me=Me(user);
        await Coded(await me.PostAsJsonAsync("/api/v2/me/person-links",Request(w,code,"Mehmet")),"BF_MOBILE_LINK_REJECTED");
        await Coded(await me.PostAsJsonAsync("/api/v2/me/person-links",Request(w,code) with{PhoneNumber="+905550000000"}),"BF_MOBILE_LINK_REJECTED");
        var response=await me.PostAsJsonAsync("/api/v2/me/person-links",Request(w,code));var linked=await Ok<LinkedCentralPerson>(response);Assert.Equal(w.Ahmet,linked.Person.Id);Assert.Equal("*******8901",linked.Person.MaskedIdentity);
        var json=await response.Content.ReadAsStringAsync();foreach(var value in new[]{"12345678901",code,"issuer","subject"})Assert.DoesNotContain(value,json);
        using var other=Me(Guid.NewGuid().ToString());await Coded(await other.PostAsJsonAsync("/api/v2/me/person-links",Request(w,code)),"BF_MOBILE_LINK_REJECTED");
        await Coded(await m.PostAsync(Person(w,w.Ahmet)+"/account-link-codes",null),"BF_PERSON_LINK_UNAVAILABLE");
        Assert.True((await Ok<PersonAccountStatus>(await m.GetAsync(Person(w,w.Ahmet)+"/account-status"))).IsLinked);
        await factory.WithDb(async db=>{var account=await db.Set<PersonUserAccount>().SingleAsync(a=>a.Subject==user);Assert.True(await db.Set<PersonTenantMembership>().AnyAsync(x=>x.TenantId==w.Tenant && x.UserAccountId==account.Id));Assert.Single(await db.Set<CentralPersonAccountLink>().Where(l=>l.PersonId==w.Ahmet && l.EndedAt==null).ToArrayAsync());
            Assert.DoesNotContain(code,await db.Database.SqlQuery<string>($"SELECT row_to_json(c)::text AS \"Value\" FROM person_link_codes c WHERE person_id={w.Ahmet}").SingleAsync());});
        using var second=Me(user);var mehmetCode=await Issue(w,w.Mehmet);await Coded(await second.PostAsJsonAsync("/api/v2/me/person-links",new LinkMyPersonRequest(mehmetCode,"Mehmet",w.Last,"+905559876543",null)),"BF_MOBILE_LINK_REJECTED");
    }
    [PostgresFact]public async Task CodesAreRevokedExpireLockAfterFailuresAndStayInsideManagedScope()
    {
        var w=await Seed();var foreign=await Seed();var first=await Issue(w);var second=await Issue(w);
        using var me=Me(Guid.NewGuid().ToString());await Coded(await me.PostAsJsonAsync("/api/v2/me/person-links",Request(w,first)),"BF_MOBILE_LINK_REJECTED");
        await factory.WithDb(db=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE person_link_codes SET expires_at=now()-interval '1 minute' WHERE person_id={w.Ahmet} AND revoked_at IS NULL"));
        using var late=Me(Guid.NewGuid().ToString());await Coded(await late.PostAsJsonAsync("/api/v2/me/person-links",Request(w,second)),"BF_MOBILE_LINK_REJECTED");
        var fresh=await Issue(w);var attacker=Guid.NewGuid().ToString();using var brute=Me(attacker);
        for(var i=0;i<PersonMatching.MaxFailures;i++)await Coded(await brute.PostAsJsonAsync("/api/v2/me/person-links",Request(w,"987123")),"BF_MOBILE_LINK_REJECTED");
        await Coded(await brute.PostAsJsonAsync("/api/v2/me/person-links",Request(w,fresh)),"BF_MOBILE_LINK_LOCKED");
        using var m=Manager(w);Assert.Equal(HttpStatusCode.NotFound,(await m.PostAsync(Person(foreign,foreign.Ahmet)+"/account-link-codes",null)).StatusCode);
        var stranger=new CentralPerson(w.Tenant,"NATURAL","DOMESTIC","TR","12345678901","Ayse",w.Last,null,null,Phone,null);await factory.WithDb(async db=>{db.Add(stranger);await db.SaveChangesAsync();});
        await Coded(await m.PostAsync(Person(w,stranger.Id)+"/account-link-codes",null),"BF_PERSON_LINK_UNAVAILABLE");
        foreach(var role in new[]{"OWNER","TENANT","RESIDENT","ACCOUNTANT"}){using var denied=Client(w.Manager,role,false);Assert.Equal(HttpStatusCode.Forbidden,(await denied.PostAsync(Person(w,w.Ahmet)+"/account-link-codes",null)).StatusCode);}
        Assert.Equal(HttpStatusCode.BadRequest,(await brute.PostAsJsonAsync("/api/v2/me/person-links",new{code=fresh,firstName="Ahmet",lastName=w.Last,phoneNumber=Phone,personId=w.Ahmet})).StatusCode);
    }
    [PostgresFact]public async Task MePortfolioGroupsUnitsWithCapabilitiesKeepsWorkspacesApartAndHidesOthers()
    {
        var w=await Seed();var user=await Linked(w);await factory.WithDb(async db=>{db.AddRange(new PropertyMembership(w.Apartment,user),new ManagementAssignment(w.Tenant,w.Apartment,w.Ahmet,"MANAGER",Today.AddDays(-1)));await db.SaveChangesAsync();});
        using var me=Me(user,"MANAGER");var response=await me.GetAsync("/api/v2/me/portfolio");Assert.Equal("no-store",response.Headers.CacheControl?.ToString());var view=await Ok<MyPortfolio>(response);var json=await response.Content.ReadAsStringAsync();
        var person=Assert.Single(view.People);Assert.Equal(w.Ahmet,person.PersonId);Assert.Equal(2,person.Properties.Count);
        var units=view.Units.ToDictionary(u=>u.UnitId);Assert.Equal(4,units.Count);
        Assert.Equal(PersonPortfolio.RentedOut,units[w.D5].Status);Assert.Contains(PortfolioCapabilities.ViewLease,units[w.D5].Capabilities);Assert.Contains(PortfolioCapabilities.ViewDues,units[w.D5].Capabilities);
        Assert.Equal(PersonPortfolio.TenantResident,units[w.D12].Status);Assert.Equal(2,units[w.D12].Contexts.Count);Assert.Equal(PersonPortfolio.OwnerOccupied,units[w.D3].Status);Assert.DoesNotContain(PortfolioCapabilities.ViewLease,units[w.D3].Capabilities);
        Assert.Equal(PersonPortfolio.TenantNotResident,units[w.D7].Status);Assert.All(view.Units.SelectMany(u=>u.Capabilities),c=>Assert.DoesNotContain("MANAGE",c));
        Assert.Contains(units[w.D12].Contexts,c=>c.ContextKey==$"{w.Tenant:D}:TENANT:{w.Rows.TenantD12:D}");
        var duty=Assert.Single(view.Workspaces);Assert.Equal("MANAGEMENT",duty.Kind);Assert.Null(duty.UnitId);
        foreach(var value in new[]{"Mehmet",w.Mehmet.ToString(),w.Rows.MehmetD5.ToString(),"12345678901"})Assert.DoesNotContain(value,json);
        var again=await Ok<MyPortfolio>(await me.GetAsync("/api/v2/me/portfolio"));Assert.Equal(view.Fingerprint,again.Fingerprint);
        await factory.WithDb(db=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_residencies SET end_date={Today.AddDays(-1)} WHERE id={w.Rows.ResidentD3}"));
        var changed=await Ok<MyPortfolio>(await me.GetAsync("/api/v2/me/portfolio"));Assert.NotEqual(view.Fingerprint,changed.Fingerprint);Assert.Equal(PersonPortfolio.OwnerOnly,changed.Units.Single(u=>u.UnitId==w.D3).Status);
        using var stranger=Me(Guid.NewGuid().ToString());var empty=await Ok<MyPortfolio>(await stranger.GetAsync("/api/v2/me/portfolio"));Assert.Empty(empty.People);Assert.Empty(empty.Units);Assert.Empty(empty.Workspaces);
        using var closed=Client(user,"TENANT",false);Assert.Equal(HttpStatusCode.NotFound,(await closed.GetAsync("/api/v2/me/portfolio")).StatusCode);
    }
    [PostgresFact]public async Task ActiveContextIsResolvedFromCurrentFactsAndRejectsEveryForgedOrStaleSelection()
    {
        var w=await Seed();var foreign=await Seed();var user=await Linked(w);var foreignUser=await Linked(foreign);using var me=Me(user);
        string Url(Guid property,Guid? unit,string capacity,Guid relation)=>$"/api/v2/me/active-context?propertyId={property}&capacity={capacity}&relationId={relation}"+(unit==null?"":$"&unitId={unit}");
        var context=await Ok<ActiveContext>(await me.GetAsync(Url(w.Site,w.D12,"TENANT",w.Rows.TenantD12)));Assert.Equal($"{w.Tenant:D}:TENANT:{w.Rows.TenantD12:D}",context.ContextKey);Assert.Contains(PortfolioCapabilities.ViewLease,context.Capabilities);Assert.Equal("D12",context.Context.UnitNumber);
        Assert.Contains(PortfolioCapabilities.ViewLease,(await Ok<ActiveContext>(await me.GetAsync(Url(w.Site,w.D5,"OWNER",w.Rows.OwnerD5)))).Capabilities);
        Assert.DoesNotContain(PortfolioCapabilities.ViewDues,(await Ok<ActiveContext>(await me.GetAsync(Url(w.Site,w.D12,"RESIDENT",w.Rows.ResidentD12)))).Capabilities);
        foreach(var url in new[]{Url(w.Site,w.D5,"TENANT",w.Rows.TenantD12),Url(w.Apartment,w.D12,"TENANT",w.Rows.TenantD12),Url(w.Site,w.D12,"OWNER",w.Rows.TenantD12),Url(w.Site,w.D5,"TENANT",w.Rows.MehmetD5),Url(foreign.Site,foreign.D12,"TENANT",foreign.Rows.TenantD12)})
            Assert.Equal(HttpStatusCode.NotFound,(await me.GetAsync(url)).StatusCode);
        foreach(var url in new[]{Url(w.Site,null,"TENANT",w.Rows.TenantD12),Url(w.Site,w.D12,"MANAGEMENT",w.Rows.TenantD12),Url(w.Site,w.D12,"LANDLORD",w.Rows.TenantD12)})Assert.Equal(HttpStatusCode.BadRequest,(await me.GetAsync(url)).StatusCode);
        await factory.WithDb(async db=>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_ownerships SET end_date={Today.AddDays(-1)} WHERE id={w.Rows.OwnerD3}");
            await Assert.ThrowsAsync<Npgsql.PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_residencies SET verification_status='PENDING',verified_by_account_id=NULL,verified_at=NULL WHERE id={w.Rows.ResidentD3}"));
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE lease_contracts SET is_enabled=false WHERE id={w.Rows.LeaseD7}");
            var rejected=new UnitResidency(w.Tenant,w.Site,w.D5,w.Ahmet,Today.AddDays(-3));rejected.Reject(w.ManagerAccount,DateTimeOffset.UtcNow);var pending=new UnitResidency(w.Tenant,w.Apartment,w.D7,w.Ahmet,Today.AddDays(-3));db.AddRange(rejected,pending);await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.NotFound,(await me.GetAsync(Url(w.Site,w.D5,"RESIDENT",rejected.Id))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await me.GetAsync(Url(w.Apartment,w.D7,"RESIDENT",pending.Id))).StatusCode);
        });
        Assert.Equal(HttpStatusCode.NotFound,(await me.GetAsync(Url(w.Apartment,w.D3,"OWNER",w.Rows.OwnerD3))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await me.GetAsync(Url(w.Apartment,w.D7,"TENANT",w.Rows.TenantD7))).StatusCode);
        var units=(await Ok<MyPortfolio>(await me.GetAsync("/api/v2/me/portfolio"))).Units.Select(u=>u.UnitId).ToArray();Assert.DoesNotContain(w.D7,units);Assert.Contains(w.D3,units);
        using var m=Manager(w);await Ok<PersonAccountStatus>(await m.PostAsync(Person(w,w.Ahmet)+"/account-link/end",null));
        Assert.Equal(HttpStatusCode.NotFound,(await me.GetAsync(Url(w.Site,w.D12,"TENANT",w.Rows.TenantD12))).StatusCode);Assert.Empty((await Ok<MyPortfolio>(await me.GetAsync("/api/v2/me/portfolio"))).Units);
        using var foreignMe=Me(foreignUser);Assert.Equal(HttpStatusCode.OK,(await foreignMe.GetAsync(Url(foreign.Site,foreign.D12,"TENANT",foreign.Rows.TenantD12))).StatusCode);
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
}
