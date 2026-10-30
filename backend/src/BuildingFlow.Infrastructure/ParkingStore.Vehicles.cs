using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed partial class ParkingStore
{
    private static ResidentVehicleResponse ResidentResponse(Vehicle x) =>
        new(x.Id, x.UnitId, x.Plate, x.Type.ToString(), x.Brand, x.Model, x.Color, x.Status.ToString(), x.ReviewReason, x.Version);

    private async Task<VehicleResponse> VehicleResponse(Vehicle x, CancellationToken token)
    {
        var person = await db.People.Where(p => p.Id == x.PersonId).Select(p => p.FirstName + " " + p.LastName).SingleAsync(token);
        var unit = await db.Units.Where(u => u.Id == x.UnitId).Select(u => u.Number).SingleAsync(token);
        return new(x.Id, x.PersonId, person, x.UnitId, unit, x.Plate, x.Type.ToString(), x.Brand, x.Model, x.Color, x.Status.ToString(), x.ReviewedAt, x.ReviewReason, x.Version, x.CreatedAt);
    }

    public async Task<ParkingPage<VehicleResponse>?> Vehicles(Guid property, VehicleQuery q, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Visible(property, access, token)) return null;
        var rows = db.Vehicles.AsNoTracking().Where(x => x.PropertyId == property && x.IsEnabled && x.PersonalDataRedactedAt == null);
        if (q.Status is not null) { var status = Enum.Parse<VehicleStatus>(q.Status); rows = rows.Where(x => x.Status == status); }
        if (q.UnitId is Guid unit) rows = rows.Where(x => x.UnitId == unit);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var plate = new string(q.Search.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            var pattern = "%" + q.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            rows = rows.Where(x => (plate.Length > 0 && x.Plate.Contains(plate)) || db.People.Any(p => p.Id == x.PersonId &&
                (EF.Functions.ILike(p.FirstName, pattern, "\\") || EF.Functions.ILike(p.LastName, pattern, "\\"))));
        }
        var total = await rows.CountAsync(token);
        var items = await (from v in rows join p in db.People on v.PersonId equals p.Id join u in db.Units on v.UnitId equals u.Id
                           orderby v.Status, v.Plate, v.Id
                           select new VehicleResponse(v.Id, v.PersonId, p.FirstName + " " + p.LastName, v.UnitId, u.Number, v.Plate, v.Type.ToString(), v.Brand, v.Model, v.Color,
                               v.Status.ToString(), v.ReviewedAt, v.ReviewReason, v.Version, v.CreatedAt)).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(token);
        await tx.CommitAsync(token); return new(items, q.Page, q.PageSize, total);
    }

    public async Task<VehicleResponse?> CreateVehicle(Guid property, VehicleRequest r, VehicleType type, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var plate = VehiclePlate.Normalize(r.Plate!);
        var replay = await Replay(property, r.RequestId, v => v.PersonId == r.PersonId && v.UnitId == r.UnitId && v.Plate == plate && v.Type == type && v.Brand == Clean(r.Brand) && v.Model == Clean(r.Model) && v.Color == Clean(r.Color), token);
        if (replay is not null) { await tx.CommitAsync(token); return await VehicleResponse(replay, token); }
        var value = await NewVehicle(property, r.PersonId, r.UnitId, plate, type, r.Brand, r.Model, r.Color, r.RequestId, token);
        Transition(() => value.Approve(clock.GetUtcNow()));
        await Commit(parent, tx, token); return await VehicleResponse(value, token);
    }

    private async Task<Vehicle> NewVehicle(Guid property, Guid person, Guid unit, string plate, VehicleType type, string? brand, string? model, string? color, Guid requestId, CancellationToken token)
    {
        if (!await HasRelation(property, unit, person, token)) throw Conflict(ErrorCatalog.ParkingRelationRequired);
        await DefinitionPolicy.EnsureSelectableAsync(db, property, "VEHICLE_TYPE", type.ToString(), null, "type", token);
        var policy = await CurrentPolicy(property, token) ?? throw Conflict(ErrorCatalog.ParkingPolicyRequired);
        await EnsureVehicleQuota(policy, unit, person, token);
        if (await db.Vehicles.AnyAsync(v => v.PropertyId == property && v.Plate == plate && (v.Status == VehicleStatus.PENDING || v.Status == VehicleStatus.APPROVED), token))
            throw Conflict(ErrorCatalog.ParkingPlateTaken);
        var value = new Vehicle(property, person, unit, plate, type, brand, model, color, requestId); db.Add(value);
        return value;
    }

    public async Task<VehicleResponse?> EditVehicle(Guid property, Guid id, VehicleEditRequest r, VehicleType type, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var value = await db.Vehicles.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id && x.IsEnabled, token); if (value is null) return null;
        Version(r.Version, value.Version);
        await DefinitionPolicy.EnsureSelectableAsync(db, property, "VEHICLE_TYPE", type.ToString(), value.Type.ToString(), "type", token);
        Transition(() => value.Edit(type, r.Brand, r.Model, r.Color));
        await Commit(parent, tx, token); return await VehicleResponse(value, token);
    }

    public async Task<VehicleResponse?> ReviewVehicle(Guid property, Guid id, VehicleAction action, VehicleReviewRequest r, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var value = await db.Vehicles.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id && x.IsEnabled, token); if (value is null) return null;
        Version(r.Version, value.Version);
        var now = clock.GetUtcNow();
        if (action == VehicleAction.APPROVE && value.Status == VehicleStatus.PENDING)
        {
            var policy = await CurrentPolicy(property, token) ?? throw Conflict(ErrorCatalog.ParkingPolicyRequired);
            var approved = db.Vehicles.Where(v => v.PropertyId == property && v.IsEnabled && v.PersonalDataRedactedAt == null && v.Status == VehicleStatus.APPROVED);
            if (!policy.AllowsVehicle(await approved.CountAsync(v => v.UnitId == value.UnitId, token), await approved.CountAsync(v => v.PersonId == value.PersonId, token)))
                throw Conflict(ErrorCatalog.ParkingQuotaExceeded);
        }
        if (action == VehicleAction.DEACTIVATE) await EndVehicleAllocations(value.Id, token);
        Transition(() => { switch (action) { case VehicleAction.APPROVE: value.Approve(now); break; case VehicleAction.REJECT: value.Reject(now, r.Reason!); break; default: value.Deactivate(now); break; } });
        await Commit(parent, tx, token); return await VehicleResponse(value, token);
    }

    private async Task EndVehicleAllocations(Guid vehicle, CancellationToken token)
    {
        var today = Today;
        foreach (var allocation in await db.ParkingAllocations.Where(a => a.VehicleId == vehicle && a.IsEnabled && (a.EndDate == null || a.EndDate >= today)).ToListAsync(token))
            if (allocation.StartDate > today) db.Remove(allocation); else allocation.End(today);
    }

    public async Task<IReadOnlyList<PlateResponse>?> Plates(Guid property, string search, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        return await (from v in db.Vehicles.AsNoTracking() join u in db.Units on v.UnitId equals u.Id
                      where v.PropertyId == property && v.IsEnabled && v.PersonalDataRedactedAt == null && v.Status == VehicleStatus.APPROVED && v.Plate.Contains(search)
                      orderby v.Plate
                      select new PlateResponse(v.Plate, u.Number, db.Buildings.Where(b => b.Id == u.BuildingId).Select(b => b.Name).FirstOrDefault(), v.Type.ToString()))
            .Take(ParkingService.MaxSearch).ToListAsync(token);
    }

    public async Task<IReadOnlyList<ParkingUnitOption>?> Units(Guid property, string? search, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token)) return null;
        var rows = db.Units.AsNoTracking().Where(u => u.PropertyId == property && u.IsEnabled);
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            rows = rows.Where(u => EF.Functions.ILike(u.Number, pattern, "\\") || db.Buildings.Any(b => b.Id == u.BuildingId && EF.Functions.ILike(b.Name, pattern, "\\")));
        }
        return await rows.OrderBy(u => u.BuildingId).ThenBy(u => u.Number).Take(ParkingService.MaxSearch)
            .Select(u => new ParkingUnitOption(u.Id, u.Number, db.Buildings.Where(b => b.Id == u.BuildingId).Select(b => b.Name).FirstOrDefault())).ToListAsync(token);
    }

    public async Task<IReadOnlyList<ParkingResidentOption>?> Residents(Guid property, Guid unit, AccessScope access, CancellationToken token)
    {
        if (!await Visible(property, access, token) || !await db.Units.AnyAsync(u => u.Id == unit && u.PropertyId == property && u.IsEnabled, token)) return null;
        var today = Today;
        var rows = await (from o in db.UnitOccupancies.AsNoTracking() join p in db.People on o.PersonId equals p.Id
                          where o.PropertyId == property && o.UnitId == unit && o.IsEnabled && p.IsEnabled && p.PropertyId == property && ResidentTypes.Contains(o.Type) &&
                                o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
                          orderby p.FirstName, p.LastName, o.Type
                          select new { p.Id, Name = p.FirstName + " " + p.LastName, o.Type }).Take(ParkingService.MaxSearch).ToListAsync(token);
        return rows.Select(r => new ParkingResidentOption(r.Id, r.Name, r.Type.ToString())).ToArray();
    }

    private Task<Guid> ResidentPerson(Guid property, Guid unit, string userId, CancellationToken token)
    {
        var today = Today;
        return (from o in db.UnitOccupancies join p in db.People on o.PersonId equals p.Id
                where o.PropertyId == property && o.UnitId == unit && o.IsEnabled && ResidentTypes.Contains(o.Type) && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today) &&
                      p.PropertyId == property && p.LinkedUserId == userId && p.IsEnabled &&
                      db.Properties.Any(x => x.Id == property && x.IsEnabled) && db.Units.Any(u => u.Id == unit && u.PropertyId == property && u.IsEnabled)
                orderby p.Id select p.Id).FirstOrDefaultAsync(token);
    }

    public async Task<IReadOnlyList<ResidentVehicleResponse>> MyVehicles(Guid property, Guid unit, string userId, CancellationToken token)
    {
        var person = await ResidentPerson(property, unit, userId, token);
        if (person == Guid.Empty) throw new UnauthorizedAccessException();
        var rows = await db.Vehicles.AsNoTracking().Where(v => v.PropertyId == property && v.UnitId == unit && v.PersonId == person && v.IsEnabled && v.PersonalDataRedactedAt == null)
            .OrderBy(v => v.Status).ThenBy(v => v.Plate).Take(ParkingService.MaxSearch).ToListAsync(token);
        return rows.Select(ResidentResponse).ToArray();
    }

    public async Task<ResidentVehicleResponse> RegisterMyVehicle(ResidentVehicleRequest r, VehicleType type, string userId, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var person = await ResidentPerson(r.PropertyId, r.UnitId, userId, token);
        if (person == Guid.Empty) throw new UnauthorizedAccessException();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {r.PropertyId} FOR UPDATE", token);
        var parent = await db.Properties.SingleAsync(p => p.Id == r.PropertyId, token);
        var plate = VehiclePlate.Normalize(r.Plate!);
        var replay = await Replay(r.PropertyId, r.RequestId, v => v.PersonId == person && v.UnitId == r.UnitId && v.Plate == plate && v.Type == type && v.Brand == Clean(r.Brand) && v.Model == Clean(r.Model) && v.Color == Clean(r.Color), token);
        if (replay is not null) { await tx.CommitAsync(token); return ResidentResponse(replay); }
        var value = await NewVehicle(r.PropertyId, person, r.UnitId, plate, type, r.Brand, r.Model, r.Color, r.RequestId, token);
        await Commit(parent, tx, token); return ResidentResponse(value);
    }

    public async Task<ResidentVehicleResponse?> DeactivateMyVehicle(Guid property, Guid id, long version, string userId, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var value = await db.Vehicles.SingleOrDefaultAsync(v => v.PropertyId == property && v.Id == id && v.IsEnabled, token);
        if (value is null || await ResidentPerson(property, value.UnitId, userId, token) != value.PersonId) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var parent = await db.Properties.SingleAsync(p => p.Id == property, token);
        Version(version, value.Version);
        await EndVehicleAllocations(value.Id, token);
        Transition(() => value.Deactivate(clock.GetUtcNow()));
        await Commit(parent, tx, token); return ResidentResponse(value);
    }
}
