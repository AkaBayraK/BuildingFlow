using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MyUnitTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>,IDisposable
{
    private const string Issuer="http://localhost:8080/realms/buildingflow";
    private readonly List<WebApplicationFactory<Program>> children=[];
    private static DateOnly Today=>DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private HttpClient Me(string user,string role="OWNER")
    {
        var flags=new Dictionary<string,string?>{{"PersonRefactor:Enabled","true"},{"PersonRefactor:SessionContextsEnabled","true"}};
        var child=factory.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(flags)));children.Add(child);var c=child.CreateClient();using var signed=TestToken.Client(factory,user,role);c.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;return c;
    }
    private static async Task<T> Ok<T>(HttpResponseMessage r){Assert.True(r.StatusCode==HttpStatusCode.OK,$"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return(await r.Content.ReadFromJsonAsync<T>())!;}
    private sealed record World(Guid Tenant,Guid Property,Guid D3,Guid D7,string Ahmet,string Mehmet,Guid OwnerD3,Guid ResidentD3,Guid OwnerD7,Guid TenantD7,Guid Lease,Guid ChargeD3,Guid ChargeD7,Guid Published,Guid Draft,Guid AhmetPerson);
    private async Task<World> Seed()
    {
        var t=new PersonTenant("Unit fixture");var p=new Property("Örnek Apartmanı","City","District","Address",PropertyType.APARTMENT);var d3=p.AddUnit("D3",1,90);var d7=p.AddUnit("D7",2,90);
        string ahmetUser=Guid.NewGuid().ToString(),mehmetUser=Guid.NewGuid().ToString();var ahmetAccount=new PersonUserAccount(Issuer,ahmetUser);var mehmetAccount=new PersonUserAccount(Issuer,mehmetUser);
        var ahmet=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Ahmet","Fixture",null,null,null,null);var mehmet=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","10987654321","Mehmet","Fixture",null,null,null,null);
        var start=Today.AddYears(-1);var lease=new LeaseContract(t.Id,p.Id,d7.Id,start,null,"KS-7");
        var ownerD3=new UnitOwnership(t.Id,p.Id,d3.Id,ahmet.Id,start);var residentD3=new UnitResidency(t.Id,p.Id,d3.Id,ahmet.Id,start);var ownerD7=new UnitOwnership(t.Id,p.Id,d7.Id,ahmet.Id,start);var tenantD7=new UnitTenancy(lease,mehmet.Id,start);
        VerifiableRecord[] rows=[lease,ownerD3,residentD3,ownerD7,tenantD7];foreach(var row in rows)row.MarkLegacy();
        var assessment=new DuesAssessment(p.Id,Guid.NewGuid(),new string('A',64),new DateOnly(2026,9,1),1000m,"TRY",new DateOnly(2026,9,15),"Aidat");var c3=assessment.AddCharge(d3.Id);var c7=assessment.AddCharge(d7.Id);
        var payment=new Payment(p.Id,d3.Id,Guid.NewGuid(),new string('B',64),400m,"TRY",new DateOnly(2026,9,10),PaymentMethod.CASH,null,"Tahsilat");payment.Allocate(c3,400m);
        var published=new Announcement(p.Id,"Su kesintisi","Yarın 10:00-12:00",ContentAudience.ALL_MEMBERS);published.Publish(DateTimeOffset.UtcNow);var draft=new Announcement(p.Id,"Taslak","Gizli",ContentAudience.ALL_MEMBERS);var internalNote=new Announcement(p.Id,"Yönetim","İç",ContentAudience.MANAGEMENT);internalNote.Publish(DateTimeOffset.UtcNow);
        var document=new StoredDocument(p.Id,Guid.NewGuid(),new string('C',64),published.Id,ContentAudience.ALL_MEMBERS,"plan.pdf","application/pdf",10,new string('D',64));
        await factory.WithDb(async db=>
        {
            db.AddRange(t,p,d3,d7,ahmetAccount,mehmetAccount,ahmet,mehmet,new PersonTenantMembership(t.Id,ahmetAccount.Id),new PersonTenantMembership(t.Id,mehmetAccount.Id),new PersonPropertyScope(t.Id,p.Id));await db.SaveChangesAsync();
            db.AddRange(new CentralPersonAccountLink(t.Id,ahmetAccount.Id,ahmet.Id,DateTimeOffset.UtcNow.AddMinutes(-5)),new CentralPersonAccountLink(t.Id,mehmetAccount.Id,mehmet.Id,DateTimeOffset.UtcNow.AddMinutes(-5)));db.AddRange(rows);
            db.AddRange(assessment,payment,published,draft,internalNote,document);await db.SaveChangesAsync();
        });
        return new(t.Id,p.Id,d3.Id,d7.Id,ahmetUser,mehmetUser,ownerD3.Id,residentD3.Id,ownerD7.Id,tenantD7.Id,lease.Id,c3.Id,c7.Id,published.Id,draft.Id,ahmet.Id);
    }
    private static string Url(World w,Guid unit,string section,string capacity,Guid relation,string extra="")=>$"/api/v2/me/units/{unit}/{section}?propertyId={w.Property}&capacity={capacity}&relationId={relation}{extra}";
    [PostgresFact]public async Task OwnerAndTenantReadOnlyTheirSelectedUnitByCapability()
    {
        var w=await Seed();using var ahmet=Me(w.Ahmet);using var mehmet=Me(w.Mehmet,"TENANT");
        var response=await ahmet.GetAsync(Url(w,w.D3,"dues","OWNER",w.OwnerD3));Assert.Equal("no-store",response.Headers.CacheControl?.ToString());var dues=await Ok<MyDues>(response);
        Assert.Equal($"{w.Tenant:D}:OWNER:{w.OwnerD3:D}",dues.ContextKey);Assert.Equal(w.ChargeD3,Assert.Single(dues.Rows.Items).Id);Assert.Equal("600.00",dues.Summary.Balance);Assert.Equal("PARTIALLY_PAID",dues.Rows.Items[0].Status=="OVERDUE"?"PARTIALLY_PAID":dues.Rows.Items[0].Status);
        Assert.Equal("400.00",Assert.Single((await Ok<MyUnitPage<MyPaymentRow>>(await ahmet.GetAsync(Url(w,w.D3,"payments","OWNER",w.OwnerD3)))).Rows.Items).Amount);
        Assert.Equal(w.ChargeD7,Assert.Single((await Ok<MyDues>(await mehmet.GetAsync(Url(w,w.D7,"dues","TENANT",w.TenantD7)))).Rows.Items).Id);
        Assert.Empty((await Ok<MyUnitPage<MyPaymentRow>>(await mehmet.GetAsync(Url(w,w.D7,"payments","TENANT",w.TenantD7)))).Rows.Items);
        var announcements=await Ok<MyUnitPage<MyAnnouncementRow>>(await ahmet.GetAsync(Url(w,w.D3,"announcements","RESIDENT",w.ResidentD3)));Assert.Equal(w.Published,Assert.Single(announcements.Rows.Items).Id);
        Assert.Equal("plan.pdf",Assert.Single((await Ok<MyUnitPage<MyDocumentRow>>(await ahmet.GetAsync(Url(w,w.D3,"documents","OWNER",w.OwnerD3)))).Rows.Items).FileName);
        var tenantLease=Assert.Single((await Ok<MyUnitPage<MyLeaseRow>>(await mehmet.GetAsync(Url(w,w.D7,"leases","TENANT",w.TenantD7)))).Rows.Items);Assert.True(tenantLease.IsParty);Assert.Equal(1,tenantLease.PartyCount);
        var ownerLeaseResponse=await ahmet.GetAsync(Url(w,w.D7,"leases","OWNER",w.OwnerD7));var ownerLease=Assert.Single((await Ok<MyUnitPage<MyLeaseRow>>(ownerLeaseResponse)).Rows.Items);Assert.False(ownerLease.IsParty);Assert.Equal("KS-7",ownerLease.ContractReference);
        Assert.DoesNotContain("Mehmet",await ownerLeaseResponse.Content.ReadAsStringAsync());
        foreach(var url in new[]{Url(w,w.D3,"dues","RESIDENT",w.ResidentD3),Url(w,w.D3,"payments","RESIDENT",w.ResidentD3),Url(w,w.D3,"documents","RESIDENT",w.ResidentD3),Url(w,w.D3,"leases","OWNER",w.OwnerD3)})Assert.Equal(HttpStatusCode.Forbidden,(await ahmet.GetAsync(url)).StatusCode);
    }
    [PostgresFact]public async Task ForgedStaleOrForeignSelectionsNeverReturnData()
    {
        var w=await Seed();var foreign=await Seed();using var ahmet=Me(w.Ahmet);
        foreach(var url in new[]{Url(w,w.D7,"dues","TENANT",w.TenantD7),Url(w,w.D7,"dues","OWNER",w.OwnerD3),Url(w,w.D3,"dues","OWNER",w.OwnerD7),Url(w,w.D3,"dues","TENANT",w.OwnerD3),Url(foreign,foreign.D3,"dues","OWNER",foreign.OwnerD3),Url(w,foreign.D3,"dues","OWNER",w.OwnerD3)})
            Assert.Equal(HttpStatusCode.NotFound,(await ahmet.GetAsync(url)).StatusCode);
        foreach(var url in new[]{Url(w,w.D3,"dues","OWNER",w.OwnerD3,"&pageSize=51"),Url(w,w.D3,"dues","MANAGEMENT",w.OwnerD3),Url(w,w.D3,"dues","OWNER",w.OwnerD3,"&page=0")})Assert.Equal(HttpStatusCode.BadRequest,(await ahmet.GetAsync(url)).StatusCode);
        await factory.WithDb(db=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_ownerships SET end_date={Today.AddDays(-1)} WHERE id={w.OwnerD3}"));
        Assert.Equal(HttpStatusCode.NotFound,(await ahmet.GetAsync(Url(w,w.D3,"dues","OWNER",w.OwnerD3))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await ahmet.GetAsync(Url(w,w.D3,"announcements","RESIDENT",w.ResidentD3))).StatusCode);
        await factory.WithDb(db=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE person_account_links SET ended_at=now() WHERE person_id={w.AhmetPerson}"));
        Assert.Equal(HttpStatusCode.NotFound,(await ahmet.GetAsync(Url(w,w.D3,"announcements","RESIDENT",w.ResidentD3))).StatusCode);
        using var closed=factory.CreateClient();using var signed=TestToken.Client(factory,w.Mehmet,"TENANT");closed.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;
        Assert.Equal(HttpStatusCode.NotFound,(await closed.GetAsync(Url(w,w.D7,"dues","TENANT",w.TenantD7))).StatusCode);
    }
    [PostgresFact]public async Task FinanceSummaryCountsASharedChargeOnceAndDownloadRechecksTheContext()
    {
        var w=await Seed();await factory.WithDb(async db=>{db.Add(TestRelations.Effective(new UnitTenancy(w.Tenant,w.Property,w.D3,w.AhmetPerson,Today.AddDays(-10))));await db.SaveChangesAsync();});
        using var ahmet=Me(w.Ahmet);var summary=await Ok<MyFinanceSummary>(await ahmet.GetAsync("/api/v2/me/finance-summary"));
        Assert.Equal(2,summary.ChargeCount);Assert.Equal(1,summary.SharedCharges);Assert.Equal("2000.00",summary.Summary.Amount);Assert.Equal("400.00",summary.Summary.Paid);
        Assert.Equal(3,summary.Scope.Count);Assert.Contains(summary.Scope,x=>x.UnitId==w.D3 && x.Capacity=="TENANT");Assert.DoesNotContain(summary.Scope,x=>x.Capacity=="RESIDENT");
        using var mehmet=Me(w.Mehmet,"TENANT");var tenant=await Ok<MyFinanceSummary>(await mehmet.GetAsync("/api/v2/me/finance-summary"));Assert.Equal(1,tenant.ChargeCount);Assert.Equal(w.D7,Assert.Single(tenant.Scope).UnitId);
        Assert.Equal(HttpStatusCode.NotFound,(await ahmet.GetAsync(Url(w,w.D3,"documents/"+Guid.NewGuid()+"/download","OWNER",w.OwnerD3))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await ahmet.GetAsync(Url(w,w.D3,"documents/"+Guid.NewGuid()+"/download","RESIDENT",w.ResidentD3))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await mehmet.GetAsync(Url(w,w.D3,"documents/"+Guid.NewGuid()+"/download","OWNER",w.OwnerD3))).StatusCode);
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
}
