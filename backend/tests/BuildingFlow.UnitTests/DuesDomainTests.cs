using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class DuesDomainTests
{
    private static readonly DateOnly Period = new(2026, 10, 1);
    private static DuesAssessment Assessment(decimal amount = 12.35m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), Period, amount, "TRY", Period.AddDays(10), " Aidat ");
    [Fact] public void AssessmentCopiesExactAmountAndPreservesUnitIdentity()
    {
        var batch = Assessment(); var unit = Guid.NewGuid(); var charge = batch.AddCharge(unit);
        Assert.Equal(batch.Id, charge.AssessmentId); Assert.Equal(batch.PropertyId, charge.PropertyId);
        Assert.Equal(unit, charge.UnitId); Assert.Equal(12.35m, charge.Amount); Assert.Equal("Aidat", batch.Description);
        Assert.Equal("OPEN", charge.Status(charge.DueDate)); Assert.Equal("OVERDUE", charge.Status(charge.DueDate.AddDays(1)));
        Assert.Throws<ArgumentException>(() => batch.AddCharge(unit)); Assert.Single(batch.Charges);
    }
    [Theory] [InlineData("0")] [InlineData("-1")] [InlineData("1.001")] [InlineData("100000000")]
    public void InvalidMoneyIsRejectedWithoutRounding(string value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Assessment(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));
    [Fact] public void InvalidPeriodCurrencyDueDateAndFingerprintAreRejected()
    {
        var id = Guid.NewGuid(); var hash = new string('a',64);
        Assert.Throws<ArgumentException>(() => new DuesAssessment(id,id,hash,Period.AddDays(1),1,"TRY",Period.AddDays(10),"Dues"));
        Assert.Throws<ArgumentException>(() => new DuesAssessment(id,id,hash,new(1999,1,1),1,"TRY",Period,"Dues"));
        Assert.Throws<ArgumentException>(() => new DuesAssessment(id,id,hash,Period,1,"EUR",Period,"Dues"));
        Assert.Throws<ArgumentException>(() => new DuesAssessment(id,id,hash,Period,1,"TRY",Period.AddDays(-1),"Dues"));
        Assert.Throws<ArgumentException>(() => new DuesAssessment(id,id,null!,Period,1,"TRY",Period,"Dues"));
    }
    [Fact] public void BatchLimitCannotLeaveAnExtraCharge()
    {
        var batch=Assessment(); for(var i=0;i<500;i++)batch.AddCharge(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(()=>batch.AddCharge(Guid.NewGuid())); Assert.Equal(500,batch.Charges.Count);
    }
    [Fact] public void CancellationPreservesAmountAndHistoryAndCannotBeRewritten()
    {
        var charge=Assessment().AddCharge(Guid.NewGuid()); var id=charge.Id;
        Assert.Throws<ArgumentException>(()=>charge.Cancel(" ",DateTimeOffset.UtcNow)); Assert.False(charge.IsCancelled);
        charge.Cancel(" Düzeltme ",DateTimeOffset.UtcNow);
        Assert.Equal(id,charge.Id); Assert.Equal(12.35m,charge.Amount); Assert.Equal(0,charge.Balance);
        Assert.Equal("CANCELLED",charge.Status(Period)); Assert.Equal("Düzeltme",charge.CancellationReason);
        Assert.False(charge.IsDeleted); Assert.True(charge.IsEnabled);
        Assert.Throws<InvalidOperationException>(()=>charge.Cancel("Other",DateTimeOffset.UtcNow));
    }
}

