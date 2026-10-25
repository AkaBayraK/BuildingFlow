using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class OnlinePaymentTests
{
    private static readonly Guid Property = Guid.NewGuid();
    private static DuesCharge Charge(ChargeKind kind = ChargeKind.DUES, ChargeCategory? category = null, decimal amount = 500m)
    {
        var assessment = new DuesAssessment(Property, Guid.NewGuid(), new string('a', 64), new(2026, 10, 1), amount, "TRY", new(2026, 10, 15), "Fixture", "UNIT", kind, category);
        return assessment.AddCharge(Guid.NewGuid());
    }
    private static PaymentMerchantAccount Merchant() => new(Property, Guid.NewGuid(), null, "SANDBOX", "POS", "M-1", "sandbox-merchant", PaymentEnvironment.SANDBOX);
    private static PaymentIntent Intent(DuesCharge charge) => new(Property, charge.UnitId, Guid.NewGuid(), new string('f', 64), "user", "OWNER", Merchant(), DateTimeOffset.UtcNow);

    [Theory]
    [InlineData("TR400006200000000000000001", true)]
    [InlineData("tr40 0006 2000 0000 0000 0000 01", true)]
    [InlineData("TR400006200000000000000002", false)]
    [InlineData("DE89370400440532013000", false)]
    [InlineData("TR40000620000000000000000", false)]
    [InlineData("TR40000620000000000000000A", false)]
    public void IbanUsesTurkishFormatAndMod97(string value, bool valid) => Assert.Equal(valid, Iban.IsValid(Iban.Normalize(value)));

    [Fact]
    public void ExtraChargesNeedACategoryAndCopyItToTheCharge()
    {
        Assert.Throws<ArgumentException>(() => Charge(ChargeKind.EXTRA));
        Assert.Throws<ArgumentException>(() => Charge(ChargeKind.DUES, ChargeCategory.REPAIR));
        var charge = Charge(ChargeKind.EXTRA, ChargeCategory.FIXTURE);
        Assert.Equal((ChargeKind.EXTRA, (ChargeCategory?)ChargeCategory.FIXTURE), (charge.Kind, charge.Category));
        Assert.Equal(ChargeKind.DUES, Charge().Kind);
    }

    [Fact]
    public void IntentFollowsTheStateMachineAndOnlyServerLinesSetItsAmount()
    {
        var charge = Charge(); var intent = Intent(charge);
        intent.AddLine(charge, AllocationKind.PRINCIPAL, 300m);
        Assert.Throws<InvalidOperationException>(() => intent.AddLine(charge, AllocationKind.PRINCIPAL, 1m));
        intent.AddLine(charge, AllocationKind.PENALTY, 12.50m);
        Assert.Equal(312.50m, intent.Amount);
        intent.Open("sbx_ref", "https://sandbox.example/checkout/sbx_ref");
        Assert.Throws<InvalidOperationException>(() => intent.AddLine(Charge(), AllocationKind.PRINCIPAL, 1m));
        Assert.True(intent.IsOpen(DateTimeOffset.UtcNow)); Assert.False(intent.IsOpen(DateTimeOffset.UtcNow.Add(PaymentIntent.Lifetime).AddSeconds(1)));
        intent.Succeed("txn", new(2026, 10, 20), 6.22m, null, DateTimeOffset.UtcNow);
        Assert.Equal(PaymentIntentStatus.REVIEW, intent.Status);
        Assert.Throws<InvalidOperationException>(() => intent.Fail(PaymentIntentStatus.FAILED, "LATE", DateTimeOffset.UtcNow));
        intent.Refund(DateTimeOffset.UtcNow);
        Assert.Equal(PaymentIntentStatus.REFUNDED, intent.Status);
        Assert.Throws<InvalidOperationException>(() => intent.ChargeBack(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ClosedIntentsNeverBecomeSuccessful()
    {
        var charge = Charge(); var intent = Intent(charge);
        intent.AddLine(charge, AllocationKind.PRINCIPAL, 100m); intent.Open("ref", "https://sandbox.example/c/ref");
        intent.Fail(PaymentIntentStatus.CANCELLED, "PAYER_CANCELLED", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => intent.Succeed("txn", new(2026, 10, 20), 0m, null, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => intent.Fail(PaymentIntentStatus.SUCCEEDED, "x", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SucceededIntentRequiresAMatchingPayment()
    {
        var charge = Charge(); var intent = Intent(charge);
        intent.AddLine(charge, AllocationKind.PRINCIPAL, 100m); intent.Open("ref", "https://sandbox.example/c/ref");
        var wrong = new Payment(Property, charge.UnitId, Guid.NewGuid(), new string('b', 64), 99m, "TRY", new(2026, 10, 20), PaymentMethod.ONLINE_CARD, "txn", "Online");
        Assert.Throws<InvalidOperationException>(() => intent.Succeed("txn", new(2026, 10, 20), 0m, wrong, DateTimeOffset.UtcNow));
        var payment = new Payment(Property, charge.UnitId, Guid.NewGuid(), new string('b', 64), 100m, "TRY", new(2026, 10, 20), PaymentMethod.ONLINE_CARD, "txn", "Online");
        intent.Succeed("txn", new(2026, 10, 20), 1.99m, payment, DateTimeOffset.UtcNow);
        Assert.Equal((PaymentIntentStatus.SUCCEEDED, (Guid?)payment.Id), (intent.Status, intent.PaymentId));
        Assert.Throws<ArgumentException>(() => new Payment(Property, charge.UnitId, Guid.NewGuid(), new string('b', 64), 1m, "TRY", new(2026, 10, 20), PaymentMethod.ONLINE_CARD, null, "Online"));
    }

    [Fact]
    public void LiveMerchantsAndPlainSecretsAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new PaymentMerchantAccount(Property, Guid.NewGuid(), null, "SANDBOX", "POS", "M", "ref", PaymentEnvironment.LIVE));
        Assert.Throws<ArgumentException>(() => new PaymentMerchantAccount(Property, Guid.NewGuid(), null, "SANDBOX", "POS", "M", "sk_live abc/def", PaymentEnvironment.SANDBOX));
    }

    [Fact]
    public void BankAccountIbanIsImmutableAndDualApprovalNeedsAnotherPerson()
    {
        var account = new PaymentBankAccount(Property, Guid.NewGuid(), null, "Banka", "Yönetim", "TR400006200000000000000001", BankAccountPurpose.DUES, new(2026, 1, 1), null);
        Assert.Throws<InvalidOperationException>(() => account.MarkDefault(true));
        account.Approve("second", true, DateTimeOffset.UtcNow);
        Assert.True(account.UsableOn(new(2026, 6, 1))); Assert.False(account.UsableOn(new(2025, 12, 31)));
        Assert.True(account.Serves(ChargeKind.DUES)); Assert.False(account.Serves(ChargeKind.EXTRA));
        account.Close(new(2026, 6, 30), "Banka değişti");
        Assert.True(account.UsableOn(new(2026, 6, 30))); Assert.False(account.UsableOn(new(2026, 7, 1)));
        Assert.Throws<ArgumentException>(() => new PaymentBankAccount(Property, Guid.NewGuid(), null, "B", "H", "TR400006200000000000000002", BankAccountPurpose.ALL, new(2026, 1, 1), null));
    }

    [Fact]
    public void LedgerSignsReceiptNumbersAndRefundApprovalAreEnforced()
    {
        var unit = Guid.NewGuid();
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaymentLedgerEntry(Property, unit, PaymentLedgerType.COLLECTION, -1m, new(2026, 10, 1), "CASH"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaymentLedgerEntry(Property, unit, PaymentLedgerType.ONLINE_REFUND, 1m, new(2026, 10, 1), "ONLINE_CARD"));
        var payment = new Payment(Property, unit, Guid.NewGuid(), new string('b', 64), 10m, "TRY", new(2026, 10, 20), PaymentMethod.CASH, null, "Nakit");
        Assert.Equal("SITE-2026-000042", new PaymentReceipt(payment, "SITE", 2026, 42).Number);
        var charge = Charge(); var intent = Intent(charge);
        intent.AddLine(charge, AllocationKind.PRINCIPAL, 100m); intent.Open("ref", "https://sandbox.example/c/ref");
        Assert.Throws<InvalidOperationException>(() => new PaymentRefund(intent, Guid.NewGuid(), "Erken"));
        intent.Succeed("txn", new(2026, 10, 20), 0m, null, DateTimeOffset.UtcNow);
        var refund = new PaymentRefund(intent, Guid.NewGuid(), "Mükerrer");
        refund.Succeed("approver", "rf_1", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => refund.Reject("other", "late", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SettingsKeepCommissionAbsorbedAndValidatePrefix()
    {
        var settings = new PropertyPaymentSettings(Property);
        Assert.Equal((false, true, "BF", CommissionPolicy.ABSORBED), (settings.OnlineEnabled, settings.RequireDualApproval, settings.ReceiptPrefix, settings.CommissionPolicy));
        settings.Update(true, true, true, false, "site1");
        Assert.Equal("SITE1", settings.ReceiptPrefix);
        Assert.Throws<ArgumentException>(() => settings.Update(true, true, true, false, "A-B"));
    }
}
