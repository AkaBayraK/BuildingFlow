using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SessionContextTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private static readonly DateOnly Today=new(2026,10,18);private static readonly DateTimeOffset Now=new(2026,10,18,12,0,0,TimeSpan.Zero);
    private sealed class Clock:TimeProvider{public override DateTimeOffset GetUtcNow()=>Now;}
    private readonly List<WebApplicationFactory<Program>> children=[];
    private HttpClient Client(string user,string role="MANAGER",bool enabled=true)
    {
        var child=factory.WithWebHostBuilder(b=>{b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"PersonRefactor:Enabled","true"},{"PersonRefactor:SessionContextsEnabled",enabled?"true":"false"},{"PersonRefactor:AssignmentsEnabled","true"}}));b.ConfigureServices(s=>{s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(new Clock());});});children.Add(child);var c=child.CreateClient();using var signed=TestToken.Client(factory,user,role);c.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return c;
    }
    private sealed record Fixture(Guid Tenant,Guid Property,Guid Unit,Guid Person,Guid Account,Guid Link,string User);
    private async Task<Fixture> Seed(string? user=null)
    {
        var t=new PersonTenant("Context fixture");var p=new Property("Context property","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,100);var subject=user??Guid.NewGuid().ToString();var a=new PersonUserAccount("http://localhost:8080/realms/buildingflow",subject);var person=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Fixture","Self",null,null,null,null);var link=new CentralPersonAccountLink(t.Id,a.Id,person.Id,Now.AddDays(-1));await factory.WithDb(async db=>{db.AddRange(t,p,a,person,link,new PersonTenantMembership(t.Id,a.Id),new PersonPropertyScope(t.Id,p.Id),new PropertyMembership(p.Id,subject));await db.SaveChangesAsync();});return new(t.Id,p.Id,u.Id,person.Id,a.Id,link.Id,subject);
    }
    private async Task<ManagementAssignment> Duty(Fixture s,string role="MANAGER",DateOnly? start=null,DateOnly? end=null)
    {var a=new ManagementAssignment(s.Tenant,s.Property,s.Person,role,start??Today,end);await factory.WithDb(async db=>{db.Add(a);await db.SaveChangesAsync();});return a;}
    private const string Root="/api/v2/session/contexts";
    private static async Task<T> Ok<T>(HttpResponseMessage r){Assert.True(r.StatusCode==HttpStatusCode.OK,$"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return(await r.Content.ReadFromJsonAsync<T>())!;}
    [PostgresFact]public async Task SingleManagementContextIsScopedAndResolvedWithoutCredentials()
    {
        var s=await Seed();var foreign=await Seed();var a=await Duty(s);var other=await Duty(foreign);using var c=Client(s.User);var page=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root));var row=Assert.Single(page.Items);Assert.Equal(a.Id,row.Id);Assert.Equal("MANAGEMENT",row.Kind);Assert.True(row.CanManagePeople);Assert.False(row.CanManageAssignments);
        var detail=await Ok<SessionContextDetail>(await c.GetAsync(Root+$"/MANAGEMENT/{a.Id}"));Assert.Equal(s.Person,detail.Self!.Id);Assert.Equal("*******8901",detail.Self.MaskedIdentity);Assert.False(detail.Self.CanEdit);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root+$"/MANAGEMENT/{other.Id}")).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Root+$"/UNKNOWN/{a.Id}")).StatusCode);
        var json=await (await c.GetAsync(Root)).Content.ReadAsStringAsync();foreach(var value in new[]{"identityNumber","issuer","subject","password","12345678901"})Assert.DoesNotContain(value,json);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync($"/api/v1/properties/{s.Property}/units")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/properties/{foreign.Property}/units")).StatusCode);
    }
    [PostgresFact]public async Task MultipleIndependentRelationsAreNotCollapsedOrInferred()
    {
        var s=await Seed();var o=TestRelations.Effective(new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,Today));var r=TestRelations.Effective(new UnitResidency(s.Tenant,s.Property,s.Unit,s.Person,Today));await factory.WithDb(async db=>{db.AddRange(o,r);await db.SaveChangesAsync();});using var c=Client(s.User,"TENANT");var page=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root+"?pageSize=1"));Assert.Equal(2,page.TotalCount);Assert.Single(page.Items);var second=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root+"?page=2&pageSize=1"));Assert.NotEqual(page.Items[0].Kind,second.Items[0].Kind);
        var detail=await Ok<SessionContextDetail>(await c.GetAsync(Root+$"/OWNER/{o.Id}"));Assert.False(detail.Context.CanManagePeople);Assert.False(detail.Context.CanManageFinance);Assert.Equal(s.Person,detail.Self!.Id);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/properties/{s.Property}/units")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v2/tenants/{s.Tenant}/people")).StatusCode);
    }
    [PostgresFact]public async Task DutyCannotInventRealmRoleAndAccountantDutyCannotUseManagerClaimForPeople()
    {
        var s=await Seed();await Duty(s,"SITE_ADMIN");using var resident=Client(s.User,"RESIDENT");Assert.Empty((await Ok<PagedRows<SessionContext>>(await resident.GetAsync(Root))).Items);using var manager=Client(s.User);Assert.Empty((await Ok<PagedRows<SessionContext>>(await manager.GetAsync(Root))).Items);
        var a=await Seed();await Duty(a,"ACCOUNTANT");using var c=Client(a.User);var row=Assert.Single((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);Assert.True(row.CanManageFinance);Assert.False(row.CanManagePeople);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/properties/{a.Property}/people")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v2/tenants/{a.Tenant}/people")).StatusCode);Assert.Equal(HttpStatusCode.OK,(await c.GetAsync($"/api/v1/properties/{a.Property}/dues")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/properties/{a.Property}/dues/person-choices")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/properties/{a.Property}/dues?personId={a.Person}")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync($"/api/v1/properties/{a.Property}/units/{a.Unit}",new UpdateUnitRequest("1",0,100,true,1))).StatusCode);
    }
    [PostgresFact]public async Task RevocationClosesDeepLinksPropertyListsAndCentralPeopleOnNextRequest()
    {
        var s=await Seed();var a=await Duty(s);using var c=Client(s.User);Assert.Single((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);await factory.WithDb(async db=>{var duty=await db.Set<ManagementAssignment>().SingleAsync(x=>x.Id==a.Id);duty.Cancel(Now,"Fixture revoked");await db.SaveChangesAsync();});
        Assert.Empty((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root+$"/MANAGEMENT/{a.Id}")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/properties/{s.Property}/units")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v2/tenants/{s.Tenant}/people")).StatusCode);Assert.Empty((await Ok<PagedProperties>(await c.GetAsync("/api/v1/properties"))).Items);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/v1/users")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/v1/consents/records")).StatusCode);
    }
    [PostgresTheory][InlineData("account")][InlineData("link")][InlineData("membership")][InlineData("property")][InlineData("scope")][InlineData("person")][InlineData("tenant")][InlineData("propertyMember")]
    public async Task DisabledParentsAndIdentityLinksRevokeImmediately(string target)
    {
        var s=await Seed();var a=await Duty(s);using var c=Client(s.User);Assert.Single((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);await factory.WithDb(async db=>{BaseEntity entity=target switch{"account"=>await db.Set<PersonUserAccount>().SingleAsync(x=>x.Id==s.Account),"link"=>await db.Set<CentralPersonAccountLink>().SingleAsync(x=>x.Id==s.Link),"membership"=>await db.Set<PersonTenantMembership>().SingleAsync(x=>x.TenantId==s.Tenant),"property"=>await db.Properties.SingleAsync(x=>x.Id==s.Property),"tenant"=>await db.Set<PersonTenant>().SingleAsync(x=>x.Id==s.Tenant),"propertyMember"=>await db.PropertyMemberships.SingleAsync(x=>x.PropertyId==s.Property),"scope"=>await db.Set<PersonPropertyScope>().SingleAsync(x=>x.PropertyId==s.Property),_=>await db.Set<CentralPerson>().SingleAsync(x=>x.Id==s.Person)};db.Entry(entity).Property(x=>x.IsEnabled).CurrentValue=false;await db.SaveChangesAsync();});Assert.Empty((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root+$"/MANAGEMENT/{a.Id}")).StatusCode);
    }
    [PostgresFact]public async Task CalendarBoundariesAndEmptyOrInvalidPagesAreHandled()
    {
        var s=await Seed();var a=await Duty(s,start:Today.AddDays(-1),end:Today);await Duty(s,"ACCOUNTANT",Today.AddDays(1));using var c=Client(s.User);Assert.Equal(a.Id,Assert.Single((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items).Id);Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Root+"?pageSize=101")).StatusCode);Assert.Empty((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root+"?page=2"))).Items);using var off=Client(s.User,enabled:false);Assert.Equal(HttpStatusCode.NotFound,(await off.GetAsync(Root)).StatusCode);
    }
    [PostgresFact]public async Task SuperAdminUsesExplicitPlatformContextWithoutInventedPerson()
    {
        var s=await Seed();using var c=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");var rows=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root));Assert.Contains(rows.Items,x=>x.Kind=="PLATFORM"&&x.Id==s.Property);var detail=await Ok<SessionContextDetail>(await c.GetAsync(Root+$"/PLATFORM/{s.Property}"));Assert.Null(detail.Self);Assert.True(detail.Context.CanManageAssignments);
    }
    [PostgresFact]public async Task SameSubjectWithWrongIssuerDoesNotMatchTheAccount()
    {
        var s=await Seed();await Duty(s);using var c=Client(s.User);await factory.WithDb(async db=>{await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE person_user_accounts SET issuer='https://other.example.test/realms/fixture' WHERE id={s.Account}");});Assert.Empty((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);
    }
    [PostgresFact]public async Task ManagementOwnerAndResidentRemainIndependentWorkspaces()
    {
        var s=await Seed();var a=await Duty(s);var owner=TestRelations.Effective(new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,Today));var resident=TestRelations.Effective(new UnitResidency(s.Tenant,s.Property,s.Unit,s.Person,Today));await factory.WithDb(async db=>{db.AddRange(owner,resident);await db.SaveChangesAsync();});using var c=Client(s.User);var page=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root));Assert.Equal(3,page.TotalCount);Assert.Equal(new[]{"MANAGEMENT","OWNER","RESIDENT"},page.Items.Select(x=>x.Kind));Assert.Equal(a.Id,page.Items[0].Id);
    }
    [PostgresFact]public async Task ImportedRelationIsRevokedWhenItsLegacySourceChanges()
    {
        var s=await Seed();var legacy=new Person(s.Property,"Fixture","Legacy","+905551234567","fixture@example.test");var source=new UnitOccupancy(s.Property,s.Unit,legacy.Id,OccupancyType.OWNER,Today);var target=TestRelations.Effective(new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,Today));
        await factory.WithDb(async db=>{db.Add(legacy);await db.SaveChangesAsync();db.AddRange(source,target,new LegacyPersonMap(s.Tenant,legacy,s.Person,"{}"));await db.SaveChangesAsync();db.Add(new LegacyOccupancyMap(s.Tenant,source,target.Id,null,null));await db.SaveChangesAsync();});using var c=Client(s.User,"OWNER");Assert.Single((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);
        await factory.WithDb(async db=>{var row=await db.UnitOccupancies.SingleAsync(x=>x.Id==source.Id);row.End(Today);await db.SaveChangesAsync();});Assert.Empty((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root+$"/OWNER/{target.Id}")).StatusCode);
    }
    [PostgresFact]public async Task ExpiredOrFutureUnitRelationsAreNotCurrentAndDisabledUnitRevokesSelfContext()
    {
        var s=await Seed();var current=TestRelations.Effective(new UnitResidency(s.Tenant,s.Property,s.Unit,s.Person,Today,Today));await factory.WithDb(async db=>{db.AddRange(current,TestRelations.Effective(new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,Today.AddDays(-3),Today.AddDays(-1))),TestRelations.Effective(new UnitTenancy(s.Tenant,s.Property,s.Unit,s.Person,Today.AddDays(1))));await db.SaveChangesAsync();});using var c=Client(s.User,"RESIDENT");Assert.Equal(current.Id,Assert.Single((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items).Id);await factory.WithDb(async db=>{var unit=await db.Units.SingleAsync(u=>u.Id==s.Unit);db.Entry(unit).Property(u=>u.IsEnabled).CurrentValue=false;await db.SaveChangesAsync();});Assert.Empty((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root))).Items);
    }
    [PostgresFact]public async Task FeatureOffPreservesLegacyButClientContextHeadersCannotGrantNewAccess()
    {
        var s=await Seed();using var off=Client(s.User,enabled:false);Assert.Equal(HttpStatusCode.OK,(await off.GetAsync($"/api/v1/properties/{s.Property}/units")).StatusCode);Assert.Equal(HttpStatusCode.OK,(await off.GetAsync($"/api/v2/tenants/{s.Tenant}/people")).StatusCode);
        using var enabled=Client(s.User);enabled.DefaultRequestHeaders.Add("X-Workspace-Role","SUPER_ADMIN");enabled.DefaultRequestHeaders.Add("X-Workspace-Id",s.Property.ToString());Assert.Equal(HttpStatusCode.NotFound,(await enabled.GetAsync($"/api/v1/properties/{s.Property}/units")).StatusCode);Assert.Empty((await Ok<PagedRows<SessionContext>>(await enabled.GetAsync(Root))).Items);
    }
    [PostgresFact]public async Task TenantAndResidentShareUnitWithoutInventingOwnershipOrManagement()
    {
        var s=await Seed();var tenant=TestRelations.Effective(new UnitTenancy(s.Tenant,s.Property,s.Unit,s.Person,Today));var resident=TestRelations.Effective(new UnitResidency(s.Tenant,s.Property,s.Unit,s.Person,Today));await factory.WithDb(async db=>{db.AddRange(tenant,resident);await db.SaveChangesAsync();});using var c=Client(s.User,"TENANT");var page=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root));Assert.Equal(new[]{"RESIDENT","TENANT"},page.Items.Select(x=>x.Kind));Assert.All(page.Items,x=>{Assert.False(x.CanManagePeople);Assert.False(x.CanManageFinance);Assert.False(x.CanManageAssignments);});Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v2/tenants/{s.Tenant}/people")).StatusCode);
    }
    [PostgresFact]public async Task RemoteOwnerHasSeparateUnitsAcrossBlocksWithoutResidencyOrPropertyMembership()
    {
        var s=await Seed();var property=new Property("Remote fixture","City","District","Address",PropertyType.SITE);var a=property.AddBuilding("A").AddUnit("1",0,100);var b=property.AddBuilding("B").AddUnit("2",0,100);await factory.WithDb(async db=>{db.AddRange(property,new PersonPropertyScope(s.Tenant,property.Id),TestRelations.Effective(new UnitOwnership(s.Tenant,property.Id,a.Id,s.Person,Today)),TestRelations.Effective(new UnitOwnership(s.Tenant,property.Id,b.Id,s.Person,Today)));await db.SaveChangesAsync();});using var c=Client(s.User,"OWNER");var page=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Root));Assert.Equal(2,page.TotalCount);Assert.All(page.Items,x=>{Assert.Equal("OWNER",x.Kind);Assert.Equal(property.Id,x.PropertyId);Assert.False(x.CanManagePeople);});Assert.Equal(2,page.Items.Select(x=>x.UnitId).Distinct().Count());Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/properties/{property.Id}/units")).StatusCode);
    }

    private async Task<WebsiteSite> Website(Fixture s,bool published=true)
    {
        var site=new WebsiteSite(s.Property,Guid.NewGuid(),"entry-"+Guid.NewGuid().ToString("N"),"Entry fixture");
        await factory.WithDb(async db=>{db.Add(site);if(published){site.MarkPublished();db.Add(new WebsitePublication(s.Property,site.Id,Guid.NewGuid(),1,"{}",new string('a',64),Now,null,null));}await db.SaveChangesAsync();});return site;
    }
    [PostgresFact]public async Task WebsiteEntrySeparatesAudiencePropertyAndCurrentIdentity()
    {
        var s=await Seed();var fp=new Property("Other entry","City","District","Address",PropertyType.APARTMENT);var fu=fp.AddUnit("2",0,100);await factory.WithDb(async db=>{db.AddRange(fp,new PersonPropertyScope(s.Tenant,fp.Id),new PropertyMembership(fp.Id,s.User));await db.SaveChangesAsync();});var foreign=new Fixture(s.Tenant,fp.Id,fu.Id,s.Person,s.Account,s.Link,s.User);var duty=await Duty(s);await Duty(foreign);var site=await Website(s);var other=await Website(foreign);
        var owner=TestRelations.Effective(new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,Today));var resident=TestRelations.Effective(new UnitResidency(s.Tenant,s.Property,s.Unit,s.Person,Today));
        await factory.WithDb(async db=>{db.AddRange(owner,resident);await db.SaveChangesAsync();});using var c=Client(s.User);
        string Entry(string key,string audience)=>Root+"/website/"+key+"/"+audience;
        var response=await c.GetAsync(Entry(site.SiteKey,"management"));Assert.Equal("no-store",response.Headers.CacheControl!.ToString());var page=await Ok<PagedRows<SessionContext>>(response);Assert.Equal(duty.Id,Assert.Single(page.Items).Id);Assert.All(page.Items,x=>Assert.Equal(s.Property,x.PropertyId));
        var all=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Entry(site.SiteKey,"all")));Assert.Equal(3,all.TotalCount);Assert.All(all.Items,x=>Assert.Equal(s.Property,x.PropertyId));Assert.Equal(new[]{"MANAGEMENT","OWNER","RESIDENT"},all.Items.Select(x=>x.Kind));
        var members=await Ok<PagedRows<SessionContext>>(await c.GetAsync(Entry(site.SiteKey,"resident")+"?pageSize=1"));Assert.Equal(2,members.TotalCount);Assert.Single(members.Items);Assert.False(members.Items[0].CanManageFinance);
        using var stranger=Client(Guid.NewGuid().ToString());Assert.Equal(HttpStatusCode.NotFound,(await stranger.GetAsync(Entry(site.SiteKey,"all"))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await stranger.GetAsync(Entry(site.SiteKey,"management"))).StatusCode);
        using var memberRole=Client(s.User,"RESIDENT");Assert.Equal(HttpStatusCode.NotFound,(await memberRole.GetAsync(Entry(site.SiteKey,"management"))).StatusCode);Assert.Equal(2,(await Ok<PagedRows<SessionContext>>(await memberRole.GetAsync(Entry(site.SiteKey,"resident")))).TotalCount);Assert.Equal(2,(await Ok<PagedRows<SessionContext>>(await memberRole.GetAsync(Entry(site.SiteKey,"all")))).TotalCount);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Entry(site.SiteKey,"SUPER_ADMIN"))).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Entry(site.SiteKey,"resident")+"?pageSize=101")).StatusCode);
        using var off=Client(s.User,enabled:false);Assert.Equal(HttpStatusCode.NotFound,(await off.GetAsync(Entry(site.SiteKey,"management"))).StatusCode);
        using var anon=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Entry(site.SiteKey,"management"))).StatusCode);anon.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer","invalid");Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Entry(site.SiteKey,"management"))).StatusCode);
        await factory.WithDb(async db=>{var a=await db.Set<ManagementAssignment>().SingleAsync(x=>x.Id==duty.Id);a.Cancel(Now,"Entry fixture revoke");await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Entry(site.SiteKey,"management"))).StatusCode);
        Assert.All((await Ok<PagedRows<SessionContext>>(await c.GetAsync(Entry(other.SiteKey,"management")))).Items,x=>Assert.Equal(foreign.Property,x.PropertyId));
    }
    [PostgresFact]public async Task WebsiteEntryRequiresActivePublishedSiteAndDoesNotInferResidentFromManagement()
    {
        var s=await Seed();await Duty(s);var site=await Website(s,false);using var c=Client(s.User);var path=Root+"/website/"+site.SiteKey;
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(path+"/management")).StatusCode);
        await factory.WithDb(async db=>{var w=await db.Set<WebsiteSite>().SingleAsync(x=>x.Id==site.Id);w.MarkPublished();db.Add(new WebsitePublication(s.Property,w.Id,Guid.NewGuid(),1,"{}",new string('a',64),Now,null,null));await db.SaveChangesAsync();});
        Assert.Single((await Ok<PagedRows<SessionContext>>(await c.GetAsync(path+"/management"))).Items);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(path+"/resident")).StatusCode);
        await factory.WithDb(async db=>{var w=await db.Set<WebsiteSite>().SingleAsync(x=>x.Id==site.Id);w.Suspend();await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(path+"/management")).StatusCode);
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
}
