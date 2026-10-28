using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class CommonAreaStore
{
    public async Task<CommonAreaReportResponse?> Report(Guid property, Guid area, DateOnly from, DateOnly to, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(property, token)) return null;
        var target = await db.Set<CommonArea>().AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == area, token);
        if (target is null) return null;
        // Aggregation is independent of list pagination and never selects private notes or unit/person data.
        var bookings = await db.Set<CommonAreaReservation>().AsNoTracking().Where(x => x.PropertyId == property && x.CommonAreaId == area && x.Date >= from && x.Date <= to)
            .GroupBy(x => new { x.Date, x.StartsAt, x.EndsAt, x.Status })
            .Select(g => new { g.Key.Date, g.Key.StartsAt, g.Key.EndsAt, g.Key.Status, Count = g.Count(), PartySize = g.Sum(x => x.PartySize) }).Take(100001).ToArrayAsync(token);
        if (bookings.Length > 100000) CommonAreaReservationService.Invalid("to");
        var closures = await db.Set<CommonAreaClosure>().AsNoTracking().Where(x => x.PropertyId == property && x.CommonAreaId == area && x.Date >= from && x.Date <= to && x.ReleasedAt == null && x.IsEnabled)
            .Select(x => new { x.Date, x.StartsAt, x.EndsAt }).ToArrayAsync(token);
        var days = new List<CommonAreaReportDay>();
        for (var day = from; day <= to; day = day.AddDays(1)) {
            var rows = bookings.Where(x => x.Date == day).ToArray();
            var metrics = CommonAreaReportMath.Measure(rows.Where(x => x.Status == CommonAreaReservationStatus.CONFIRMED).Sum(x => x.Count), rows.Where(x => x.Status == CommonAreaReservationStatus.CANCELLED).Sum(x => x.Count), target.Capacity,
                target.OpensAt is {} start ? start.Hour * 60 + start.Minute : null, target.ClosesAt is {} end ? end.Hour * 60 + end.Minute : null,
                rows.Where(x => x.Status == CommonAreaReservationStatus.CONFIRMED).Select(x => (x.StartsAt.Hour * 60 + x.StartsAt.Minute, x.EndsAt.Hour * 60 + x.EndsAt.Minute, x.PartySize)).ToArray(),
                closures.Where(x => x.Date == day).Select(x => (x.StartsAt.Hour * 60 + x.StartsAt.Minute, x.EndsAt.Hour * 60 + x.EndsAt.Minute)).ToArray());
            days.Add(new(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), metrics));
        }
        var result = new CommonAreaReportResponse(property, area, from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), clock.GetUtcNow(),
            target.Version, target.Capacity, target.OpensAt is {} o ? Hm(o) : null, target.ClosesAt is {} e ? Hm(e) : null, true, false,
            days.Count(x => x.Metrics.OpeningMinutes.HasValue), CommonAreaReportMath.Total(days.Select(x => x.Metrics).ToArray()), days);
        await tx.CommitAsync(token); return result;
    }
}
