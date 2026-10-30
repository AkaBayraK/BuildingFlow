using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MaintenanceStore(BuildingFlowDbContext db,TimeProvider clock) : IMaintenanceStore
{
    private IQueryable<Property> Properties(AccessScope scope) => db.Properties.Where(p => !p.IsDeleted && (scope.ReadAll || db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == scope.UserId && m.IsEnabled && !m.IsDeleted)));
    private IQueryable<MaintenancePlan> Plans(Guid property) => db.MaintenancePlans.AsNoTracking().Where(x => x.PropertyId == property && x.IsEnabled);
    private static MaintenancePlanResponse Response(MaintenancePlan x) => new(x.Id, x.PropertyId, x.Title, x.Description, x.Type.ToString(), x.PlannedDate, x.Location, x.Responsible, x.Status.ToString(), x.Version, x.CreatedAt, x.ModifiedAt);
    private static string Pattern(string search) => "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    private static void Version(long expected, long actual) { if (expected != actual) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    private async Task<Property?> Lock(Guid property, AccessScope scope, CancellationToken token)
    {
        if (!await Properties(scope).AnyAsync(p => p.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var parent = await Properties(scope).SingleOrDefaultAsync(p => p.Id == property, token);
        if (parent is not null && !parent.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        return parent;
    }
    public async Task<MaintenancePlanPage?> List(Guid property, MaintenanceQuery query, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        var rows = Plans(property);
        if (!string.IsNullOrEmpty(query.Status)) { var status = Enum.Parse<MaintenanceStatus>(query.Status); rows = rows.Where(x => x.Status == status); }
        if (query.From.HasValue) rows = rows.Where(x => x.PlannedDate >= query.From.Value);
        if (query.To.HasValue) rows = rows.Where(x => x.PlannedDate <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var pattern = Pattern(query.Search); rows = rows.Where(x => EF.Functions.ILike(x.Title, pattern, "\\") || EF.Functions.ILike(x.Description ?? "", pattern, "\\") || EF.Functions.ILike(x.Location ?? "", pattern, "\\") || EF.Functions.ILike(x.Responsible ?? "", pattern, "\\")); }
        var grouped = await rows.GroupBy(x => x.Type).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(token);
        var counts = Enum.GetValues<MaintenanceType>().Select(t => new MaintenanceTypeCount(t.ToString(), grouped.SingleOrDefault(g => g.Key == t)?.Count ?? 0)).ToArray();
        if (!string.IsNullOrEmpty(query.Type)) { var type = Enum.Parse<MaintenanceType>(query.Type); rows = rows.Where(x => x.Type == type); }
        var total = await rows.CountAsync(token);
        var values = await rows.OrderBy(x => x.PlannedDate).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        await tx.CommitAsync(token); return new(values.Select(Response).ToArray(), query.Page, query.PageSize, total, counts);
    }
    public async Task<MaintenancePlanResponse?> Get(Guid property, Guid id, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        var value = await Plans(property).SingleOrDefaultAsync(x => x.Id == id, token); return value is null ? null : Response(value);
    }
    public async Task<MaintenanceUpcoming?> Upcoming(Guid property, DateOnly today, int limit, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(p => p.Id == property, token)) return null;
        var open = Plans(property).Where(x => x.PlannedDate >= today && (x.Status == MaintenanceStatus.PLANNED || x.Status == MaintenanceStatus.IN_PROGRESS));
        var horizon = today.AddDays(MaintenanceService.UpcomingDays);
        var next = await open.CountAsync(x => x.PlannedDate <= horizon, token);
        var values = await open.OrderBy(x => x.PlannedDate).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id).Take(limit).ToListAsync(token);
        await tx.CommitAsync(token); return new(today, values.Select(Response).ToArray(), next);
    }
    public async Task<MaintenancePlanResponse?> Save(Guid property, Guid? id, MaintenancePlanCommand command, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        MaintenancePlan value;
        if (id.HasValue) {
            var current = await db.MaintenancePlans.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id && x.IsEnabled, token);
            if (current is null) return null; Version(command.Version, current.Version); value = current;
            await DefinitionPolicy.EnsureSelectableAsync(db, property, "MAINTENANCE_TYPE", command.Type.ToString(), current.Type.ToString(), "type", token);
            value.Edit(command.Title, command.Description, command.Type, command.PlannedDate, command.Location, command.Responsible, command.Status);
            db.Entry(value).Property(x => x.IsEnabled).IsModified = true;
        } else {
            await DefinitionPolicy.EnsureSelectableAsync(db, property, "MAINTENANCE_TYPE", command.Type.ToString(), null, "type", token);
            value = new(property, command.Title, command.Description, command.Type, command.PlannedDate, command.Location, command.Responsible, command.Status); db.Add(value);
        }
        await CommonAreaMaintenanceEvents.PlanChanged(db,value,false,clock.GetUtcNow(),token);
        db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return Response(value);
    }
    public async Task<bool> Delete(Guid property, Guid id, long version, AccessScope access, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return false;
        var value = await db.MaintenancePlans.SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id && x.IsEnabled, token); if (value is null) return false;
        Version(version, value.Version);
        await CommonAreaMaintenanceEvents.PlanChanged(db,value,true,clock.GetUtcNow(),token);
        db.Remove(value); db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
    }
}
