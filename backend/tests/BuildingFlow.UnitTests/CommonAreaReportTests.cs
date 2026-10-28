using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class CommonAreaReportTests
{
    [Fact] public void OverlapsAreUnionedAndClosedTimeIsRemovedFromBothDenominators()
    {
        var v=CommonAreaReportMath.Measure(2,1,14,480,1320,[(600,660,4),(630,690,6)],[(720,840),(780,900)]);
        Assert.Equal(840,v.OpeningMinutes);Assert.Equal(180,v.ClosedMinutes);Assert.Equal(660,v.AvailableMinutes);Assert.Equal(90,v.BookedMinutes);Assert.Equal(600,v.PersonMinutes);Assert.Equal("13.64",v.OccupancyPercent);Assert.Equal("6.49",v.CapacityUsePercent);
    }
    [Fact] public void ZeroAndUnknownDenominatorsAreNotInventedAsZeroPercent()
    {
        var unknown=CommonAreaReportMath.Measure(1,1,14,null,null,[(600,660,4)],[]);Assert.Null(unknown.AvailableMinutes);Assert.Null(unknown.OccupancyPercent);
        var closed=CommonAreaReportMath.Measure(0,0,14,480,600,[],[(0,1440)]);Assert.Equal(0,closed.AvailableMinutes);Assert.Null(closed.OccupancyPercent);Assert.Null(closed.CapacityUsePercent);
        var total=CommonAreaReportMath.Total([unknown,closed]);Assert.Equal(1,total.Confirmed);Assert.Equal(1,total.Cancelled);Assert.Equal(0,total.AvailableMinutes);Assert.Null(total.OccupancyPercent);
    }
    [Fact] public void ClipToCurrentHoursAndAggregateFromMinutesInsteadOfAveragingPercentages()
    {
        var a=CommonAreaReportMath.Measure(1,0,10,600,660,[(540,720,5)],[]);var b=CommonAreaReportMath.Measure(0,0,10,600,780,[],[]);
        Assert.Equal(60,a.BookedMinutes);Assert.Equal(300,a.PersonMinutes);var total=CommonAreaReportMath.Total([a,b]);Assert.Equal("25.00",total.OccupancyPercent);Assert.Equal("12.50",total.CapacityUsePercent);
        Assert.Null(CommonAreaReportMath.Total([]).OpeningMinutes);
        Assert.Equal("3.13",CommonAreaReportMath.Measure(1,0,14,0,32,[(0,1,1)],[]).OccupancyPercent);
        Assert.Equal("250.00",CommonAreaReportMath.Measure(1,0,2,600,660,[(600,660,5)],[]).CapacityUsePercent);
    }
}
