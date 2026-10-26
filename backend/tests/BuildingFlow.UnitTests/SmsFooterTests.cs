using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SmsFooterTests
{
    [Fact] public void FooterCarriesSenderOptOutAndMersis()
    {
        var footer = SmsLegalFooter.Build(new SmsLegalSender(null, "BuildingFlow A.Ş.", "0123456789012345", "destek@example.test", "RET BF", "4567", true, false, null));
        Assert.Equal("\nBuildingFlow A.Ş.\nSMS almak istemiyorsanız RET BF yazıp 4567 numarasına ücretsiz gönderiniz.\nMERSIS: 0123456789012345", footer);
    }

    [Fact] public void NonTraderSenderFallsBackToContactInformation()
    {
        var footer = SmsLegalFooter.Build(new SmsLegalSender(Guid.NewGuid(), "Gül Sitesi Yönetimi", null, "0212 000 00 00", "RET GUL", "4567", true, false, null));
        Assert.EndsWith("\n0212 000 00 00", footer); Assert.DoesNotContain("MERSIS", footer);
    }

    [Fact] public void DisabledOptOutCannotCarryCommercialSms()
    {
        var sender = new SmsLegalSender(null, "T", null, "c", "RET BF", "4567", false, false, null);
        Assert.False(SmsLegalFooter.CanOptOut(sender));
        sender.Update("T", null, "c", "RET BF", "4567", true, false, null); Assert.True(SmsLegalFooter.CanOptOut(sender));
        sender.SetEnabled(false); Assert.False(SmsLegalFooter.CanOptOut(sender));
    }

    [Fact] public void LengthReportsEncodingCharactersAndSegments()
    {
        Assert.Equal(new SmsLength("GSM7", 160, 1), SmsText.Measure(new string('a', 160)));
        Assert.Equal(new SmsLength("UCS2", 71, 2), SmsText.Measure("ş" + new string('a', 70)));
        var footer = SmsLegalFooter.Build(new SmsLegalSender(null, "BuildingFlow A.Ş.", "0123456789012345", "c", "RET BF", "4567", true, false, null));
        Assert.Equal("UCS2", SmsText.Measure("Kampanya" + footer).Encoding); Assert.Equal(2, SmsText.Measure("Kampanya" + footer).Segments);
    }
}
