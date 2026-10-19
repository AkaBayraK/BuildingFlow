using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class PersonNamesTests
{
    [Theory]
    [InlineData("abdulkadir", "Abdulkadir")]
    [InlineData("  ABDULKADİR   ismail ", "Abdulkadir İsmail")]
    [InlineData("IŞIL", "Işıl")]
    [InlineData("ayşe-nur", "Ayşe-Nur")]
    [InlineData("çağrı ömer", "Çağrı Ömer")]
    public void GivenNamesKeepOnlyTheFirstLetterCapitalised(string input, string expected) => Assert.Equal(expected, PersonNames.Given(input));

    [Theory]
    [InlineData("altunbayrak", "ALTUNBAYRAK")]
    [InlineData(" şimşek  yılmaz ", "ŞİMŞEK YILMAZ")]
    [InlineData("ışık", "IŞIK")]
    public void FamilyNamesAreCapitals(string input, string expected) => Assert.Equal(expected, PersonNames.Family(input));

    [Fact] public void ForeignCitizensUseLanguageNeutralCasing()
    {
        Assert.Equal("William", PersonNames.Given("WILLIAM", "FOREIGN"));Assert.Equal("SMITH", PersonNames.Family("smith", "FOREIGN"));
        Assert.Equal("Wıllıam", PersonNames.Given("WILLIAM", "DOMESTIC"));
    }

    [Fact] public void CompanyTitlesAndMissingValuesAreLeftAlone()
    {
        Assert.Equal(("Örnek Yapı A.Ş.", null), PersonNames.Apply(" Örnek Yapı A.Ş. ", null, "LEGAL", "DOMESTIC"));
        Assert.Equal(("Ali", "VELİ"), PersonNames.Apply("ali", "veli", "NATURAL", "DOMESTIC"));Assert.Null(PersonNames.Given(null));
    }
}
