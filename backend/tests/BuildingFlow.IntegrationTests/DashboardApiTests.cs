using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// İŞ20 Dashboard summary: scoped, read-only, no person data; same occupancy meaning as the monthly report, evaluated for today.
public sealed class DashboardApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today=>DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private async Task<(Property P,string Actor)> Seed(PropertyType type=PropertyType.APARTMENT){var actor=Guid.NewGuid().ToString();var p=new Property("Dashboard "+actor,"City","District","Address",type);await factory.WithDb(async db=>{db.Add(p);db.Add(new PropertyMembership(p.Id,actor));await db.SaveChangesAsync();});return(p,actor);}
    private static string Root(Guid p)=>$"/api/v1/properties/{p}/dashboard";
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")][InlineData("ACCOUNTANT")]
    public async Task EmptyAreaIsZeroWithSixMonthTrendAndAllUnitTypes(string role)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,role);var r=(await c.GetFromJsonAsync<DashboardSummary>(Root(x.P.Id)))!;
        Assert.Equal(x.P.Id,r.PropertyId);Assert.Equal(Today,r.Today);Assert.Equal(0,r.UnitCount);Assert.Equal("0.00",r.OverdueAmount);Assert.Equal("0.00",r.ReceiptsThisMonth);
        Assert.Equal(6,r.Trend.Count);Assert.Equal(Today.ToString("yyyy-MM"),r.Trend[^1].Period);Assert.Equal(Today.AddMonths(-5).ToString("yyyy-MM"),r.Trend[0].Period);
        Assert.Equal(Enum.GetValues<UnitType>().Length,r.UnitTypes.Count);Assert.Empty(r.UpcomingDues);Assert.False(r.MaintenanceAvailable);
        if(role=="ACCOUNTANT")Assert.Null(r.Requests);else Assert.Equal(new DashboardRequests(0,0),r.Requests);
    }
    [PostgresFact] public async Task SummarySeparatesOccupancyOverdueUpcomingCashTrendAndOpenRequestsWithoutNames()
    {
        var x=await Seed(PropertyType.SITE);var t=Today;var month=new DateOnly(t.Year,t.Month,1);
        await factory.WithDb(async db=>{var p=await db.Properties.Include(p=>p.Units).Include(p=>p.Buildings).SingleAsync(p=>p.Id==x.P.Id);var block=p.AddBuilding("A");
            var home=block.AddUnit("1",0,80);var shop=block.AddUnit("2",0,40,UnitType.SHOP);var vacant=block.AddUnit("3",1,90);var off=block.AddUnit("4",2,70,UnitType.OFFICE);off.SetEnabled(false);
            var person=new Person(p.Id,"SECRET_NAME","SECRET_SURNAME");db.Add(person);
            db.Add(new UnitOccupancy(p.Id,home.Id,person.Id,OccupancyType.TENANT,t.AddMonths(-2)));
            db.Add(new UnitOccupancy(p.Id,shop.Id,person.Id,OccupancyType.RESIDENT,t.AddMonths(-2),t.AddDays(-1)));
            db.Add(new UnitOccupancy(p.Id,vacant.Id,person.Id,OccupancyType.OWNER,t.AddMonths(-2)));
            DuesCharge Charge(DateOnly due,decimal amount,Guid unit,int period){var a=new DuesAssessment(p.Id,Guid.NewGuid(),new string('a',64),month.AddMonths(period),amount,"TRY",due,"Dues");var c=a.AddCharge(unit);db.Add(a);return c;}
            var late=Charge(t.AddDays(-3),100,home.Id,-1);Charge(t.AddDays(-3),50,shop.Id,-1);Charge(t.AddDays(5),70,home.Id,0);Charge(t.AddDays(5),30,shop.Id,0);Charge(t.AddDays(45),80,home.Id,1);
            var paid=Charge(t.AddDays(-40),60,shop.Id,-2);
            var payment=new Payment(p.Id,home.Id,Guid.NewGuid(),new string('a',64),25,"TRY",t,PaymentMethod.CASH,null,"Partial");payment.Allocate(late,25);payment.ValidateComplete();db.Add(payment);
            var full=new Payment(p.Id,shop.Id,Guid.NewGuid(),new string('a',64),60,"TRY",month.AddMonths(-1),PaymentMethod.CASH,null,"Full");full.Allocate(paid,60);full.ValidateComplete();db.Add(full);
            db.Add(new Expense(p.Id,Guid.NewGuid(),new string('a',64),ExpenseCategory.ENERGY,12.35m,month.AddMonths(-2),"Energy",null));
            var cancelled=new Expense(p.Id,Guid.NewGuid(),new string('a',64),ExpenseCategory.OTHER,1000,month,"Wrong",null);cancelled.Cancel("Wrong",DateTimeOffset.UtcNow);db.Add(cancelled);
            db.Add(new MobileRequest(Guid.NewGuid(),p.Id,null,"MANAGER",x.Actor,"Open"));var progress=new MobileRequest(Guid.NewGuid(),p.Id,null,"MANAGER",x.Actor,"Working");progress.Manage("IN_PROGRESS","On it");db.Add(progress);
            var done=new MobileRequest(Guid.NewGuid(),p.Id,null,"MANAGER",x.Actor,"Closed");done.Manage("REJECTED","No");db.Add(done);
            await db.SaveChangesAsync();});
        using var c=TestToken.Client(factory,x.Actor,"MANAGER");var response=await c.GetAsync(Root(x.P.Id));Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var text=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("SECRET",text);Assert.DoesNotContain("personId",text);
        var r=(await response.Content.ReadFromJsonAsync<DashboardSummary>())!;
        Assert.Equal(3,r.UnitCount);Assert.Equal(1,r.OccupiedUnitCount);Assert.Equal(2,r.VacantUnitCount);
        Assert.Equal(2,r.UnitTypes.Single(u=>u.Type=="APARTMENT").Count);Assert.Equal(1,r.UnitTypes.Single(u=>u.Type=="SHOP").Count);Assert.Equal(0,r.UnitTypes.Single(u=>u.Type=="OFFICE").Count);
        Assert.Equal("125.00",r.OverdueAmount);Assert.Equal(2,r.OverdueChargeCount);Assert.Equal("100.00",r.UpcomingAmount);Assert.Equal(2,r.UpcomingChargeCount);
        Assert.Equal([new DashboardDue(t.AddDays(5),2,"100.00"),new DashboardDue(t.AddDays(45),1,"80.00")],r.UpcomingDues);
        Assert.Equal("25.00",r.ReceiptsThisMonth);Assert.Equal("60.00",r.Trend[^2].Receipts);Assert.Equal("12.35",r.Trend[^3].Expenses);Assert.Equal("0.00",r.Trend[^1].Expenses);
        Assert.Equal(new DashboardRequests(1,1),r.Requests);
    }
    [PostgresFact] public async Task DashboardRejectsAnonymousResidentOutsiderAndDeletedArea()
    {
        var x=await Seed();using var anon=factory.CreateClient();using var resident=TestToken.Client(factory,x.Actor,"RESIDENT");using var outsider=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");using var admin=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Root(x.P.Id))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await resident.GetAsync(Root(x.P.Id))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(Root(x.P.Id))).StatusCode);
        await factory.WithDb(async db=>{db.Remove(await db.Properties.SingleAsync(p=>p.Id==x.P.Id));await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync(Root(x.P.Id))).StatusCode);
    }
}
