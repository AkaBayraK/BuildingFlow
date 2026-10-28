using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed partial class ParkingStore
{
    private IQueryable<ParkingAllocationResponse> AllocationRows(IQueryable<ParkingAllocation> rows) =>
        from a in rows join s in db.ParkingSpaces on a.SpaceId equals s.Id join u in db.Units on a.UnitId equals u.Id
        select new ParkingAllocationResponse(a.Id, a.SpaceId, s.Code, a.UnitId, u.Number, a.VehicleId,
            db.Vehicles.Where(v => v.Id == a.VehicleId && v.PersonalDataRedactedAt == null).Select(v => v.Plate).FirstOrDefault(), a.StartDate, a.EndDate, a.Version);

    private IQueryable<GuestVehicleResponse> GuestRows(IQueryable<GuestVehicle> rows) =>
        from g in rows.Where(g => g.PersonalDataRedactedAt == null) join u in db.Units on g.HostUnitId equals u.Id
        select new GuestVehicleResponse(g.Id, g.HostUnitId, u.Number, g.SpaceId, db.ParkingSpaces.Where(s => s.Id == g.SpaceId).Select(s => s.Code).FirstOrDefault(),
            g.Plate, g.GuestName, g.Note, g.StartsAt, g.EndsAt, g.Status.ToString(), g.ArrivedAt, g.DepartedAt, g.Version);

    public async Task<ParkingPage<ParkingAllocationResponse>?> Allocations(Guid property, ParkingAllocationQuery q, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Visible(property, access, token)) return null;
        var rows = db.ParkingAllocations.AsNoTracking().Where(a => a.PropertyId == property && a.IsEnabled);
        if (q.UnitId is Guid unit) rows = rows.Where(a => a.UnitId == unit);
        if (q.SpaceId is Guid space) rows = rows.Where(a => a.SpaceId == space);
        if (q.ActiveOnly) { var today = Today; rows = rows.Where(a => a.EndDate == null || a.EndDate >= today); }
        var total = await rows.CountAsync(token);
        var items = await AllocationRows(rows.OrderByDescending(a => a.StartDate).ThenBy(a => a.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)).ToListAsync(token);
        await tx.CommitAsync(token); return new(items, q.Page, q.PageSize, total);
    }

    public async Task<ParkingAllocationResponse?> Allocate(Guid property, ParkingAllocationRequest r, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var space = await db.ParkingSpaces.SingleOrDefaultAsync(s => s.PropertyId == property && s.Id == r.SpaceId && s.IsEnabled, token);
        if (space is null || !await db.Units.AnyAsync(u => u.Id == r.UnitId && u.PropertyId == property && u.IsEnabled, token)) return null;
        if (!space.IsAllocatable) throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        if (await ClosureOverlap(property, space.Id, IstanbulMidnight(r.StartDate), r.EndDate is null ? DateTimeOffset.MaxValue : IstanbulMidnight(r.EndDate.Value.AddDays(1)), token))
            throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        if (r.VehicleId is Guid vehicle && !await db.Vehicles.AnyAsync(v => v.PropertyId == property && v.Id == vehicle && v.UnitId == r.UnitId && v.IsEnabled && v.Status == VehicleStatus.APPROVED, token))
            throw Conflict(ErrorCatalog.ParkingStateInvalid);
        var policy = await CurrentPolicy(property, token) ?? throw Conflict(ErrorCatalog.ParkingPolicyRequired);
        var allocations = await db.ParkingAllocations.Where(a => a.PropertyId == property && a.IsEnabled && (a.SpaceId == r.SpaceId || a.UnitId == r.UnitId)).ToListAsync(token);
        if (allocations.Any(a => a.SpaceId == r.SpaceId && a.Overlaps(r.StartDate, r.EndDate))) throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        // Quota is the peak simultaneous use, not the count of all reservations touching a long request.
        var unitAllocations = allocations.Where(a => a.UnitId == r.UnitId && a.Overlaps(r.StartDate, r.EndDate)).ToArray();
        var boundaries = unitAllocations.Select(a => a.StartDate < r.StartDate ? r.StartDate : a.StartDate).Append(r.StartDate).Distinct();
        if (boundaries.Any(day => !policy.AllowsSpace(unitAllocations.Count(a => a.Contains(day))))) throw Conflict(ErrorCatalog.ParkingQuotaExceeded);
        var value = new ParkingAllocation(property, space, r.UnitId, r.VehicleId, r.StartDate, r.EndDate); db.Add(value);
        await Commit(parent, tx, token);
        return await AllocationRows(db.ParkingAllocations.AsNoTracking().Where(a => a.Id == value.Id)).SingleAsync(token);
    }

    public async Task<ParkingAllocationResponse?> EndAllocation(Guid property, Guid id, ParkingAllocationEndRequest r, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var value = await db.ParkingAllocations.SingleOrDefaultAsync(a => a.PropertyId == property && a.Id == id && a.IsEnabled, token); if (value is null) return null;
        Version(r.Version, value.Version);
        if (r.EndDate < value.StartDate) throw new RequestValidationException(new() { ["endDate"] = [ErrorCatalog.ParkingInvalid.Create()] });
        Transition(() => value.End(r.EndDate));
        await Commit(parent, tx, token);
        return await AllocationRows(db.ParkingAllocations.AsNoTracking().Where(a => a.Id == value.Id)).SingleAsync(token);
    }

    public async Task<ParkingPage<GuestVehicleResponse>?> Guests(Guid property, GuestVehicleQuery q, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Visible(property, access, token)) return null;
        var rows = db.GuestVehicles.AsNoTracking().Where(g => g.PropertyId == property && g.IsEnabled && g.PersonalDataRedactedAt == null);
        if (q.From is DateTimeOffset from) rows = rows.Where(g => g.EndsAt > from);
        if (q.To is DateTimeOffset to) rows = rows.Where(g => g.StartsAt < to);
        if (q.Status is not null) { var status = Enum.Parse<GuestVehicleStatus>(q.Status); rows = rows.Where(g => g.Status == status); }
        var total = await rows.CountAsync(token);
        var items = await GuestRows(rows.OrderBy(g => g.StartsAt).ThenBy(g => g.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)).ToListAsync(token);
        await tx.CommitAsync(token); return new(items, q.Page, q.PageSize, total);
    }

    public async Task<GuestVehicleResponse?> CreateGuest(Guid property, GuestVehicleRequest r, AccessScope access, CancellationToken token, MobileParkingContext? mobile = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (mobile is not null) await LockMobileTenant(mobile, token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        if (mobile is not null) { var authorized = await AuthorizeMobile(mobile, token); if (!authorized.Manager || mobile.PropertyId != property) throw new UnauthorizedAccessException(); }
        var plate = VehiclePlate.Normalize(r.Plate!);
        var existing = await db.GuestVehicles.SingleOrDefaultAsync(g => g.PropertyId == property && g.RequestId == r.RequestId, token);
        if (existing is not null)
        {
            if (existing.PersonalDataRedactedAt is not null || existing.HostUnitId != r.HostUnitId || existing.Plate != plate || existing.StartsAt != r.StartsAt || existing.EndsAt != r.EndsAt || existing.SpaceId != r.SpaceId || existing.GuestName != Clean(r.GuestName) || existing.Note != Clean(r.Note))
                throw Conflict(ErrorCatalog.ParkingRequestReused);
            await tx.CommitAsync(token); return await GuestRows(db.GuestVehicles.AsNoTracking().Where(g => g.Id == existing.Id)).SingleAsync(token);
        }
        if (!await db.Units.AnyAsync(u => u.Id == r.HostUnitId && u.PropertyId == property && u.IsEnabled, token)) return null;
        var policy = await CurrentPolicy(property, token) ?? throw Conflict(ErrorCatalog.ParkingPolicyRequired);
        if (r.EndsAt <= clock.GetUtcNow()) throw Conflict(ErrorCatalog.ParkingStateInvalid);
        if (!policy.AllowsGuestWindow(r.StartsAt, r.EndsAt)) throw Conflict(ErrorCatalog.ParkingQuotaExceeded);
        if (await db.GuestVehicles.AnyAsync(g => g.PropertyId == property && g.Plate == plate && g.IsEnabled &&
            (g.Status == GuestVehicleStatus.ARRIVED || (g.Status == GuestVehicleStatus.PLANNED && g.StartsAt < r.EndsAt && r.StartsAt < g.EndsAt)), token))
            throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        if (r.SpaceId is Guid spaceId) await EnsureGuestSpace(property, spaceId, r.StartsAt, r.EndsAt, token);
        var value = new GuestVehicle(policy, r.HostUnitId, plate, r.GuestName, r.StartsAt, r.EndsAt, r.SpaceId, r.Note, r.RequestId); db.Add(value);
        await Commit(parent, tx, token);
        return await GuestRows(db.GuestVehicles.AsNoTracking().Where(g => g.Id == value.Id)).SingleAsync(token);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task EnsureGuestSpace(Guid property, Guid spaceId, DateTimeOffset start, DateTimeOffset end, CancellationToken token)
    {
        var space = await db.ParkingSpaces.SingleOrDefaultAsync(s => s.PropertyId == property && s.Id == spaceId && s.IsEnabled, token);
        if (space is null || space.Type != ParkingSpaceType.VISITOR) throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        if (await ClosureOverlap(property, spaceId, start, end, token)) throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
        var taken = await db.GuestVehicles.AnyAsync(g => g.PropertyId == property && g.SpaceId == spaceId && g.IsEnabled &&
            (g.Status == GuestVehicleStatus.ARRIVED || (g.Status == GuestVehicleStatus.PLANNED && g.StartsAt < end && start < g.EndsAt)), token);
        if (taken) throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
    }

    public async Task<GuestVehicleResponse?> ChangeGuest(Guid property, Guid id, GuestAction action, long version, AccessScope access, CancellationToken token, MobileParkingContext? mobile = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (mobile is not null) await LockMobileTenant(mobile, token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        if (mobile is not null) { var authorized = await AuthorizeMobile(mobile, token); if (!authorized.Manager || mobile.PropertyId != property) throw new UnauthorizedAccessException(); }
        var value = await db.GuestVehicles.SingleOrDefaultAsync(g => g.PropertyId == property && g.Id == id && g.IsEnabled, token); if (value is null) return null;
        Version(version, value.Version);
        var now = clock.GetUtcNow();
        if (action == GuestAction.ARRIVE)
        {
            if (value.Status != GuestVehicleStatus.PLANNED || now < value.StartsAt || now >= value.EndsAt) throw Conflict(ErrorCatalog.ParkingStateInvalid);
            if (await db.GuestVehicles.AnyAsync(g => g.PropertyId == property && g.Id != id && g.Plate == value.Plate && g.IsEnabled && g.Status == GuestVehicleStatus.ARRIVED, token))
                throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
            if (value.SpaceId is Guid space)
            {
                if (await ClosureOverlap(property, space, now, value.EndsAt, token) ||
                    await db.GuestVehicles.AnyAsync(g => g.PropertyId == property && g.Id != id && g.SpaceId == space && g.IsEnabled && g.Status == GuestVehicleStatus.ARRIVED, token))
                    throw Conflict(ErrorCatalog.ParkingSpaceOccupied);
            }
        }
        Transition(() => { switch (action) { case GuestAction.ARRIVE: value.MarkArrived(now); break; case GuestAction.DEPART: value.MarkDeparted(now); break; default: value.Cancel(now); break; } });
        await Commit(parent, tx, token);
        return await GuestRows(db.GuestVehicles.AsNoTracking().Where(g => g.Id == value.Id)).SingleAsync(token);
    }
}
