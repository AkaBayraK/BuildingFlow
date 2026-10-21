using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Planlama & Bakımlar API and announcement categories against a dedicated PostgreSQL test database.
public sealed class MaintenanceApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Root(Guid p)=>$"/api/v1/properties/{p}/maintenance-plans";
    private async Task<(Property P,string Actor)> Seed(){var actor=Guid.NewGuid().ToString();var p=new Property("Plan "+actor,"City","District","Address",PropertyType.APARTMENT);await factory.WithDb(async db=>{db.Add(p);db.Add(new PropertyMembership(p.Id,actor));await db.SaveChangesAsync();});return(p,actor);}
    private static MaintenancePlanRequest Plan(string title,string type,DateOnly date,string status="PLANNED",long version=0)=>new(title," Açıklama ",type,date," A Blok ","Teknik Ekip",status,version);
    private static async Task<MaintenancePlanResponse> Create(HttpClient c,Guid p,MaintenancePlanRequest request){var r=await c.PostAsJsonAsync(Root(p),request);Assert.Equal(HttpStatusCode.Created,r.StatusCode);return(await r.Content.ReadFromJsonAsync<MaintenancePlanResponse>())!;}
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task ManagersCreateEditAndSoftDeleteWithVersionAndAudit(string role)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,role);
        var plan=await Create(c,x.P.Id,Plan(" Jeneratör Bakımı ","MAINTENANCE",Today.AddDays(5)));
        Assert.Equal("Jeneratör Bakımı",plan.Title);Assert.Equal("Açıklama",plan.Description);Assert.Equal("A Blok",plan.Location);Assert.Equal(1,plan.Version);
        var r=await c.PutAsJsonAsync(Root(x.P.Id)+"/"+plan.Id,Plan("Jeneratör Bakımı","MAINTENANCE",Today.AddDays(6),"IN_PROGRESS",plan.Version));Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var edited=(await r.Content.ReadFromJsonAsync<MaintenancePlanResponse>())!;Assert.Equal("IN_PROGRESS",edited.Status);Assert.Equal(2,edited.Version);Assert.Equal(plan.CreatedAt,edited.CreatedAt);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(Root(x.P.Id)+"/"+plan.Id,Plan("Stale","OTHER",Today,"PLANNED",plan.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.DeleteAsync(Root(x.P.Id)+$"/{plan.Id}?version={plan.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync(Root(x.P.Id)+$"/{plan.Id}?version={edited.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(x.P.Id)+"/"+plan.Id)).StatusCode);
        await factory.WithDb(async db=>{var row=await db.MaintenancePlans.IgnoreQueryFilters().SingleAsync(p=>p.Id==plan.Id);Assert.True(row.IsDeleted);Assert.Equal(x.Actor,row.CreatedBy);});
    }
    [PostgresFact] public async Task ListFiltersSearchCountsAndUpcomingAreScoped()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");
        await Create(c,x.P.Id,Plan("Asansör Periyodik Kontrol","INSPECTION",Today.AddDays(10)));
        await Create(c,x.P.Id,Plan("Merdiven Temizliği","CLEANING",Today.AddDays(3)));
        await Create(c,x.P.Id,Plan("Yangın Tatbikatı","DRILL",Today.AddDays(40)));
        await Create(c,x.P.Id,Plan("Eski bakım","MAINTENANCE",Today.AddDays(-2)));
        await Create(c,x.P.Id,Plan("İptal edilen","MAINTENANCE",Today.AddDays(1),"CANCELLED"));
        await Create(c,x.P.Id,Plan("100%_özel","OTHER",Today,"COMPLETED"));
        var page=(await c.GetFromJsonAsync<MaintenancePlanPage>(Root(x.P.Id)))!;Assert.Equal(6,page.TotalCount);Assert.Equal(Today.AddDays(-2),page.Items[0].PlannedDate);
        Assert.Equal(new[]{"MAINTENANCE:2","CLEANING:1","INSPECTION:1","DRILL:1","OTHER:1"},page.TypeCounts.Select(t=>$"{t.Type}:{t.Count}"));
        var typed=(await c.GetFromJsonAsync<MaintenancePlanPage>(Root(x.P.Id)+"?type=MAINTENANCE&status=PLANNED"))!;Assert.Single(typed.Items);Assert.Equal(1,typed.TypeCounts.Single(t=>t.Type=="CLEANING").Count);
        var ranged=(await c.GetFromJsonAsync<MaintenancePlanPage>(Root(x.P.Id)+$"?from={Today:yyyy-MM-dd}&to={Today.AddDays(10):yyyy-MM-dd}"))!;Assert.Equal(4,ranged.TotalCount);
        var search=(await c.GetFromJsonAsync<MaintenancePlanPage>(Root(x.P.Id)+"?search=%25_"))!;Assert.Equal("100%_özel",Assert.Single(search.Items).Title);
        Assert.Equal(6,(await c.GetFromJsonAsync<MaintenancePlanPage>(Root(x.P.Id)+"?search=blok"))!.TotalCount);
        var upcoming=(await c.GetFromJsonAsync<MaintenanceUpcoming>(Root(x.P.Id)+"/upcoming"))!;Assert.Equal(Today,upcoming.Today);
        Assert.Equal(new[]{"Merdiven Temizliği","Asansör Periyodik Kontrol","Yangın Tatbikatı"},upcoming.Items.Select(i=>i.Title));Assert.Equal(2,upcoming.NextDays);
        Assert.Single((await c.GetFromJsonAsync<MaintenanceUpcoming>(Root(x.P.Id)+"/upcoming?limit=1"))!.Items);
        foreach(var bad in new[]{"?type=FIRE","?status=DONE","?page=0","?pageSize=101",$"?from={Today:yyyy-MM-dd}&to={Today.AddDays(-1):yyyy-MM-dd}","/upcoming?limit=0","/upcoming?limit=11"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Root(x.P.Id)+bad)).StatusCode);
    }
    [PostgresTheory][InlineData("","MAINTENANCE","PLANNED")][InlineData("T","FIRE","PLANNED")][InlineData("T","DRILL","DONE")][InlineData("T","maintenance","PLANNED")]
    public async Task InvalidPlansAreCodedAndNothingIsSaved(string title,string type,string status)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");
        var r=await c.PostAsJsonAsync(Root(x.P.Id),new MaintenancePlanRequest(title,null,type,Today,null,null,status));Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Contains("BF_PLAN_INVALID",await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(x.P.Id),new MaintenancePlanRequest("T",null,"OTHER",null,null,null,"PLANNED"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(x.P.Id),new MaintenancePlanRequest("T",new string('x',2001),"OTHER",Today,null,null,"PLANNED"))).StatusCode);
        await factory.WithDb(async db=>Assert.False(await db.MaintenancePlans.AnyAsync(p=>p.PropertyId==x.P.Id)));
        var valid=await Create(c,x.P.Id,Plan("Valid","OTHER",Today));
        await factory.WithDb(async db=>await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE maintenance_plans SET type='FIRE' WHERE id={valid.Id}")));
    }
    [PostgresFact] public async Task ScopeJwtRolesAccountantReadOnlyAndInactiveParent()
    {
        var x=await Seed();var other=await Seed();using var admin=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var plan=await Create(admin,x.P.Id,Plan("Kontrol","INSPECTION",Today));
        using var anon=factory.CreateClient();using var outsider=TestToken.Client(factory,other.Actor,"MANAGER");using var resident=TestToken.Client(factory,x.Actor,"RESIDENT");using var accountant=TestToken.Client(factory,x.Actor,"ACCOUNTANT");
        foreach(var suffix in new[]{"","/upcoming",$"/{plan.Id}"}){
            Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Root(x.P.Id)+suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(Root(x.P.Id)+suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await resident.GetAsync(Root(x.P.Id)+suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await accountant.GetAsync(Root(x.P.Id)+suffix)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden,(await accountant.PostAsJsonAsync(Root(x.P.Id),Plan("X","OTHER",Today))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await accountant.DeleteAsync(Root(x.P.Id)+$"/{plan.Id}?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await outsider.PutAsJsonAsync(Root(x.P.Id)+"/"+plan.Id,Plan("X","OTHER",Today,"PLANNED",1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync(Root(other.P.Id)+"/"+plan.Id)).StatusCode);
        await factory.WithDb(async db=>{(await db.Properties.SingleAsync(p=>p.Id==x.P.Id)).SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(Root(x.P.Id),Plan("New","OTHER",Today))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await admin.GetAsync(Root(x.P.Id))).StatusCode);
    }
    [PostgresFact] public async Task AnnouncementCategoriesFilterCountAndOlderClientsKeepCategory()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var root=$"/api/v1/properties/{x.P.Id}/announcements";
        async Task<AnnouncementResponse> Post(AnnouncementRequest request){var r=await c.PostAsJsonAsync(root,request);Assert.Equal(HttpStatusCode.Created,r.StatusCode);return(await r.Content.ReadFromJsonAsync<AnnouncementResponse>())!;}
        var legacy=await Post(new("Genel","İçerik","ALL_MEMBERS"));Assert.Equal("GENERAL",legacy.Category);
        var dues=await Post(new("Aidat","Son gün 31 Ekim","ALL_MEMBERS",Category:"DUES"));await Post(new("Toplantı","Genel kurul","ALL_MEMBERS",Category:"MEETING"));await Post(new("Acil","Su kesintisi","ALL_MEMBERS",Category:"URGENT"));
        var r=await c.PutAsJsonAsync(root+"/"+dues.Id,new AnnouncementRequest("Aidat","Güncel","ALL_MEMBERS",dues.Version));Assert.Equal("DUES",(await r.Content.ReadFromJsonAsync<AnnouncementResponse>())!.Category);
        var page=(await c.GetFromJsonAsync<ContentPage<AnnouncementResponse>>(root+"?category=DUES"))!;Assert.Equal("Aidat",Assert.Single(page.Items).Title);
        Assert.Equal(new[]{"GENERAL:1","MEETING:1","DUES:1","MAINTENANCE:0","URGENT:1","OTHER:0"},page.CategoryCounts!.Select(t=>$"{t.Category}:{t.Count}"));
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(root+"?category=FIRE")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(root,new AnnouncementRequest("x","y","ALL_MEMBERS",Category:"general"))).StatusCode);
        await factory.WithDb(async db=>await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE announcements SET category='FIRE' WHERE id={legacy.Id}")));
    }
}
