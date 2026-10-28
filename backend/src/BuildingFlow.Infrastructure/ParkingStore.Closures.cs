using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed partial class ParkingStore
{
    private IQueryable<ParkingClosureResponse> ClosureRows(IQueryable<ParkingClosure> rows) =>
        from c in rows join s in db.ParkingSpaces on c.SpaceId equals s.Id
        select new ParkingClosureResponse(c.Id, c.SpaceId, s.Code, c.StartsAt, c.EndsAt, c.Reason, c.CancelledAt, c.CancellationReason, c.Version);

    public async Task<ParkingPage<ParkingClosureResponse>?> Closures(Guid property, int page, int pageSize, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Visible(property, access, token)) return null;
        var rows = db.ParkingClosures.AsNoTracking().Where(c => c.PropertyId == property && c.IsEnabled);
        var count = await rows.CountAsync(token);
        var items = await ClosureRows(rows.OrderByDescending(c => c.StartsAt).ThenBy(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, count);
    }

    private static DateTimeOffset IstanbulMidnight(DateOnly date) => new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime();
    private Task<bool> ClosureOverlap(Guid property, Guid space, DateTimeOffset start, DateTimeOffset end, CancellationToken token) =>
        db.ParkingClosures.AnyAsync(c => c.PropertyId == property && c.SpaceId == space && c.IsEnabled && c.CancelledAt == null && c.StartsAt < end && start < c.EndsAt, token);

    public async Task<ParkingClosureResponse?> CreateClosure(Guid property, ParkingClosureRequest r, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var previous = await db.ParkingClosures.SingleOrDefaultAsync(c => c.PropertyId == property && c.RequestId == r.RequestId, token);
        if (previous is not null)
        {
            if (previous.SpaceId != r.SpaceId || previous.StartsAt != r.StartsAt || previous.EndsAt != r.EndsAt || previous.Reason != r.Reason!.Trim())
                throw Conflict(ErrorCatalog.ParkingRequestReused);
            await tx.CommitAsync(token); return await ClosureRows(db.ParkingClosures.Where(c => c.Id == previous.Id)).SingleAsync(token);
        }
        if (!await db.ParkingSpaces.AnyAsync(s => s.PropertyId == property && s.Id == r.SpaceId && s.IsEnabled &&
            db.ParkingFacilities.Any(f => f.Id == s.FacilityId && f.PropertyId == property && f.IsEnabled), token)) return null;
        if (r.EndsAt <= clock.GetUtcNow()) throw Conflict(ErrorCatalog.ParkingStateInvalid);
        if (await ClosureOverlap(property, r.SpaceId, r.StartsAt, r.EndsAt, token)) throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        var allocations = await db.ParkingAllocations.Where(a => a.PropertyId == property && a.SpaceId == r.SpaceId && a.IsEnabled).ToListAsync(token);
        if (allocations.Any(a => IstanbulMidnight(a.StartDate) < r.EndsAt && (a.EndDate == null || r.StartsAt < IstanbulMidnight(a.EndDate.Value.AddDays(1)))))
            throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        if (await db.GuestVehicles.AnyAsync(g => g.PropertyId == property && g.SpaceId == r.SpaceId && g.IsEnabled &&
            (g.Status == GuestVehicleStatus.ARRIVED || (g.Status == GuestVehicleStatus.PLANNED && g.StartsAt < r.EndsAt && r.StartsAt < g.EndsAt)), token))
            throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        var value = new ParkingClosure(property, r.SpaceId, r.RequestId, r.StartsAt, r.EndsAt, r.Reason!); db.Add(value);
        await Commit(parent, tx, token); return await ClosureRows(db.ParkingClosures.Where(c => c.Id == value.Id)).SingleAsync(token);
    }

    public async Task<ParkingClosureResponse?> CancelClosure(Guid property, Guid id, ParkingClosureCancelRequest r, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var value = await db.ParkingClosures.SingleOrDefaultAsync(c => c.PropertyId == property && c.Id == id && c.IsEnabled, token); if (value is null) return null;
        Version(r.Version, value.Version); Transition(() => value.Cancel(clock.GetUtcNow(), r.Reason!));
        await Commit(parent, tx, token); return await ClosureRows(db.ParkingClosures.Where(c => c.Id == value.Id)).SingleAsync(token);
    }
}
