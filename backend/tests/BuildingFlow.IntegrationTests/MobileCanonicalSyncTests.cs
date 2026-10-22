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
public sealed class MobileCanonicalSyncTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private static readonly DateTimeOffset Now=new(2026,10,18,12,0,0,TimeSpan.Zero);private static readonly DateOnly Today=new(2026,10,18);
    private sealed class Clock:TimeProvider{public override DateTimeOffset GetUtcNow()=>Now;}
    private readonly List<WebApplicationFactory<Program>> children=[];
    private HttpClient Client(string? subject,string role="MANAGER",bool enabled=true,bool contexts=true,bool sync=true)
    {
        var child=factory.WithWebHostBuilder(b=>{b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"PersonRefactor:MobileSyncEnabled",sync?"true":"false"},{"PersonRefactor:Enabled","true"},{"PersonRefactor:SessionContextsEnabled",contexts?"true":"false"},{"PersonRefactor:MobileAuthorizationEnabled",enabled?"true":"false"}}));b.ConfigureServices(s=>{s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(new Clock());});});children.Add(child);var c=child.CreateClient();using var signed=TestToken.Client(factory,subject,role);c.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return c;
    }
    private sealed record Fixture(Guid Tenant,Guid Property,Guid Unit,Guid Person,string User);
    private async Task<Fixture> Seed()
    {
        var t=new PersonTenant("Mobile authorization fixture");var p=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,100);var user=Guid.NewGuid().ToString();var account=new PersonUserAccount("http://localhost:8080/realms/buildingflow",user);var person=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Fixture","Self",null,null,null,null);await factory.WithDb(async db=>{db.AddRange(t,p,account,person,new PersonTenantMembership(t.Id,account.Id),new CentralPersonAccountLink(t.Id,account.Id,person.Id,Now.AddDays(-1)),new PropertyMembership(p.Id,user),new PersonPropertyScope(t.Id,p.Id));await db.SaveChangesAsync();});return new(t.Id,p.Id,u.Id,person.Id,user);
    }
    private async Task<(Fixture S,Guid Context)> Setup(string role="MANAGER"){
        var s=await Seed();Guid id=Guid.Empty;await factory.WithDb(async db=>{if(role=="RESIDENT"){var r=TestRelations.Effective(new UnitResidency(s.Tenant,s.Property,s.Unit,s.Person,Today));id=r.Id;db.Add(r);}else{var a=new ManagementAssignment(s.Tenant,s.Property,s.Person,role,Today);id=a.Id;db.Add(a);}await db.SaveChangesAsync();});return(s,id);
    }
    private static MobileSyncCommand Command(Fixture s,Guid id,string role="MANAGER")=>new(Guid.NewGuid(),Guid.NewGuid(),s.Property,role=="RESIDENT"?s.Unit:null,role,"CREATE",0,"Canonical request","FAULT","HIGH",ContextKind:role=="RESIDENT"?"RESIDENT":"MANAGEMENT",ContextId:id);
    private static Task<HttpResponseMessage> Push(HttpClient c,MobileSyncCommand cmd)=>c.PostAsJsonAsync("/api/v1/mobile/sync/commands",cmd);
    private static string Pull(MobileSyncCommand c)=>$"/api/v1/mobile/sync/requests?propertyId={c.PropertyId}&role={c.Role}&contextKind={c.ContextKind}&contextId={c.ContextId}"+(c.UnitId==null?"":$"&unitId={c.UnitId}");
    private static async Task<MobileSyncResult> Ok(HttpResponseMessage r){Assert.Equal(HttpStatusCode.OK,r.StatusCode);return(await r.Content.ReadFromJsonAsync<MobileSyncResult>())!;}
    [PostgresTheory][InlineData("MANAGER")][InlineData("RESIDENT")] public async Task CanonicalCrudReplayConflictAndTombstone(string role){
        var(s,id)=await Setup(role);using var c=Client(s.User,role);var cmd=Command(s,id,role);var first=await Ok(await Push(c,cmd));Assert.False(first.Replayed);Assert.True((await Ok(await Push(c,cmd))).Replayed);Assert.Equal(HttpStatusCode.Conflict,(await Push(c,cmd with{Description="Different"})).StatusCode);
        var edit=cmd with{OperationId=Guid.NewGuid(),Action="UPDATE",ExpectedVersion=first.Record.Version,Description="Edited"};var changed=await Ok(await Push(c,edit));Assert.Equal(2,changed.Record.Version);Assert.Equal(HttpStatusCode.Conflict,(await Push(c,edit with{OperationId=Guid.NewGuid()})).StatusCode);
        var deleted=await Ok(await Push(c,cmd with{OperationId=Guid.NewGuid(),Action="DELETE",ExpectedVersion=changed.Record.Version,Description=null,Category=null,Priority=null}));Assert.True(deleted.Record.Deleted);
        var response=await c.GetAsync(Pull(cmd));Assert.Equal(HttpStatusCode.OK,response.StatusCode);var snapshot=(await response.Content.ReadFromJsonAsync<MobileRequestSnapshot>())!;Assert.Equal($"{s.Tenant}:{cmd.ContextKind}:{id}",snapshot.ContextKey);Assert.True(Assert.Single(snapshot.Records).Deleted);
    }
    [PostgresFact]public async Task RevocationRejectsUnsentCommandReplayPullAndPhoto(){
        var(s,id)=await Setup();using var c=Client(s.User);var cmd=Command(s,id);await Ok(await Push(c,cmd));await factory.WithDb(async db=>{(await db.Set<ManagementAssignment>().SingleAsync(x=>x.Id==id)).Cancel(Now,"Fixture revoke");await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Forbidden,(await Push(c,cmd)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await Push(c,cmd with{OperationId=Guid.NewGuid(),RecordId=Guid.NewGuid()})).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Pull(cmd))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/mobile/sync/requests/{cmd.RecordId}/photo?contextKind=MANAGEMENT&contextId={id}")).StatusCode);
    }
    [PostgresFact]public async Task ForeignContextWrongRoleAndLegacyCommandsCannotGrantAccess(){
        var(s,id)=await Setup("RESIDENT");var(other,otherId)=await Setup();using var c=Client(s.User,"MANAGER");var cmd=Command(s,id,"RESIDENT");
        foreach(var bad in new[]{cmd with{ContextId=otherId},cmd with{PropertyId=other.Property},cmd with{Role="MANAGER",UnitId=null},cmd with{ContextId=null,ContextKind=null},cmd with{ContextKind="PLATFORM"}})Assert.Equal(HttpStatusCode.Forbidden,(await Push(c,bad)).StatusCode);
        using var foreign=Client(other.User);Assert.Equal(HttpStatusCode.Forbidden,(await Push(foreign,cmd)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=RESIDENT&unitId={s.Unit}")).StatusCode);
    }
    [PostgresFact]public async Task AccountantAndDisabledSyncCannotWriteRequests(){
        var(s,id)=await Setup("ACCOUNTANT");using var accountant=Client(s.User,"ACCOUNTANT");Assert.Equal(HttpStatusCode.Forbidden,(await Push(accountant,Command(s,id,"ACCOUNTANT"))).StatusCode);var(m,duty)=await Setup();using var off=Client(m.User,sync:false);Assert.Equal(HttpStatusCode.Forbidden,(await Push(off,Command(m,duty))).StatusCode);
    }
    [PostgresFact]public async Task ProfileDraftChangesOnlyOwnAccountAndDisabledCanonicalAccountIsRejected(){
        var(s,id)=await Setup("RESIDENT");using var c=Client(s.User,"RESIDENT");var cmd=new MobileProfileCommand(Guid.NewGuid(),0,"Account","Profile",null,"KEEP",null);var r=await c.PostAsJsonAsync("/api/v1/mobile/profile/commands",cmd);Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.True((await(await c.PostAsJsonAsync("/api/v1/mobile/profile/commands",cmd)).Content.ReadFromJsonAsync<MobileProfileResult>())!.Replayed);
        await factory.WithDb(async db=>{var person=await db.Set<CentralPerson>().SingleAsync(p=>p.Id==s.Person);Assert.Equal("Fixture",person.FirstName);var account=await db.Set<PersonUserAccount>().SingleAsync(a=>a.Subject==s.User);account.SetEnabled(false);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/v1/mobile/profile")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/v1/mobile/consents")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await Push(c,Command(s,id,"RESIDENT"))).StatusCode);
    }
    [PostgresFact]public async Task PhotoIsOptionalConsentProtectedAndBoundToCurrentContext(){
        var(s,id)=await Setup("RESIDENT");using var c=Client(s.User,"RESIDENT");var photo=Convert.ToBase64String(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","avatar.jpg")));var cmd=Command(s,id,"RESIDENT") with{PhotoAction="REPLACE",PhotoBase64=photo};Assert.Equal(HttpStatusCode.Forbidden,(await Push(c,cmd)).StatusCode);await ConsentTestHelper.GrantAsync(factory,c,"REQUEST_PHOTO");var result=await Ok(await Push(c,cmd));Assert.NotNull(result.Record.PhotoId);Assert.True((await Ok(await Push(c,cmd))).Replayed);
        var path=$"/api/v1/mobile/sync/requests/{cmd.RecordId}/photo?contextKind=RESIDENT&contextId={id}";Assert.Equal(HttpStatusCode.OK,(await c.GetAsync(path)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(path.Replace(id.ToString(),Guid.NewGuid().ToString()))).StatusCode);using var foreign=Client(Guid.NewGuid().ToString(),"RESIDENT");Assert.Equal(HttpStatusCode.Forbidden,(await foreign.GetAsync(path)).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await Push(c,cmd with{OperationId=Guid.NewGuid(),RecordId=Guid.NewGuid(),PhotoBase64="invalid"})).StatusCode);
    }
    [PostgresFact]public async Task ConcurrentIdenticalCanonicalPushCreatesOneRequest(){
        var(s,id)=await Setup();using var c=Client(s.User);var cmd=Command(s,id);var results=await Task.WhenAll(Push(c,cmd),Push(c,cmd));foreach(var result in results)await Ok(result);await factory.WithDb(async db=>{Assert.Equal(1,await db.Set<MobileRequest>().CountAsync(r=>r.Id==cmd.RecordId));Assert.Equal(1,await db.Set<MobileOperationReceipt>().CountAsync(r=>r.OperationId==cmd.OperationId && r.OwnerUserId==s.User));});
    }
    [PostgresFact]public async Task ExistingRequestV2ReceiptFingerprintRemainsReplayCompatible(){
        var(s,id)=await Setup();using var c=TestToken.Client(factory,s.User,"MANAGER");var cmd=Command(s,id) with{ContextKind=null,ContextId=null};var record=new MobileRequest(cmd.RecordId,cmd.PropertyId,null,"MANAGER",s.User,cmd.Description!);record.Details(cmd.Category!,cmd.Priority!,null);
        var serialized="request-v2:"+System.Text.Json.JsonSerializer.Serialize(new{cmd.OperationId,cmd.RecordId,cmd.PropertyId,cmd.UnitId,cmd.Role,cmd.Action,cmd.ExpectedVersion,cmd.Description,cmd.Category,cmd.Priority,cmd.PhotoAction,cmd.PhotoBase64});var fingerprint=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(serialized)));
        await factory.WithDb(async db=>{db.Add(record);await db.SaveChangesAsync();var dto=new MobileRequestDto(record.Id,record.PropertyId,null,"MANAGER",record.Description,record.Version,false,record.ModifiedAt,record.Category,record.Priority);db.Add(new MobileOperationReceipt(s.User,cmd.OperationId,fingerprint,System.Text.Json.JsonSerializer.Serialize(dto)));await db.SaveChangesAsync();});Assert.True((await Ok(await Push(c,cmd))).Replayed);
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
}
