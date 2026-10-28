using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class CommonAreaStore
{
    private const int HorizonDays = 365;
    private static void Closed() => throw new HierarchyConflictException(ErrorCatalog.CommonAreaReservationClosed.Create());
    private static void Full() => throw new HierarchyConflictException(ErrorCatalog.CommonAreaReservationFull.Create());
    private static string Hm(TimeOnly value) => value.ToString("HH:mm", CultureInfo.InvariantCulture);
    private static CommonAreaReservationResponse Response(CommonAreaReservation x, string? unitNumber, bool replay = false) => new(x.Id, x.PropertyId,
        x.CommonAreaId, x.RequestId, x.UnitId, unitNumber, x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Hm(x.StartsAt), Hm(x.EndsAt),
        x.PartySize, x.Note, x.Status.ToString(), x.CancelReason, x.CancelledAt, x.Version, x.CreatedAt, x.ModifiedAt, replay);
    private async Task<string?> UnitNumber(Guid? unit, CancellationToken token) =>
        unit is null ? null : await db.Units.IgnoreQueryFilters().Where(u => u.Id == unit).Select(u => u.Number).SingleOrDefaultAsync(token);
    private IQueryable<CommonAreaReservation> Confirmed(Guid property, Guid area, DateOnly date) => db.Set<CommonAreaReservation>()
        .Where(x => x.PropertyId == property && x.CommonAreaId == area && x.Date == date && x.Status == CommonAreaReservationStatus.CONFIRMED);
    private bool Started(DateOnly date, TimeOnly time)
    {
        var now = BusinessDate.Now(clock); var today = DateOnly.FromDateTime(now);
        return date < today || date == today && time <= TimeOnly.FromDateTime(now);
    }
    public async Task<PagedRows<CommonAreaReservationResponse>?> Reservations(Guid property, Guid area, CommonAreaReservationFilter f, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(property, token) || !await db.Set<CommonArea>().AnyAsync(x => x.PropertyId == property && x.Id == area, token)) return null;
        var rows = db.Set<CommonAreaReservation>().AsNoTracking().Where(x => x.PropertyId == property && x.CommonAreaId == area && x.Date >= f.From && x.Date <= f.To);
        if (f.Status.HasValue) rows = rows.Where(x => x.Status == f.Status);
        var count = await rows.CountAsync(token);
        var values = await rows.OrderBy(x => x.Date).ThenBy(x => x.StartsAt).ThenBy(x => x.Id).Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)
            .Select(x => new { Row = x, Number = db.Units.IgnoreQueryFilters().Where(u => u.Id == x.UnitId).Select(u => u.Number).FirstOrDefault() }).ToArrayAsync(token);
        await tx.CommitAsync(token);
        return new(values.Select(v => Response(v.Row, v.Number)).ToArray(), f.Page, f.PageSize, count);
    }
    public async Task<CommonAreaReservationResponse?> SaveReservation(Guid property, Guid area, Guid? id, CommonAreaReservationCommand c, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, token); if (parent is null) return null;
        var target = await db.Set<CommonArea>().SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == area, token); if (target is null) return null;
        CommonAreaReservation? value;
        if (id is null) {
            // Child soft-delete filter only; the parent scope was rechecked under the Property lock.
            value = await db.Set<CommonAreaReservation>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PropertyId == property && x.RequestId == c.RequestId, token);
            if (value is not null) {
                if (value.IsDeleted || value.Fingerprint != c.Fingerprint || value.CommonAreaId != area) Conflict();
                return Response(value, await UnitNumber(value.UnitId, token), true);
            }
        } else {
            value = await db.Set<CommonAreaReservation>().SingleOrDefaultAsync(x => x.PropertyId == property && x.CommonAreaId == area && x.Id == id, token);
            if (value is null) return null; Expected(c.Version, value.Version); if (value.RequestId != c.RequestId) Conflict();
            if (value.Status != CommonAreaReservationStatus.CONFIRMED || Started(value.Date, value.StartsAt)) Closed();
        }
        if (target.BuildingId is {} building && !await db.Buildings.AnyAsync(b=>b.PropertyId==property && b.Id==building && b.IsEnabled,token)) Closed();
        if (!target.IsEnabled || target.Usage == CommonAreaUsage.OPEN_USE || Started(c.Date, c.StartsAt) ||
            c.Date > BusinessDate.Today(clock).AddDays(HorizonDays) ||
            target.OpensAt.HasValue && (c.StartsAt < target.OpensAt || c.EndsAt > target.ClosesAt)) Closed();
        if (await db.Set<CommonAreaClosure>().AnyAsync(x=>x.PropertyId==property && x.CommonAreaId==area && x.Date==c.Date && x.ReleasedAt==null && x.IsEnabled && x.StartsAt<c.EndsAt && c.StartsAt<x.EndsAt,token)) MaintenanceConflict();
        if (c.PartySize > target.Capacity) Full();
        if (c.UnitId is {} unitId) {
            var unit = await db.Units.AsNoTracking().SingleOrDefaultAsync(u => u.Id == unitId && u.PropertyId == property && u.IsEnabled, token);
            if (unit is null || target.Visibility == CommonAreaVisibility.BUILDING && unit.BuildingId != target.BuildingId) CommonAreaReservationService.Invalid("unitId");
        }
        var others = await Confirmed(property, area, c.Date).Where(x => x.Id != id).Select(x => new { x.StartsAt, x.EndsAt, x.PartySize }).ToArrayAsync(token);
        var load = CommonAreaReservationService.PeakLoad(others.Select(x => (x.StartsAt, x.EndsAt, x.PartySize)).ToArray(), c.StartsAt, c.EndsAt);
        if (load + c.PartySize > target.Capacity) Full();
        if (id is null) { value = new(property, area, c.RequestId, c.Fingerprint, c.UnitId, c.Date, c.StartsAt, c.EndsAt, c.PartySize, c.Note); db.Add(value); }
        else value!.Edit(c.UnitId, c.Date, c.StartsAt, c.EndsAt, c.PartySize, c.Note);
        // Area hour/capacity edits and this booking cannot interleave unnoticed.
        db.Entry(target).Property(x => x.IsEnabled).IsModified = true; db.Entry(parent).Property(p => p.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return Response(value!, await UnitNumber(value!.UnitId, token));
    }
    public async Task<CommonAreaReservationResponse?> CancelReservation(Guid property, Guid area, Guid id, string reason, long version, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, token); if (parent is null) return null;
        var value = await db.Set<CommonAreaReservation>().SingleOrDefaultAsync(x => x.PropertyId == property && x.CommonAreaId == area && x.Id == id, token);
        if (value is null) return null; Expected(version, value.Version);
        if (value.Status != CommonAreaReservationStatus.CONFIRMED || Started(value.Date, value.EndsAt)) Closed();
        value.Cancel(reason, clock.GetUtcNow()); db.Entry(parent).Property(p => p.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return Response(value, await UnitNumber(value.UnitId, token));
    }
    public async Task<CommonAreaScheduleResponse?> Schedule(Guid property, Guid area, DateOnly date, ActiveContextQuery? context, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        CommonArea? target;
        if (context is null) target = await Allowed(property, token) ? await db.Set<CommonArea>().AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == area, token) : null;
        else {
            var visible = context.PropertyId == property ? await UnitAreas(context, token) : null;
            target = visible is null ? null : await visible.SingleOrDefaultAsync(x => x.Id == area, token);
        }
        if (target is null) return null;
        var rows = await Confirmed(property, area, date).AsNoTracking().Select(x => new { x.StartsAt, x.EndsAt, x.PartySize }).ToArrayAsync(token);
        var closures=await db.Set<CommonAreaClosure>().AsNoTracking().Where(x=>x.PropertyId==property && x.CommonAreaId==area && x.Date==date && x.ReleasedAt==null && x.IsEnabled).Select(x=>new{x.StartsAt,x.EndsAt}).ToArrayAsync(token);
        var intervals=CommonAreaReservationService.Busy(rows.Select(x=>(x.StartsAt,x.EndsAt,x.PartySize)).ToArray(),target.Capacity)
            .Concat(closures.Select(c=>new CommonAreaBusyInterval(Hm(c.StartsAt),Hm(c.EndsAt),0,"CLOSED"))).OrderBy(x=>x.StartsAt,StringComparer.Ordinal).ToArray();
        await tx.CommitAsync(token);
        return new(target.Id, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), target.OpensAt is {} o ? Hm(o) : null, target.ClosesAt is {} e ? Hm(e) : null,
            target.Capacity, target.IsEnabled && target.Usage != CommonAreaUsage.OPEN_USE,
            intervals);
    }
    private async Task FutureReservationsFit(CommonArea area, CommonAreaCommand next, CancellationToken token)
    {
        var today = BusinessDate.Today(clock);
        var rows = await db.Set<CommonAreaReservation>().AsNoTracking().Where(x => x.PropertyId == area.PropertyId && x.CommonAreaId == area.Id &&
            x.Status == CommonAreaReservationStatus.CONFIRMED && x.Date >= today).Select(x => new { x.Date, x.StartsAt, x.EndsAt, x.PartySize }).ToArrayAsync(token);
        if (rows.Length == 0) return;
        if (next.Usage == CommonAreaUsage.OPEN_USE || next.OpensAt.HasValue && rows.Any(r => r.StartsAt < next.OpensAt || r.EndsAt > next.ClosesAt) ||
            rows.GroupBy(r => r.Date).Any(g => CommonAreaReservationService.PeakLoad(g.Select(r => (r.StartsAt, r.EndsAt, r.PartySize)).ToArray(), TimeOnly.MinValue, TimeOnly.MaxValue) > next.Capacity))
            Conflict();
    }
}
