using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// TNM 03: area-specific visibility/order, membership scope, and the write guard that keeps hidden or disabled codes off new records.
public sealed class PropertyDefinitionApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static string Root(Guid p)=>$"/api/v1/properties/{p}";
    private static string Setting(Guid p,string list,string code)=>$"{Root(p)}/definitions/{list}/values/{code}";
    private async Task<(Property Property,string Actor)> Seed()
    {var actor=Guid.NewGuid().ToString();var p=new Property("Definitions "+actor,"City","District","Address",PropertyType.APARTMENT);await factory.WithDb(async db=>{db.Add(p);db.Add(new PropertyMembership(p.Id,actor));await db.SaveChangesAsync();});return(p,actor);}
    private static async Task<JsonElement[]> Values(HttpClient c,Guid p,string list)
    {using var json=JsonDocument.Parse(await c.GetStringAsync(Root(p)+"/definitions"));return json.RootElement.GetProperty("lists").EnumerateArray().Single(x=>x.GetProperty("key").GetString()==list).GetProperty("values").EnumerateArray().Select(x=>x.Clone()).ToArray();}
    private static async Task<string?> FieldCode(HttpResponseMessage r)=>(await r.Content.ReadAsStringAsync()) is var text&&text.Contains("BF_DEFINITION_UNAVAILABLE")?"BF_DEFINITION_UNAVAILABLE":text;

    [PostgresFact] public async Task ManagerHidesAndOrdersOnlyInOwnArea()
    {
        var a=await Seed();var b=await Seed();using var c=TestToken.Client(factory,a.Actor,"MANAGER");
        var hide=await c.PutAsJsonAsync(Setting(a.Property.Id,"ANNOUNCEMENT_CATEGORY","MEETING"),new SavePropertyDefinitionRequest(true,0,0));Assert.Equal(HttpStatusCode.OK,hide.StatusCode);
        var saved=(await hide.Content.ReadFromJsonAsync<PropertyDefinitionValueView>())!;Assert.True(saved.IsHidden);Assert.False(saved.Selectable);Assert.Equal(1,saved.Version);
        Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync(Setting(a.Property.Id,"ANNOUNCEMENT_CATEGORY","MEETING"),new SavePropertyDefinitionRequest(true,0,0))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(Setting(a.Property.Id,"ANNOUNCEMENT_CATEGORY","MEETING"),new SavePropertyDefinitionRequest(false,null,0))).StatusCode);
        var values=await Values(c,a.Property.Id,"ANNOUNCEMENT_CATEGORY");var codes=values.Select(x=>x.GetProperty("code").GetString()).ToList();Assert.True(codes.IndexOf("MEETING")<codes.IndexOf("DUES"));var meeting=values.Single(x=>x.GetProperty("code").GetString()=="MEETING");Assert.True(meeting.GetProperty("isHidden").GetBoolean());Assert.Equal(0,meeting.GetProperty("sortOrder").GetInt32());
        using var other=TestToken.Client(factory,b.Actor,"MANAGER");
        Assert.False((await Values(other,b.Property.Id,"ANNOUNCEMENT_CATEGORY")).Single(x=>x.GetProperty("code").GetString()=="MEETING").GetProperty("isHidden").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(Root(a.Property.Id)+"/definitions")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await other.PutAsJsonAsync(Setting(a.Property.Id,"ANNOUNCEMENT_CATEGORY","URGENT"),new SavePropertyDefinitionRequest(true,null,0))).StatusCode);
        using var accountant=TestToken.Client(factory,a.Actor,"ACCOUNTANT");Assert.Equal(HttpStatusCode.Forbidden,(await accountant.PutAsJsonAsync(Setting(a.Property.Id,"ANNOUNCEMENT_CATEGORY","URGENT"),new SavePropertyDefinitionRequest(true,null,0))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await accountant.GetAsync(Root(a.Property.Id)+"/definitions")).StatusCode);
    }

    [PostgresFact] public async Task LockedSystemOnlyAndInvalidSettingsAreRejected()
    {
        var a=await Seed();using var c=TestToken.Client(factory,a.Actor,"SITE_ADMIN");
        var locked=await c.PutAsJsonAsync(Setting(a.Property.Id,"ANNOUNCEMENT_CATEGORY","OTHER"),new SavePropertyDefinitionRequest(true,null,0));Assert.Equal(HttpStatusCode.BadRequest,locked.StatusCode);Assert.Contains("BF_DEFINITION_LOCKED",await locked.Content.ReadAsStringAsync());
        foreach(var (list,code) in new[]{("VEHICLE_TYPE","VAN"),("SURVEY_CATEGORY","SOCIAL"),("UNIT_TYPE","VILLA")})Assert.Equal(HttpStatusCode.BadRequest,(await c.PutAsJsonAsync(Setting(a.Property.Id,list,code),new SavePropertyDefinitionRequest(true,null,0))).StatusCode);
        foreach(var body in new[]{new SavePropertyDefinitionRequest(null,null,0),new SavePropertyDefinitionRequest(true,1000,0),new SavePropertyDefinitionRequest(true,null,-1)})Assert.Equal(HttpStatusCode.BadRequest,(await c.PutAsJsonAsync(Setting(a.Property.Id,"MAINTENANCE_TYPE","DRILL"),body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync(Setting(a.Property.Id,"MAINTENANCE_TYPE","PAINTING"),new SavePropertyDefinitionRequest(true,null,0))).StatusCode);
    }

    [PostgresFact] public async Task HiddenAndDisabledCodesAreRefusedForNewRecordsButKeptOnExistingOnes()
    {
        var a=await Seed();var b=await Seed();using var c=TestToken.Client(factory,a.Actor,"MANAGER");
        var draft=await c.PostAsJsonAsync(Root(a.Property.Id)+"/announcements",new AnnouncementRequest("Meeting","Body","ALL_MEMBERS",0,"MEETING"));Assert.Equal(HttpStatusCode.Created,draft.StatusCode);var existing=(await draft.Content.ReadFromJsonAsync<AnnouncementResponse>())!;
        await c.PutAsJsonAsync(Setting(a.Property.Id,"ANNOUNCEMENT_CATEGORY","MEETING"),new SavePropertyDefinitionRequest(true,null,0));
        var refused=await c.PostAsJsonAsync(Root(a.Property.Id)+"/announcements",new AnnouncementRequest("Meeting 2","Body","ALL_MEMBERS",0,"MEETING"));Assert.Equal(HttpStatusCode.BadRequest,refused.StatusCode);Assert.Equal("BF_DEFINITION_UNAVAILABLE",await FieldCode(refused));
        var kept=await c.PutAsJsonAsync(Root(a.Property.Id)+"/announcements/"+existing.Id,new AnnouncementRequest("Meeting edited","Body","ALL_MEMBERS",existing.Version,"MEETING"));Assert.Equal(HttpStatusCode.OK,kept.StatusCode);
        using var bc=TestToken.Client(factory,b.Actor,"MANAGER");Assert.Equal(HttpStatusCode.Created,(await bc.PostAsJsonAsync(Root(b.Property.Id)+"/announcements",new AnnouncementRequest("Other area","Body","ALL_MEMBERS",0,"MEETING"))).StatusCode);
        using var admin=TestToken.Client(factory,Guid.NewGuid().ToString(),"SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.OK,(await admin.PutAsJsonAsync("/api/v1/definitions/REQUEST_CATEGORY/values/RENOVATION",new SaveDefinitionValueRequest(null,null,null,null,4,false,0))).StatusCode);
        var request=await c.PostAsJsonAsync(Root(a.Property.Id)+"/requests",new CreateRequest(Guid.NewGuid(),null,"RENOVATION","NORMAL","Paint the hall"));Assert.Equal(HttpStatusCode.BadRequest,request.StatusCode);Assert.Equal("BF_DEFINITION_UNAVAILABLE",await FieldCode(request));
        Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync(Root(a.Property.Id)+"/requests",new CreateRequest(Guid.NewGuid(),null,"WATER","NORMAL","Leak"))).StatusCode);
        var renovation=(await Values(c,a.Property.Id,"REQUEST_CATEGORY")).Single(x=>x.GetProperty("code").GetString()=="RENOVATION");Assert.False(renovation.GetProperty("isEnabled").GetBoolean());Assert.False(renovation.GetProperty("selectable").GetBoolean());
    }
}
