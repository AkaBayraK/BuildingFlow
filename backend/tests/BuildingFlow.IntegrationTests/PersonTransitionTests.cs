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
public sealed class PersonTransitionTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private WebApplicationFactory<Program>? enabled;
    private HttpClient Client(string role="SUPER_ADMIN",bool feature=true,string? user=null)
    {
        enabled??=factory.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"PersonRefactor:Enabled","true"},{"PersonRefactor:TransitionEnabled","true"}})));
        var c=(feature?enabled:factory).CreateClient();using var auth=TestToken.Client(factory,user??Guid.NewGuid().ToString(),role);c.DefaultRequestHeaders.Authorization=auth.DefaultRequestHeaders.Authorization;return c;
    }
    private sealed record Fixture(Guid Tenant,Guid Property,Guid Unit,Guid Person,Guid Link);
    private async Task<Fixture> Seed(bool profile=true)
    {
        var t=new PersonTenant("Fixture tenant");var p=new Property("Altunbayrak Rezidans fixture","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",1,100);
        var person=new Person(p.Id,"Fixture","Person","+905551234567","fixture@example.test");
        if(profile)person.SetProfile("NATURAL","DOMESTIC","TR","12345678901",null,null,"Local address",null);
        var second=new Person(p.Id,"Co","Owner","+905551234567","fixture@example.test");second.SetProfile("NATURAL","DOMESTIC","TR","12345678901",null,null,null,null);
        var owner=new UnitOccupancy(p.Id,u.Id,person.Id,OccupancyType.OWNER,new(2025,1,1),null,true);
        var owner2=new UnitOccupancy(p.Id,u.Id,second.Id,OccupancyType.OWNER,new(2025,1,1));
        var tenant=new UnitOccupancy(p.Id,u.Id,person.Id,OccupancyType.TENANT,new(2025,1,1),new(2025,12,31));
        var resident=new UnitOccupancy(p.Id,u.Id,second.Id,OccupancyType.RESIDENT,new(2026,1,1));
        await factory.WithDb(async db=>{db.AddRange(t,p,person,second,owner,owner2,tenant,resident);await db.SaveChangesAsync();});return new(t.Id,p.Id,u.Id,person.Id,tenant.Id);
    }
    private static string Root(Fixture s)=>$"/api/v2/tenants/{s.Tenant}/properties/{s.Property}/person-transition";
    private static async Task<T> Ok<T>(HttpResponseMessage response){Assert.Equal(HttpStatusCode.OK,response.StatusCode);return(await response.Content.ReadFromJsonAsync<T>())!;}
    [PostgresFact]public async Task DryRunIsReadonlyAndAtomicApplyPreservesLegacyIdsProfilesAndUnknownShares()
    {
        var s=await Seed();using var c=Client();string before="";await factory.WithDb(async db=>before=await db.Database.SqlQuery<string>($"SELECT jsonb_agg(to_jsonb(p) ORDER BY id)::text AS \"Value\" FROM people p WHERE property_id={s.Property}").SingleAsync());
        var preview=await Ok<PersonTransitionPreview>(await c.GetAsync(Root(s)+"/preview"));Assert.True(preview.CanApply);Assert.Equal(2,preview.People);Assert.Equal(2,preview.Ownerships);Assert.Equal(1,preview.Tenancies);Assert.Equal(1,preview.Residencies);
        await factory.WithDb(async db=>Assert.False(await db.Set<PersonPropertyScope>().AnyAsync(p=>p.PropertyId==s.Property)));
        var applied=await Ok<PersonTransitionComparison>(await c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash)));Assert.True(applied.Matches);Assert.Equal(2,applied.PersonMaps);Assert.Equal(4,applied.OccupancyMaps);
        var repeat=await Ok<PersonTransitionComparison>(await c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash)));Assert.Equal(applied,repeat);
        var compatible=await Ok<CompatiblePerson>(await c.GetAsync(Root(s)+"/people/"+s.Person));Assert.Equal(s.Person,compatible.LegacyId);Assert.Equal(1,compatible.LegacyViewVersion);Assert.NotEqual(s.Person,compatible.CanonicalId);Assert.Equal("Local address",compatible.LocalProfile.Address);
        await factory.WithDb(async db=>{Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT jsonb_agg(to_jsonb(p) ORDER BY id)::text AS \"Value\" FROM people p WHERE property_id={s.Property}").SingleAsync());Assert.All(await db.Set<UnitOwnership>().Where(r=>r.PropertyId==s.Property).ToArrayAsync(),r=>Assert.Null(r.ShareNumerator));Assert.Single(await db.Set<UnitTenancy>().Where(r=>r.PropertyId==s.Property).ToArrayAsync());Assert.Single(await db.Set<UnitResidency>().Where(r=>r.PropertyId==s.Property).ToArrayAsync());Assert.Equal(s.Link,(await db.Set<LegacyOccupancyMap>().SingleAsync(m=>m.LegacyOccupancyId==s.Link)).LegacyOccupancyId);Assert.Equal(2,await db.Set<CentralPerson>().CountAsync(p=>p.TenantId==s.Tenant));});
        var centralUpdate=new UpdateCentralPersonRequest(Guid.NewGuid(),1,compatible.Identity,true);Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v2/tenants/{s.Tenant}/people/{compatible.CanonicalId}",centralUpdate)).StatusCode);
    }
    [PostgresFact]public async Task MissingProfileAndStaleDryRunRejectWithoutPartialMappings()
    {
        using var c=Client();var incomplete=await Seed(false);var bad=await Ok<PersonTransitionPreview>(await c.GetAsync(Root(incomplete)+"/preview"));Assert.False(bad.CanApply);Assert.Equal(1,bad.UnprofiledPeople);Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(incomplete)+"/apply",new ApplyPersonTransitionRequest(bad.SourceHash))).StatusCode);
        var s=await Seed();var preview=await Ok<PersonTransitionPreview>(await c.GetAsync(Root(s)+"/preview"));await factory.WithDb(async db=>{var p=await db.People.FindAsync(s.Person);p!.Update("Updated",p.LastName,p.PhoneNumber,p.Email,true);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash))).StatusCode);
        await factory.WithDb(async db=>{Assert.False(await db.Set<LegacyPersonMap>().AnyAsync(m=>m.PropertyId==s.Property || m.PropertyId==incomplete.Property));Assert.False(await db.Set<CentralPerson>().AnyAsync(p=>p.TenantId==s.Tenant || p.TenantId==incomplete.Tenant));});
    }
    [PostgresFact]public async Task ConcurrentApplyCreatesOneSnapshotAndLegacyEditsInvalidateShadowReads()
    {
        var s=await Seed();using var c=Client();var preview=await Ok<PersonTransitionPreview>(await c.GetAsync(Root(s)+"/preview"));var responses=await Task.WhenAll(c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash)),c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash)));foreach(var response in responses)Assert.True((await Ok<PersonTransitionComparison>(response)).Matches);
        await factory.WithDb(async db=>{var link=await db.UnitOccupancies.FindAsync(s.Link);db.Entry(link!).Property(p=>p.IsEnabled).IsModified=true;await db.SaveChangesAsync();});var compared=await Ok<PersonTransitionComparison>(await c.GetAsync(Root(s)+"/comparison"));Assert.False(compared.Matches);Assert.Equal(1,compared.DriftedOccupancies);
        await factory.WithDb(async db=>{var p=await db.People.FindAsync(s.Person);p!.Update("Changed",p.LastName,p.PhoneNumber,p.Email,true);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.Conflict,(await c.GetAsync(Root(s)+"/people/"+s.Person)).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash))).StatusCode);
    }
    [PostgresTheory][InlineData("OWNER")][InlineData("TENANT")][InlineData("RESIDENT")][InlineData("ACCOUNTANT")][InlineData("MANAGER")]
    public async Task MigrationIsSuperAdminOnlyAndGeneralResidentsCannotRead(string role)
    {
        var s=await Seed();using var c=Client(role);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Root(s)+"/preview")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(new string('A',64)))).StatusCode);Assert.Equal(role=="MANAGER"?HttpStatusCode.NotFound:HttpStatusCode.Forbidden,(await c.GetAsync(Root(s)+"/comparison")).StatusCode);
    }
    [PostgresFact]public async Task FeatureDefaultsOffAndForeignTenantCannotRemapAnExistingProperty()
    {
        var s=await Seed();using var off=Client(feature:false);Assert.Equal(HttpStatusCode.NotFound,(await off.GetAsync(Root(s)+"/preview")).StatusCode);using var c=Client();var preview=await Ok<PersonTransitionPreview>(await c.GetAsync(Root(s)+"/preview"));await Ok<PersonTransitionComparison>(await c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash)));var other=await Seed();var wrong=s with{Tenant=other.Tenant};var different=await Ok<PersonTransitionPreview>(await c.GetAsync(Root(wrong)+"/preview"));Assert.False(different.CanApply);Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(wrong)+"/apply",new ApplyPersonTransitionRequest(different.SourceHash))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(wrong)+"/people/"+s.Person)).StatusCode);
    }

    [PostgresFact]public async Task ManagerNeedsBothActiveTenantAndPropertyMembershipsWithExactIssuer()
    {
        var s=await Seed();using var super=Client();var preview=await Ok<PersonTransitionPreview>(await super.GetAsync(Root(s)+"/preview"));await Ok<PersonTransitionComparison>(await super.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash)));
        var user=Guid.NewGuid().ToString();var account=new PersonUserAccount("http://localhost:8080/realms/buildingflow",user);var member=new PersonTenantMembership(s.Tenant,account.Id);var propertyMember=new PropertyMembership(s.Property,user);
        await factory.WithDb(async db=>{db.AddRange(account,member);await db.SaveChangesAsync();});using var manager=Client("MANAGER",user:user);Assert.Equal(HttpStatusCode.NotFound,(await manager.GetAsync(Root(s)+"/people/"+s.Person)).StatusCode);
        await factory.WithDb(async db=>{db.Add(propertyMember);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.OK,(await manager.GetAsync(Root(s)+"/people/"+s.Person)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await manager.GetAsync(Root(s)+"/comparison")).StatusCode);
        await factory.WithDb(async db=>{(await db.Set<PersonTenantMembership>().FindAsync(member.Id))!.SetEnabled(false);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.NotFound,(await manager.GetAsync(Root(s)+"/people/"+s.Person)).StatusCode);
        var wrong=Guid.NewGuid().ToString();var wrongAccount=new PersonUserAccount("https://wrong-issuer.invalid/realm",wrong);await factory.WithDb(async db=>{db.AddRange(wrongAccount,new PersonTenantMembership(s.Tenant,wrongAccount.Id),new PropertyMembership(s.Property,wrong));await db.SaveChangesAsync();});using var wrongIssuer=Client("MANAGER",user:wrong);Assert.Equal(HttpStatusCode.NotFound,(await wrongIssuer.GetAsync(Root(s)+"/people/"+s.Person)).StatusCode);
    }
    [PostgresFact]public async Task DisabledAndDeletedLegacyHistoryIsCopiedButNeverExposedAsAnActivePerson()
    {
        var s=await Seed();await factory.WithDb(async db=>{var person=await db.People.FindAsync(s.Person);person!.SetEnabled(false);db.Remove(person);var link=await db.UnitOccupancies.FindAsync(s.Link);db.Remove(link!);await db.SaveChangesAsync();});using var c=Client();var preview=await Ok<PersonTransitionPreview>(await c.GetAsync(Root(s)+"/preview"));Assert.True(preview.CanApply);Assert.True((await Ok<PersonTransitionComparison>(await c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash)))).Matches);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(s)+"/people/"+s.Person)).StatusCode);
        await factory.WithDb(async db=>{var map=await db.Set<LegacyPersonMap>().SingleAsync(m=>m.LegacyPersonId==s.Person);var canonical=await db.Set<CentralPerson>().IgnoreQueryFilters().SingleAsync(p=>p.Id==map.PersonId);Assert.True(canonical.IsDeleted);Assert.False(canonical.IsEnabled);var link=await db.Set<LegacyOccupancyMap>().SingleAsync(m=>m.LegacyOccupancyId==s.Link);Assert.True((await db.Set<UnitTenancy>().IgnoreQueryFilters().SingleAsync(r=>r.Id==link.TenancyId)).IsDeleted);});
    }

    [PostgresFact]public async Task DryRunRejectsFiniteCrossKindPrimaryConflictBeforeWritingAnyShadowRows()
    {
        var s=await Seed();await factory.WithDb(async db=>{db.Add(new UnitOccupancy(s.Property,s.Unit,s.Person,OccupancyType.RESIDENT,new(2026,2,1),new(2026,2,2),true));await db.SaveChangesAsync();});using var c=Client();var preview=await Ok<PersonTransitionPreview>(await c.GetAsync(Root(s)+"/preview"));Assert.False(preview.CanApply);Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(s)+"/apply",new ApplyPersonTransitionRequest(preview.SourceHash))).StatusCode);await factory.WithDb(async db=>Assert.False(await db.Set<LegacyPersonMap>().AnyAsync(m=>m.PropertyId==s.Property)));
    }
    public void Dispose()=>enabled?.Dispose();
}
