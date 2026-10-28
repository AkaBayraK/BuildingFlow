using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

public sealed partial class ParkingStore(BuildingFlowDbContext db, TimeProvider clock, ICurrentUserService actor, IMobileSessionStore sessions, ISessionContextStore contexts, IConfiguration config) : IParkingStore, IParkingOperations
{
    private static readonly Dictionary<string, ErrorDefinition> UniqueErrors = new()
    {
        ["IX_vehicles_property_id_plate"] = ErrorCatalog.ParkingPlateTaken,
        ["IX_parking_allocations_space_id"] = ErrorCatalog.ParkingSpaceOccupied,
        ["IX_parking_facilities_property_id_name"] = ErrorCatalog.ParkingNameTaken,
        ["IX_parking_levels_facility_id_name"] = ErrorCatalog.ParkingNameTaken,
        ["IX_parking_spaces_facility_id_code"] = ErrorCatalog.ParkingNameTaken,
        ["IX_vehicles_property_id_request_id"] = ErrorCatalog.ParkingRequestReused,
        ["IX_guest_vehicles_property_id_request_id"] = ErrorCatalog.ParkingRequestReused,
    };
    private static readonly OccupancyType[] ResidentTypes = [OccupancyType.OWNER, OccupancyType.TENANT, OccupancyType.RESIDENT];

    private DateOnly Today => BusinessDate.Today(clock);
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());
    private static void Version(long expected, long actual) { if (expected != actual) throw Conflict(ErrorCatalog.RecordChanged); }
    private IQueryable<Property> Properties(AccessScope scope) => db.Properties.Where(p => !p.IsDeleted &&
        (scope.ReadAll || db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == scope.UserId && m.IsEnabled && !m.IsDeleted)));
    private Task<bool> Visible(Guid property, AccessScope scope, CancellationToken token) => Properties(scope).AnyAsync(p => p.Id == property && p.IsEnabled, token);

    private async Task<Property?> Lock(Guid property, AccessScope scope, CancellationToken token)
    {
        if (!await Properties(scope).AnyAsync(p => p.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var parent = await Properties(scope).SingleOrDefaultAsync(p => p.Id == property, token);
        if (parent is not null && !parent.IsEnabled) throw Conflict(ErrorCatalog.ParentInactive);
        return parent;
    }

    private async Task Commit(Property parent, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, CancellationToken token)
    {
        await QueueLifecycleNotifications(token);
        db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } unique)
        {
            throw Conflict(UniqueErrors.GetValueOrDefault(unique.ConstraintName ?? "", ErrorCatalog.RecordChanged));
        }
        await tx.CommitAsync(token);
    }

    private static void Transition(Action change)
    {
        try { change(); }
        catch (InvalidOperationException) { throw Conflict(ErrorCatalog.ParkingStateInvalid); }
    }

    private Task<ParkingPolicy?> CurrentPolicy(Guid property, CancellationToken token) =>
        db.ParkingPolicies.SingleOrDefaultAsync(x => x.PropertyId == property && x.IsEnabled, token);

    private Task<bool> HasRelation(Guid property, Guid unit, Guid person, CancellationToken token)
    {
        var today = Today;
        return db.UnitOccupancies.AnyAsync(o => o.PropertyId == property && o.UnitId == unit && o.PersonId == person && o.IsEnabled &&
            ResidentTypes.Contains(o.Type) && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today) &&
            db.Units.Any(u => u.Id == unit && u.PropertyId == property && u.IsEnabled) &&
            db.People.Any(p => p.Id == person && p.PropertyId == property && p.IsEnabled), token);
    }

    private async Task EnsureVehicleQuota(ParkingPolicy policy, Guid unit, Guid person, CancellationToken token)
    {
        var active = db.Vehicles.Where(v => v.PropertyId == policy.PropertyId && v.IsEnabled && (v.Status == VehicleStatus.PENDING || v.Status == VehicleStatus.APPROVED));
        if (!policy.AllowsVehicle(await active.CountAsync(v => v.UnitId == unit, token), await active.CountAsync(v => v.PersonId == person, token)))
            throw Conflict(ErrorCatalog.ParkingQuotaExceeded);
    }

    private async Task<Vehicle?> Replay(Guid property, Guid requestId, Func<Vehicle, bool> same, CancellationToken token)
    {
        var existing = await db.Vehicles.SingleOrDefaultAsync(v => v.PropertyId == property && v.RequestId == requestId, token);
        if (existing is not null && (existing.PersonalDataRedactedAt is not null || !same(existing))) throw Conflict(ErrorCatalog.ParkingRequestReused);
        return existing;
    }
}
