using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class DuesApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Root(Guid property) => $"/api/v1/properties/{property}/dues";
    private async Task<(string Actor, Property Property, Unit[] Units)> Seed(bool apartment=true)
    {
        var actor=Guid.NewGuid().ToString(); var property=new Property("Dues "+actor,"City","District","Address",apartment?PropertyType.APARTMENT:PropertyType.SITE);
        var building=apartment?null:property.AddBuilding("A_%");
        var units=Enumerable.Range(1,3).Select(i=>building is null?property.AddUnit(i+"_U",i,100):building.AddUnit(i+"_U",i,100)).ToArray();
        await factory.WithDb(async db=>{db.Add(property);db.Add(new PropertyMembership(property.Id,actor));await db.SaveChangesAsync();});
        return(actor,property,units);
    }
    private static CreateAssessmentRequest Request(IEnumerable<Unit> units,DateOnly? due=null) =>
        new(Guid.NewGuid(),units.Select(value=>value.Id).ToArray(),Today.AddMonths(-1).ToString("yyyy-MM"),"12.35","TRY",due??Today.AddDays(2)," Monthly dues_% ");
    private static async Task<AssessmentResponse> Create(HttpClient client,Guid property,CreateAssessmentRequest request)
    {
        var response=await client.PostAsJsonAsync(Root(property)+"/assessments",request);
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        return(await response.Content.ReadFromJsonAsync<AssessmentResponse>())!;
    }
    [PostgresTheory] [InlineData("SUPER_ADMIN")] [InlineData("SITE_ADMIN")] [InlineData("MANAGER")] [InlineData("ACCOUNTANT")]
    public async Task FinanceRolesCreateExactBulkChargesAndTrustedAudit(string role)
    {
        var x=await Seed(role=="SUPER_ADMIN");using var client=TestToken.Client(factory,x.Actor,role);
        var batch=await Create(client,x.Property.Id,Request(x.Units));
        Assert.Equal(3,batch.UnitCount);Assert.Equal("37.05",batch.TotalAmount);Assert.False(batch.Replayed);
        var page=(await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"?pageSize=1"))!;
        Assert.Equal(3,page.TotalCount);Assert.Single(page.Items);Assert.Equal("37.05",page.Summary.OutstandingAmount);
        Assert.Equal("0.00",page.Summary.OverdueAmount);Assert.Equal(3,page.Summary.OpenCount);
        Assert.Equal("12.35",page.Items[0].Balance);Assert.Equal(1,page.Items[0].Version);
        var choices=(await client.GetFromJsonAsync<PagedRows<UnitChoice>>(Root(x.Property.Id)+"/unit-choices?pageSize=1"))!;
        Assert.Equal(3,choices.TotalCount);Assert.Single(choices.Items);
        await factory.WithDb(async db=>{
            var saved=await db.DuesAssessments.Include(value=>value.Charges).SingleAsync(value=>value.Id==batch.Id);
            Assert.Equal(x.Actor,saved.CreatedBy);Assert.All(saved.Charges,value=>Assert.Equal(x.Actor,value.CreatedBy));
        });
        if(role=="ACCOUNTANT")Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/v1/properties/{x.Property.Id}/people")).StatusCode);
    }
    [PostgresTheory] [InlineData("RESIDENT")] [InlineData("")] [InlineData("spoofed")] [InlineData("client-role")]
    public async Task UnprivilegedAndForgedRolesAreRejected(string role)
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,role);
        foreach(var path in new[]{"","/unit-choices","/"+Guid.NewGuid(),"/assessments/"+Guid.NewGuid()})
            Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(Root(x.Property.Id)+path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",Request(x.Units))).StatusCode);
    }
    [PostgresFact] public async Task AnonymousAndWrongPropertyAreRejected()
    {
        var x=await Seed();var other=await Seed();
        using var anonymous=TestToken.Client(factory,null,"");using var client=TestToken.Client(factory,x.Actor,"ACCOUNTANT");
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Root(x.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",Request(x.Units))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(Root(other.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsJsonAsync(Root(other.Property.Id)+"/assessments",Request(other.Units))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",Request(other.Units))).StatusCode);
    }
    [PostgresFact] public async Task IdempotencyReplaysNormalizedContentAndRejectsChangedContent()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"MANAGER");var request=Request(x.Units);
        var first=await Create(client,x.Property.Id,request);
        var response=await client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",request with{Amount="0012.35",Description="Monthly dues_%",UnitIds=request.UnitIds!.Reverse().ToArray()});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);var replay=(await response.Content.ReadFromJsonAsync<AssessmentResponse>())!;
        Assert.Equal(first.Id,replay.Id);Assert.True(replay.Replayed);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",request with{Amount="15.00"})).StatusCode);
        Assert.Equal(3,(await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)))!.TotalCount);
    }
    [PostgresFact] public async Task ConcurrentSameRequestCreatesOneAssessmentAndReplaysTheOther()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var request=Request(x.Units);
        var results=await Task.WhenAll(client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",request),client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",request));
        Assert.Single(results,value=>value.StatusCode==HttpStatusCode.Created);Assert.Single(results,value=>value.StatusCode==HttpStatusCode.OK);
        var a=(await results[0].Content.ReadFromJsonAsync<AssessmentResponse>())!;var b=(await results[1].Content.ReadFromJsonAsync<AssessmentResponse>())!;
        Assert.Equal(a.Id,b.Id);Assert.Equal(3,(await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)))!.TotalCount);
    }
    [PostgresFact] public async Task ConcurrentDifferentRequestsAndPartialDuplicateNeverLeavePartialDebt()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");
        var first=Request([x.Units[0]]);var second=first with{RequestId=Guid.NewGuid()};
        var results=await Task.WhenAll(client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",first),client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",second));
        Assert.Single(results,value=>value.StatusCode==HttpStatusCode.Created);Assert.Single(results,value=>value.StatusCode==HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",Request(x.Units))).StatusCode);
        Assert.Equal(1,(await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)))!.TotalCount);
        await factory.WithDb(async db=>Assert.Equal(1,await db.DuesAssessments.CountAsync(value=>value.PropertyId==x.Property.Id)));
    }
    [PostgresFact] public async Task CancellationUsesVersionKeepsHistoryAndAllowsNewAssessment()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");await Create(client,x.Property.Id,Request([x.Units[0]],Today.AddDays(-1)));
        var charge=Assert.Single((await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)))!.Items);
        Assert.Equal("OVERDUE",charge.Status);
        var url=Root(x.Property.Id)+"/"+charge.Id+"/cancel";
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync(url,new CancelChargeRequest(" ",1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(url,new CancelChargeRequest("Correction",99))).StatusCode);
        var response=await client.PutAsJsonAsync(url,new CancelChargeRequest(" Correction ",charge.Version));
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);var cancelled=(await response.Content.ReadFromJsonAsync<DuesChargeResponse>())!;
        Assert.Equal("CANCELLED",cancelled.Status);Assert.Equal("0.00",cancelled.Balance);Assert.Equal("12.35",cancelled.Amount);
        Assert.Equal(charge.CreatedAt,cancelled.CreatedAt);Assert.Equal(2,cancelled.Version);Assert.Equal("Correction",cancelled.CancellationReason);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync(url,new CancelChargeRequest("Rewrite",cancelled.Version))).StatusCode);
        await Create(client,x.Property.Id,Request([x.Units[0]]));
        var page=(await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)))!;
        Assert.Equal(2,page.TotalCount);Assert.Equal("12.35",page.Summary.OutstandingAmount);Assert.Equal("12.35",page.Summary.CancelledAmount);
    }
    [PostgresFact] public async Task HistorySurvivesUnitDeletionButDeletedMembershipAndPropertyNeverLeak()
    {
        var x=await Seed(false);using var manager=TestToken.Client(factory,x.Actor,"MANAGER");using var admin=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");
        await Create(manager,x.Property.Id,Request([x.Units[0]]));
        var charge=Assert.Single((await manager.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)))!.Items);
        await factory.WithDb(async db=>{db.Remove(await db.Units.SingleAsync(value=>value.Id==x.Units[0].Id));await db.SaveChangesAsync();});
        Assert.Equal(charge.UnitNumber,(await manager.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/"+charge.Id))!.UnitNumber);
        await factory.WithDb(async db=>{db.Remove(await db.PropertyMemberships.SingleAsync(value=>value.PropertyId==x.Property.Id&&value.UserId==x.Actor));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.NotFound,(await manager.GetAsync(Root(x.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await manager.GetAsync(Root(x.Property.Id)+"/"+charge.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await admin.GetAsync(Root(x.Property.Id)+"/"+charge.Id)).StatusCode);
        await factory.WithDb(async db=>{db.Remove(await db.Properties.SingleAsync(value=>value.Id==x.Property.Id));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync(Root(x.Property.Id)+"/"+charge.Id)).StatusCode);
    }
    [PostgresFact] public async Task FinancialHistoryBlocksPropertyDeletionAndCannotBeHiddenBySoftDelete()
    {
        var x=await Seed();using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var batch=await Create(client,x.Property.Id,Request([x.Units[0]]));
        await factory.WithDb(async db=>{foreach(var unit in await db.Units.Where(value=>value.PropertyId==x.Property.Id).ToListAsync())db.Remove(unit);await db.SaveChangesAsync();});
        var property=(await client.GetFromJsonAsync<PropertyResponse>($"/api/v1/properties/{x.Property.Id}"))!;
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync($"/api/v1/properties/{x.Property.Id}?version={property.Version}")).StatusCode);
        await factory.WithDb(async db=>{db.Remove(await db.DuesCharges.SingleAsync(value=>value.AssessmentId==batch.Id));await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());});
        await factory.WithDb(async db=>{(await db.DuesAssessments.SingleAsync(value=>value.Id==batch.Id)).SetEnabled(false);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());});
    }
    [PostgresFact] public async Task ValidationFilterSummaryLiteralSearchAndInactiveParents()
    {
        var x=await Seed(false);using var client=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var request=Request(x.Units,Today.AddDays(-1));
        foreach(var invalid in new[]{request with{Amount="1.001"},request with{Amount="1,25"},request with{Currency="USD"},request with{RequestId=Guid.Empty},request with{UnitIds=[]},request with{UnitIds=[x.Units[0].Id,x.Units[0].Id]},request with{Period="2026-13"},request with{DueDate=null}})
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",invalid)).StatusCode);
        await Create(client,x.Property.Id,request);
        var page=(await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"?search="+Uri.EscapeDataString("_%")+"&pageSize=1&status=OVERDUE"))!;
        Assert.Equal(3,page.TotalCount);Assert.Equal("37.05",page.Summary.OverdueAmount);Assert.Single(page.Items);
        Assert.Equal(0,(await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"?status=OPEN"))!.TotalCount);
        var filtered=(await client.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"?unitId="+x.Units[1].Id))!;
        Assert.Equal(1,filtered.TotalCount);Assert.Equal("12.35",filtered.Summary.OutstandingAmount);
        foreach(var q in new[]{"page=0","pageSize=101","sortBy=secret","status=unknown","period=bad","unitId="+Guid.Empty,"dueFrom=2026-11-01&dueTo=2026-10-01"})
            Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(Root(x.Property.Id)+"?"+q)).StatusCode);
        await factory.WithDb(async db=>{(await db.Buildings.SingleAsync(value=>value.PropertyId==x.Property.Id)).SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Empty((await client.GetFromJsonAsync<PagedRows<UnitChoice>>(Root(x.Property.Id)+"/unit-choices"))!.Items);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(Root(x.Property.Id)+"/assessments",request with{RequestId=Guid.NewGuid(),Period=Today.ToString("yyyy-MM"),DueDate=Today.AddDays(2)})).StatusCode);
    }
}
