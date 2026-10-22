using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class PersonPortfolioTests
{
    private static readonly Guid Ahmet=Guid.NewGuid(),Mehmet=Guid.NewGuid(),Site=Guid.NewGuid(),Apartment=Guid.NewGuid(),BlockA=Guid.NewGuid(),BlockB=Guid.NewGuid(),D5=Guid.NewGuid(),D12=Guid.NewGuid(),D3=Guid.NewGuid(),D7=Guid.NewGuid();
    private static readonly DateOnly Today=new(2026,10,22);
    private static PortfolioRelationSource Row(Guid person,string kind,Guid property,Guid unit,DateOnly start,DateOnly? end=null,PortfolioLease? lease=null,string status="VERIFIED",bool enabled=true)
    {
        var (name,building,block)=property==Site?("Örnek Sitesi",(Guid?)(unit==D5?BlockA:BlockB),unit==D5?"A Blok":"B Blok"):("Örnek Apartmanı",null,(string?)null);
        var number=unit==D5?"D5":unit==D12?"D12":unit==D3?"D3":"D7";return new(Guid.NewGuid(),kind,person,property,name,building,block,unit,number,start,end,enabled,status,lease);
    }
    private static PortfolioLease Lease(DateOnly start,DateOnly? end=null)=>new(Guid.NewGuid(),start,end,null,"VERIFIED");
    [Fact]public void AhmetScenarioIsGroupedPerUnitWithDerivedStatusAndActiveLease()
    {
        var rented=Lease(new(2026,3,1));var own=Lease(new(2026,1,1),new(2026,12,31));var oldLease=Lease(new(2025,1,1),new(2025,12,31));var second=Lease(new(2026,1,1));
        var rows=new[]{Row(Ahmet,"OWNER",Site,D5,new(2020,1,1)),Row(Mehmet,"TENANT",Site,D5,new(2026,3,1),lease:rented),Row(Mehmet,"RESIDENT",Site,D5,new(2026,3,1)),
            Row(Ahmet,"TENANT",Site,D12,new(2026,1,1),new(2026,12,31),own),Row(Ahmet,"RESIDENT",Site,D12,new(2026,1,1)),
            Row(Ahmet,"OWNER",Apartment,D3,new(2018,5,1)),Row(Ahmet,"RESIDENT",Apartment,D3,new(2018,5,1)),
            Row(Ahmet,"TENANT",Apartment,D7,new(2025,1,1),new(2025,12,31),oldLease),Row(Ahmet,"TENANT",Apartment,D7,new(2026,1,1),lease:second)};
        var portfolio=PersonPortfolio.Build(Ahmet,rows,Today);
        Assert.Equal(["Örnek Apartmanı","Örnek Sitesi"],portfolio.Select(p=>p.PropertyName));
        var apartment=Assert.Single(Assert.Single(portfolio,p=>p.PropertyId==Apartment).Buildings);Assert.Null(apartment.BuildingId);
        var d3=Assert.Single(apartment.Units,u=>u.UnitId==D3);Assert.Equal(PersonPortfolio.OwnerOccupied,d3.Status);Assert.Equal(["OWNER","RESIDENT"],d3.Capacities);Assert.Null(d3.ActiveLease);
        var d7=Assert.Single(apartment.Units,u=>u.UnitId==D7);Assert.Equal(PersonPortfolio.TenantNotResident,d7.Status);Assert.Equal(second,d7.ActiveLease);Assert.Equal(second,Assert.Single(d7.Relations).Lease);
        var site=Assert.Single(portfolio,p=>p.PropertyId==Site);Assert.Equal(["A Blok","B Blok"],site.Buildings.Select(b=>b.BuildingName));
        var d5=Assert.Single(site.Buildings[0].Units);Assert.Equal(PersonPortfolio.RentedOut,d5.Status);Assert.Equal(["OWNER"],d5.Capacities);Assert.Equal(rented,d5.ActiveLease);Assert.Single(d5.Relations);
        var d12=Assert.Single(site.Buildings[1].Units);Assert.Equal(PersonPortfolio.TenantResident,d12.Status);Assert.Equal(own,d12.ActiveLease);Assert.Equal(["TENANT","RESIDENT"],d12.Relations.Select(r=>r.Kind));
        Assert.DoesNotContain(portfolio.SelectMany(p=>p.Buildings).SelectMany(b=>b.Units).SelectMany(u=>u.Relations),r=>rows.Any(x=>x.RelationId==r.RelationId && x.PersonId!=Ahmet));
    }
    [Fact]public void OnlyEffectiveRelationsOnInclusiveIstanbulDayGrantCapacity()
    {
        var rows=new[]{Row(Ahmet,"OWNER",Apartment,D3,Today,Today),Row(Ahmet,"RESIDENT",Apartment,D3,Today.AddDays(1)),Row(Mehmet,"TENANT",Apartment,D3,Today,status:"PENDING"),
            Row(Ahmet,"TENANT",Apartment,D7,new(2026,1,1),Today.AddDays(-1)),Row(Ahmet,"RESIDENT",Apartment,D7,new(2026,1,1),status:"REJECTED"),Row(Ahmet,"OWNER",Site,D5,new(2026,1,1),enabled:false),
            Row(Ahmet,"RESIDENT",Site,D12,new(2026,1,1),status:"LEGACY")};
        var units=PersonPortfolio.Build(Ahmet,rows,Today).SelectMany(p=>p.Buildings).SelectMany(b=>b.Units).ToArray();
        Assert.Equal(2,units.Length);Assert.Equal(PersonPortfolio.OwnerOnly,Assert.Single(units,u=>u.UnitId==D3).Status);Assert.Equal(PersonPortfolio.ResidentOnly,Assert.Single(units,u=>u.UnitId==D12).Status);
        Assert.DoesNotContain(PersonPortfolio.Build(Ahmet,rows,Today.AddDays(-1)).SelectMany(p=>p.Buildings).SelectMany(b=>b.Units),u=>u.UnitId==D3);
    }
}
