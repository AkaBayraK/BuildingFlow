using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class AccountPhoneVerificationTests
{
    [Theory]
    [InlineData("0555 123 45 67")][InlineData("5551234567")][InlineData("+90 (555) 123-45-67")]
    public void EquivalentTurkishPhoneValuesHaveSameKey(string value) => Assert.Equal("+905551234567", PhoneContactKey.FromLegacy(value));

    [Fact] public void EvidenceSurvivesCandidateAndEquivalentFormattingUntilExplicitRemoval()
    {
        var p = new AccountPhoneVerification("owner"); var time = DateTimeOffset.UtcNow;
        Assert.True(p.Stage("05551234567")); Assert.Null(p.VerifiedAt);
        p.Confirm("+905551234567", time);
        Assert.False(p.Stage("+90 555 123 45 67")); Assert.Equal(time, p.VerifiedAt);
        Assert.True(p.Stage("05559876543")); Assert.Equal("05551234567", p.VerifiedPhone);
        Assert.Equal("05559876543", p.PendingPhone); Assert.Equal(time, p.VerifiedAt);
        Assert.Throws<InvalidOperationException>(() => p.Confirm("+905551234567", time));
        Assert.True(p.Stage("+905551234567")); Assert.Null(p.PendingPhone); Assert.Equal(time, p.VerifiedAt);
        Assert.True(p.Stage(null)); Assert.Null(p.VerifiedAt); Assert.Null(p.VerifiedPhone); Assert.Null(p.PendingKey);
    }

    [Theory][InlineData("1234567")][InlineData("555+1234567")]
    public void AmbiguousLegacyValuesRemainUnverified(string value)
    {
        var p = new AccountPhoneVerification("owner"); p.Stage(value);
        Assert.Throws<InvalidOperationException>(() => p.Confirm(p.PendingKey!, DateTimeOffset.UtcNow));
        Assert.Null(p.VerifiedAt);
    }
}
