using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MobileSyncTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory> {
    private async Task<(HttpClient Client,Guid Property,string Actor,Guid Membership)> Setup(string tokenRole="MANAGER") {
        var actor=Guid.NewGuid().ToString();var property=new Property("Sync fixture","City","District","Address");var member=new PropertyMembership(property.Id,actor);
        await factory.WithDb(async db=>{db.Add(property);db.Add(member);await db.SaveChangesAsync();});
        return (TestToken.Client(factory,actor,tokenRole),property.Id,actor,member.Id);
    }
    private static MobileSyncCommand Create(Guid property)=>new(Guid.NewGuid(),Guid.NewGuid(),property,null,"MANAGER","CREATE",0,"Fixture request");
    private static Task<HttpResponseMessage> Push(HttpClient c,MobileSyncCommand command)=>c.PostAsJsonAsync("/api/v1/mobile/sync/commands",command);
    [PostgresFact] public async Task SameOperationReplaysAndDifferentPayloadConflicts() {
        var s=await Setup();using var c=s.Client;var cmd=Create(s.Property);
        var first=await (await Push(c,cmd)).Content.ReadFromJsonAsync<MobileSyncResult>();Assert.NotNull(first);Assert.False(first.Replayed);
        var again=await (await Push(c,cmd)).Content.ReadFromJsonAsync<MobileSyncResult>();Assert.True(again!.Replayed);Assert.Equal(first.Record.Id,again.Record.Id);
        Assert.Equal(HttpStatusCode.Conflict,(await Push(c,cmd with {Description="Different"})).StatusCode);
        var list=await c.GetFromJsonAsync<MobileRequestSnapshot>($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=MANAGER");Assert.Single(list!.Records);
    }
    [PostgresFact] public async Task ConcurrentDuplicateCreatesOnlyOneRecord() {
        var s=await Setup();using var c=s.Client;var cmd=Create(s.Property);
        var responses=await Task.WhenAll(Push(c,cmd),Push(c,cmd));
        Assert.All(responses,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));
        var list=await c.GetFromJsonAsync<MobileRequestSnapshot>($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=MANAGER");Assert.Single(list!.Records);
    }
    [PostgresFact] public async Task UpdateConflictDeleteTombstoneAndReplayPreserveHistory() {
        var s=await Setup();using var c=s.Client;var cmd=Create(s.Property);
        var created=(await (await Push(c,cmd)).Content.ReadFromJsonAsync<MobileSyncResult>())!;
        var edit=cmd with {OperationId=Guid.NewGuid(),Action="UPDATE",ExpectedVersion=created.Record.Version,Description="Changed"};
        var updated=(await (await Push(c,edit)).Content.ReadFromJsonAsync<MobileSyncResult>())!;Assert.Equal(created.Record.Version+1,updated.Record.Version);
        Assert.Equal(HttpStatusCode.Conflict,(await Push(c,edit with {OperationId=Guid.NewGuid()})).StatusCode);
        var deleted=edit with {OperationId=Guid.NewGuid(),Action="DELETE",ExpectedVersion=updated.Record.Version,Description=null};
        var result=(await (await Push(c,deleted)).Content.ReadFromJsonAsync<MobileSyncResult>())!;Assert.True(result.Record.Deleted);Assert.Null(result.Record.Description);
        Assert.True((await (await Push(c,deleted)).Content.ReadFromJsonAsync<MobileSyncResult>())!.Replayed);
        var list=await c.GetFromJsonAsync<MobileRequestSnapshot>($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=MANAGER");Assert.True(Assert.Single(list!.Records).Deleted);
        Assert.Equal(HttpStatusCode.Conflict,(await Push(c,edit with {OperationId=Guid.NewGuid(),ExpectedVersion=result.Record.Version})).StatusCode);
    }
    [PostgresFact] public async Task OtherAccountInactiveMembershipAndForgedRoleCannotReplayOrRead() {
        var s=await Setup();using var c=s.Client;var cmd=Create(s.Property);Assert.Equal(HttpStatusCode.OK,(await Push(c,cmd)).StatusCode);
        using var foreign=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");
        Assert.Equal(HttpStatusCode.Forbidden,(await Push(foreign,cmd)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=OWNER")).StatusCode);
        await factory.WithDb(async db=>{var m=await db.PropertyMemberships.FindAsync(s.Membership);m!.SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Forbidden,(await Push(c,cmd)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=MANAGER")).StatusCode);
    }
    [PostgresFact] public async Task SamePropertyOtherMemberCannotEditAnotherAuthorsRequest() {
        var s=await Setup();using var c=s.Client;var cmd=Create(s.Property);var row=(await (await Push(c,cmd)).Content.ReadFromJsonAsync<MobileSyncResult>())!;
        var other=Guid.NewGuid().ToString();await factory.WithDb(async db=>{db.Add(new PropertyMembership(s.Property,other));await db.SaveChangesAsync();});
        using var client=TestToken.Client(factory,other,"MANAGER");
        Assert.Equal(HttpStatusCode.Forbidden,(await Push(client,cmd with {OperationId=Guid.NewGuid(),Action="UPDATE",ExpectedVersion=row.Record.Version})).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<MobileRequestSnapshot>($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=MANAGER"))!.Records);
    }
    [PostgresFact] public async Task ActiveOccupantCanSubmitOnlyForOwnUnitAndExpiredOccupancyCannotReplay() {
        var actor=Guid.NewGuid().ToString();var property=new Property("Occupant sync","City","District","Address");var building=property.AddBuilding("A");var unit=building.AddUnit("1",1,90m);
        var person=new Person(property.Id,"Fixture","User","5550000000","fixture@example.test");person.SetProfile("NATURAL","FOREIGN","US","TEST1234",null,null,null,actor);
        var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
        var occupancy=new UnitOccupancy(property.Id,unit.Id,person.Id,OccupancyType.TENANT,today.AddDays(-1));
        await factory.WithDb(async db=>{db.Add(property);db.Add(person);db.Add(occupancy);db.Add(new PropertyMembership(property.Id,actor));await db.SaveChangesAsync();});
        using var client=TestToken.Client(factory,actor,"RESIDENT");var cmd=Create(property.Id) with {UnitId=unit.Id,Role="TENANT"};
        Assert.Equal(HttpStatusCode.OK,(await Push(client,cmd)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await Push(client,cmd with {OperationId=Guid.NewGuid(),RecordId=Guid.NewGuid(),UnitId=Guid.NewGuid()})).StatusCode);
        await factory.WithDb(async db=>{var o=await db.UnitOccupancies.FindAsync(occupancy.Id);o!.SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Forbidden,(await Push(client,cmd)).StatusCode);
    }
    [PostgresFact] public async Task AnonymousValidationAndAccountingOnlyRoleAreRejected() {
        using var anon=TestToken.Client(factory,null,"");Assert.Equal(HttpStatusCode.Unauthorized,(await Push(anon,Create(Guid.NewGuid()))).StatusCode);
        var s=await Setup();using var c=s.Client;var cmd=Create(s.Property);
        Assert.Equal(HttpStatusCode.BadRequest,(await Push(c,cmd with {ExpectedVersion=1})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Push(c,cmd with {Description=new string('x',2001)})).StatusCode);
        var a=await Setup("ACCOUNTANT");using var account=a.Client;Assert.Equal(HttpStatusCode.Forbidden,(await Push(account,Create(a.Property) with {Role="ACCOUNTANT"})).StatusCode);
    }
}
