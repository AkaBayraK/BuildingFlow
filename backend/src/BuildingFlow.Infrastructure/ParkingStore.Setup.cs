using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed partial class ParkingStore
{
    private static ParkingPolicyResponse PolicyResponse(Guid property, ParkingPolicy? x) => x is null
        ? new(property, false, 0, 0, 0, 0, ParkingPolicy.DefaultRetentionDays, 0)
        : new(property, true, x.MaxVehiclesPerUnit, x.MaxVehiclesPerPerson, x.MaxSpacesPerUnit, x.GuestMaxHours, x.RetentionDays, x.Version);
    private static ParkingLevelResponse LevelResponse(ParkingLevel x) => new(x.Id, x.FacilityId, x.Name, x.SortOrder, x.Version);

    public async Task<ParkingPolicyResponse?> Policy(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        return PolicyResponse(property, await db.ParkingPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property && x.IsEnabled, token));
    }

    public async Task<ParkingPolicyResponse?> SavePolicy(Guid property, ParkingPolicyRequest r, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var policy = await CurrentPolicy(property, token);
        Version(r.Version, policy?.Version ?? 0);
        if (policy is null) { policy = new(property, r.MaxVehiclesPerUnit, r.MaxVehiclesPerPerson, r.MaxSpacesPerUnit, r.GuestMaxHours, r.RetentionDays); db.Add(policy); }
        else policy.Edit(r.MaxVehiclesPerUnit, r.MaxVehiclesPerPerson, r.MaxSpacesPerUnit, r.GuestMaxHours, r.RetentionDays);
        await Commit(parent, tx, token); return PolicyResponse(property, policy);
    }

    public async Task<IReadOnlyList<ParkingFacilityResponse>?> Facilities(Guid property, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Visible(property, access, token)) return null;
        var facilities = await db.ParkingFacilities.AsNoTracking().Where(x => x.PropertyId == property && x.IsEnabled).OrderBy(x => x.Name).Take(ParkingService.MaxPageSize).ToListAsync(token);
        var ids = facilities.Select(x => x.Id).ToArray();
        var levels = await db.ParkingLevels.AsNoTracking().Where(x => x.PropertyId == property && ids.Contains(x.FacilityId) && x.IsEnabled).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(token);
        var counts = await db.ParkingSpaces.Where(x => x.PropertyId == property && x.IsEnabled && ids.Contains(x.FacilityId)).GroupBy(x => x.FacilityId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, token);
        var buildingIds = facilities.Where(x => x.BuildingId != null).Select(x => x.BuildingId!.Value).ToArray();
        var buildings = await db.Buildings.Where(b => b.PropertyId == property && buildingIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name, token);
        await tx.CommitAsync(token);
        return facilities.Select(f => new ParkingFacilityResponse(f.Id, f.BuildingId, f.BuildingId is Guid b ? buildings.GetValueOrDefault(b) : null, f.Name,
            counts.GetValueOrDefault(f.Id), levels.Where(l => l.FacilityId == f.Id).Select(LevelResponse).ToArray(), f.Version)).ToArray();
    }

    public async Task<ParkingFacilityResponse?> SaveFacility(Guid property, Guid? id, ParkingFacilityRequest r, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        string? buildingName = null;
        if (r.BuildingId is Guid building)
        {
            buildingName = await db.Buildings.Where(b => b.Id == building && b.PropertyId == property && b.IsEnabled).Select(b => b.Name).SingleOrDefaultAsync(token);
            if (buildingName is null) return null;
        }
        ParkingFacility value;
        if (id is Guid existing)
        {
            var current = await db.ParkingFacilities.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == existing && x.IsEnabled, token);
            if (current is null) return null; Version(r.Version, current.Version);
            current.Edit(r.BuildingId, r.Name!); value = current;
        }
        else { value = new(property, r.BuildingId, r.Name!); db.Add(value); }
        await Commit(parent, tx, token);
        var spaces = await db.ParkingSpaces.CountAsync(x => x.FacilityId == value.Id && x.IsEnabled, token);
        var levels = await db.ParkingLevels.AsNoTracking().Where(x => x.FacilityId == value.Id && x.IsEnabled).OrderBy(x => x.SortOrder).Select(x => LevelResponse(x)).ToListAsync(token);
        return new(value.Id, value.BuildingId, buildingName, value.Name, spaces, levels, value.Version);
    }

    public async Task<bool> DeleteFacility(Guid property, Guid id, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return false;
        var value = await db.ParkingFacilities.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id && x.IsEnabled, token); if (value is null) return false;
        Version(version, value.Version);
        if (await db.ParkingSpaces.AnyAsync(x => x.FacilityId == id, token) || await db.ParkingLevels.AnyAsync(x => x.FacilityId == id, token)) throw Conflict(ErrorCatalog.ParkingInUse);
        db.Remove(value); await Commit(parent, tx, token); return true;
    }

    public async Task<ParkingLevelResponse?> SaveLevel(Guid property, Guid? id, ParkingLevelRequest r, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        ParkingLevel value;
        if (id is Guid existing)
        {
            var current = await db.ParkingLevels.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == existing && x.IsEnabled, token);
            if (current is null || current.FacilityId != r.FacilityId) return null; Version(r.Version, current.Version);
            current.Edit(r.Name!, r.SortOrder); value = current;
        }
        else
        {
            if (!await db.ParkingFacilities.AnyAsync(x => x.PropertyId == property && x.Id == r.FacilityId && x.IsEnabled, token)) return null;
            value = new(property, r.FacilityId, r.Name!, r.SortOrder); db.Add(value);
        }
        await Commit(parent, tx, token); return LevelResponse(value);
    }

    public async Task<bool> DeleteLevel(Guid property, Guid id, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return false;
        var value = await db.ParkingLevels.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id && x.IsEnabled, token); if (value is null) return false;
        Version(version, value.Version);
        if (await db.ParkingSpaces.AnyAsync(x => x.LevelId == id, token)) throw Conflict(ErrorCatalog.ParkingInUse);
        db.Remove(value); await Commit(parent, tx, token); return true;
    }

    public async Task<ParkingPage<ParkingSpaceResponse>?> Spaces(Guid property, ParkingSpaceQuery q, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Visible(property, access, token)) return null;
        var rows = db.ParkingSpaces.AsNoTracking().Where(x => x.PropertyId == property && x.IsEnabled);
        if (q.FacilityId is Guid facility) rows = rows.Where(x => x.FacilityId == facility);
        if (q.LevelId is Guid level) rows = rows.Where(x => x.LevelId == level);
        if (q.Type is not null) { var type = Enum.Parse<ParkingSpaceType>(q.Type); rows = rows.Where(x => x.Type == type); }
        var total = await rows.CountAsync(token);
        var page = await rows.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(token);
        var items = await SpaceResponses(property, page, token);
        await tx.CommitAsync(token); return new(items, q.Page, q.PageSize, total);
    }

    private async Task<IReadOnlyList<ParkingSpaceResponse>> SpaceResponses(Guid property, IReadOnlyList<ParkingSpace> spaces, CancellationToken token)
    {
        var ids = spaces.Select(x => x.Id).ToArray(); var today = Today;
        var current = await (from a in db.ParkingAllocations where a.PropertyId == property && ids.Contains(a.SpaceId) && a.IsEnabled && a.StartDate <= today && (a.EndDate == null || a.EndDate >= today)
                             join u in db.Units on a.UnitId equals u.Id select new { a.SpaceId, a.UnitId, u.Number }).ToListAsync(token);
        var now = clock.GetUtcNow();
        var closed = await db.ParkingClosures.Where(c => c.PropertyId == property && ids.Contains(c.SpaceId) && c.IsEnabled && c.CancelledAt == null && c.StartsAt <= now && now < c.EndsAt).Select(c => c.SpaceId).ToListAsync(token);
        return spaces.Select(s => { var a = current.FirstOrDefault(x => x.SpaceId == s.Id); return new ParkingSpaceResponse(s.Id, s.FacilityId, s.LevelId, s.Code, s.Type.ToString(), a?.UnitId, a?.Number, s.Version, closed.Contains(s.Id)); }).ToArray();
    }

    public async Task<ParkingSpaceResponse?> SaveSpace(Guid property, Guid? id, ParkingSpaceRequest r, ParkingSpaceType type, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        if (!await db.ParkingFacilities.AnyAsync(x => x.PropertyId == property && x.Id == r.FacilityId && x.IsEnabled, token)) return null;
        if (r.LevelId is Guid level && !await db.ParkingLevels.AnyAsync(x => x.FacilityId == r.FacilityId && x.Id == level && x.IsEnabled, token)) return null;
        ParkingSpace value;
        if (id is Guid existing)
        {
            var current = await db.ParkingSpaces.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == existing && x.IsEnabled, token);
            if (current is null || current.FacilityId != r.FacilityId) return null; Version(r.Version, current.Version);
            if (type == ParkingSpaceType.VISITOR && current.Type != type && await OpenAllocations(existing).AnyAsync(token)) throw Conflict(ErrorCatalog.ParkingInUse);
            if (type != current.Type && await db.GuestVehicles.AnyAsync(g => g.PropertyId == property && g.SpaceId == existing && g.IsEnabled &&
                (g.Status == GuestVehicleStatus.ARRIVED || (g.Status == GuestVehicleStatus.PLANNED && g.EndsAt > clock.GetUtcNow())), token)) throw Conflict(ErrorCatalog.ParkingInUse);
            current.Edit(r.LevelId, r.Code!, type); value = current;
        }
        else { value = new(property, r.FacilityId, r.LevelId, r.Code!, type); db.Add(value); }
        await Commit(parent, tx, token); return (await SpaceResponses(property, [value], token))[0];
    }

    private IQueryable<ParkingAllocation> OpenAllocations(Guid space)
    {
        var today = Today;
        return db.ParkingAllocations.Where(a => a.SpaceId == space && a.IsEnabled && (a.EndDate == null || a.EndDate >= today));
    }

    public async Task<bool> DeleteSpace(Guid property, Guid id, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return false;
        var value = await db.ParkingSpaces.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id && x.IsEnabled, token); if (value is null) return false;
        Version(version, value.Version);
        var now = clock.GetUtcNow();
        if (await OpenAllocations(id).AnyAsync(token) || await db.ParkingClosures.AnyAsync(c => c.PropertyId == property && c.SpaceId == id, token) ||
            await db.GuestVehicles.AnyAsync(g => g.PropertyId == property && g.SpaceId == id &&
            (g.Status == GuestVehicleStatus.ARRIVED || (g.Status == GuestVehicleStatus.PLANNED && g.EndsAt > now)), token)) throw Conflict(ErrorCatalog.ParkingInUse);
        db.Remove(value); await Commit(parent, tx, token); return true;
    }
}
