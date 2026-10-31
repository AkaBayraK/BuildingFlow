using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// TNM 02: system-wide definition overrides are SUPER_ADMIN-only, versioned and limited to lists planned for TNM 02.
public sealed class DefinitionValueApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static string Path(string list,string code)=>$"/api/v1/definitions/{list}/values/{code}";
    private static SaveDefinitionValueRequest Body(long version,string? name="Asansör",bool enabled=true,string? icon="wrench",int order=2)=>new(name,"Elevator",icon,"red",order,enabled,version);
    private static async Task<string?> Code(HttpResponseMessage r)=>JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.TryGetProperty("code",out var c)?c.GetString():null;

    [PostgresFact] public async Task SuperAdminSavesVersionedOverrideAndStaleWritesConflict()
    {
        using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),"SUPER_ADMIN");
        var first=await c.PutAsJsonAsync(Path("REQUEST_CATEGORY","ELEVATOR"),Body(0));Assert.Equal(HttpStatusCode.OK,first.StatusCode);
        var saved=(await first.Content.ReadFromJsonAsync<DefinitionValueView>())!;Assert.Equal(1,saved.Version);Assert.Equal("Asansör",saved.NameTr);Assert.Equal(2,saved.SortOrder);
        var replay=await c.PutAsJsonAsync(Path("REQUEST_CATEGORY","ELEVATOR"),Body(0));Assert.Equal(HttpStatusCode.OK,replay.StatusCode);Assert.Equal(1,(await replay.Content.ReadFromJsonAsync<DefinitionValueView>())!.Version);
        var second=await c.PutAsJsonAsync(Path("REQUEST_CATEGORY","ELEVATOR"),Body(1,"Asansör arızası"));Assert.Equal(2,(await second.Content.ReadFromJsonAsync<DefinitionValueView>())!.Version);
        var stale=await c.PutAsJsonAsync(Path("REQUEST_CATEGORY","ELEVATOR"),Body(1,"Eski ad"));Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);Assert.Equal("BF_RECORD_CHANGED",await Code(stale));
        var disabled=await c.PutAsJsonAsync(Path("REQUEST_CATEGORY","WATER"),Body(0,null,false,null,0));Assert.Equal(HttpStatusCode.OK,disabled.StatusCode);
        using var json=JsonDocument.Parse(await c.GetStringAsync("/api/v1/definitions"));
        var values=json.RootElement.GetProperty("lists").EnumerateArray().Single(x=>x.GetProperty("key").GetString()=="REQUEST_CATEGORY").GetProperty("values").EnumerateArray().ToArray();
        var elevator=values.Single(x=>x.GetProperty("code").GetString()=="ELEVATOR");Assert.Equal("Asansör arızası",elevator.GetProperty("nameTr").GetString());Assert.Equal(2,elevator.GetProperty("version").GetInt64());
        Assert.False(values.Single(x=>x.GetProperty("code").GetString()=="WATER").GetProperty("isEnabled").GetBoolean());
        var other=values.Single(x=>x.GetProperty("code").GetString()=="OTHER");Assert.True(other.GetProperty("locked").GetBoolean());Assert.Equal(0,other.GetProperty("version").GetInt64());
    }

    [PostgresTheory][InlineData("SITE_ADMIN")][InlineData("MANAGER")][InlineData("ACCOUNTANT")]
    public async Task OnlySuperAdminWrites(string role)
    {
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PutAsJsonAsync(Path("VEHICLE_TYPE","CAR"),Body(0))).StatusCode);
        using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),role);Assert.Equal(HttpStatusCode.Forbidden,(await c.PutAsJsonAsync(Path("VEHICLE_TYPE","CAR"),Body(0))).StatusCode);
    }

    [PostgresFact] public async Task FixedFinanceUnknownLockedAndInvalidValuesAreRejected()
    {
        using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),"SUPER_ADMIN");
        foreach(var (list,code) in new[]{("UNIT_TYPE","VILLA"),("PAYMENT_METHOD","CASH")}){var r=await c.PutAsJsonAsync(Path(list,code),Body(0));Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);}
        Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync(Path("REQUEST_CATEGORY","PLUMBING"),Body(0))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync(Path("UNKNOWN_LIST","OTHER"),Body(0))).StatusCode);
        var locked=await c.PutAsJsonAsync(Path("ANNOUNCEMENT_CATEGORY","OTHER"),Body(0,enabled:false));Assert.Equal(HttpStatusCode.BadRequest,locked.StatusCode);Assert.Contains("BF_DEFINITION_LOCKED",await locked.Content.ReadAsStringAsync());
        foreach(var body in new[]{Body(0,icon:"<svg>"),Body(0,order:1000),Body(-1),Body(0,new string('a',101)),new SaveDefinitionValueRequest(null,null,null,"pink",0,true,0),new SaveDefinitionValueRequest(null,null,null,null,null,true,0),new SaveDefinitionValueRequest(null,null,null,null,0,null,0)})
        {var r=await c.PutAsJsonAsync(Path("SURVEY_CATEGORY","SOCIAL"),body);Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Contains("BF_DEFINITION_INVALID",await r.Content.ReadAsStringAsync());}
        using var json=JsonDocument.Parse(await c.GetStringAsync("/api/v1/definitions"));
        Assert.All(json.RootElement.GetProperty("lists").EnumerateArray(),x=>Assert.Equal(x.GetProperty("level").GetString()!="FIXED"&&(x.GetProperty("plannedPhase").GetString() is "TNM_02" or "TNM_05"),x.GetProperty("editable").GetBoolean()));
    }
}
