using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class PaymentApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Root(Guid property) => $"/api/v1/properties/{property}/payments";
    private static string Dues(Guid property) => $"/api/v1/properties/{property}/dues";
    private sealed record Fixture(string Actor, Property Property, Unit Unit, Unit OtherUnit, DuesCharge[] Charges);
    private async Task<Fixture> Seed(bool apartment=true)
    {
        var actor=Guid.NewGuid().ToString(); var property=new Property("Payments "+actor,"City","District","Address",apartment?PropertyType.APARTMENT:PropertyType.SITE);
        var building=apartment?null:property.AddBuilding("A_%");var unit=building is null?property.AddUnit("1_%",1,100):building.AddUnit("1_%",1,100);
        var other=building is null?property.AddUnit("2",2,100):building.AddUnit("2",2,100);
        var assessments=Enumerable.Range(1,3).Select(i=>new DuesAssessment(property.Id,Guid.NewGuid(),new string('a',64),new(Today.AddMonths(-i).Year,Today.AddMonths(-i).Month,1),12.35m,"TRY",Today.AddDays(2),"Dues")).ToArray();
        var charges=assessments.Select(batch=>batch.AddCharge(unit.Id)).ToArray();
        await factory.WithDb(async db=>{db.Add(property);db.Add(new PropertyMembership(property.Id,actor));db.AddRange(assessments);await db.SaveChangesAsync();});
        return new(actor,property,unit,other,charges);
    }
    private static CreatePaymentRequest Request(Fixture x,string amount="3.10") => new(Guid.NewGuid(),x.Unit.Id,amount,"TRY",Today,"CASH",null," Cash_% ",[new(x.Charges[0].Id,amount,x.Charges[0].Version)]);
    private static async Task<PaymentResponse> Create(HttpClient client,Guid property,CreatePaymentRequest request)
    {
        var response=await client.PostAsJsonAsync(Root(property),request);Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        return(await response.Content.ReadFromJsonAsync<PaymentResponse>())!;
    }
    [PostgresTheory] [InlineData("SUPER_ADMIN")] [InlineData("SITE_ADMIN")] [InlineData("MANAGER")] [InlineData("ACCOUNTANT")]
    public async Task FinanceRolesAllocatePartialPaymentWithTrustedAudit(string role)
    {
        var x=await Seed(role!="MANAGER");using var client=TestToken.Client(factory,x.Actor,role);var payment=await Create(client,x.Property.Id,Request(x));
        Assert.Equal("3.10",payment.Amount);Assert.Equal("RECORDED",payment.Status);Assert.Single(payment.Allocations);Assert.False(payment.Replayed);
        var charge=(await client.GetFromJsonAsync<DuesChargeResponse>(Dues(x.Property.Id)+"/"+x.Charges[0].Id))!;
        Assert.Equal("9.25",charge.Balance);Assert.Equal("PARTIALLY_PAID",charge.Status);Assert.Equal(2,charge.Version);
        var page=(await client.GetFromJsonAsync<DuesPage>(Dues(x.Property.Id)+"?pageSize=1"))!;Assert.Equal("33.95",page.Summary.OutstandingAmount);
        await factory.WithDb(async db=>{var saved=await db.Payments.Include(value=>value.Allocations).SingleAsync(value=>value.Id==payment.Id);Assert.Equal(x.Actor,saved.CreatedBy);Assert.Equal(x.Actor,Assert.Single(saved.Allocations).CreatedBy);});
        if(role=="ACCOUNTANT")Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/v1/properties/{x.Property.Id}/people")).StatusCode);
    }
    [PostgresTheory] [InlineData("RESIDENT")] [InlineData("")] [InlineData("spoofed")] [InlineData("client-role")]
    public async Task ForgedAndUnprivilegedRolesCannotReadCreateOrCancel(string role)
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,role);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(Root(x.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(Root(x.Property.Id)+"/"+Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(Root(x.Property.Id),Request(x))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync(Root(x.Property.Id)+"/"+Guid.NewGuid()+"/cancel",new CancelChargeRequest("Correction",1))).StatusCode);
    }
    [PostgresFact] public async Task AnonymousCrossPropertyAndCrossUnitRequestsDoNotLeakOrWrite()
    {
        var x=await Seed();var other=await Seed();using var anon=TestToken.Client(factory,null,"");using var client=TestToken.Client(factory,x.Actor,"MANAGER");
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Root(x.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.PostAsJsonAsync(Root(x.Property.Id),Request(x))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(Root(other.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsJsonAsync(Root(other.Property.Id),Request(other))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsJsonAsync(Root(x.Property.Id),Request(x) with{UnitId=x.OtherUnit.Id})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsJsonAsync(Root(x.Property.Id),Request(x) with{Allocations=[new(other.Charges[0].Id,"3.10",1)]})).StatusCode);
    }
    [PostgresFact] public async Task AllocationTotalsValidationFutureDateAndAtomicRollback()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var request=Request(x);
        foreach(var invalid in new[]{request with{Amount="1.001"},request with{Amount="1,25"},request with{Currency="USD"},request with{Method="CARD"},request with{PaidDate=null},request with{PaidDate=Today.AddDays(1)},request with{UnitId=Guid.Empty},request with{RequestId=Guid.Empty},request with{Allocations=[]},request with{Allocations=null},request with{Allocations=[new(x.Charges[0].Id,"3.10",0)]},request with{Amount="4.00"},request with{Method="BANK_TRANSFER",Reference=" "},request with{Allocations=[new(x.Charges[0].Id,"1.55",1),new(x.Charges[0].Id,"1.55",1)]}})
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(Root(x.Property.Id),invalid)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(Root(x.Property.Id),request with{Amount="16.00",Allocations=[new(x.Charges[0].Id,"3.00",1),new(x.Charges[1].Id,"13.00",1)]})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(Root(x.Property.Id),request with{Allocations=[new(x.Charges[0].Id,"3.10",999)]})).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<PaymentPage>(Root(x.Property.Id)))!.Items);
        Assert.Equal("12.35",(await client.GetFromJsonAsync<DuesChargeResponse>(Dues(x.Property.Id)+"/"+x.Charges[0].Id))!.Balance);
    }
    [PostgresFact] public async Task NormalizedReplayBankReferenceAndChangedContentAreSafe()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var request=Request(x) with{Method="BANK_TRANSFER",Reference=" bank_123 ",Amount="6.20",Allocations=[new(x.Charges[0].Id,"3.10",1),new(x.Charges[1].Id,"3.10",1)]};
        var first=await Create(client,x.Property.Id,request);Assert.Equal("BANK_123",first.Reference);
        var response=await client.PostAsJsonAsync(Root(x.Property.Id),request with{Amount="0006.20",Reference="BANK_123",Description="Cash_%",Allocations=request.Allocations!.Reverse().ToArray()});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);var replay=(await response.Content.ReadFromJsonAsync<PaymentResponse>())!;Assert.Equal(first.Id,replay.Id);Assert.True(replay.Replayed);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(Root(x.Property.Id),request with{Description="Changed"})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(Root(x.Property.Id),Request(x) with{Method="BANK_TRANSFER",Reference="bank_123",Allocations=[new(x.Charges[2].Id,"3.10",1)]})).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<PaymentPage>(Root(x.Property.Id)))!.Items);
        Assert.Equal("12.35",(await client.GetFromJsonAsync<DuesChargeResponse>(Dues(x.Property.Id)+"/"+x.Charges[2].Id))!.Balance);
    }
    [PostgresFact] public async Task ConcurrentSameRequestReplaysAndCompetingPaymentsCannotOverpay()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var request=Request(x,"12.35");
        var results=await Task.WhenAll(client.PostAsJsonAsync(Root(x.Property.Id),request),client.PostAsJsonAsync(Root(x.Property.Id),request));
        Assert.Single(results,value=>value.StatusCode==HttpStatusCode.Created);Assert.Single(results,value=>value.StatusCode==HttpStatusCode.OK);
        var a=(await results[0].Content.ReadFromJsonAsync<PaymentResponse>())!;var b=(await results[1].Content.ReadFromJsonAsync<PaymentResponse>())!;Assert.Equal(a.Id,b.Id);
        var competing=Request(x,"8.00") with{Allocations=[new(x.Charges[1].Id,"8.00",1)]};
        var race=await Task.WhenAll(client.PostAsJsonAsync(Root(x.Property.Id),competing),client.PostAsJsonAsync(Root(x.Property.Id),competing with{RequestId=Guid.NewGuid()}));
        Assert.Single(race,value=>value.StatusCode==HttpStatusCode.Created);Assert.Single(race,value=>value.StatusCode==HttpStatusCode.Conflict);
        var paid=(await client.GetFromJsonAsync<DuesChargeResponse>(Dues(x.Property.Id)+"/"+x.Charges[0].Id))!;Assert.Equal("PAID",paid.Status);Assert.Equal("0.00",paid.Balance);
        Assert.Equal("PAID",Assert.Single((await client.GetFromJsonAsync<DuesPage>(Dues(x.Property.Id)+"?status=PAID"))!.Items).Status);
        Assert.Equal(2,(await client.GetFromJsonAsync<DuesPage>(Dues(x.Property.Id)+"?status=OUTSTANDING"))!.TotalCount);
    }
    [PostgresFact] public async Task CancellationRestoresAllBalancesKeepsReceiptAndBlocksDebtCancellationUntilReversed()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var request=Request(x) with{Amount="15.45",Allocations=[new(x.Charges[0].Id,"12.35",1),new(x.Charges[1].Id,"3.10",1)]};
        var payment=await Create(client,x.Property.Id,request);var paid=(await client.GetFromJsonAsync<DuesChargeResponse>(Dues(x.Property.Id)+"/"+x.Charges[0].Id))!;
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(Dues(x.Property.Id)+"/"+paid.Id+"/cancel",new CancelChargeRequest("Wrong debt",paid.Version))).StatusCode);
        var url=Root(x.Property.Id)+"/"+payment.Id+"/cancel";
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync(url,new CancelChargeRequest(" ",payment.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(url,new CancelChargeRequest("Correction",999))).StatusCode);
        var response=await client.PutAsJsonAsync(url,new CancelChargeRequest(" Correction ",payment.Version));Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var cancelled=(await response.Content.ReadFromJsonAsync<PaymentResponse>())!;Assert.Equal("CANCELLED",cancelled.Status);Assert.Equal("15.45",cancelled.Amount);Assert.Equal(2,cancelled.Allocations.Count);Assert.Equal(payment.CreatedAt,cancelled.CreatedAt);Assert.Equal(2,cancelled.Version);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(url,new CancelChargeRequest("Rewrite",cancelled.Version))).StatusCode);
        var page=(await client.GetFromJsonAsync<DuesPage>(Dues(x.Property.Id)))!;Assert.Equal("37.05",page.Summary.OutstandingAmount);Assert.All(page.Items,value=>Assert.Equal("12.35",value.Balance));
        var history=(await client.GetFromJsonAsync<PaymentPage>(Root(x.Property.Id)+"?chargeId="+paid.Id))!;Assert.Single(history.Items);Assert.Equal("15.45",history.Summary.CancelledAmount);
        var replay=await client.PostAsJsonAsync(Root(x.Property.Id),request);Assert.Equal(HttpStatusCode.OK,replay.StatusCode);Assert.Equal("CANCELLED",(await replay.Content.ReadFromJsonAsync<PaymentResponse>())!.Status);
    }
    [PostgresFact] public async Task ConcurrentCancelAndPaymentLeaveConsistentLedger()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var payment=await Create(client,x.Property.Id,Request(x,"5.00"));
        var current=(await client.GetFromJsonAsync<DuesChargeResponse>(Dues(x.Property.Id)+"/"+x.Charges[0].Id))!;
        var results=await Task.WhenAll(client.PutAsJsonAsync(Root(x.Property.Id)+"/"+payment.Id+"/cancel",new CancelChargeRequest("Reverse",payment.Version)),
            client.PostAsJsonAsync(Root(x.Property.Id),Request(x,"7.35") with{Allocations=[new(current.Id,"7.35",current.Version)]}));
        Assert.Equal(HttpStatusCode.OK,results[0].StatusCode);Assert.Contains(results[1].StatusCode,new[]{HttpStatusCode.Created,HttpStatusCode.Conflict});
        await factory.WithDb(async db=>{var charge=await db.DuesCharges.SingleAsync(value=>value.Id==current.Id);var actual=await db.PaymentAllocations.Where(part=>part.ChargeId==charge.Id&&db.Payments.Any(p=>p.Id==part.PaymentId&&!p.IsCancelled)).SumAsync(part=>part.Amount);Assert.Equal(actual,charge.PaidAmount);Assert.InRange(charge.Balance,0m,charge.Amount);});
    }
    [PostgresFact] public async Task FilteredSummaryPaginationLiteralSearchAndInactiveProperty()
    {
        var x=await Seed(false);using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");
        await Create(client,x.Property.Id,Request(x));await Create(client,x.Property.Id,Request(x) with{Allocations=[new(x.Charges[1].Id,"3.10",1)]});
        var page=(await client.GetFromJsonAsync<PaymentPage>(Root(x.Property.Id)+"?pageSize=1&search=%5F%25&status=RECORDED"))!;Assert.Equal(2,page.TotalCount);Assert.Single(page.Items);Assert.Equal("6.20",page.Summary.RecordedAmount);
        Assert.Single((await client.GetFromJsonAsync<PaymentPage>(Root(x.Property.Id)+"?chargeId="+x.Charges[1].Id))!.Items);
        foreach(var query in new[]{"page=0","pageSize=101","status=PAID","method=CARD","sortBy=secret","from=2026-11-01&to=2026-10-01"})Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(Root(x.Property.Id)+"?"+query)).StatusCode);
        await factory.WithDb(async db=>{(await db.Properties.SingleAsync(value=>value.Id==x.Property.Id)).SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(Root(x.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(Root(x.Property.Id),Request(x) with{Allocations=[new(x.Charges[2].Id,"3.10",1)]})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(Root(x.Property.Id)+"/"+page.Items[0].Id+"/cancel",new CancelChargeRequest("Correction",1))).StatusCode);
    }
    [PostgresFact] public async Task HistoricalUnitsStayReadableButDeletedMembershipAndPropertyNeverLeak()
    {
        var x=await Seed();using var manager=TestToken.Client(factory,x.Actor,"MANAGER");using var admin=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var payment=await Create(manager,x.Property.Id,Request(x));
        await factory.WithDb(async db=>{db.Remove(await db.Units.SingleAsync(value=>value.Id==x.Unit.Id));await db.SaveChangesAsync();});
        Assert.Equal(payment.UnitNumber,(await manager.GetFromJsonAsync<PaymentResponse>(Root(x.Property.Id)+"/"+payment.Id))!.UnitNumber);
        await Create(manager,x.Property.Id,Request(x) with{Allocations=[new(x.Charges[1].Id,"3.10",1)]});
        await factory.WithDb(async db=>{db.Remove(await db.PropertyMemberships.SingleAsync(value=>value.PropertyId==x.Property.Id&&value.UserId==x.Actor));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.NotFound,(await manager.GetAsync(Root(x.Property.Id))).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await manager.GetAsync(Root(x.Property.Id)+"/"+payment.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await manager.PutAsJsonAsync(Root(x.Property.Id)+"/"+payment.Id+"/cancel",new CancelChargeRequest("Correction",1))).StatusCode);
        await factory.WithDb(async db=>{db.Remove(await db.Properties.SingleAsync(value=>value.Id==x.Property.Id));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync(Root(x.Property.Id)+"/"+payment.Id)).StatusCode);
    }
}
