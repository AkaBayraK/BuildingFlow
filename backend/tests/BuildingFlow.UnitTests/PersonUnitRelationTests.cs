using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class PersonUnitRelationTests
{
    private static readonly Guid T=Guid.NewGuid(),P=Guid.NewGuid(),U=Guid.NewGuid(),Person=Guid.NewGuid();
    private static readonly DateOnly Start=new(2026,1,1);
    private static UnitOwnership Owner(Guid? person=null,DateOnly? start=null,DateOnly? end=null,long? n=null,long? d=null,bool primary=false)=>new(T,P,U,person??Person,start??Start,end,primary,n,d);
    [Fact]public void CoOwnersAndCoTenantsAreIndependentAndUnknownShareStaysNull()
    {
        var owner=Owner();var tenant=new UnitTenancy(T,P,U,Person,Start);var other=new UnitTenancy(T,P,U,Guid.NewGuid(),Start);
        PersonUnitRules.ValidateSet([owner,Owner(Guid.NewGuid()),tenant,other]);Assert.Null(owner.ShareNumerator);Assert.Null(owner.ShareDenominator);Assert.NotEqual(tenant.Kind,new UnitResidency(T,P,U,Person,Start).Kind);
    }
    [Fact]public void InclusiveDateBoundariesAndPrimaryContactAcrossKindsConflict()
    {
        var end=Start.AddDays(2);Assert.Throws<InvalidOperationException>(()=>PersonUnitRules.ValidateSet([Owner(end:end),Owner(start:end)]));
        PersonUnitRules.ValidateSet([Owner(end:end),Owner(start:end.AddDays(1))]);
        Assert.Throws<InvalidOperationException>(()=>PersonUnitRules.ValidateSet([Owner(primary:true),new UnitTenancy(T,P,U,Guid.NewGuid(),Start,null,true)]));
    }
    [Fact]public void ExactRationalSharesCheckedAtEveryBoundaryWithoutFloatingPointOrOverflow()
    {
        PersonUnitRules.ValidateSet([Owner(n:1,d:3),Owner(Guid.NewGuid(),n:2,d:3),Owner(Guid.NewGuid())]);
        Assert.Throws<InvalidOperationException>(()=>PersonUnitRules.ValidateSet([Owner(n:1,d:2),Owner(Guid.NewGuid(),start:Start.AddDays(3),end:Start.AddDays(4),n:2,d:3)]));
        PersonUnitRules.ValidateSet([Owner(n:long.MaxValue-1,d:long.MaxValue),Owner(Guid.NewGuid(),n:1,d:long.MaxValue)]);
        Assert.Throws<InvalidOperationException>(()=>PersonUnitRules.ValidateSet([Owner(n:long.MaxValue-1,d:long.MaxValue),Owner(Guid.NewGuid(),n:2,d:long.MaxValue)]));
    }
    [Fact]public void SharesAndDatesValidatedAndEndingCanOnlyHappenOnce()
    {
        foreach(var pair in new(long?,long?)[]{(1,null),(null,1),(0,2),(3,2),(-1,2)})Assert.Throws<ArgumentException>(()=>Owner(n:pair.Item1,d:pair.Item2));
        Assert.Throws<ArgumentException>(()=>Owner(end:Start.AddDays(-1)));Assert.Throws<ArgumentException>(()=>new UnitResidency(Guid.Empty,P,U,Person,Start));
        var row=Owner();Assert.Throws<ArgumentException>(()=>row.End(Start.AddDays(-1)));row.End(Start);Assert.Throws<InvalidOperationException>(()=>row.End(Start.AddDays(1)));
    }
}
