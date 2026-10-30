using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SupplierQuoteTests
{
    private static (SupplierInquiry Inquiry, SupplierInvitation Invitation, SupplierQuote Quote) Fixture(decimal amount = 100m, decimal vat = 20m, string currency = "TRY")
    {
        var inquiry = new SupplierInquiry(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Elevator", "Annual service scope", new(2027, 1, 1));
        var invitation = new SupplierInvitation(inquiry, Guid.NewGuid(), Guid.NewGuid());
        return (inquiry, invitation, new(invitation, amount, vat, currency, new(2027, 2, 1), "Parts included", "issuer", "entrant"));
    }
    [Theory] [InlineData("TRY")] [InlineData("USD")] [InlineData("EUR")]
    public void VatRoundsHalfUpWithoutConvertingCurrency(string currency)
    {
        var (_, _, quote) = Fixture(.05m, 10m, currency);
        Assert.Equal(.01m, quote.VatAmount); Assert.Equal(.06m, quote.GrossAmount); Assert.Equal(currency, quote.Currency);
    }
    [Fact] public void OwnApprovalAndOtherEntrantEditsCannotBypassSeparation()
    {
        var (_, _, quote) = Fixture();
        Assert.Throws<InvalidOperationException>(() => quote.Decide(true, "Best value", "issuer", "entrant", new(2027, 1, 1)));
        Assert.Throws<InvalidOperationException>(() => quote.Enter(90m, 20m, "TRY", new(2027, 2, 1), "Changed", "issuer", "reviewer"));
        Assert.Equal(100m, quote.NetAmount); Assert.Equal(SupplierQuoteStatus.PENDING, quote.Status);
        quote.Decide(true, "Best value", "issuer", "reviewer", new(2027, 1, 1));
        Assert.Throws<InvalidOperationException>(() => quote.Enter(90m, 20m, "TRY", new(2027, 2, 1), "Changed", "issuer", "entrant"));
        Assert.Throws<InvalidOperationException>(() => quote.Decide(false, "Changed mind", "issuer", "reviewer", new(2027, 1, 1)));
    }
    [Fact] public void ApprovalChecksIssuerSubjectPairAndExpiryInclusive()
    {
        var (_, _, quote) = Fixture();
        quote.Decide(true, "Best value", "other-issuer", "entrant", new(2027, 2, 1));
        var (_, _, expired) = Fixture();
        Assert.Throws<InvalidOperationException>(() => expired.Decide(true, "Best value", "issuer", "reviewer", new(2027, 2, 2)));
        Assert.Equal(SupplierQuoteStatus.PENDING, expired.Status);
    }
    [Theory] [InlineData(-1, 20, "TRY")] [InlineData(1.001, 20, "TRY")] [InlineData(1, -1, "TRY")] [InlineData(1, 100.01, "TRY")] [InlineData(1, 1.001, "TRY")] [InlineData(1, 20, "GBP")] [InlineData(99999999.99, 20, "TRY")]
    public void InvalidAmountsRatesAndCurrencyAreRejected(decimal amount, decimal rate, string currency)
    { Assert.ThrowsAny<ArgumentException>(() => Fixture(amount, rate, currency)); }
    [Fact] public void ReasonsAreValidatedBeforeMutationAndFinalDecisionsStayImmutable()
    {
        var (inquiry, _, quote) = Fixture();
        Assert.Throws<ArgumentException>(() => quote.Decide(false, "\t", "issuer", "reviewer", new(2027, 1, 1)));
        Assert.Null(quote.DecisionReason); Assert.Equal(SupplierQuoteStatus.PENDING, quote.Status);
        Assert.Throws<ArgumentException>(() => inquiry.Cancel("\n")); Assert.Equal(SupplierInquiryStatus.OPEN, inquiry.Status);
        quote.Decide(false, "Scope incomplete", "issuer", "reviewer", new(2027, 1, 1));
        Assert.Throws<InvalidOperationException>(() => quote.Decide(true, "Override", "issuer", "reviewer", new(2027, 1, 1)));
    }
    [Fact] public void ConversionRequiresAwardAndApprovedQuoteInExactScope()
    {
        var (inquiry, invitation, quote) = Fixture();
        Assert.Throws<InvalidOperationException>(() => new SupplierWorkOrder(inquiry, invitation, quote));
        quote.Decide(true, "Best value", "issuer", "reviewer", new(2027, 1, 1)); inquiry.Award();
        var order = new SupplierWorkOrder(inquiry, invitation, quote);
        Assert.Equal(SupplierWorkOrderStatus.DRAFT, order.Status); Assert.Equal(120m, order.Amount); Assert.Equal(quote.Id, order.QuoteId);
        var other = Fixture(); Assert.Throws<InvalidOperationException>(() => new SupplierWorkOrder(inquiry, other.Invitation, quote));
        Assert.Throws<InvalidOperationException>(() => inquiry.Cancel("After award"));
    }
}
