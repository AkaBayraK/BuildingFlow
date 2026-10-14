using BuildingFlow.Application;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class MobileSessionStore(BuildingFlowDbContext db, TimeProvider clock) : IMobileSessionStore
{
    public async Task<MobileSession> ReadAsync(string userId, string? username, IReadOnlyList<string> roles, CancellationToken token)
    {
        var super = roles.Contains("SUPER_ADMIN");
        var properties = db.Properties.AsNoTracking().Where(p => p.IsEnabled &&
            (super || db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == userId && m.IsEnabled)));
        var management = roles.Where(r => r is "SUPER_ADMIN" or "SITE_ADMIN" or "MANAGER" or "ACCOUNTANT").ToArray();
        var result = new List<MobileContext>();
        foreach (var role in management)
        {
            var values = await properties.OrderBy(p => p.Id).Take(501).Select(p => new MobileContext(p.Id, p.Name, null, null, role)).ToListAsync(token);
            result.AddRange(values);
        }
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
        var occupants = await (from o in db.UnitOccupancies.AsNoTracking()
            join p in properties on o.PropertyId equals p.Id
            join person in db.People on o.PersonId equals person.Id
            join unit in db.Units on o.UnitId equals unit.Id
            where person.LinkedUserId == userId && person.IsEnabled && unit.IsEnabled && o.IsEnabled &&
                unit.PropertyId == p.Id && person.PropertyId == p.Id &&
                (unit.BuildingId == null || db.Buildings.Any(b => b.Id == unit.BuildingId && b.PropertyId == p.Id && b.IsEnabled)) &&
                o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
            orderby p.Id, unit.Id, o.Id
            select new { p.Id, p.Name, UnitId = unit.Id, unit.Number, o.Type }).Take(501).ToListAsync(token);
        result.AddRange(occupants.Select(o => new MobileContext(o.Id, o.Name, o.UnitId, o.Number, o.Type.ToString())));
        var unique = result.Distinct().OrderBy(c => c.PropertyId).ThenBy(c => c.UnitId).ThenBy(c => c.Role).ToArray();
        return new(userId, username, unique.Take(500).ToArray(), unique.Length > 500);
    }
}
