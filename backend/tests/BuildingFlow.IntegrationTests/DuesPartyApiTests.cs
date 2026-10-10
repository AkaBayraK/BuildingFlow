using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class DuesPartyApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly DateOnly Start = new(2026,9,1), End = new(2026,10,15);
    private sealed record Fixture(string Actor,Property Property,Unit[] Units,Person Tenant,Person Owner,UnitOccupancy Link);
    private static string Root(Guid id)=>$"/api/v1/properties/{id}";
    private async Task<Fixture> Seed(bool apartment=true)
    {
        var actor=Guid.NewGuid().ToString();var p=new Property("Parties "+actor,"City","District","Address",apartment?PropertyType.APARTMENT:PropertyType.SITE);
        var b=apartment?null:p.AddBuilding("A");var units=Enumerable.Range(1,2).Select(i=>b is null?p.AddUnit(i.ToString(),i,100):b.AddUnit(i.ToString(),i,100)).ToArray();
        var tenant=new Person(p.Id,"Tenant_%","Original");var owner=new Person(p.Id,"Owner","Original");
        var link=new UnitOccupancy(p.Id,units[0].Id,tenant.Id,OccupancyType.TENANT,Start);
        await factory.WithDb(async db=>{db.Add(p);db.Add(new PropertyMembership(p.Id,actor));db.AddRange(tenant,owner,link,new UnitOccupancy(p.Id,units[0].Id,owner.Id,OccupancyType.OWNER,Start));await db.SaveChangesAsync();});
        return new(actor,p,units,tenant,owner,link);
    }
    private static CreateAssessmentRequest Request(Fixture x,string period="2026-11",string mode="TENANT",Guid? unit=null)=>new(Guid.NewGuid(),[unit??x.Units[0].Id],period,"12.35","TRY",DateOnly.Parse(period+"-28"),"Dues",mode);
    private static async Task<DuesChargeResponse> Charge(HttpClient c,Fixture x,string period="2026-11",string mode="TENANT")
    {
        Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",Request(x,period,mode))).StatusCode);
        return Assert.Single((await c.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"/dues?period="+period))!.Items);
    }
    private static Task<TenantMoveOutPreview?> Preview(HttpClient c,Fixture x,DateOnly? end=null,long? version=null)=>c.GetFromJsonAsync<TenantMoveOutPreview>(Root(x.Property.Id)+$"/occupancies/{x.Link.Id}/end-preview?endDate={end??End:yyyy-MM-dd}&version={version??x.Link.Version}");
    private static Task<HttpResponseMessage> Finish(HttpClient c,Fixture x,IReadOnlyList<ChargeCancellationApproval>? approvals=null,long? version=null)=>c.PutAsJsonAsync(Root(x.Property.Id)+$"/occupancies/{x.Link.Id}/end",new EndOccupancyRequest(End,version??x.Link.Version,approvals));

    [PostgresTheory][InlineData(true)][InlineData(false)]
    public async Task SnapshotSurvivesPersonRenameAndRelationshipEndAndPersonChoicesUseOnlyDebts(bool apartment)
    {
        var x=await Seed(apartment);using var c=TestToken.Client(factory,x.Actor,"MANAGER");var charge=await Charge(c,x);
        Assert.Equal(apartment,charge.BuildingId is null);Assert.True(charge.PartiesRecorded);Assert.True(charge.PartiesVisible);
        Assert.Equal(2,charge.Parties!.Count);Assert.Equal("Tenant_% Original",Assert.Single(charge.Parties,p=>p.IsDebtor).PersonName);
        Assert.Contains(charge.Parties,p=>p.Type=="OWNER"&&p.PersonId==x.Owner.Id);
        var snapshot=charge.Parties.ToArray();
        await factory.WithDb(async db=>{var person=await db.People.SingleAsync(p=>p.Id==x.Tenant.Id);person.Update("Renamed","Now",null,null,true);db.Add(new Person(x.Property.Id,"Never","Assessed"));await db.SaveChangesAsync();});
        var end=await Finish(c,x);Assert.Equal(HttpStatusCode.OK,end.StatusCode);var ended=(await end.Content.ReadFromJsonAsync<OccupancyResponse>())!;
        Assert.Equal(0,ended.TenantDebtResult!.CancelledCount);Assert.Equal(1,ended.TenantDebtResult.PendingCount);
        var saved=(await c.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/dues/"+charge.Id))!;
        Assert.Equal(snapshot,saved.Parties);Assert.Equal(charge.Version,saved.Version);Assert.Equal(charge.Amount,saved.Balance);
        var options=(await c.GetFromJsonAsync<PagedRows<DuesPersonChoice>>(Root(x.Property.Id)+"/dues/person-choices?pageSize=1"))!;
        Assert.Equal(2,options.TotalCount);Assert.Single(options.Items);
        var matching=(await c.GetFromJsonAsync<PagedRows<DuesPersonChoice>>(Root(x.Property.Id)+"/dues/person-choices?search="+Uri.EscapeDataString("_%")))!;
        Assert.Equal("Tenant_% Original",Assert.Single(matching.Items).Name);
        foreach(var query in new[]{"personId="+x.Tenant.Id,"personId="+x.Owner.Id+"&partyRole=OWNER","search="+Uri.EscapeDataString("_%")})
        {var page=(await c.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"/dues?"+query))!;Assert.Equal(1,page.TotalCount);Assert.Equal("12.35",page.Summary.OutstandingAmount);}
        Assert.Empty((await c.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"/dues?personId="+x.Owner.Id+"&partyRole=TENANT"))!.Items);
        // After the last valid day, a new tenant assessment cannot silently reuse this relationship.
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",Request(x,"2026-12"))).StatusCode);
    }
    [PostgresFact] public async Task MissingOrAmbiguousDebtorRollsBackEntireBatchAndUnitModeRecordsAllParties()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var request=Request(x) with{UnitIds=x.Units.Select(u=>u.Id).ToArray()};
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",request)).StatusCode);
        Assert.Equal(0,(await c.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"/dues"))!.TotalCount);
        await factory.WithDb(async db=>{var second=new Person(x.Property.Id,"Second","Tenant");db.Add(second);db.Add(new UnitOccupancy(x.Property.Id,x.Units[0].Id,second.Id,OccupancyType.TENANT,Start));await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",Request(x))).StatusCode);
        var charge=await Charge(c,x,mode:"UNIT");Assert.Equal(3,charge.Parties!.Count);Assert.All(charge.Parties,p=>Assert.False(p.IsDebtor));
    }
    [PostgresFact] public async Task PeriodStartInclusiveAndOwnerSelectionReplayAreStable()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");
        await factory.WithDb(async db=>{var person=new Person(x.Property.Id,"Boundary","Resident");db.Add(person);db.Add(new UnitOccupancy(x.Property.Id,x.Units[0].Id,person.Id,OccupancyType.RESIDENT,new(2026,11,1),new(2026,11,1)));db.Add(new UnitOccupancy(x.Property.Id,x.Units[1].Id,x.Tenant.Id,OccupancyType.TENANT,new(2026,11,2)));await db.SaveChangesAsync();});
        var request=Request(x,mode:"OWNER");var first=await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",request);Assert.Equal(HttpStatusCode.Created,first.StatusCode);
        var retry=await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",request);Assert.Equal(HttpStatusCode.OK,retry.StatusCode);Assert.True((await retry.Content.ReadFromJsonAsync<AssessmentResponse>())!.Replayed);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",request with{DebtorType="TENANT"})).StatusCode);
        var charge=Assert.Single((await c.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"/dues"))!.Items);Assert.Equal(3,charge.Parties!.Count);Assert.Equal(x.Owner.Id,Assert.Single(charge.Parties,p=>p.IsDebtor).PersonId);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",Request(x,unit:x.Units[1].Id))).StatusCode);
        Assert.Equal(0,(await Preview(c,x))!.EligibleCount);
    }
    [PostgresFact] public async Task AccountantCannotReadSearchFilterOrAssignPersonalParties()
    {
        var x=await Seed();using var manager=TestToken.Client(factory,x.Actor,"MANAGER");var charge=await Charge(manager,x);
        using var c=TestToken.Client(factory,x.Actor,"ACCOUNTANT");var root=Root(x.Property.Id)+"/dues";
        var response=(await c.GetFromJsonAsync<DuesChargeResponse>(root+"/"+charge.Id))!;Assert.False(response.PartiesVisible);Assert.Empty(response.Parties!);
        Assert.DoesNotContain("Tenant_%",await c.GetStringAsync(root));
        Assert.Empty((await c.GetFromJsonAsync<DuesPage>(root+"?search=Tenant"))!.Items);
        foreach(var path in new[]{"/person-choices","?personId="+x.Tenant.Id,"?partyRole=TENANT"})Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(root+path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(root+"/assessments",Request(x,"2026-12"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Root(x.Property.Id)+$"/occupancies/{x.Link.Id}/end-preview?endDate=2026-10-15&version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync(root+"/cancellation-reasons")).StatusCode);
    }
    [PostgresFact] public async Task NewEndpointsKeepMembershipDeletedParentAnonymousAndForgedRoleGuards()
    {
        var x=await Seed();using var manager=TestToken.Client(factory,x.Actor,"MANAGER");await Charge(manager,x);
        var paths=new[]{Root(x.Property.Id)+"/dues/person-choices",Root(x.Property.Id)+"/dues/cancellation-reasons",Root(x.Property.Id)+$"/occupancies/{x.Link.Id}/end-preview?endDate=2026-10-15&version=1"};
        using var anonymous=TestToken.Client(factory,null,"");using var forged=TestToken.Client(factory,x.Actor,"spoofed");using var outsider=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");
        foreach(var path in paths){Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(path)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await forged.GetAsync(path)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(path)).StatusCode);}
        await factory.WithDb(async db=>{db.Remove(await db.PropertyMemberships.SingleAsync(m=>m.PropertyId==x.Property.Id));await db.SaveChangesAsync();});
        foreach(var path in paths)Assert.Equal(HttpStatusCode.NotFound,(await manager.GetAsync(path)).StatusCode);
        await factory.WithDb(async db=>{db.Remove(await db.Properties.SingleAsync(p=>p.Id==x.Property.Id));await db.SaveChangesAsync();});
        using var admin=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");foreach(var path in paths)Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync(path)).StatusCode);
    }
    [PostgresTheory][InlineData(false)][InlineData(true)]
    public async Task TenantMoveOutRequiresExplicitApprovalAndPreservesSameMonthOwnerAndUnitDebts(bool approve)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");
        var past=await Charge(c,x,"2026-09");var same=await Charge(c,x,"2026-10");var future=await Charge(c,x,"2026-11");var owner=await Charge(c,x,"2026-12","OWNER");var unit=await Charge(c,x,"2027-01","UNIT");
        var preview=(await Preview(c,x))!;Assert.Equal(1,preview.EligibleCount);Assert.Equal("12.35",preview.TotalAmount);Assert.Equal(future.Id,Assert.Single(preview.Items).Id);
        var result=await Finish(c,x,approve?preview.Items.Select(i=>new ChargeCancellationApproval(i.Id,i.Version)).ToArray():null);Assert.Equal(HttpStatusCode.OK,result.StatusCode);
        var body=(await result.Content.ReadFromJsonAsync<OccupancyResponse>())!;Assert.Equal(approve?1:0,body.TenantDebtResult!.CancelledCount);Assert.Equal(approve?0:1,body.TenantDebtResult.PendingCount);
        var saved=(await c.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/dues/"+future.Id))!;Assert.Equal(approve?"CANCELLED":future.Status,saved.Status);Assert.Equal(future.Amount,saved.Amount);Assert.Equal(future.CreatedAt,saved.CreatedAt);
        if(approve){Assert.Equal("TENANT_MOVED_OUT",saved.CancellationReasonCode);Assert.Equal(future.Version+1,saved.Version);await factory.WithDb(async db=>Assert.Equal(x.Link.Id,(await db.DuesCharges.SingleAsync(d=>d.Id==future.Id)).CancelledByOccupancyId));}
        foreach(var keep in new[]{past,same,owner,unit})Assert.Equal(keep.Version,(await c.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/dues/"+keep.Id))!.Version);
    }
    [PostgresFact] public async Task PreviewConsentDoesNotIncludeNewDebtsCreatedAfterReview()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");await Charge(c,x);var preview=(await Preview(c,x))!;var later=await Charge(c,x,"2026-12");
        var response=await Finish(c,x,preview.Items.Select(i=>new ChargeCancellationApproval(i.Id,i.Version)).ToArray());Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal(1,(await response.Content.ReadFromJsonAsync<OccupancyResponse>())!.TenantDebtResult!.PendingCount);
        Assert.NotEqual("CANCELLED",(await c.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/dues/"+later.Id))!.Status);
    }
    [PostgresTheory][InlineData(false)][InlineData(true)]
    public async Task PaidAndReversedPaymentHistoryAreExcludedAndStaleApprovalRollsBackEnd(bool reverse)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var charge=await Charge(c,x);var preview=(await Preview(c,x))!;
        var paymentRequest=new CreatePaymentRequest(Guid.NewGuid(),x.Units[0].Id,"1.00","TRY",DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)),"CASH",null,"Receipt",[new(charge.Id,"1.00",charge.Version)]);
        var payment=await c.PostAsJsonAsync(Root(x.Property.Id)+"/payments",paymentRequest);Assert.Equal(HttpStatusCode.Created,payment.StatusCode);
        var receipt=(await payment.Content.ReadFromJsonAsync<PaymentResponse>())!;
        if(reverse)Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync(Root(x.Property.Id)+$"/payments/{receipt.Id}/cancel",new CancelChargeRequest("Correction",receipt.Version))).StatusCode);
        var refreshed=(await Preview(c,x))!;Assert.Equal(0,refreshed.EligibleCount);Assert.Equal(1,refreshed.SkippedPaymentCount);
        Assert.Equal(HttpStatusCode.Conflict,(await Finish(c,x,preview.Items.Select(i=>new ChargeCancellationApproval(i.Id,i.Version)).ToArray())).StatusCode);
        Assert.Null((await c.GetFromJsonAsync<OccupancyResponse>(Root(x.Property.Id)+"/occupancies/"+x.Link.Id))!.EndDate);
        Assert.NotEqual("CANCELLED",(await c.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/dues/"+charge.Id))!.Status);
    }
    [PostgresFact] public async Task InvalidForeignAndStaleApprovalVersionsCannotPartiallyEndOrCancel()
    {
        var x=await Seed();var other=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var charge=await Charge(c,x);var foreign=await Charge(c,other);
        Assert.Equal(HttpStatusCode.Conflict,(await Finish(c,x,[new(charge.Id,charge.Version+1)])).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Finish(c,x,[new(charge.Id,charge.Version),new(foreign.Id,foreign.Version)])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Finish(c,x,[new(charge.Id,charge.Version),new(charge.Id,charge.Version)])).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Finish(c,x,[new(charge.Id,charge.Version)],x.Link.Version+1)).StatusCode);
        Assert.Null((await c.GetFromJsonAsync<OccupancyResponse>(Root(x.Property.Id)+"/occupancies/"+x.Link.Id))!.EndDate);
        Assert.Equal(charge.Version,(await c.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/dues/"+charge.Id))!.Version);
    }
    [PostgresFact] public async Task ReasonCatalogOtherValidationAndVersionKeepCancellationHistory()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var charge=await Charge(c,x);var root=Root(x.Property.Id)+"/dues";
        var reasons=(await c.GetFromJsonAsync<CancellationReasonResponse[]>(root+"/cancellation-reasons"))!;Assert.Equal(5,reasons.Length);Assert.True(Assert.Single(reasons,r=>r.Code=="OTHER").RequiresExplanation);
        foreach(var bad in new[]{new CancelChargeRequest("",charge.Version,DuesCancellationReason.OtherId),new CancelChargeRequest("",charge.Version,Guid.NewGuid())})Assert.Equal(HttpStatusCode.BadRequest,(await c.PutAsJsonAsync(root+"/"+charge.Id+"/cancel",bad)).StatusCode);
        var known=Assert.Single(reasons,r=>r.Code=="WRONG_AMOUNT");var cancelled=await c.PutAsJsonAsync(root+"/"+charge.Id+"/cancel",new CancelChargeRequest(null,charge.Version,known.Id));Assert.Equal(HttpStatusCode.OK,cancelled.StatusCode);
        var saved=(await cancelled.Content.ReadFromJsonAsync<DuesChargeResponse>())!;Assert.Equal("WRONG_AMOUNT",saved.CancellationReasonCode);Assert.Equal(known.Name,saved.CancellationReason);Assert.Equal(charge.Amount,saved.Amount);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(root+"/"+charge.Id+"/cancel",new CancelChargeRequest("Overwrite",saved.Version,known.Id))).StatusCode);
        await factory.WithDb(async db=>{
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_cancellation_reasons SET name={"New label"},version=version+1 WHERE id={known.Id}");
            var row=await db.DuesCharges.AsNoTracking().SingleAsync(d=>d.Id==charge.Id);Assert.Equal(known.Name,row.CancellationReasonName);
            Assert.All(await db.DuesChargeParties.Where(p=>p.ChargeId==charge.Id).ToListAsync(),p=>{Assert.Equal(x.Actor,p.CreatedBy);Assert.Equal(1,p.Version);});
        });
    }
    [PostgresFact] public async Task CompositePartyReferencesAndOneDebtorConstraintRejectInvalidHistory()
    {
        var x=await Seed();var other=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var charge=await Charge(c,x);
        await factory.WithDb(async db=>{
            var tenant=await db.DuesChargeParties.SingleAsync(p=>p.ChargeId==charge.Id&&p.IsDebtor);
            foreach(var sql in new[]{FormattableStringFactory.Create("UPDATE dues_charge_parties SET person_id={0} WHERE id={1}",other.Tenant.Id,tenant.Id),FormattableStringFactory.Create("UPDATE dues_charge_parties SET unit_id={0} WHERE id={1}",x.Units[1].Id,tenant.Id)})
                Assert.Equal(PostgresErrorCodes.ForeignKeyViolation,(await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync(sql))).SqlState);
            Assert.Equal(PostgresErrorCodes.UniqueViolation,(await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charge_parties SET is_debtor=true WHERE charge_id={charge.Id} AND type='OWNER'"))).SqlState);
            Assert.Equal(PostgresErrorCodes.CheckViolation,(await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charge_parties SET is_deleted=true WHERE id={tenant.Id}"))).SqlState);
        });
    }
    [PostgresFact] public async Task LegacyRowsRemainUnknownAndDeletedPeopleStillHaveReadableSnapshotHistory()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var charge=await Charge(c,x);
        var old=new DuesAssessment(x.Property.Id,Guid.NewGuid(),new string('c',64),new(2026,8,1),1.25m,"TRY",new(2026,8,28),"Legacy");old.AddCharge(x.Units[1].Id);
        await factory.WithDb(async db=>{db.Add(old);await db.SaveChangesAsync();});
        var legacy=Assert.Single((await c.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"/dues?period=2026-08"))!.Items);Assert.False(legacy.PartiesRecorded);Assert.Empty(legacy.Parties!);
        var end=await c.PutAsJsonAsync(Root(x.Property.Id)+$"/occupancies/{x.Link.Id}/end",new EndOccupancyRequest(new(2026,9,30),x.Link.Version));Assert.Equal(HttpStatusCode.OK,end.StatusCode);
        var person=(await c.GetFromJsonAsync<PersonResponse>(Root(x.Property.Id)+"/people/"+x.Tenant.Id))!;
        Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync(Root(x.Property.Id)+$"/people/{person.Id}?version={person.Version}")).StatusCode);
        Assert.Equal("Tenant_% Original",Assert.Single((await c.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/dues/"+charge.Id))!.Parties!,p=>p.IsDebtor).PersonName);
        Assert.Equal(1,(await c.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"/dues?personId="+x.Tenant.Id))!.TotalCount);
    }
    [PostgresFact] public async Task ConcurrentPaymentAndApprovedEndCannotLoseAReceiptOrCancelAPaidDebt()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var charge=await Charge(c,x);
        var payment=new CreatePaymentRequest(Guid.NewGuid(),x.Units[0].Id,"1.00","TRY",DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)),"CASH",null,"Race receipt",[new(charge.Id,"1.00",charge.Version)]);
        var results=await Task.WhenAll(Finish(c,x,[new(charge.Id,charge.Version)]),c.PostAsJsonAsync(Root(x.Property.Id)+"/payments",payment));
        Assert.Contains(results,r=>r.StatusCode==HttpStatusCode.Conflict);
        Assert.Equal(1,results.Count(r=>r.IsSuccessStatusCode));
        var saved=(await c.GetFromJsonAsync<DuesChargeResponse>(Root(x.Property.Id)+"/dues/"+charge.Id))!;
        var link=(await c.GetFromJsonAsync<OccupancyResponse>(Root(x.Property.Id)+"/occupancies/"+x.Link.Id))!;
        if(results[0].IsSuccessStatusCode){Assert.Equal("CANCELLED",saved.Status);Assert.Equal(End,link.EndDate);Assert.Equal(0,(await c.GetFromJsonAsync<PaymentPage>(Root(x.Property.Id)+"/payments"))!.TotalCount);}
        else {Assert.Null(link.EndDate);Assert.Equal("11.35",saved.Balance);Assert.NotEqual("CANCELLED",saved.Status);Assert.Equal(1,(await c.GetFromJsonAsync<PaymentPage>(Root(x.Property.Id)+"/payments"))!.TotalCount);}
    }
    [PostgresFact] public async Task ConcurrentAssessmentAndMoveOutCannotAssignANewChargeToAnAlreadyEndedTenant()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");await Charge(c,x);
        var results=await Task.WhenAll(Finish(c,x),c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",Request(x,"2026-12")));
        Assert.Equal(HttpStatusCode.OK,results[0].StatusCode);Assert.Contains(results[1].StatusCode,new[]{HttpStatusCode.Created,HttpStatusCode.BadRequest});
        var page=(await c.GetFromJsonAsync<DuesPage>(Root(x.Property.Id)+"/dues"))!;Assert.Equal(results[1].IsSuccessStatusCode?2:1,page.TotalCount);Assert.All(page.Items,d=>Assert.NotEqual("CANCELLED",d.Status));
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/dues/assessments",Request(x,"2027-01"))).StatusCode);
    }
    [PostgresFact] public async Task NullApprovalEntriesAreValidationErrorsWithoutEndingTheRelationship()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");
        var response=await c.PutAsJsonAsync(Root(x.Property.Id)+$"/occupancies/{x.Link.Id}/end",new{endDate=End,version=x.Link.Version,cancellationApprovals=new object?[]{null}});
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Null((await c.GetFromJsonAsync<OccupancyResponse>(Root(x.Property.Id)+"/occupancies/"+x.Link.Id))!.EndDate);
    }
}
