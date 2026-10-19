using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
public sealed class ManagementAssignmentTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private static readonly DateOnly Today=new(2026,10,17);
    private static readonly DateTimeOffset Now=new(2026,10,17,12,0,0,TimeSpan.Zero);
    private readonly List<WebApplicationFactory<Program>> children=[];
    private sealed class Clock:TimeProvider{public override DateTimeOffset GetUtcNow()=>Now;}
    private sealed class Identity(bool qualified):IKeycloakAdmin
    {
        public Task<KeycloakUser?> GetAsync(string id,CancellationToken token)=>Task.FromResult<KeycloakUser?>(new(id,"fixture",null,null,null,true,true,null));
        public Task<IReadOnlyList<string>> RealmRolesAsync(string id,CancellationToken token)=>Task.FromResult<IReadOnlyList<string>>(qualified?["SITE_ADMIN"]:[]);
        public Task<(IReadOnlyList<KeycloakUser> Users,int Total)> SearchAsync(string? s,int f,int m,CancellationToken t)=>throw new NotSupportedException();
        public Task<string> CreateAsync(KeycloakUserDraft d,CancellationToken t)=>throw new NotSupportedException();
        public Task UpdateAsync(string id,KeycloakUserDraft d,CancellationToken t)=>throw new NotSupportedException();
        public Task ChangeRealmRolesAsync(string id,IReadOnlyCollection<string> a,IReadOnlyCollection<string> r,CancellationToken t)=>throw new NotSupportedException();
        public Task SetPasswordAsync(string id,string p,bool v,CancellationToken t)=>throw new NotSupportedException();
        public Task SendActionsEmailAsync(string id,IReadOnlyCollection<string> a,CancellationToken t)=>throw new NotSupportedException();
        public Task DeleteAsync(string id,CancellationToken t)=>throw new NotSupportedException();
    }
    private HttpClient Client(string? user,string role,Guid? tenant=null,string mode="LEGACY",bool enabled=true,bool qualified=true)
    {
        var values=new Dictionary<string,string?>{{"PersonRefactor:Enabled","true"},{"PersonRefactor:AssignmentsEnabled",enabled?"true":"false"},{"PersonRefactor:TransitionEnabled","true"}};
        if(tenant!=null)values[$"PersonRefactor:ManagementModes:{tenant:D}"]=mode;
        var child=factory.WithWebHostBuilder(b=>{b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(values));b.ConfigureServices(s=>{s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(new Clock());s.RemoveAll<IKeycloakAdmin>();s.AddSingleton<IKeycloakAdmin>(new Identity(qualified));});});children.Add(child);
        var c=child.CreateClient();using var signed=TestToken.Client(factory,user,role);c.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return c;
    }
    private sealed record Fixture(Guid Tenant,Guid Property,string Admin,Guid AdminPerson,string Manager,Guid ManagerPerson,Guid Account,Guid Member);
    private async Task<Fixture> Seed()
    {
        var t=new PersonTenant("Fixture tenant");var p=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);var admin=Guid.NewGuid().ToString();var manager=Guid.NewGuid().ToString();
        var a=new PersonUserAccount("http://localhost:8080/realms/buildingflow",admin);var b=new PersonUserAccount("http://localhost:8080/realms/buildingflow",manager);var member=new PersonTenantMembership(t.Id,b.Id);
        CentralPerson Person(string name)=>new(t.Id,"NATURAL","DOMESTIC","TR","12345678901",name,"Fixture",null,null,null,null);
        var ap=Person("Admin");var mp=Person("Manager");await factory.WithDb(async db=>{db.AddRange(t,p,a,b,new PersonTenantMembership(t.Id,a.Id),member,ap,mp,new PropertyMembership(p.Id,admin),new PropertyMembership(p.Id,manager),new PersonPropertyScope(t.Id,p.Id),new CentralPersonAccountLink(t.Id,a.Id,ap.Id,Now.AddDays(-1)),new CentralPersonAccountLink(t.Id,b.Id,mp.Id,Now.AddDays(-1)));await db.SaveChangesAsync();});return new(t.Id,p.Id,admin,ap.Id,manager,mp.Id,b.Id,member.Id);
    }
    private static string Root(Fixture s)=>$"/api/v2/tenants/{s.Tenant}/properties/{s.Property}/management-assignments";
    private static string Access(Fixture s)=>$"/api/v2/tenants/{s.Tenant}/properties/{s.Property}/management-access";
    private static CreateManagementAssignmentRequest Draft(Guid person,string role="MANAGER",DateOnly? start=null,DateOnly? end=null,Guid? op=null)=>new(op??Guid.NewGuid(),person,role,start??Today,end,"Fixture appointment");
    private static async Task<T> Ok<T>(HttpResponseMessage r){Assert.Equal(HttpStatusCode.OK,r.StatusCode);return(await r.Content.ReadFromJsonAsync<T>())!;}
    [PostgresFact]public async Task SiteAdminCreatesIndependentMultipleDutiesAndVersionedEndWithImmutableHistory()
    {
        var s=await Seed();using var c=Client(s.Admin,"SITE_ADMIN");var draft=Draft(s.ManagerPerson);var created=await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(s),draft));Assert.Equal("ACTIVE",created.Record.Status);Assert.Equal(1,created.Record.Version);
        await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,"ACCOUNTANT")));var page=await Ok<PagedRows<ManagementAssignmentRow>>(await c.GetAsync(Root(s)));Assert.Equal(2,page.TotalCount);
        var end=new EndManagementAssignmentRequest(Guid.NewGuid(),1,Today,"Term ended");var ended=await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(s)+$"/{created.Record.Id}/end",end));Assert.Equal(2,ended.Record.Version);Assert.Equal("ACTIVE",ended.Record.Status);
        var replay=await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(s),draft));Assert.True(replay.Replayed);Assert.Equal(2,replay.Record.Version);Assert.Equal(1,replay.AppliedVersion);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(s)+$"/{created.Record.Id}/end",end with{OperationId=Guid.NewGuid()})).StatusCode);
        var history=await Ok<PagedRows<ManagementAssignmentHistory>>(await c.GetAsync(Root(s)+$"/{created.Record.Id}/history"));Assert.Equal(new[]{"CREATED","ENDED"},history.Items.Select(e=>e.Action));Assert.All(history.Items,e=>Assert.Equal(s.Admin,e.CreatedBy));
        await factory.WithDb(async db=>{Assert.False(await db.UnitOccupancies.AnyAsync(o=>o.PropertyId==s.Property));Assert.False(await db.Set<UnitOwnership>().AnyAsync(o=>o.PropertyId==s.Property));await Assert.ThrowsAsync<Npgsql.PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE management_assignment_events SET reason='changed' WHERE assignment_id={created.Record.Id}"));await Assert.ThrowsAsync<Npgsql.PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM management_assignment_operations WHERE assignment_id={created.Record.Id}"));});
    }
    [PostgresFact]public async Task ConcurrentSameOperationIsIdempotentAndDifferentPayloadOrSameVersionConflicts()
    {
        var s=await Seed();using var c=Client(s.Admin,"SITE_ADMIN");var draft=Draft(s.ManagerPerson);var responses=await Task.WhenAll(c.PostAsJsonAsync(Root(s),draft),c.PostAsJsonAsync(Root(s),draft));var rows=await Task.WhenAll(responses.Select(Ok<ManagementAssignmentMutation>));Assert.Equal(rows[0].Record.Id,rows[1].Record.Id);Assert.Single(rows,r=>r.Replayed);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(s),draft with{Role="ACCOUNTANT"})).StatusCode);
        var id=rows[0].Record.Id;var results=await Task.WhenAll(c.PostAsJsonAsync(Root(s)+$"/{id}/end",new EndManagementAssignmentRequest(Guid.NewGuid(),1,Today,"End")),c.PostAsJsonAsync(Root(s)+$"/{id}/cancel",new CancelManagementAssignmentRequest(Guid.NewGuid(),1,"Cancel")));Assert.Single(results,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Conflict);
    }
    [PostgresFact]public async Task FiniteInclusiveOverlapRejectedAndFutureCancellationDoesNotCreateResidency()
    {
        var s=await Seed();using var c=Client(s.Admin,"SITE_ADMIN");await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,start:Today,end:Today.AddDays(2))));Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,start:Today.AddDays(2),end:Today.AddDays(4)))).StatusCode);
        var future=await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,start:Today.AddDays(3))));Assert.Equal("UPCOMING",future.Record.Status);var cancel=new CancelManagementAssignmentRequest(Guid.NewGuid(),1,"Future duty revoked");var cancelled=await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(s)+$"/{future.Record.Id}/cancel",cancel));Assert.Equal("CANCELLED",cancelled.Record.Status);Assert.Equal(Today.AddDays(3),cancelled.Record.StartDate);Assert.Null(cancelled.Record.EndDate);Assert.Equal(Now,cancelled.Record.CancelledAt);Assert.True((await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(s)+$"/{future.Record.Id}/cancel",cancel))).Replayed);
    }
    [PostgresTheory][InlineData("OWNER")][InlineData("TENANT")][InlineData("RESIDENT")][InlineData("ACCOUNTANT")][InlineData("MANAGER")]
    public async Task NonAdministratorsCannotAssignOrEscalate(string role)
    {
        var s=await Seed();using var c=Client(s.Manager,role);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root(s)+$"/{Guid.NewGuid()}/cancel",new CancelManagementAssignmentRequest(Guid.NewGuid(),1,"Cancel"))).StatusCode);if(role!="MANAGER")Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Root(s))).StatusCode);
    }
    [PostgresFact]public async Task SiteAdminCannotAppointOrCloseSiteAdminAndForeignObjectsAreNotVisible()
    {
        var s=await Seed();var other=await Seed();using var c=Client(s.Admin,"SITE_ADMIN");Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root(s),Draft(s.AdminPerson,"SITE_ADMIN"))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync(Root(s),Draft(other.ManagerPerson))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(other))).StatusCode);
        using var super=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");var siteAdmin=await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s),Draft(s.AdminPerson,"SITE_ADMIN")));Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root(s)+$"/{siteAdmin.Record.Id}/cancel",new CancelManagementAssignmentRequest(Guid.NewGuid(),1,"Cancel peer"))).StatusCode);
    }
    [PostgresFact]public async Task ShadowDoesNotChangeLegacyWhileEnforcedRequiresCurrentAssignmentAndRevokesImmediately()
    {
        var s=await Seed();using var super=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");using var legacy=Client(s.Manager,"MANAGER");using var shadow=Client(s.Manager,"MANAGER",s.Tenant,"SHADOW");using var enforced=Client(s.Manager,"MANAGER",s.Tenant,"ENFORCED");
        var old=await Ok<ManagementAccessDecision>(await legacy.GetAsync(Access(s)));Assert.True(old.Allowed);Assert.False(old.AssignmentAllowed);var comparison=await Ok<ManagementAccessDecision>(await shadow.GetAsync(Access(s)));Assert.True(comparison.Allowed);Assert.False(comparison.AssignmentAllowed);
        Assert.False((await Ok<ManagementAccessDecision>(await enforced.GetAsync(Access(s)))).Allowed);Assert.Equal(HttpStatusCode.NotFound,(await enforced.GetAsync($"/api/v2/tenants/{s.Tenant}/people")).StatusCode);
        var row=await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson)));Assert.True((await Ok<ManagementAccessDecision>(await enforced.GetAsync(Access(s)))).Allowed);Assert.Equal(HttpStatusCode.OK,(await enforced.GetAsync($"/api/v2/tenants/{s.Tenant}/people")).StatusCode);
        await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s)+$"/{row.Record.Id}/cancel",new CancelManagementAssignmentRequest(Guid.NewGuid(),1,"Revoke")));Assert.False((await Ok<ManagementAccessDecision>(await enforced.GetAsync(Access(s)))).Allowed);Assert.Equal(HttpStatusCode.NotFound,(await enforced.GetAsync($"/api/v2/tenants/{s.Tenant}/people")).StatusCode);Assert.True((await Ok<ManagementAccessDecision>(await legacy.GetAsync(Access(s)))).Allowed);
    }
    [PostgresFact]public async Task AssignmentNeverCreatesRealmCapabilityAndAccountantCannotReadPeople()
    {
        var s=await Seed();using var super=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,"SITE_ADMIN")));using var resident=Client(s.Manager,"RESIDENT",s.Tenant,"ENFORCED");Assert.False((await Ok<ManagementAccessDecision>(await resident.GetAsync(Access(s)+"?role=SITE_ADMIN"))).Allowed);Assert.Equal(HttpStatusCode.Forbidden,(await resident.GetAsync($"/api/v2/tenants/{s.Tenant}/people")).StatusCode);
        var s2=await Seed();await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s2),Draft(s2.ManagerPerson,"ACCOUNTANT")));using var accountant=Client(s2.Manager,"ACCOUNTANT",s2.Tenant,"ENFORCED");Assert.True((await Ok<ManagementAccessDecision>(await accountant.GetAsync(Access(s2)+"?role=ACCOUNTANT"))).Allowed);Assert.Equal(HttpStatusCode.Forbidden,(await accountant.GetAsync($"/api/v2/tenants/{s2.Tenant}/people")).StatusCode);
    }
    [PostgresFact]public async Task LastEligibleAdministratorCannotBeRemovedOrReplacedWithFutureGap()
    {
        var s=await Seed();using var super=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN",s.Tenant,"ENFORCED");var admin=await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s),Draft(s.AdminPerson,"SITE_ADMIN")));
        var cancel=new CancelManagementAssignmentRequest(Guid.NewGuid(),1,"Revoke admin");Assert.Equal(HttpStatusCode.Conflict,(await super.PostAsJsonAsync(Root(s)+$"/{admin.Record.Id}/cancel",cancel)).StatusCode);
        await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,"SITE_ADMIN",Today.AddDays(1))));Assert.Equal(HttpStatusCode.Conflict,(await super.PostAsJsonAsync(Root(s)+$"/{admin.Record.Id}/cancel",cancel)).StatusCode);
        using var unqualified=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN",s.Tenant,"ENFORCED",qualified:false);Assert.Equal(HttpStatusCode.Conflict,(await unqualified.PostAsJsonAsync(Root(s)+$"/{admin.Record.Id}/end",new EndManagementAssignmentRequest(Guid.NewGuid(),1,Today,"Unqualified replacement"))).StatusCode);
        var end=await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s)+$"/{admin.Record.Id}/end",new EndManagementAssignmentRequest(Guid.NewGuid(),1,Today,"Handover tomorrow")));Assert.Equal(2,end.Record.Version);
    }
    [PostgresTheory][InlineData("FUTURE")][InlineData("ENDED")][InlineData("LINK_ENDED")][InlineData("ACCOUNT_DISABLED")][InlineData("MEMBERSHIP_DISABLED")][InlineData("WRONG_ISSUER")][InlineData("ASSIGNMENT_DISABLED")][InlineData("PERSON_DISABLED")][InlineData("PROPERTY_DISABLED")][InlineData("TENANT_DISABLED")]
    public async Task InactiveOrInvalidBindingsNeverSatisfyEnforcedAccess(string mode)
    {
        var s=await Seed();using var super=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");var draft=Draft(s.ManagerPerson,start:mode=="FUTURE"?Today.AddDays(1):mode=="ENDED"?Today.AddDays(-3):Today,end:mode=="ENDED"?Today.AddDays(-1):null);await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s),draft));
        await factory.WithDb(async db=>{if(mode=="LINK_ENDED")(await db.Set<CentralPersonAccountLink>().SingleAsync(l=>l.TenantId==s.Tenant && l.PersonId==s.ManagerPerson)).End(Now);if(mode=="ACCOUNT_DISABLED")(await db.Set<PersonUserAccount>().FindAsync(s.Account))!.SetEnabled(false);if(mode=="MEMBERSHIP_DISABLED")(await db.Set<PersonTenantMembership>().FindAsync(s.Member))!.SetEnabled(false);if(mode=="WRONG_ISSUER")db.Entry((await db.Set<PersonUserAccount>().FindAsync(s.Account))!).Property(a=>a.Issuer).CurrentValue="https://wrong.invalid/realm";if(mode=="ASSIGNMENT_DISABLED")(await db.Set<ManagementAssignment>().SingleAsync(a=>a.TenantId==s.Tenant)).SetEnabled(false);if(mode=="PERSON_DISABLED")(await db.Set<CentralPerson>().FindAsync(s.ManagerPerson))!.SetEnabled(false);if(mode=="PROPERTY_DISABLED")(await db.Properties.FindAsync(s.Property))!.SetEnabled(false);if(mode=="TENANT_DISABLED")(await db.Set<PersonTenant>().FindAsync(s.Tenant))!.SetEnabled(false);await db.SaveChangesAsync();});using var c=Client(s.Manager,"MANAGER",s.Tenant,"ENFORCED");Assert.False((await Ok<ManagementAccessDecision>(await c.GetAsync(Access(s)))).Allowed);
    }
    [PostgresFact]public async Task FeatureOffAnonymousValidationAndActivePersonGuardAreSafe()
    {
        var s=await Seed();using var off=Client(s.Admin,"SITE_ADMIN",enabled:false);Assert.Equal(HttpStatusCode.NotFound,(await off.GetAsync(Root(s))).StatusCode);using var anon=Client(null,"");Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Root(s))).StatusCode);using var super=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.BadRequest,(await super.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson) with{OperationId=Guid.Empty})).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await super.GetAsync(Root(s)+"?pageSize=101")).StatusCode);
        using var forged=new StringContent(JsonSerializer.Serialize(new{operationId=Guid.NewGuid(),personId=s.ManagerPerson,role="MANAGER",startDate=Today,reason="Fixture",is_owner=true}),Encoding.UTF8,"application/json");Assert.Equal(HttpStatusCode.BadRequest,(await super.PostAsync(Root(s),forged)).StatusCode);
        await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,start:Today.AddDays(2))));var profile=new CentralPersonProfile("NATURAL","DOMESTIC","TR","12345678901","Manager","Fixture",null,null,null,null);Assert.Equal(HttpStatusCode.Conflict,(await super.PutAsJsonAsync($"/api/v2/tenants/{s.Tenant}/people/{s.ManagerPerson}",new UpdateCentralPersonRequest(Guid.NewGuid(),1,profile,false))).StatusCode);
    }

    [PostgresFact]public async Task OverlapRaceAndMultiplePropertiesKeepSeparateAssignmentsAndHistoryScope()
    {
        var s=await Seed();using var c=Client(s.Admin,"SITE_ADMIN");var results=await Task.WhenAll(c.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,end:Today.AddDays(2))),c.PostAsJsonAsync(Root(s),Draft(s.ManagerPerson,end:Today.AddDays(3))));Assert.Single(results,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Conflict);
        var row=await Ok<ManagementAssignmentMutation>(results.Single(r=>r.StatusCode==HttpStatusCode.OK));var other=new Property("Second fixture","City","District","Address",PropertyType.APARTMENT);await factory.WithDb(async db=>{db.AddRange(other,new PersonPropertyScope(s.Tenant,other.Id),new PropertyMembership(other.Id,s.Admin));await db.SaveChangesAsync();});var second=s with{Property=other.Id};await Ok<ManagementAssignmentMutation>(await c.PostAsJsonAsync(Root(second),Draft(s.ManagerPerson)));Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(second)+$"/{row.Record.Id}/history")).StatusCode);
    }

    [PostgresFact]public async Task StaleLegacyShadowNeverSatisfiesAnEnforcedManagementGrant()
    {
        var s=await Seed();var old=new Person(s.Property,"Legacy","Fixture","+905551234567","fixture@example.test");old.SetProfile("NATURAL","DOMESTIC","TR","12345678901",null,null,null,null);await factory.WithDb(async db=>{db.Add(old);await db.SaveChangesAsync();});using var super=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");var transition=$"/api/v2/tenants/{s.Tenant}/properties/{s.Property}/person-transition";var preview=await Ok<PersonTransitionPreview>(await super.GetAsync(transition+"/preview"));await Ok<PersonTransitionComparison>(await super.PostAsJsonAsync(transition+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash)));
        Guid canonical=default;await factory.WithDb(async db=>{canonical=(await db.Set<LegacyPersonMap>().SingleAsync(m=>m.LegacyPersonId==old.Id)).PersonId;var prior=await db.Set<CentralPersonAccountLink>().SingleAsync(l=>l.TenantId==s.Tenant && l.PersonId==s.ManagerPerson);prior.End(Now);await db.SaveChangesAsync();db.Add(new CentralPersonAccountLink(s.Tenant,s.Account,canonical,Now.AddMinutes(-1)));await db.SaveChangesAsync();});await Ok<ManagementAssignmentMutation>(await super.PostAsJsonAsync(Root(s),Draft(canonical)));using var manager=Client(s.Manager,"MANAGER",s.Tenant,"ENFORCED");Assert.True((await Ok<ManagementAccessDecision>(await manager.GetAsync(Access(s)))).Allowed);
        await factory.WithDb(async db=>{var source=await db.People.FindAsync(old.Id);source!.Update("Changed",source.LastName,source.PhoneNumber,source.Email,true);await db.SaveChangesAsync();});Assert.False((await Ok<ManagementAccessDecision>(await manager.GetAsync(Access(s)))).Allowed);Assert.Equal(HttpStatusCode.NotFound,(await super.PostAsJsonAsync(Root(s),Draft(canonical,"ACCOUNTANT"))).StatusCode);
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
}
