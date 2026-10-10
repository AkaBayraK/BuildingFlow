using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class DuesPartyDomainTests
{
    private static readonly DateOnly Period=new(2026,11,1);
    private static DuesCharge Charge(Guid property,Guid unit)=>new DuesAssessment(property,Guid.NewGuid(),new string('a',64),Period,12.35m,"TRY",Period,"Dues","TENANT").AddCharge(unit);
    [Fact] public void SnapshotNameAndDatesSurviveRenamingAndEndingTheSourceRelationship()
    {
        var property=Guid.NewGuid();var unit=Guid.NewGuid();var person=new Person(property,"Ada","Original");var link=new UnitOccupancy(property,unit,person.Id,OccupancyType.TENANT,Period);
        var charge=Charge(property,unit);var party=new DuesChargeParty(charge,link,person,true);charge.RecordParties([party]);
        person.Update("Ada","Changed",null,null,true);link.End(Period.AddDays(10));
        Assert.Equal("Ada Original",party.PersonName);Assert.Null(party.EndDate);Assert.True(charge.PartiesRecorded);Assert.True(party.IsDebtor);
        Assert.Throws<InvalidOperationException>(()=>charge.RecordParties([]));
    }
    [Fact] public void ScopeDateAndResidentDebtorCannotProduceAnIncorrectSnapshot()
    {
        var property=Guid.NewGuid();var unit=Guid.NewGuid();var person=new Person(property,"Ada","Kaya");var charge=Charge(property,unit);
        foreach(var link in new[]{new UnitOccupancy(property,unit,person.Id,OccupancyType.TENANT,Period.AddDays(1)),new UnitOccupancy(property,unit,person.Id,OccupancyType.TENANT,Period.AddDays(-2),Period.AddDays(-1)),new UnitOccupancy(property,Guid.NewGuid(),person.Id,OccupancyType.TENANT,Period),new UnitOccupancy(property,unit,person.Id,OccupancyType.RESIDENT,Period)})
            Assert.Throws<ArgumentException>(()=>new DuesChargeParty(charge,link,person,true));
    }
    [Fact] public void CancellationRetainsPartyHistoryOriginalAmountAndReasonSnapshot()
    {
        var property=Guid.NewGuid();var unit=Guid.NewGuid();var person=new Person(property,"Ada","Kaya");var link=new UnitOccupancy(property,unit,person.Id,OccupancyType.TENANT,Period);
        var charge=Charge(property,unit);charge.RecordParties([new(charge,link,person,true)]);
        charge.Cancel("Moved",DateTimeOffset.UtcNow,DuesCancellationReason.TenantMovedOutId,"TENANT_MOVED_OUT","Kiracı taşınması",link.Id);
        Assert.Single(charge.Parties);Assert.Equal(12.35m,charge.Amount);Assert.Equal(0,charge.Balance);Assert.Equal(link.Id,charge.CancelledByOccupancyId);Assert.Equal("TENANT_MOVED_OUT",charge.CancellationReasonCode);
        Assert.Throws<InvalidOperationException>(()=>charge.Cancel("Overwrite",DateTimeOffset.UtcNow));
    }
}
