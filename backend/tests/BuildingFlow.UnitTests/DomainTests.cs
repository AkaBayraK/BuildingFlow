using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class DomainTests
{
    private static Site Create() => new("BuildingFlow", "İstanbul", "Kadıköy", "Test address");
    [Fact] public void SiteCreatesWithIdentityAndTrimmedName() { var site = new Site(" Site ", "City", "District", "Address"); Assert.NotEqual(Guid.Empty, site.Id); Assert.Equal("Site", site.Name); Assert.True(site.IsEnabled); }
    [Theory][InlineData("")][InlineData(" ")] public void RejectsEmptySiteName(string name) => Assert.Throws<ArgumentException>(() => new Site(name, "City", "District", "Address"));
    [Fact] public void BlockHasSiteIdentity() { var site = Create(); Assert.Equal(site.Id, site.AddBlock("A").SiteId); }
    [Fact] public void RejectsEmptyBlockName() => Assert.Throws<ArgumentException>(() => Create().AddBlock(" "));
    [Fact] public void RejectsDuplicateBlockIgnoringCase() { var site = Create(); site.AddBlock("A"); Assert.Throws<ArgumentException>(() => site.AddBlock(" a ")); }
    [Theory][InlineData(0)][InlineData(-1)] public void RejectsNonPositiveArea(int area) => Assert.Throws<ArgumentOutOfRangeException>(() => Create().AddBlock("A").AddApartment("1", 0, area));
    [Fact] public void ApartmentPreservesRelationshipAndBasementFloor() { var block = Create().AddBlock("A"); var apartment = block.AddApartment("1", -1, 120); Assert.Equal(block.Id, apartment.BlockId); Assert.Equal(-1, apartment.Floor); }
    [Fact] public void RejectsEmptyApartmentNumber() => Assert.Throws<ArgumentException>(() => Create().AddBlock("A").AddApartment(" ", 1, 120));
    [Fact] public void RejectsDuplicateApartment() { var block = Create().AddBlock("A"); block.AddApartment("A1", 0, 100); Assert.Throws<ArgumentException>(() => block.AddApartment(" a1 ", 0, 100)); }
}
