using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class CommonAreaReservationTests
{
    private static CommonAreaReservation Booking(int start = 10, int end = 11, int size = 4) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), null, new(2030, 1, 1), new(start, 0), new(end, 0), size, " Not ");
    [Fact] public void EnforcesSlotsSizeAndImmutableCancellation()
    {
        var booking = Booking(); Assert.Equal("Not", booking.Note); Assert.Equal(CommonAreaReservationStatus.CONFIRMED, booking.Status);
        Assert.Throws<ArgumentException>(() => booking.Edit(null, booking.Date, new(10, 10), new(11, 0), 4, null));
        Assert.Throws<ArgumentException>(() => booking.Edit(null, booking.Date, new(11, 0), new(11, 0), 4, null));
        Assert.Throws<ArgumentException>(() => booking.Edit(Guid.Empty, booking.Date, new(10, 0), new(11, 0), 4, null));
        Assert.Throws<ArgumentException>(() => booking.Edit(null, booking.Date, new(10, 0), new(11, 0), 0, null));
        Assert.Throws<ArgumentException>(() => booking.Cancel(" ", DateTimeOffset.UtcNow));
        booking.Cancel(" Yağmur ", DateTimeOffset.UtcNow); Assert.Equal("Yağmur", booking.CancelReason);
        Assert.Throws<InvalidOperationException>(() => booking.Cancel("Tekrar", DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => booking.Edit(null, booking.Date, new(10, 0), new(11, 0), 4, null));
    }
    [Fact] public void LoadSegmentsMergeAdjacentEqualLoadAndPeakIgnoresTouchingBookings()
    {
        (TimeOnly, TimeOnly, int)[] rows = [(new(10, 0), new(11, 0), 10), (new(10, 30), new(11, 30), 4), (new(11, 30), new(12, 0), 4), (new(13, 0), new(14, 0), 14)];
        Assert.Equal([new("10:00", "10:30", 10, "PARTIAL"), new("10:30", "11:00", 14, "FULL"), new("11:00", "12:00", 4, "PARTIAL"), new("13:00", "14:00", 14, "FULL")],
            CommonAreaReservationService.Busy(rows, 14));
        Assert.Equal(14, CommonAreaReservationService.PeakLoad(rows, new(10, 45), new(10, 50)));
        Assert.Equal(0, CommonAreaReservationService.PeakLoad(rows, new(12, 0), new(13, 0)));
        Assert.Equal(4, CommonAreaReservationService.PeakLoad(rows, new(11, 0), new(12, 0)));
        Assert.Empty(CommonAreaReservationService.Busy([], 14));
    }
}
