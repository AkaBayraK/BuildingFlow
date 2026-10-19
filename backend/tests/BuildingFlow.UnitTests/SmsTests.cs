using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SmsTests
{
    [Theory]
    [InlineData("+90 555 123 45 67", "+905551234567")] [InlineData("0555 123 45 67", "+905551234567")] [InlineData("5551234567", "+905551234567")]
    [InlineData("0090 (555) 123-45-67", "+905551234567")] [InlineData("+4915123456789", "+4915123456789")]
    public void PhonesNormalizeToE164(string input, string expected) => Assert.Equal(expected, SmsText.NormalizePhone(input));

    [Theory] [InlineData(null)] [InlineData("")] [InlineData("555")] [InlineData("+90 555 abc")] [InlineData("12+34567890")] [InlineData("+0123456789")]
    public void InvalidPhonesAreRejected(string? input) => Assert.Null(SmsText.NormalizePhone(input));

    [Fact] public void PhoneMaskKeepsOnlyPrefixAndLastDigits() => Assert.Equal("+90********67", SmsText.MaskPhone("+905551234567"));

    [Fact] public void SegmentsFollowGsmAndUnicodeLimits()
    {
        Assert.Equal(0, SmsText.Segments("")); Assert.Equal(1, SmsText.Segments(new string('a', 160))); Assert.Equal(2, SmsText.Segments(new string('a', 161)));
        Assert.Equal(1, SmsText.Segments(new string('€', 80))); Assert.Equal(2, SmsText.Segments(new string('€', 81)));
        Assert.Equal(1, SmsText.Segments("Kayıt" + new string('a', 65))); Assert.Equal(2, SmsText.Segments("Kayıt" + new string('a', 66)));
    }

    [Fact] public void RenderingMasksSecretParametersOnly()
    {
        var type = SmsTypes.Find(SmsTypes.PersonRegistered)!;
        var values = new Dictionary<string, string> { ["code"] = "482917", ["fullName"] = "Ada Kaya", ["propertyName"] = "Gül Sitesi" };
        Assert.Equal("Sayın Ada Kaya, 482917 kod ile kişi kaydınız kayıt altına alınmıştır. Gül Sitesi", SmsText.Render(type.DefaultContent, values));
        var masked = SmsText.Mask(type.DefaultContent, type, values);
        Assert.DoesNotContain("482917", masked); Assert.Contains("******", masked); Assert.Contains("Ada Kaya", masked);
        Assert.Equal(["fullName", "code", "propertyName"], SmsText.Placeholders(type.DefaultContent));
    }

    [Fact] public void AccountCreditCannotGoNegativeOrPastLimit()
    {
        var account = new SmsAccount(Guid.NewGuid(), "Test", SmsProvider.MOCK, "BUILDING", 10);
        var load = new SmsCreditTransaction(account, SmsCreditTransactionType.LOAD, 100, 0.25m, "try", "R1", null, null);
        Assert.Equal(100, load.Quantity); Assert.Equal(100, load.BalanceAfter); Assert.Equal("TRY", load.Currency);
        Assert.Throws<InvalidOperationException>(() => new SmsCreditTransaction(account, SmsCreditTransactionType.ADJUST, -101, null, null, null, null, null));
        Assert.Throws<ArgumentException>(() => new SmsCreditTransaction(account, SmsCreditTransactionType.CONSUME, 0, null, null, null, null, null));
        Assert.Throws<ArgumentException>(() => new SmsCreditTransaction(account, SmsCreditTransactionType.LOAD, 5, 1m, null, null, null, null));
        Assert.Throws<ArgumentException>(() => new SmsCreditTransaction(account, SmsCreditTransactionType.LOAD, SmsAccount.MaxCredit, null, null, null, null, null));
        Assert.Equal(100, account.CreditBalance);
        Assert.Equal(-40, new SmsCreditTransaction(account, SmsCreditTransactionType.CONSUME, 40, null, null, null, null, Guid.NewGuid()).Quantity);
        Assert.Equal(60, account.CreditBalance);
    }

    [Theory] [InlineData("AB")] [InlineData("TOOLONGHEADER")] [InlineData("BAD*HDR")]
    public void SenderHeaderIsValidated(string header) => Assert.Throws<ArgumentException>(() => new SmsAccount(Guid.NewGuid(), "Test", SmsProvider.MOCK, header, 0));

    [Fact] public void MessageLifecycleClearsProtectedPayloadWhenFinal()
    {
        var message = new SmsMessage(Guid.NewGuid(), Guid.NewGuid(), null, null, SmsTypes.Manual, SmsPurpose.OPERATIONAL, "+90****67", "Body", 1, DateTimeOffset.UtcNow);
        message.Protect("secret");
        message.RecordFailure("PROVIDER_ERROR", DateTimeOffset.UtcNow.AddSeconds(16), false);
        Assert.Equal(SmsMessageStatus.QUEUED, message.Status); Assert.Equal("secret", message.ProtectedPayload); Assert.Equal(1, message.Attempts);
        message.MarkSent(SmsProvider.MOCK, "MOCK-1", DateTimeOffset.UtcNow);
        Assert.Equal(SmsMessageStatus.SENT, message.Status); Assert.Null(message.ProtectedPayload); Assert.Null(message.ErrorCode);
        Assert.Throws<InvalidOperationException>(() => message.Reject("LATE"));
        var failing = new SmsMessage(Guid.NewGuid(), Guid.NewGuid(), null, null, SmsTypes.Manual, SmsPurpose.OPERATIONAL, "+90****67", "Body", 1, DateTimeOffset.UtcNow);
        failing.Protect("secret");
        for (var i = 0; i < SmsMessage.MaxAttempts; i++) failing.RecordFailure("PROVIDER_ERROR", DateTimeOffset.UtcNow, false);
        Assert.Equal(SmsMessageStatus.FAILED, failing.Status); Assert.Null(failing.ProtectedPayload);
    }
}
