using System.Globalization;
namespace BuildingFlow.Application;
public sealed record CommonAreaUtilization(int Confirmed, int Cancelled, int? OpeningMinutes, int? ClosedMinutes,
    int? AvailableMinutes, int? BookedMinutes, long? PersonMinutes, long? CapacityMinutes, string? OccupancyPercent, string? CapacityUsePercent);
public sealed record CommonAreaReportDay(string Date, CommonAreaUtilization Metrics);
public sealed record CommonAreaReportResponse(Guid PropertyId, Guid CommonAreaId, string From, string To, DateTimeOffset AsOf,
    long AreaVersion, int Capacity, string? OpensAt, string? ClosesAt, bool CurrentConfiguration, bool PaymentSupported,
    int MeasuredDays, CommonAreaUtilization Totals, IReadOnlyList<CommonAreaReportDay> Days);
public sealed partial class CommonAreaReservationService
{
    public Task<CommonAreaReportResponse?> Report(Guid property, Guid area, string? from, string? to, CancellationToken token)
    {
        Manager(); var first = ParseDate(from, "from"); var last = ParseDate(to, "to");
        if (last < first || last.DayNumber - first.DayNumber > 92) Invalid("to");
        return store.Report(property, area, first, last, token);
    }
}
public static class CommonAreaReportMath
{
    private static string? Rate(long? value, long? denominator) => value.HasValue && denominator > 0
        ? Math.Round(100m * value.Value / denominator.Value, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture) : null;
    public static CommonAreaUtilization Measure(int confirmed, int cancelled, int capacity, int? opens, int? closes,
        IReadOnlyList<(int Start, int End, int Size)> bookings, IReadOnlyList<(int Start, int End)> closures)
    {
        if (opens is null || closes is null || closes <= opens || capacity < 1)
            return new(confirmed, cancelled, null, null, null, null, null, null, null, null);
        var first = opens.Value; var last = closes.Value;
        var points = bookings.SelectMany(x => new[] { x.Start, x.End }).Concat(closures.SelectMany(x => new[] { x.Start, x.End }))
            .Append(first).Append(last).Where(x => x >= first && x <= last).Distinct().Order().ToArray();
        var closed = 0; var booked = 0; long people = 0;
        for (var i = 0; i + 1 < points.Length; i++) {
            var start = points[i]; var end = points[i + 1]; var duration = end - start;
            if (closures.Any(x => x.Start < end && start < x.End)) { closed += duration; continue; }
            var size = bookings.Where(x => x.Start < end && start < x.End).Sum(x => x.Size);
            if (size > 0) { booked += duration; people += (long)size * duration; }
        }
        var opening = last - first; var available = opening - closed; var capacityMinutes = (long)available * capacity;
        return new(confirmed, cancelled, opening, closed, available, booked, people, capacityMinutes, Rate(booked, available), Rate(people, capacityMinutes));
    }
    public static CommonAreaUtilization Total(IReadOnlyList<CommonAreaUtilization> rows)
    {
        var measured = rows.Where(x => x.OpeningMinutes.HasValue).ToArray();
        int? opening = measured.Length == 0 ? null : measured.Sum(x => x.OpeningMinutes!.Value);
        int? closed = measured.Length == 0 ? null : measured.Sum(x => x.ClosedMinutes!.Value);
        int? available = measured.Length == 0 ? null : measured.Sum(x => x.AvailableMinutes!.Value);
        int? booked = measured.Length == 0 ? null : measured.Sum(x => x.BookedMinutes!.Value);
        long? people = measured.Length == 0 ? null : measured.Sum(x => x.PersonMinutes!.Value);
        long? capacity = measured.Length == 0 ? null : measured.Sum(x => x.CapacityMinutes!.Value);
        return new(rows.Sum(x => x.Confirmed), rows.Sum(x => x.Cancelled), opening, closed, available, booked, people, capacity, Rate(booked, available), Rate(people, capacity));
    }
}
