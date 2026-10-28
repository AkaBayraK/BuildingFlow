using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed partial class ParkingStore
{
    private sealed class ParkingDayCounts
    {
        public DateOnly Day { get; set; }
        public int Entries { get; set; }
        public int Exits { get; set; }
        public int Reservations { get; set; }
    }
    public async Task<ParkingReport?> Report(Guid property, DateOnly from, DateOnly through, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Visible(property, access, token)) return null;
        var now = clock.GetUtcNow(); var today = Today;
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        DateTimeOffset Midnight(DateOnly date) => new(TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone));
        var start = Midnight(from); var end = Midnight(through.AddDays(1));
        var spaces = db.ParkingSpaces.Where(x => x.PropertyId == property && x.IsEnabled);
        var allocations = db.ParkingAllocations.Where(x => x.PropertyId == property && x.IsEnabled && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today));
        var guests = db.GuestVehicles.Where(x => x.PropertyId == property && x.IsEnabled);
        // Explicit scope and soft-delete filters; group before materialization, with historical Istanbul DST.
        var counts = await db.Database.SqlQuery<ParkingDayCounts>($"""
            SELECT day AS "Day", sum(entries)::int AS "Entries", sum(exits)::int AS "Exits", sum(reservations)::int AS "Reservations"
            FROM (
              SELECT (arrived_at AT TIME ZONE 'Europe/Istanbul')::date AS day, count(*)::int AS entries, 0 AS exits, 0 AS reservations
              FROM guest_vehicles WHERE property_id={property} AND NOT is_deleted AND is_enabled AND arrived_at >= {start} AND arrived_at < {end} GROUP BY 1
              UNION ALL
              SELECT (departed_at AT TIME ZONE 'Europe/Istanbul')::date, 0, count(*)::int, 0
              FROM guest_vehicles WHERE property_id={property} AND NOT is_deleted AND is_enabled AND departed_at >= {start} AND departed_at < {end} GROUP BY 1
              UNION ALL
              SELECT (starts_at AT TIME ZONE 'Europe/Istanbul')::date, 0, 0, count(*)::int
              FROM guest_vehicles WHERE property_id={property} AND NOT is_deleted AND is_enabled AND status <> 'CANCELLED' AND starts_at >= {start} AND starts_at < {end} GROUP BY 1
            ) movements GROUP BY day
            """).ToListAsync(token);
        var days = Enumerable.Range(0, through.DayNumber - from.DayNumber + 1).Select(i => {
            var day = from.AddDays(i); var row = counts.SingleOrDefault(x => x.Day == day);
            return new ParkingReportDay(day, row?.Entries ?? 0, row?.Exits ?? 0, row?.Reservations ?? 0);
        }).ToArray();
        var result = new ParkingReport(property, from, through, now, await spaces.CountAsync(token),
            await allocations.Where(a => spaces.Any(s => s.Id == a.SpaceId)).Select(a => a.SpaceId).Distinct().CountAsync(token),
            await guests.Where(g => g.Status == GuestVehicleStatus.PLANNED && g.StartsAt <= now && g.EndsAt > now && g.SpaceId != null && spaces.Any(s => s.Id == g.SpaceId)).Select(g => g.SpaceId).Distinct().CountAsync(token),
            await guests.CountAsync(g => g.Status == GuestVehicleStatus.ARRIVED, token),
            await guests.CountAsync(g => g.Status == GuestVehicleStatus.ARRIVED && g.EndsAt <= now, token),
            string.Equals(config["Parking:Operations:Enabled"], "true", StringComparison.OrdinalIgnoreCase), false, days);
        await tx.CommitAsync(token); return result;
    }
}
