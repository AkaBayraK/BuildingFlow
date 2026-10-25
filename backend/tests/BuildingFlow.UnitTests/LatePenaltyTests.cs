using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class LatePenaltyTests
{
    private static readonly DateOnly Due = new(2026, 1, 31);
    private static readonly PenaltySwitchPeriod[] On = [new(new(2000, 1, 1), true)];
    private static readonly PenaltyPrincipalEvent[] NoPayments = [];
    private static readonly PenaltyRatePeriod[] NoPolicies = [];
    private static decimal Total(IEnumerable<PenaltySegmentResult> segments) => LatePenaltyCalculator.Round(segments.Sum(value => value.Amount));
    private static IReadOnlyList<PenaltySegmentResult> Calc(DateOnly through, PenaltyPrincipalEvent[]? payments = null,
        PenaltySwitchPeriod[]? switches = null, PenaltyRatePeriod[]? policies = null, decimal amount = 3000m) =>
        LatePenaltyCalculator.Calculate(amount, Due, Due.AddDays(1), through, payments ?? NoPayments, switches ?? On, policies ?? NoPolicies);

    [Fact] public void PaymentOnTimeOrNoDelayProducesNoPenalty()
    {
        Assert.Empty(Calc(Due));
        Assert.Empty(Calc(new(2026, 3, 1), [new(Due, 3000m)]));
    }
    [Fact] public void OneAndThirtyDaysUseStatutoryFivePercentOnDailyThirtyBasis()
    {
        var one = Calc(new(2026, 2, 1));
        Assert.Equal(5.00m, Total(one)); Assert.Equal(LatePenaltyRules.StatutoryMonthlyRate, Assert.Single(one).MonthlyRate); Assert.Null(one[0].PolicyId);
        Assert.Equal(150.00m, Total(Calc(new(2026, 3, 2))));
    }
    [Fact] public void SwitchOffOrMissingSwitchNeverAccrues()
    {
        Assert.Empty(Calc(new(2026, 6, 1), switches: []));
        Assert.Empty(Calc(new(2026, 6, 1), switches: [new(new(2000, 1, 1), false)]));
        var later = Calc(new(2026, 3, 2), switches: [new(new(2026, 2, 21), true)]);
        Assert.Equal(new DateOnly(2026, 2, 21), later[0].From); Assert.Equal(50.00m, Total(later));
        var stopped = Calc(new(2026, 3, 31), switches: [new(new(2000, 1, 1), true), new(new(2026, 2, 11), false)]);
        Assert.Equal(new DateOnly(2026, 2, 10), Assert.Single(stopped).To);
    }
    [Fact] public void PartialPaymentsReduceOnlyFollowingDaysAndFullPaymentStopsAccrual()
    {
        var segments = Calc(new(2026, 3, 2), [new(new(2026, 2, 10), 1000m)]);
        Assert.Equal(2, segments.Count);
        Assert.Equal((3000m, 10), (segments[0].Principal, segments[0].Days));
        Assert.Equal((2000m, 20), (segments[1].Principal, segments[1].Days));
        Assert.Equal(50.00m + 66.666667m, segments.Sum(value => value.Amount));
        var closed = Calc(new(2026, 12, 31), [new(new(2026, 2, 10), 1000m), new(new(2026, 2, 20), 2000m)]);
        Assert.Equal(new DateOnly(2026, 2, 20), closed[^1].To);
        Assert.Equal(50.00m + 33.33m, Total(closed));
    }
    [Fact] public void YearTransitionAndSameYearPolicyChangesSplitPeriodsWithoutCompounding()
    {
        var policy2026 = Guid.NewGuid(); var policy2027 = Guid.NewGuid();
        PenaltyRatePeriod[] policies = [new(new(2026, 1, 1), new(2026, 6, 30), 5m, LatePenaltyMethod.DAILY_30, policy2026),
            new(new(2026, 7, 1), new(2026, 12, 31), 3m, LatePenaltyMethod.DAILY_30, null), new(new(2027, 1, 1), null, 4m, LatePenaltyMethod.DAILY_30, policy2027)];
        var segments = Calc(new(2027, 1, 30), policies: policies);
        Assert.Equal([5m, 3m, 4m], segments.Select(value => value.MonthlyRate));
        Assert.Equal(new DateOnly(2027, 1, 1), segments[2].From); Assert.Equal(policy2027, segments[2].PolicyId);
        Assert.All(segments, value => Assert.Equal(3000m, value.Principal));
        Assert.Equal(30, segments[2].Days); Assert.Equal(120.00m, segments[2].Amount);
    }
    [Fact] public void ActualMonthMethodSplitsByCalendarMonth()
    {
        PenaltyRatePeriod[] policies = [new(new(2026, 1, 1), null, 5m, LatePenaltyMethod.DAILY_ACTUAL_MONTH, Guid.NewGuid())];
        var segments = Calc(new(2026, 3, 31), policies: policies);
        Assert.Equal([28, 31], segments.Select(value => value.Days));
        Assert.Equal(300.00m, Total(segments));
    }
    [Fact] public void RoundingIsControlledAtTwoDecimalsAwayFromZero()
    {
        Assert.Equal(0.02m, Total(Calc(new(2026, 2, 1), amount: 10m)));
        Assert.Equal(0.01m, LatePenaltyCalculator.Round(0.005m));
        Assert.Equal(0.00m, LatePenaltyCalculator.Round(0.0049m));
    }
    [Fact] public void PastYearDebtStartsAtDueDateAndResumesFromGivenStart()
    {
        var full = Calc(new(2027, 1, 31)); var resumed = LatePenaltyCalculator.Calculate(3000m, Due, new(2026, 7, 1), new(2027, 1, 31), NoPayments, On, NoPolicies);
        Assert.Equal(new DateOnly(2026, 2, 1), full[0].From); Assert.Equal(new DateOnly(2026, 7, 1), resumed[0].From);
        Assert.True(Total(full) > Total(resumed));
    }

    private static LatePenaltyPolicy Draft(decimal rate = 5m, LatePenaltyLegalBasis basis = LatePenaltyLegalBasis.KMK_20, Guid? document = null, DateOnly? to = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new(2027, 1, 1), to, rate, LatePenaltyMethod.DAILY_30, basis, " KMK m.20 ", document);
    [Theory] [InlineData("0")] [InlineData("5.0001")] [InlineData("6")] [InlineData("-1")] [InlineData("1.00001")] [InlineData("4")] [InlineData("4.99")]
    public void OnlyTheStatutoryRateIsAccepted(string rate) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Draft(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)));
    [Fact] public void OnlyTheStatutoryPolicyCanBeDefinedAndApproved()
    {
        var statutory = Draft(); statutory.Approve("approver", DateTimeOffset.UtcNow);
        Assert.Equal(LatePenaltyPolicyStatus.APPROVED, statutory.Status); Assert.Equal(2027, statutory.Year); Assert.Equal("KMK m.20", statutory.LegalReference);
        Assert.Throws<InvalidOperationException>(() => statutory.UpdateDraft(new(2027, 1, 1), null, 5m, LatePenaltyMethod.DAILY_30, LatePenaltyLegalBasis.KMK_20, "x", null));
        Assert.Throws<ArgumentException>(() => Draft(basis: LatePenaltyLegalBasis.MANAGEMENT_PLAN, document: Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new LatePenaltyPolicy(Guid.NewGuid(), Guid.NewGuid(), new(2027, 1, 1), null, 5m,
            LatePenaltyMethod.DAILY_ACTUAL_MONTH, LatePenaltyLegalBasis.KMK_20, "KMK", null));
    }
    [Fact] public void UserExampleThreeThousandLiraGivesOneFiftyAtThirtyAndThreeHundredAtSixtyDays()
    {
        var due = new DateOnly(2026, 1, 31);
        decimal At(int days) => Total(LatePenaltyCalculator.Calculate(3000m, due, due.AddDays(1), due.AddDays(days), NoPayments, On, NoPolicies));
        Assert.Equal((150.00m, 300.00m), (At(30), At(60)));
        Assert.Equal(3300.00m, 3000m + At(60));
        Assert.All(LatePenaltyCalculator.Calculate(3000m, due, due.AddDays(1), due.AddDays(60), NoPayments, On, NoPolicies), value => Assert.Equal(3000m, value.Principal));
    }
    [Fact] public void ApprovedPeriodsDetectOverlapAndCanOnlyBeShortened()
    {
        var open = Draft(); var closed = Draft(to: new(2027, 6, 30)); var later = new LatePenaltyPolicy(Guid.NewGuid(), Guid.NewGuid(), new(2027, 7, 1), null, 5m,
            LatePenaltyMethod.DAILY_30, LatePenaltyLegalBasis.KMK_20, "KMK", null);
        Assert.True(open.Overlaps(later)); Assert.False(closed.Overlaps(later));
        open.Approve("approver", DateTimeOffset.UtcNow);
        open.EndOn(new(2027, 3, 31), "Yeni karar");
        Assert.True(open.Covers(new(2027, 3, 31))); Assert.False(open.Covers(new(2027, 4, 1)));
        Assert.Throws<InvalidOperationException>(() => open.EndOn(new(2027, 5, 31), "Uzatma"));
        var draft = Draft(); draft.Cancel("Hatalı"); Assert.Throws<InvalidOperationException>(() => draft.Approve("approver", DateTimeOffset.UtcNow));
    }

    private static DuesCharge Charge(decimal amount = 3000m) =>
        new DuesAssessment(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), new(2026, 1, 1), amount, "TRY", Due, "Dues").AddCharge(Guid.NewGuid());
    [Fact] public void PenaltyAllocationPaysOldestAccrualFirstAndReversesExactly()
    {
        var charge = Charge();
        var first = new LatePenaltyAccrual(charge, Calc(new(2026, 2, 28)));
        var second = new LatePenaltyAccrual(charge, LatePenaltyCalculator.Calculate(3000m, Due, new(2026, 3, 1), new(2026, 3, 31), NoPayments, On, NoPolicies));
        Assert.Equal((140.00m, 155.00m), (first.Amount, second.Amount));
        var payment = new Payment(charge.PropertyId, charge.UnitId, Guid.NewGuid(), new string('b', 64), 200m, "TRY", new(2026, 4, 1), PaymentMethod.CASH, null, "Gecikme");
        var parts = payment.AllocatePenalty(charge, [second, first], 200m); payment.ValidateComplete();
        Assert.Equal([first.Id, second.Id], parts.Select(value => value.AccrualId!.Value));
        Assert.All(parts, value => Assert.Equal(AllocationKind.PENALTY, value.Kind));
        Assert.Equal((0m, 95.00m), (first.Balance, second.Balance)); Assert.Equal(0m, charge.PaidAmount);
        Assert.Throws<InvalidOperationException>(() => second.Waive("Muafiyet", DateTimeOffset.UtcNow));
        first.ReversePayment(140m); second.ReversePayment(60m);
        Assert.Equal(295.00m, first.Balance + second.Balance);
        var overpay = new Payment(charge.PropertyId, charge.UnitId, Guid.NewGuid(), new string('c', 64), 300m, "TRY", new(2026, 4, 1), PaymentMethod.CASH, null, "Fazla");
        Assert.Throws<InvalidOperationException>(() => overpay.AllocatePenalty(charge, [first, second], 300m));
        Assert.Equal(0m, first.PaidAmount + second.PaidAmount);
    }
    [Fact] public void WaivedAndSupersededAccrualsKeepHistoryAndHaveNoBalance()
    {
        var charge = Charge(); var accrual = new LatePenaltyAccrual(charge, Calc(new(2026, 2, 28)));
        accrual.Waive(" Yönetim kurulu kararı ", DateTimeOffset.UtcNow);
        Assert.Equal((LatePenaltyAccrualStatus.WAIVED, 0m, 140.00m), (accrual.Status, accrual.Balance, accrual.Amount));
        Assert.Throws<InvalidOperationException>(() => accrual.Supersede("Tekrar", DateTimeOffset.UtcNow));
        Assert.NotEmpty(accrual.Segments); Assert.False(accrual.IsDeleted);
        Assert.Throws<ArgumentException>(() => new LatePenaltyAccrual(charge, []));
    }
}
