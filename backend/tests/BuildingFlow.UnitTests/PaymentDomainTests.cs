using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class PaymentDomainTests
{
    private static DuesCharge Charge() => new DuesAssessment(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), new(2026, 10, 1), 12.35m, "TRY", new(2026, 10, 31), "Dues").AddCharge(Guid.NewGuid());
    private static Payment Receipt(DuesCharge charge, decimal amount = 12.35m) => new(charge.PropertyId, charge.UnitId, Guid.NewGuid(), new string('b', 64), amount, "TRY", new(2026, 10, 9), PaymentMethod.CASH, null, " Cash ");
    [Fact] public void PartialFullAndReversedPaymentsPreserveExactAmountsAndStatuses()
    {
        var charge = Charge(); var payment = Receipt(charge, 3.10m); var allocation = payment.Allocate(charge, 3.10m); payment.ValidateComplete();
        Assert.Equal(9.25m, charge.Balance); Assert.Equal(3.10m, charge.PaidAmount);
        Assert.Equal("PARTIALLY_PAID", charge.Status(new(2026,10,9))); Assert.Equal("OVERDUE", charge.Status(new(2026,11,1)));
        var remainder = Receipt(charge, 9.25m); remainder.Allocate(charge,9.25m); remainder.ValidateComplete();
        Assert.Equal("PAID", charge.Status(new(2026,11,1))); Assert.Equal(0m,charge.Balance);
        charge.ReversePayment(allocation.Amount); payment.Cancel(" Correction ",DateTimeOffset.UtcNow);
        Assert.Equal(3.10m,charge.Balance); Assert.Equal(12.35m,charge.Amount); Assert.Equal(3.10m,payment.Amount);
        Assert.Single(payment.Allocations); Assert.Equal("Correction",payment.CancellationReason); Assert.False(payment.IsDeleted); Assert.True(payment.IsEnabled);
        Assert.Throws<InvalidOperationException>(()=>payment.Cancel("Again",DateTimeOffset.UtcNow));
    }
    [Theory] [InlineData("0")] [InlineData("-1")] [InlineData("0.001")] [InlineData("100000000")]
    public void InvalidPaymentAmountsAreNotRounded(string value) => Assert.Throws<ArgumentOutOfRangeException>(()=>Receipt(Charge(),decimal.Parse(value,System.Globalization.CultureInfo.InvariantCulture)));
    [Fact] public void AllocationRejectsOverpaymentDifferentScopeDuplicatesAndMissingDistribution()
    {
        var charge=Charge();var payment=Receipt(charge,20m);
        Assert.Throws<InvalidOperationException>(()=>payment.Allocate(charge,13m)); Assert.Empty(payment.Allocations); Assert.Equal(0m,charge.PaidAmount);
        Assert.Throws<InvalidOperationException>(()=>payment.Allocate(Charge(),1m)); Assert.Empty(payment.Allocations);
        payment.Allocate(charge,2m); Assert.Throws<InvalidOperationException>(()=>payment.Allocate(charge,1m));
        Assert.Throws<InvalidOperationException>(payment.ValidateComplete); Assert.Equal(2m,charge.PaidAmount);
    }
    [Fact] public void PaidOrCancelledChargesCannotBeCancelledOrPaidAgain()
    {
        var charge=Charge();charge.ApplyPayment(1m);
        Assert.Throws<InvalidOperationException>(()=>charge.Cancel("Wrong debt",DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(()=>charge.ReversePayment(2m));
        charge.ReversePayment(1m);charge.Cancel("Wrong debt",DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(()=>charge.ApplyPayment(1m));
    }
    [Fact] public void BankReferencesAndRequiredMetadataAreValidated()
    {
        var charge=Charge();
        Payment Bank(string? reference)=>new(charge.PropertyId,charge.UnitId,Guid.NewGuid(),new string('a',64),1m,"TRY",new(2026,10,9),PaymentMethod.BANK_TRANSFER,reference,"Transfer");
        Assert.Throws<ArgumentException>(()=>Bank(" "));Assert.Equal("ABC-123",Bank(" abc-123 ").Reference);
        Assert.Throws<ArgumentException>(()=>new Payment(Guid.Empty,charge.UnitId,Guid.NewGuid(),new string('a',64),1m,"TRY",new(2026,10,9),PaymentMethod.CASH,null,"Cash"));
        Assert.Throws<ArgumentException>(()=>new Payment(charge.PropertyId,charge.UnitId,Guid.NewGuid(),new string('a',64),1m,"USD",new(2026,10,9),PaymentMethod.CASH,null,"Cash"));
    }
}
