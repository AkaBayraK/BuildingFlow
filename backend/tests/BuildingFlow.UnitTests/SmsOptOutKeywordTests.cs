using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SmsOptOutKeywordTests
{
    [Theory] [InlineData("ret gul", "RET GUL")] [InlineData("  RET   GUL  ", "RET GUL")] [InlineData("İptal", "IPTAL")] [InlineData(null, "")]
    public void NormalizesCaseAndWhitespace(string? text, string expected) => Assert.Equal(expected, SmsOptOutKeyword.Normalize(text));

    [Theory] [InlineData("RET", true)] [InlineData("ret bf", true)] [InlineData("RETRO", false)] [InlineData("Merhaba RET", false)] [InlineData("", false)]
    public void OnlyRetCommandsCount(string text, bool expected) => Assert.Equal(expected, SmsOptOutKeyword.IsOptOut(text));

    [Fact] public void OperationIdIsStablePerSenderAndMessage()
    {
        var sender = Guid.NewGuid();
        Assert.Equal(SmsOptOutKeyword.OperationId(sender, "m-1"), SmsOptOutKeyword.OperationId(sender, "m-1"));
        Assert.NotEqual(SmsOptOutKeyword.OperationId(sender, "m-1"), SmsOptOutKeyword.OperationId(sender, "m-2"));
        Assert.NotEqual(SmsOptOutKeyword.OperationId(sender, "m-1"), SmsOptOutKeyword.OperationId(Guid.NewGuid(), "m-1"));
        Assert.NotEqual(Guid.Empty, SmsOptOutKeyword.OperationId(sender, "m-1"));
    }
}
