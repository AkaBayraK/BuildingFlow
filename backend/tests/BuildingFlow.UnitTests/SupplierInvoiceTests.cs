using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SupplierInvoiceTests
{
    private static readonly DateOnly Today = new(2027, 3, 15);
    private static SupplierInvoice Invoice(decimal net = 1000m, decimal rate = 20m, DateOnly? issue = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " GIB-2027/000123 ", issue ?? new(2027, 3, 1), new(2027, 3, 31), net, rate, ExpenseCategory.MAINTENANCE, "Elevator repair", null, null, null, "issuer", "recorder", Today);
    [Fact] public void VatIsRoundedHalfUpAndNumberKeyIgnoresSeparatorsAndCase()
    {
        var invoice = Invoice(0.05m, 10m); Assert.Equal(0.01m, invoice.VatAmount); Assert.Equal(0.06m, invoice.GrossAmount);
        Assert.Equal("GIB-2027/000123", invoice.Number); Assert.Equal("GIB2027000123", invoice.NumberKey); Assert.Equal(SupplierInvoice.Key("gib 2027.000123"), invoice.NumberKey);
        Assert.Equal(1200m, Invoice().GrossAmount); Assert.Equal(100m, Invoice(100m, 0m).GrossAmount);
    }
    [Fact] public void InvalidDatesAmountsAndRatesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Invoice(issue: Today.AddDays(1)));
        Assert.Throws<ArgumentException>(() => Invoice(rate: 100.01m)); Assert.Throws<ArgumentException>(() => Invoice(rate: 18.005m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Invoice(net: 0m)); Assert.Throws<ArgumentOutOfRangeException>(() => Invoice(net: 99999999.99m));
        Assert.Throws<ArgumentException>(() => new SupplierInvoice(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "A1", new(2027, 3, 2), new(2027, 3, 1), 1m, 0m, ExpenseCategory.OTHER, "Bad due", null, null, null, "i", "s", Today));
    }
    [Fact] public void LifecycleIsOrderedRecordsEvidenceAndBlocksCancellingPaidHistory()
    {
        var invoice = Invoice(); Assert.True(invoice.IsRecordedBy("issuer", "recorder")); Assert.False(invoice.IsRecordedBy("issuer", "approver"));
        Assert.Throws<InvalidOperationException>(() => invoice.Pay(Today, "Bank ref", Today));
        invoice.Approve(Guid.NewGuid()); Assert.Equal(SupplierInvoiceStatus.APPROVED, invoice.Status); Assert.Throws<InvalidOperationException>(() => invoice.Approve(Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => invoice.Pay(new(2027, 2, 28), "Before issue", Today)); Assert.Throws<ArgumentException>(() => invoice.Pay(Today.AddDays(1), "Future", Today));
        invoice.Pay(new(2027, 3, 10), "EFT 12345", Today); Assert.Equal("EFT 12345", invoice.PaymentReference);
        Assert.Throws<InvalidOperationException>(() => invoice.Cancel("Too late"));
        Assert.Throws<ArgumentException>(() => invoice.Reconcile(new(2027, 3, 9), "Before payment", Today));
        invoice.Reconcile(Today, "Matched bank statement", Today); Assert.Equal(SupplierInvoiceStatus.RECONCILED, invoice.Status);
        Assert.Throws<InvalidOperationException>(() => invoice.Reconcile(Today, "Again", Today));
        var other = Invoice(); Assert.Throws<ArgumentException>(() => other.Cancel(" ")); other.Cancel("Wrong amount"); Assert.Throws<InvalidOperationException>(() => other.Approve(Guid.NewGuid()));
    }
}
