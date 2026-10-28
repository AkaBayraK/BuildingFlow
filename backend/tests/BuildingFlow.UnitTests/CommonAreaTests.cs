using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class CommonAreaTests
{
    private static CommonArea Area(int capacity = 20, decimal fee = 0, Guid? building = null, CommonAreaVisibility visibility = CommonAreaVisibility.PROPERTY,
        TimeOnly? opens = null, TimeOnly? closes = null) => new(Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), " Havuz ", CommonAreaType.POOL,
            building, visibility, CommonAreaUsage.RESERVATION, capacity, " Açıklama ", " Kurallar ", opens, closes, fee);
    [Fact] public void NormalizesTextAndKeepsDefinitionSeparateFromBooking()
    {
        var area = Area(opens: new(8, 0), closes: new(22, 0), fee: 150.25m);
        Assert.Equal("Havuz", area.Name); Assert.Equal("Kurallar", area.Rules); Assert.Equal("TRY", area.Currency);
        Assert.Equal(150.25m, area.FeeAmount); Assert.True(area.IsEnabled); Assert.False(area.IsDeleted);
        var request = area.RequestId; var hash = area.Fingerprint;
        area.Edit("Salon", CommonAreaType.MEETING_ROOM, null, CommonAreaVisibility.PROPERTY, CommonAreaUsage.APPROVAL, 2, " ", null, null, null, 0, false);
        Assert.Null(area.Description); Assert.Null(area.OpensAt); Assert.False(area.IsEnabled);
        Assert.Equal(request, area.RequestId); Assert.Equal(hash, area.Fingerprint);
    }
    [Theory][InlineData(0)][InlineData(-1)][InlineData(10001)] public void RejectsInvalidCapacity(int capacity) => Assert.Throws<ArgumentException>(() => Area(capacity));
    [Theory][InlineData("-1")][InlineData("0.001")][InlineData("100000000")] public void RejectsInvalidFee(string fee) => Assert.Throws<ArgumentException>(() => Area(fee: decimal.Parse(fee, System.Globalization.CultureInfo.InvariantCulture)));
    [Fact] public void RequiresBuildingForRestrictedVisibilityAndPairedOrderedMinuteHours()
    {
        Assert.Throws<ArgumentException>(() => Area(visibility: CommonAreaVisibility.BUILDING));
        Assert.Throws<ArgumentException>(() => Area(building: Guid.Empty));
        Assert.Throws<ArgumentException>(() => Area(opens: new(8, 0)));
        Assert.Throws<ArgumentException>(() => Area(opens: new(22, 0), closes: new(8, 0)));
        Assert.Throws<ArgumentException>(() => Area(opens: new(8, 0, 1), closes: new(22, 0)));
        Assert.NotNull(Area(building: Guid.NewGuid(), visibility: CommonAreaVisibility.BUILDING));
    }
}
