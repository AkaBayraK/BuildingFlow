using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class PhoneOtpTests
{
    [Theory] [InlineData("05551234567","+905551234567")] [InlineData("+90 555 123 45 67","+905551234567")] [InlineData("5551234567","+905551234567")]
    [InlineData("+442071234567",null)] [InlineData("+902121234567",null)] [InlineData("",null)] [InlineData("+90abc5551234567",null)]
    public void OnlyTurkishMobileCandidatesAreAccepted(string input,string? expected)=>Assert.Equal(expected,PhoneOtpPolicy.Normalize(input));
    [Fact] public void FifthWrongAttemptLocksAndErasesSecrets()
    {
        var now=DateTimeOffset.UtcNow;var c=Challenge(now);
        for(var n=0;n<4;n++)Assert.Equal("REJECTED",c.Attempt(false,now));
        Assert.Equal("LOCKED",c.Attempt(false,now));Assert.Null(c.CodeMac);Assert.Null(c.ProtectedPayload);
        Assert.Equal("LOCKED",c.Attempt(true,now));Assert.Equal(5,c.Attempts);
    }
    [Fact] public void ProofExpiresAtBoundaryAndCannotBeReplayedOrRevived()
    {
        var now=DateTimeOffset.UtcNow;var expired=Challenge(now);Assert.Equal("EXPIRED",expired.Attempt(true,now.AddMinutes(5)));
        var valid=Challenge(now);Assert.Equal("VERIFIED",valid.Attempt(true,now.AddMinutes(4)));Assert.Null(valid.CodeMac);Assert.Null(valid.ProtectedPayload);
        valid.Delivery("ACCEPTED","1");Assert.Equal("VERIFIED",valid.Status);
        var cancelled=Challenge(now);cancelled.Cancel();Assert.Equal("CANCELLED",cancelled.Attempt(true,now));
    }
    [Fact] public void PolicyAndCodeSyntaxRemainBounded()
    {
        Assert.Equal(5,PhoneOtpPolicy.MaxAttempts);Assert.Equal(TimeSpan.FromMinutes(5),PhoneOtpPolicy.Lifetime);Assert.Equal(TimeSpan.FromSeconds(60),PhoneOtpPolicy.ResendDelay);
        Assert.True(PhoneOtpPolicy.IsCode("000000"));foreach(var invalid in new[]{"12345","1234567","１２３４５６","123 45","abcdef"})Assert.False(PhoneOtpPolicy.IsCode(invalid));
    }
    private static PhoneOtpChallenge Challenge(DateTimeOffset now){var c=new PhoneOtpChallenge("owner",new string('A',64),new string('B',64),now);c.SetSecret(new string('C',64),"encrypted");c.Delivery("ACCEPTED","1");return c;}
}
