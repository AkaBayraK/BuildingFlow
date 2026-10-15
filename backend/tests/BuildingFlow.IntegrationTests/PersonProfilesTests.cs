using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class PersonProfilesTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private async Task<(Property P,string Actor,string Recipient)> Seed()
    {
        var actor=Guid.NewGuid().ToString();var recipient=Guid.NewGuid().ToString();var p=new Property("Profile fixture","City","District","Address",PropertyType.APARTMENT);p.AddUnit("1",0,80);
        await factory.WithDb(async db=>{db.Add(p);db.AddRange(new PropertyMembership(p.Id,actor),new PropertyMembership(p.Id,recipient));await db.SaveChangesAsync();});return(p,actor,recipient);
    }
    private static CreatePersonRequest Request(string kind="NATURAL",string citizenship="DOMESTIC",string? account=null)=>new("Fixture",kind=="LEGAL"?"":"Example","+905551234567","fixture@example.test",kind,citizenship,citizenship=="FOREIGN"?"GB":"TR",citizenship=="FOREIGN"?(kind=="LEGAL"?"GB-REG-01":"P1234567"):kind=="LEGAL"?"1234567890":"12345678901",kind=="LEGAL"?null:new DateOnly(1990,1,1),kind=="LEGAL"?null:"FEMALE","Fixture address",account);
    [PostgresTheory][InlineData("NATURAL","DOMESTIC")][InlineData("LEGAL","DOMESTIC")][InlineData("NATURAL","FOREIGN")][InlineData("LEGAL","FOREIGN")]
    public async Task FourProfilesPersistAndKeepCurrentUnitScope(string kind,string citizenship)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var root=$"/api/v1/properties/{x.P.Id}";var r=await c.PostAsJsonAsync(root+"/people",Request(kind,citizenship));Assert.Equal(HttpStatusCode.Created,r.StatusCode);var person=(await r.Content.ReadFromJsonAsync<PersonResponse>())!;
        Assert.Equal(kind,person.PersonKind);Assert.Equal(citizenship,person.Citizenship);Assert.Equal(Request(kind,citizenship).IdentityNumber,person.IdentityNumber);Assert.Equal(x.P.Name,person.PropertyName);
        Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync(root+"/occupancies",new CreateOccupancyRequest(person.Id,x.P.Units.Single().Id,"OWNER",DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3))))).StatusCode);
        person=(await c.GetFromJsonAsync<PersonResponse>(root+"/people/"+person.Id))!;Assert.Single(person.Units!);Assert.Null(person.Units![0].BuildingId);
        var list=await c.GetFromJsonAsync<PagedRows<PersonResponse>>(root+"/people?search=fixture%40example.test");Assert.Contains(list!.Items,p=>p.Id==person.Id);
    }
    [PostgresFact] public async Task MissingContactsIdentityCountryAndFutureBirthAreRejectedAtomically()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var root=$"/api/v1/properties/{x.P.Id}/people";var valid=Request();
        foreach(var r in new[]{valid with{PhoneNumber=null},valid with{Email=null},valid with{Email="bad"},valid with{IdentityNumber=null},valid with{IdentityNumber="01234567890"},valid with{CountryCode="ZZ"},valid with{BirthDate=new(2099,1,1)},Request("LEGAL","FOREIGN") with{CountryCode=null},Request("LEGAL","FOREIGN") with{IdentityNumber=null},Request("NATURAL","FOREIGN") with{IdentityNumber="bad"},Request("LEGAL") with{BirthDate=new(1990,1,1)}})Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(root,r)).StatusCode);
        var list=await c.GetFromJsonAsync<PagedRows<PersonResponse>>(root);Assert.Empty(list!.Items);
    }
    [PostgresFact] public async Task MappingNotificationsReplayAndRecipientIsolation()
    {
        var x=await Seed();using var sender=TestToken.Client(factory,x.Actor,"MANAGER");using var receiver=TestToken.Client(factory,x.Recipient,"RESIDENT");using var outsider=TestToken.Client(factory,Guid.NewGuid().ToString(),"RESIDENT");var root=$"/api/v1/properties/{x.P.Id}/people";
        Assert.Equal(HttpStatusCode.BadRequest,(await sender.PostAsJsonAsync(root,Request(account:"not-a-member"))).StatusCode);
        var accounts=await sender.GetFromJsonAsync<PagedRows<string>>(root+"/account-choices");Assert.Contains(x.Recipient,accounts!.Items);
        Assert.Equal(HttpStatusCode.Forbidden,(await receiver.GetAsync(root+"/account-choices")).StatusCode);
        using var outsideManager=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");Assert.Equal(HttpStatusCode.NotFound,(await outsideManager.GetAsync(root+"/account-choices")).StatusCode);
        var response=await sender.PostAsJsonAsync(root,Request(account:x.Recipient));var person=(await response.Content.ReadFromJsonAsync<PersonResponse>())!;var endpoint=root+"/"+person.Id+"/messages";var req=new SendPersonMessageRequest(Guid.NewGuid(),person.Version,"Fixture title","Fixture message");
        var first=await sender.PostAsJsonAsync(endpoint,req);Assert.Equal(HttpStatusCode.OK,first.StatusCode);var message=(await first.Content.ReadFromJsonAsync<PersonMessageResponse>())!;
        var replay=(await (await sender.PostAsJsonAsync(endpoint,req)).Content.ReadFromJsonAsync<PersonMessageResponse>())!;Assert.Equal(message.Id,replay.Id);
        Assert.Equal(HttpStatusCode.Conflict,(await sender.PostAsJsonAsync(endpoint,req with{Body="Changed"})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await sender.PostAsJsonAsync(endpoint,req with{RequestId=Guid.NewGuid(),Channel="SMS"})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await receiver.PostAsJsonAsync(endpoint,req)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.PutAsync($"/api/v1/notifications/{message.Id}/read",null)).StatusCode);
        var inbox=await receiver.GetFromJsonAsync<PagedRows<PersonMessageResponse>>("/api/v1/notifications");Assert.Contains(inbox!.Items,m=>m.Id==message.Id);
        Assert.Equal(HttpStatusCode.NoContent,(await receiver.PutAsync($"/api/v1/notifications/{message.Id}/read",null)).StatusCode);
        Assert.Empty((await outsider.GetFromJsonAsync<PagedRows<PersonMessageResponse>>("/api/v1/notifications"))!.Items);
        await factory.WithDb(async db=>{var member=await db.PropertyMemberships.SingleAsync(m=>m.PropertyId==x.P.Id&&m.UserId==x.Recipient);member.SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Empty((await receiver.GetFromJsonAsync<PagedRows<PersonMessageResponse>>("/api/v1/notifications"))!.Items);
        Assert.Equal(HttpStatusCode.BadRequest,(await sender.PostAsJsonAsync(endpoint,req with{RequestId=Guid.NewGuid()})).StatusCode);
    }
    [PostgresTheory][InlineData("ACCOUNTANT")][InlineData("RESIDENT")]
    public async Task NonManagersCannotReadOrWriteSensitiveProfiles(string role)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,role);var root=$"/api/v1/properties/{x.P.Id}/people";Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(root)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(root,Request())).StatusCode);
    }
}
