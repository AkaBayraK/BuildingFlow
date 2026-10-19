using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class PersonCenterTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private readonly List<WebApplicationFactory<Program>> children=[];
    private static DateOnly Today=>DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private HttpClient Client(string user,string role="MANAGER",bool writes=true)
    {
        var child=factory.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"PersonRefactor:Enabled","true"},{"PersonRefactor:AssignmentsEnabled","true"},{"PersonRefactor:UnitRelationWritesEnabled",writes?"true":"false"}})));children.Add(child);var c=child.CreateClient();using var signed=TestToken.Client(factory,user,role);c.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return c;
    }
    private sealed record Fixture(Guid Tenant,Guid Property,Guid Unit,Guid Person,string User);
    private async Task<Fixture> Seed()
    {
        var t=new PersonTenant("Center fixture");var p=new Property("Center property","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,100);var user=Guid.NewGuid().ToString();var a=new PersonUserAccount("http://localhost:8080/realms/buildingflow",user);var person=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Center","Fixture",null,null,null,null);
        await factory.WithDb(async db=>{db.AddRange(t,p,u,a,person,new PersonTenantMembership(t.Id,a.Id),new PropertyMembership(p.Id,user),new PersonPropertyScope(t.Id,p.Id));await db.SaveChangesAsync();});return new(t.Id,p.Id,u.Id,person.Id,user);
    }
    private static string Root(Fixture s)=>$"/api/v2/tenants/{s.Tenant}/people/{s.Person}";
    private static CreatePersonUnitRelationRequest Draft(Fixture s,string kind="OWNER",string? n=null,string? d=null)=>new(Guid.NewGuid(),s.Property,s.Unit,kind,Today,null,false,n,d);
    private static async Task<T> Ok<T>(HttpResponseMessage response){Assert.True(response.StatusCode==HttpStatusCode.OK,$"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");return (await response.Content.ReadFromJsonAsync<T>())!;}
    [PostgresFact]public async Task CenterQueriesAreScopedPagedAndAccountResponseContainsNoCredentials()
    {
        var s=await Seed();var other=await Seed();using var c=Client(s.User);
        var workspaces=await Ok<PagedRows<PersonWorkspace>>(await c.GetAsync("/api/v2/person-workspaces"));Assert.Single(workspaces.Items);Assert.Equal(s.Tenant,workspaces.Items[0].Id);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v2/tenants/{other.Tenant}/person-workspace")).StatusCode);
        var choices=await Ok<PagedRows<PersonPropertyChoice>>(await c.GetAsync($"/api/v2/tenants/{s.Tenant}/person-properties"));Assert.Single(choices.Items);Assert.True(choices.Items[0].CanWriteRelations);Assert.False(choices.Items[0].CanManageAssignments);
        var detail=await Ok<PersonCenterRecord>(await c.GetAsync(Root(s)+"/center"));Assert.True(detail.CanEdit);Assert.True(detail.CanCreateUnitRelations);Assert.False(detail.IsImported);
        var units=await Ok<PagedRows<PersonUnitChoice>>(await c.GetAsync($"/api/v2/tenants/{s.Tenant}/properties/{s.Property}/person-unit-choices"));Assert.Equal(s.Unit,Assert.Single(units.Items).Id);
        var account=await c.GetAsync(Root(s)+"/account-status");Assert.False((await Ok<PersonAccountStatus>(account)).IsLinked);var json=await account.Content.ReadAsStringAsync();Assert.DoesNotContain("subject",json);Assert.DoesNotContain("issuer",json);Assert.DoesNotContain("password",json);
        Assert.Empty((await Ok<PagedRows<PersonDutyRow>>(await c.GetAsync(Root(s)+"/management-duties"))).Items);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v2/person-workspaces?pageSize=101")).StatusCode);
    }
    [PostgresTheory][InlineData("OWNER")][InlineData("TENANT")][InlineData("RESIDENT")][InlineData("ACCOUNTANT")]
    public async Task NonManagerCannotAccessAnyCenterOrWrite(string role)
    {
        var s=await Seed();using var c=Client(s.User,role);foreach(var url in new[]{"/api/v2/person-workspaces",Root(s)+"/center",Root(s)+"/account-status",Root(s)+"/unit-relations",Root(s)+"/management-duties"})Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(url)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root(s)+"/unit-relations",Draft(s))).StatusCode);
    }
    [PostgresTheory][InlineData("OWNER")][InlineData("TENANT")][InlineData("RESIDENT")]
    public async Task RelationsRoundTripEndReplayAndHistoryArePreserved(string kind)
    {
        var s=await Seed();using var c=Client(s.User);var draft=Draft(s,kind);var created=await Ok<PersonUnitRelationMutation>(await c.PostAsJsonAsync(Root(s)+"/unit-relations",draft));Assert.Equal(1,created.Record.Version);Assert.True(created.Record.CanEnd);Assert.Null(created.Record.ShareNumerator);
        var page=await Ok<PagedRows<PersonUnitRelationRow>>(await c.GetAsync(Root(s)+"/unit-relations?pageSize=1"));Assert.Equal(1,page.TotalCount);Assert.Equal(kind,Assert.Single(page.Items).Kind);
        var path=Root(s)+$"/unit-relations/{kind}/{created.Record.Id}";Assert.Equal(created.Record.Id,(await Ok<PersonUnitRelationRow>(await c.GetAsync(path))).Id);
        var end=new EndPersonUnitRelationRequest(Guid.NewGuid(),1,Today);var ended=await Ok<PersonUnitRelationMutation>(await c.PostAsJsonAsync(path+"/end",end));Assert.Equal(2,ended.Record.Version);Assert.False(ended.Record.CanEnd);
        var replay=await Ok<PersonUnitRelationMutation>(await c.PostAsJsonAsync(Root(s)+"/unit-relations",draft));Assert.True(replay.Replayed);Assert.Equal(2,replay.Record.Version);Assert.Equal(1,replay.AppliedVersion);
        Assert.True((await Ok<PersonUnitRelationMutation>(await c.PostAsJsonAsync(path+"/end",end))).Replayed);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(path+"/end",end with{OperationId=Guid.NewGuid()})).StatusCode);
        await factory.WithDb(async db=>{Assert.False(await db.UnitOccupancies.AnyAsync(o=>o.PropertyId==s.Property));await Assert.ThrowsAsync<Npgsql.PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM person_unit_operations WHERE person_id={s.Person}"));});
    }
    [PostgresFact]public async Task ExactLargeSharesConcurrencyAndForeignUnitAreProtected()
    {
        var s=await Seed();var other=await Seed();using var c=Client(s.User);var draft=Draft(s,"OWNER","9007199254740993","9223372036854775807");var responses=await Task.WhenAll(c.PostAsJsonAsync(Root(s)+"/unit-relations",draft),c.PostAsJsonAsync(Root(s)+"/unit-relations",draft));var rows=await Task.WhenAll(responses.Select(Ok<PersonUnitRelationMutation>));Assert.Equal(rows[0].Record.Id,rows[1].Record.Id);Assert.Single(rows,r=>r.Replayed);Assert.Equal(draft.ShareNumerator,rows[0].Record.ShareNumerator);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(s)+"/unit-relations",draft with{IsPrimaryContact=true})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync(Root(s)+"/unit-relations",Draft(s,"TENANT") with{UnitId=other.Unit})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(other)+"/unit-relations")).StatusCode);
        var page=await Ok<PagedRows<PersonUnitRelationRow>>(await c.GetAsync(Root(s)+"/unit-relations"));Assert.Equal(draft.ShareNumerator,Assert.Single(page.Items).ShareNumerator);
    }
    [PostgresFact]public async Task WriteFlagOffIsReadOnlyAndEmptyListsTranslate()
    {
        var s=await Seed();using var c=Client(s.User,writes:false);Assert.Empty((await Ok<PagedRows<PersonUnitRelationRow>>(await c.GetAsync(Root(s)+"/unit-relations"))).Items);Assert.False((await Ok<PersonCenterRecord>(await c.GetAsync(Root(s)+"/center"))).CanCreateUnitRelations);Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync(Root(s)+"/unit-relations",Draft(s))).StatusCode);
    }
    [PostgresFact]public async Task LegacyPropertiesAndImportedPeopleRemainReadOnly()
    {
        var s=await Seed();var old=new Person(s.Property,"Fixture","Legacy","+905551234567","fixture@example.test");await factory.WithDb(async db=>{db.Add(old);await db.SaveChangesAsync();db.Add(new LegacyPersonMap(s.Tenant,old,s.Person,"{}"));await db.SaveChangesAsync();});using var c=Client(s.User);
        var center=await Ok<PersonCenterRecord>(await c.GetAsync(Root(s)+"/center"));Assert.True(center.IsImported);Assert.False(center.CanEdit);Assert.False(center.CanCreateUnitRelations);Assert.Equal(old.Id,center.LegacyPersonId);
        Assert.False(Assert.Single((await Ok<PagedRows<PersonPropertyChoice>>(await c.GetAsync($"/api/v2/tenants/{s.Tenant}/person-properties"))).Items).CanWriteRelations);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(s)+"/unit-relations",Draft(s))).StatusCode);
        await factory.WithDb(async db=>{var legacy=await db.People.SingleAsync(p=>p.Id==old.Id);db.Entry(legacy).Property(p=>p.IsEnabled).IsModified=true;await db.SaveChangesAsync();});Assert.True((await Ok<PersonCenterRecord>(await c.GetAsync(Root(s)+"/center"))).SourceChanged);
    }
    [PostgresFact]public async Task NativeRelationsProtectPersonAndUnitAndRejectLegacySecondWriter()
    {
        var s=await Seed();using var c=Client(s.User);await Ok<PersonUnitRelationMutation>(await c.PostAsJsonAsync(Root(s)+"/unit-relations",Draft(s,"RESIDENT")));
        var detail=await Ok<PersonCenterRecord>(await c.GetAsync(Root(s)+"/center"));Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(Root(s),new UpdateCentralPersonRequest(Guid.NewGuid(),detail.Person.Version,detail.Person.Profile,false))).StatusCode);
        var unitPath=$"/api/v1/properties/{s.Property}/units/{s.Unit}";Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(unitPath,new UpdateUnitRequest("1",0,100,false,1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/v1/properties/{s.Property}/people",new {firstName="Fixture",lastName="New",phoneNumber="+905551234567",email="fixture@example.test",personKind="NATURAL",citizenship="DOMESTIC",countryCode="TR",identityNumber="12345678901"})).StatusCode);
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
}
