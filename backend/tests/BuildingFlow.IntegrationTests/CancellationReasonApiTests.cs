using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// TNM 04: dues cancellation reasons are administered system-wide; built-in reasons stay enabled and charges keep their snapshot.
public sealed class CancellationReasonApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Root="/api/v1/definitions/dues-cancellation-reasons";
    private static async Task<T> Value<T>(HttpResponseMessage r){Assert.Equal(HttpStatusCode.OK,r.StatusCode);return(await r.Content.ReadFromJsonAsync<T>())!;}

    [PostgresFact] public async Task SuperAdminCreatesRenamesAndDisablesReasons()
    {
        using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),"SUPER_ADMIN");
        var seeded=(await c.GetFromJsonAsync<CancellationReasonAdminRow[]>(Root))!;Assert.Contains(seeded,x=>x.Code=="OTHER"&&x.Locked&&x.RequiresExplanation);Assert.Contains(seeded,x=>x.Code=="TENANT_MOVED_OUT"&&x.Locked);Assert.Contains(seeded,x=>x.Code=="WRONG_AMOUNT"&&!x.Locked);
        var create=new SaveCancellationReasonRequest("BOARD_DECISION","Yönetim kurulu kararı",true,true,0);
        var created=await Value<CancellationReasonAdminRow>(await c.PostAsJsonAsync(Root,create));Assert.Equal(1,created.Version);Assert.False(created.Locked);Assert.Equal(0,created.UsageCount);
        Assert.Equal(created.Id,(await Value<CancellationReasonAdminRow>(await c.PostAsJsonAsync(Root,create))).Id);
        var dup=await c.PostAsJsonAsync(Root,create with{Name="Başka"});Assert.Equal(HttpStatusCode.Conflict,dup.StatusCode);Assert.Contains("BF_DEFINITION_DUPLICATE",await dup.Content.ReadAsStringAsync());
        var edited=await Value<CancellationReasonAdminRow>(await c.PutAsJsonAsync($"{Root}/{created.Id}",new SaveCancellationReasonRequest(null,"Kurul kararı",false,false,1)));Assert.Equal(2,edited.Version);Assert.False(edited.IsEnabled);Assert.False(edited.RequiresExplanation);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"{Root}/{created.Id}",new SaveCancellationReasonRequest(null,"Eski",true,true,1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PutAsJsonAsync($"{Root}/{created.Id}",new SaveCancellationReasonRequest("RENAMED","Kurul kararı",false,false,2))).StatusCode);
        var other=seeded.Single(x=>x.Code=="OTHER");
        var locked=await c.PutAsJsonAsync($"{Root}/{other.Id}",new SaveCancellationReasonRequest(null,"Diğer",true,false,other.Version));Assert.Equal(HttpStatusCode.BadRequest,locked.StatusCode);Assert.Contains("BF_DEFINITION_LOCKED",await locked.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PutAsJsonAsync($"{Root}/{other.Id}",new SaveCancellationReasonRequest(null,"Diğer",false,true,other.Version))).StatusCode);
        Assert.Equal("Diğer neden",(await Value<CancellationReasonAdminRow>(await c.PutAsJsonAsync($"{Root}/{other.Id}",new SaveCancellationReasonRequest(null,"Diğer neden",true,true,other.Version)))).Name);
        foreach(var bad in new[]{create with{Code="lower"},create with{Code="X"},create with{Name=""},create with{IsEnabled=null},create with{Version=1},create with{Name=new string('a',101)}})Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root,bad with{Code=bad.Code=="BOARD_DECISION"?"NEW_CODE":bad.Code})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync($"{Root}/{Guid.NewGuid()}",new SaveCancellationReasonRequest(null,"X",true,true,1))).StatusCode);
        Assert.DoesNotContain((await c.GetFromJsonAsync<CancellationReasonAdminRow[]>(Root))!,x=>x.Id==created.Id&&x.IsEnabled);
        var p=new Property("Reasons "+Guid.NewGuid(),"City","District","Address",PropertyType.APARTMENT);await factory.WithDb(async db=>{db.Add(p);await db.SaveChangesAsync();});
        using var json=System.Text.Json.JsonDocument.Parse(await c.GetStringAsync($"/api/v1/properties/{p.Id}/dues/cancellation-reasons"));
        Assert.DoesNotContain(json.RootElement.EnumerateArray(),x=>x.GetProperty("code").GetString()=="BOARD_DECISION");
    }

    [PostgresTheory][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task ManagersReadButCannotChangeReasons(string role)
    {
        using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),role);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync(Root)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root,new SaveCancellationReasonRequest("ROLE_TEST","X",true,true,0))).StatusCode);
        using var accountant=TestToken.Client(factory,Guid.NewGuid().ToString(),"ACCOUNTANT");Assert.Equal(HttpStatusCode.Forbidden,(await accountant.GetAsync(Root)).StatusCode);
    }
}
