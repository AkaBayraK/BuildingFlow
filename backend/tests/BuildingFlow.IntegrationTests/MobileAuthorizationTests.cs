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
public sealed class MobileAuthorizationTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private static readonly DateTimeOffset Now=new(2026,10,18,12,0,0,TimeSpan.Zero);private static readonly DateOnly Today=new(2026,10,18);
    private sealed class Clock:TimeProvider{public override DateTimeOffset GetUtcNow()=>Now;}
    private readonly List<WebApplicationFactory<Program>> children=[];
    private HttpClient Client(string? subject,string role="MANAGER",bool enabled=true,bool contexts=true)
    {
        var child=factory.WithWebHostBuilder(b=>{b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"PersonRefactor:Enabled","true"},{"PersonRefactor:SessionContextsEnabled",contexts?"true":"false"},{"PersonRefactor:MobileAuthorizationEnabled",enabled?"true":"false"}}));b.ConfigureServices(s=>{s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(new Clock());});});children.Add(child);var c=child.CreateClient();using var signed=TestToken.Client(factory,subject,role);c.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return c;
    }
    private sealed record Fixture(Guid Tenant,Guid Property,Guid Unit,Guid Person,string User);
    private async Task<Fixture> Seed()
    {
        var t=new PersonTenant("Mobile authorization fixture");var p=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,100);var user=Guid.NewGuid().ToString();var account=new PersonUserAccount("http://localhost:8080/realms/buildingflow",user);var person=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Fixture","Self",null,null,null,null);await factory.WithDb(async db=>{db.AddRange(t,p,account,person,new PersonTenantMembership(t.Id,account.Id),new CentralPersonAccountLink(t.Id,account.Id,person.Id,Now.AddDays(-1)),new PropertyMembership(p.Id,user),new PersonPropertyScope(t.Id,p.Id));await db.SaveChangesAsync();});return new(t.Id,p.Id,u.Id,person.Id,user);
    }
    private const string Root="/api/v2/mobile/authorization";
    private static async Task<MobileAuthorizationSnapshot> Ok(HttpResponseMessage r){Assert.True(r.StatusCode==HttpStatusCode.OK,$"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return(await r.Content.ReadFromJsonAsync<MobileAuthorizationSnapshot>())!;}
    [PostgresFact]public async Task ManagementSnapshotIsMinimalReadOnlyAndHasOneHourLease()
    {
        var s=await Seed();var duty=new ManagementAssignment(s.Tenant,s.Property,s.Person,"MANAGER",Today);await factory.WithDb(async db=>{db.Add(duty);await db.SaveChangesAsync();});using var c=Client(s.User);var response=await c.GetAsync(Root);var value=await Ok(response);Assert.Equal(1,value.PolicyVersion);Assert.True(value.OfflineReadOnly);Assert.Equal(s.User,value.UserId);Assert.Equal(Now,value.IssuedAt);Assert.Equal(Now.AddHours(1),value.ExpiresAt);Assert.Equal(s.Person,Assert.Single(value.People).Id);Assert.Equal(duty.Id,Assert.Single(value.Contexts).Id);Assert.Equal("no-store",response.Headers.CacheControl?.ToString());
        var json=await response.Content.ReadAsStringAsync();foreach(var secret in new[]{"identityNumber","12345678901","phoneNumber","issuer","password","accessToken","refreshToken"})Assert.DoesNotContain(secret,json);Assert.NotEqual(value.SnapshotId,(await Ok(await c.GetAsync(Root))).SnapshotId);
    }
    [PostgresTheory][InlineData(false)][InlineData(true)]public async Task ResidentLeaseIsAtMost24HoursAndNeverPassesInclusiveEndDay(bool endsToday)
    {
        var s=await Seed();var relation=TestRelations.Effective(new UnitResidency(s.Tenant,s.Property,s.Unit,s.Person,Today,endsToday?Today:null));await factory.WithDb(async db=>{db.Add(relation);await db.SaveChangesAsync();});using var c=Client(s.User,"RESIDENT");var value=await Ok(await c.GetAsync(Root));Assert.Equal(endsToday?new DateTimeOffset(2026,10,18,21,0,0,TimeSpan.Zero):Now.AddHours(24),value.ExpiresAt);Assert.False(Assert.Single(value.Contexts).CanManageFinance);
    }
    [PostgresFact]public async Task NextOnlineSnapshotRemovesRevokedDutyAndItsPersonProjection()
    {
        var s=await Seed();var duty=new ManagementAssignment(s.Tenant,s.Property,s.Person,"MANAGER",Today);await factory.WithDb(async db=>{db.Add(duty);await db.SaveChangesAsync();});using var c=Client(s.User);Assert.Single((await Ok(await c.GetAsync(Root))).Contexts);await factory.WithDb(async db=>{var row=await db.Set<ManagementAssignment>().SingleAsync(x=>x.Id==duty.Id);row.Cancel(Now,"Fixture revoke");await db.SaveChangesAsync();});var empty=await Ok(await c.GetAsync(Root));Assert.Empty(empty.Contexts);Assert.Empty(empty.People);Assert.Equal(Now.AddHours(1),empty.ExpiresAt);
    }
    [PostgresFact]public async Task ForeignAccountDoesNotReceiveAnotherPersonsTenantOrIdentity()
    {
        var s=await Seed();await factory.WithDb(async db=>{db.Add(TestRelations.Effective(new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,Today)));await db.SaveChangesAsync();});using var foreign=Client(Guid.NewGuid().ToString(),"OWNER");var value=await Ok(await foreign.GetAsync(Root));Assert.Empty(value.Contexts);Assert.Empty(value.People);
    }
    [PostgresFact]public async Task AnonymousAndDisabledGatesCannotFetchSnapshots()
    {
        var s=await Seed();using var anonymous=Client(null);Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Root)).StatusCode);using var off=Client(s.User,enabled:false);Assert.Equal(HttpStatusCode.NotFound,(await off.GetAsync(Root)).StatusCode);using var contextsOff=Client(s.User,contexts:false);Assert.Equal(HttpStatusCode.NotFound,(await contextsOff.GetAsync(Root)).StatusCode);
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
}
