using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;
public sealed partial class CommonAreaStore(BuildingFlowDbContext db, ICurrentUserService actor, IManagementAccess management,
    ISessionContextStore contexts, IMyPortfolioStore portfolio, IConfiguration config, IFileStorage storage, TimeProvider clock) : ICommonAreaStore, ICommonAreaPhotoStore, ICommonAreaReservationStore, ICommonAreaClosureStore
{
    private async Task<bool> Allowed(Guid property, CancellationToken token)
    {
        if (!await db.Properties.AnyAsync(p => p.Id == property, token)) return false;
        if (config.GetValue<bool>("PersonRefactor:SessionContextsEnabled"))
            return await contexts.PropertyAllowedAsync(property, "MANAGER", token);
        if (actor.IsInRole("SUPER_ADMIN")) return true;
        if (!await db.PropertyMemberships.AnyAsync(m => m.PropertyId == property && m.UserId == actor.UserId && m.IsEnabled, token)) return false;
        var scopes = await db.Set<PersonPropertyScope>().IgnoreQueryFilters().Where(s => s.PropertyId == property).ToArrayAsync(token);
        foreach (var scope in scopes)
            if (!(await management.EvaluateAsync(scope.TenantId, property, "MANAGER", token)).Allowed) return false;
        return true;
    }
    private static void Conflict() => throw new HierarchyConflictException(ErrorCatalog.CommonAreaConflict.Create());
    private static void Expected(long expected, long actual) { if (expected != actual) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    private static CommonAreaResponse Response(CommonArea x, bool replay = false) => new(x.Id, x.PropertyId, x.RequestId, x.Name,
        x.Type.ToString(), x.BuildingId, x.Visibility.ToString(), x.Usage.ToString(), x.Capacity, x.Description, x.Rules,
        x.OpensAt?.ToString("HH:mm", CultureInfo.InvariantCulture), x.ClosesAt?.ToString("HH:mm", CultureInfo.InvariantCulture),
        x.FeeAmount.ToString("0.00", CultureInfo.InvariantCulture), x.Currency, x.IsEnabled, x.Version, x.CreatedAt, x.ModifiedAt, replay);
    private static IQueryable<CommonArea> Filter(IQueryable<CommonArea> rows, CommonAreaQuery q)
    {
        if (q.IsEnabled.HasValue) rows = rows.Where(x => x.IsEnabled == q.IsEnabled.Value);
        if (q.BuildingId.HasValue) rows = rows.Where(x => x.BuildingId == q.BuildingId);
        if (q.Type is not null) { var type = Enum.Parse<CommonAreaType>(q.Type); rows = rows.Where(x => x.Type == type); }
        if (!string.IsNullOrWhiteSpace(q.Search)) {
            var pattern = "%" + q.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            rows = rows.Where(x => EF.Functions.ILike(x.Name, pattern, "\\"));
        }
        return rows;
    }
    private static async Task<PagedRows<CommonAreaResponse>> Page(IQueryable<CommonArea> rows, CommonAreaQuery q, CancellationToken token)
    {
        rows = Filter(rows, q); var count = await rows.CountAsync(token);
        var values = await rows.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToArrayAsync(token);
        return new(values.Select(x => Response(x)).ToArray(), q.Page, q.PageSize, count);
    }
    public async Task<PagedRows<CommonAreaResponse>?> List(Guid property, CommonAreaQuery q, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(property, token)) return null;
        var result = await Page(db.Set<CommonArea>().AsNoTracking().Where(x => x.PropertyId == property), q, token);
        await tx.CommitAsync(token); return result;
    }
    public async Task<CommonAreaResponse?> Get(Guid property, Guid id, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(property, token)) return null;
        var value = await db.Set<CommonArea>().AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id, token);
        await tx.CommitAsync(token); return value is null ? null : Response(value);
    }
    private async Task<Property?> Lock(Guid property, CancellationToken token)
    {
        if (!await Allowed(property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE", token);
        if (!await Allowed(property, token)) return null;
        var p = await db.Properties.SingleAsync(x => x.Id == property, token);
        if (!p.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        return p;
    }
    public async Task<CommonAreaResponse?> Save(Guid property, Guid? id, CommonAreaCommand c, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, token); if (parent is null) return null;
        CommonArea? value;
        if (id is null) {
            // Ignore only the child soft-delete filter; parent scope was rechecked under the lock.
            value = await db.Set<CommonArea>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PropertyId == property && x.RequestId == c.RequestId, token);
            if (value is not null) { if (value.IsDeleted || value.Fingerprint != c.Fingerprint) Conflict(); return Response(value, true); }
        } else {
            value = await db.Set<CommonArea>().SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id, token);
            if (value is null) return null; Expected(c.Version, value.Version); if (value.RequestId != c.RequestId) Conflict();
            await FutureReservationsFit(value, c, token);
        }
        if (c.BuildingId is {} buildingId) {
            var building = await db.Buildings.SingleOrDefaultAsync(b => b.Id == buildingId && b.PropertyId == property && b.IsEnabled, token);
            if (parent.Type == PropertyType.APARTMENT || building is null) CommonAreaService.Invalid("buildingId");
            // A concurrent disable/delete/edit of the selected building cannot silently win.
            db.Entry(building!).Property(b => b.IsEnabled).IsModified = true;
        }
        await DefinitionPolicy.EnsureSelectableAsync(db, property, "COMMON_AREA_TYPE", c.Type.ToString(), id is null ? null : value!.Type.ToString(), "type", token);
        if (id is null) {
            value = new(property, c.RequestId, c.Fingerprint, c.Name, c.Type, c.BuildingId, c.Visibility, c.Usage,
                c.Capacity, c.Description, c.Rules, c.OpensAt, c.ClosesAt, c.FeeAmount); value.SetEnabled(c.IsEnabled); db.Add(value);
        } else {
            value!.Edit(c.Name, c.Type, c.BuildingId, c.Visibility, c.Usage, c.Capacity, c.Description, c.Rules, c.OpensAt, c.ClosesAt, c.FeeAmount, c.IsEnabled);
            db.Entry(value).Property(x => x.IsEnabled).IsModified = true;
        }
        db.Entry(parent).Property(p => p.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return Response(value!);
    }
    public async Task<bool> Delete(Guid property, Guid id, long version, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, token); if (parent is null) return false;
        var value = await db.Set<CommonArea>().SingleOrDefaultAsync(x => x.PropertyId == property && x.Id == id, token);
        if (value is null) return false; Expected(version, value.Version);
        if(await db.Set<CommonAreaClosure>().IgnoreQueryFilters().AnyAsync(x=>x.PropertyId==property && x.CommonAreaId==id,token)) Conflict();
        if (await db.Set<CommonAreaReservation>().IgnoreQueryFilters().AnyAsync(x => x.PropertyId == property && x.CommonAreaId == id, token)) Conflict();
        db.Remove(value); db.Entry(parent).Property(p => p.IsEnabled).IsModified = true;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
    }
    public async Task<PagedRows<CommonAreaResponse>?> ForUnit(ActiveContextQuery context, CommonAreaQuery query, Guid? id, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        var rows = await UnitAreas(context, token); if (rows is null) return null;
        if (id.HasValue) rows = rows.Where(x => x.Id == id);
        var result = await Page(rows, query, token); await tx.CommitAsync(token); return result;
    }
    private async Task<IQueryable<CommonArea>?> UnitAreas(ActiveContextQuery context, CancellationToken token)
    {
        var active = await portfolio.ResolveAsync(context, token); if (active is null) return null;
        var unit = await db.Units.AsNoTracking().SingleOrDefaultAsync(u => u.Id == context.UnitId && u.PropertyId == context.PropertyId && u.IsEnabled, token);
        if (unit is null || !await db.Properties.AnyAsync(p => p.Id == context.PropertyId && p.IsEnabled, token)) return null;
        return db.Set<CommonArea>().AsNoTracking().Where(x => x.PropertyId == context.PropertyId && x.IsEnabled &&
            (x.BuildingId == null || db.Buildings.Any(b => b.PropertyId == x.PropertyId && b.Id == x.BuildingId && b.IsEnabled)) &&
            (x.Visibility == CommonAreaVisibility.PROPERTY || x.BuildingId == unit.BuildingId));
    }
}
