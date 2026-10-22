using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class LeaseContractTests
{
    private static readonly Guid T=Guid.NewGuid(),P=Guid.NewGuid(),U=Guid.NewGuid(),Ahmet=Guid.NewGuid(),Ayse=Guid.NewGuid(),Account=Guid.NewGuid();
    private static readonly DateOnly Start=new(2026,1,1),End=new(2026,12,31);
    private static LeaseContract Lease(DateOnly? start=null,DateOnly? end=null,string? reference=null)=>new(T,P,U,start??Start,end,reference);
    [Fact]public void JointTenantsShareOneContractAndRenewalIsANewContract()
    {
        var first=Lease(end:End,reference:"  2026/14  ");var joint=new[]{new UnitTenancy(first,Ahmet,Start,End,true),new UnitTenancy(first,Ayse,Start,End)};
        LeaseContractRules.ValidateParties(first,joint);Assert.Equal("2026/14",first.ContractReference);Assert.All(joint,p=>Assert.Equal(first.Id,p.LeaseContractId));
        var renewal=Lease(End.AddDays(1));var next=new UnitTenancy(renewal,Ahmet,End.AddDays(1));LeaseContractRules.ValidateParties(renewal,[next]);
        Assert.NotEqual(first.Id,renewal.Id);Assert.Equal(RelationVerificationStatus.PENDING,renewal.VerificationStatus);
    }
    [Fact]public void PartyMustStayInsideContractPeriodAndScope()
    {
        var lease=Lease(end:End);
        Assert.Throws<ArgumentException>(()=>new UnitTenancy(lease,Ahmet,Start.AddDays(-1),End));
        Assert.Throws<ArgumentException>(()=>new UnitTenancy(lease,Ahmet,Start));
        Assert.Throws<ArgumentException>(()=>new UnitTenancy(lease,Ahmet,Start,End.AddDays(1)));
        var foreign=new UnitTenancy(T,P,Guid.NewGuid(),Ahmet,Start,End);Assert.Throws<ArgumentException>(()=>foreign.AttachLease(lease));
        var other=new UnitTenancy(T,P,U,Ahmet,Start,End);Assert.Throws<InvalidOperationException>(()=>LeaseContractRules.ValidateParties(lease,[other]));
        var attached=new UnitTenancy(lease,Ayse,Start,End);Assert.Throws<InvalidOperationException>(()=>attached.AttachLease(Lease(end:End)));
    }
    [Fact]public void SamePersonCannotHoldOverlappingPartiesButSequentialPartiesAreAllowed()
    {
        var lease=Lease();
        Assert.Throws<InvalidOperationException>(()=>LeaseContractRules.ValidateParties(lease,[new UnitTenancy(lease,Ahmet,Start,Start.AddDays(10)),new UnitTenancy(lease,Ahmet,Start.AddDays(10))]));
        LeaseContractRules.ValidateParties(lease,[new UnitTenancy(lease,Ahmet,Start,Start.AddDays(10)),new UnitTenancy(lease,Ahmet,Start.AddDays(11))]);
    }
    [Fact]public void UnitHoldsOneLeasePerPeriodWithInclusiveBoundaries()
    {
        var first=Lease(end:End);LeaseContractRules.ValidateUnit([first,Lease(End.AddDays(1)),new LeaseContract(T,P,Guid.NewGuid(),Start)]);
        Assert.Throws<InvalidOperationException>(()=>LeaseContractRules.ValidateUnit([first,Lease(End)]));
        Assert.Throws<InvalidOperationException>(()=>LeaseContractRules.ValidateUnit([Lease(),Lease(Start.AddYears(3),Start.AddYears(4))]));
    }
    [Fact]public void ContractValidatesDatesReferenceAndEndsOnce()
    {
        Assert.Throws<ArgumentException>(()=>new LeaseContract(Guid.Empty,P,U,Start));Assert.Throws<ArgumentException>(()=>Lease(end:Start.AddDays(-1)));
        Assert.Throws<ArgumentException>(()=>Lease(reference:new string('R',101)));Assert.Throws<ArgumentException>(()=>Lease(reference:"A\nB"));Assert.Null(Lease(reference:"   ").ContractReference);
        var lease=Lease();Assert.True(lease.IsActiveOn(Start));Assert.False(lease.IsActiveOn(Start.AddDays(-1)));Assert.Throws<ArgumentException>(()=>lease.End(Start.AddDays(-1)));
        lease.End(Start);Assert.True(lease.IsActiveOn(Start));Assert.False(lease.IsActiveOn(Start.AddDays(1)));Assert.Throws<InvalidOperationException>(()=>lease.End(End));
        Assert.True(lease.Covers(Start,Start));Assert.False(lease.Covers(Start,null));
    }
    [Fact]public void VerificationIsDecidedOnceAndLegacyNeverInventsAVerifier()
    {
        var at=new DateTimeOffset(2026,10,22,8,0,0,TimeSpan.Zero);var evidence=Guid.NewGuid();
        var owner=new UnitOwnership(T,P,U,Ahmet,Start);Assert.Equal(RelationVerificationStatus.PENDING,owner.VerificationStatus);owner.Verify(Account,at,evidence);
        Assert.Equal((RelationVerificationStatus.VERIFIED,(Guid?)Account,(DateTimeOffset?)at,(Guid?)evidence),(owner.VerificationStatus,owner.VerifiedByAccountId,owner.VerifiedAt,owner.EvidenceDocumentId));
        Assert.Throws<InvalidOperationException>(()=>owner.Reject(Account,at));Assert.Throws<InvalidOperationException>(()=>owner.MarkLegacy());
        var legacy=new UnitResidency(T,P,U,Ahmet,Start);legacy.MarkLegacy();Assert.Equal(RelationVerificationStatus.LEGACY,legacy.VerificationStatus);Assert.Null(legacy.VerifiedByAccountId);Assert.Null(legacy.VerifiedAt);
        legacy.Reject(Account,at);Assert.Equal(RelationVerificationStatus.REJECTED,legacy.VerificationStatus);
        var tenancy=new UnitTenancy(T,P,U,Ahmet,Start);Assert.Throws<ArgumentException>(()=>tenancy.Verify(Guid.Empty,at));Assert.Throws<ArgumentException>(()=>tenancy.Verify(Account,default));Assert.Throws<ArgumentException>(()=>tenancy.Verify(Account,at,Guid.Empty));
        Assert.Equal(RelationVerificationStatus.PENDING,tenancy.VerificationStatus);
    }
}
