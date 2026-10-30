using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using ServiceEntity = BuildingFlow.Domain.SupplierService;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    private static void Page(int page, int size) { if (page is < 1 or > 1000000 || size is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] }); }
    private static void InvalidSupplier(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.SupplierInvalid.Create()] });
    private IQueryable<PersonPropertyScope> PropertyScopes(Guid tenant) => management.PropertyScopes().Where(s => s.TenantId == tenant);
    public async Task<PagedRows<SupplierWorkspace>?> Categories(Guid tenant, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null;
        var rows = db.Set<SupplierCategory>().AsNoTracking().Where(c => c.TenantId == tenant && c.IsEnabled);
        var total = await rows.CountAsync(token); var items = await rows.OrderBy(x => x.NameKey).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierWorkspace(x.Id, x.Name)).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, total);
    }
    public async Task<SupplierWorkspace?> CreateCategory(Guid tenant, SupplierCategoryRequest c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(c.Name) || c.Name.Trim().Length > 100 || c.Name.Any(char.IsControl)) InvalidSupplier("name");
        if (!await Allowed(tenant, token)) return null;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE", token);
        if (!await Allowed(tenant, token)) return null;
        var prior = await db.Set<SupplierCategory>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == c.OperationId, token);
        if (prior is not null)
        {
            if (prior.TenantId != tenant || prior.IsDeleted || !prior.IsEnabled || prior.Name != c.Name.Trim()) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            return new(prior.Id, prior.Name);
        }
        var key = c.Name.Trim().ToUpperInvariant();
        if (await db.Set<SupplierCategory>().AnyAsync(x => x.TenantId == tenant && x.NameKey == key, token)) throw new HierarchyConflictException(ErrorCatalog.SupplierDuplicate.Create());
        var category = new SupplierCategory(tenant, c.Name, c.OperationId); db.Add(category); await db.SaveChangesAsync(token); await tx.CommitAsync(token); return new(category.Id, category.Name);
    }
    public async Task<PagedRows<SupplierWorkspace>?> Properties(Guid tenant, int page, int pageSize, CancellationToken token, Guid? supplier = null)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null;
        var scopes = PropertyScopes(tenant); var rows = db.Properties.AsNoTracking().Where(p => p.IsEnabled && scopes.Any(s => s.PropertyId == p.Id));
        if(supplier.HasValue) rows=rows.Where(p=>db.Set<SupplierManagementArea>().Any(a=>a.TenantId==tenant && a.SupplierId==supplier && a.PropertyId==p.Id && a.IsEnabled));
        var total = await rows.CountAsync(token); var items = await rows.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierWorkspace(x.Id, x.Name)).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, total);
    }
    public async Task<PagedRows<SupplierAreaRow>?> Areas(Guid tenant, Guid supplier, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token) || !await Rows(tenant).AnyAsync(x => x.Id == supplier, token)) return null;
        var scopes = PropertyScopes(tenant);
        var rows = from link in db.Set<SupplierAreaService>().AsNoTracking()
                   join area in db.Set<SupplierManagementArea>() on new { link.TenantId, link.SupplierId, link.PropertyId } equals new { area.TenantId, area.SupplierId, area.PropertyId }
                   join service in db.Set<ServiceEntity>() on new { link.TenantId, link.SupplierId, Id = link.ServiceId } equals new { service.TenantId, service.SupplierId, service.Id }
                   join category in db.Set<SupplierCategory>() on new { service.TenantId, Id = service.CategoryId } equals new { category.TenantId, category.Id }
                   join property in db.Properties on link.PropertyId equals property.Id
                   where link.TenantId == tenant && link.SupplierId == supplier && link.IsEnabled && area.IsEnabled && service.IsEnabled && category.IsEnabled && scopes.Any(s => s.PropertyId == property.Id)
                   select new { link.Id, PropertyId = property.Id, PropertyName = property.Name, CategoryId = category.Id, CategoryName = category.Name, ServiceName = service.Name };
        var total = await rows.CountAsync(token); var items = await rows.OrderBy(x => x.PropertyName).ThenBy(x => x.ServiceName).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items.Select(x => new SupplierAreaRow(x.Id,tenant,supplier,x.PropertyId,x.PropertyName,x.CategoryId,x.CategoryName,x.ServiceName)).ToArray(), page, pageSize, total);
    }
    public async Task<SupplierMutation?> Link(Guid tenant, Guid supplier, SupplierAreaRequest c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1 || c.PropertyId == Guid.Empty || c.CategoryId == Guid.Empty || string.IsNullOrWhiteSpace(c.ServiceName) || c.ServiceName.Trim().Length > 200 || c.ServiceName.Any(char.IsControl)) InvalidSupplier("serviceName");
        if (!await Allowed(tenant, token)) return null;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id={tenant} FOR UPDATE", token);
        if (!await Allowed(tenant, token)) return null;
        if (!await PropertyScopes(tenant).AnyAsync(s => s.PropertyId == c.PropertyId, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={c.PropertyId} FOR UPDATE", token);
        if (!await PropertyScopes(tenant).AnyAsync(s => s.PropertyId == c.PropertyId, token) || !(await management.EvaluateAsync(tenant, c.PropertyId, "MANAGER", token)).Allowed) return null;
        var row = await Rows(tenant).SingleOrDefaultAsync(x => x.Id == supplier, token); if (row is null) return null;
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { action = "AREA_SERVICE", tenant, supplier, c.Version, c.PropertyId, c.CategoryId, name = c.ServiceName.Trim() }))));
        var prior = await db.Set<SupplierOperation>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.ActorIssuer == actor.Issuer && x.ActorSubject == actor.UserId && x.OperationId == c.OperationId, token);
        if (prior is not null)
        {
            if (prior.SupplierId != supplier || prior.Fingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            await tx.CommitAsync(token); return new(Response(row), true, prior.AppliedVersion);
        }
        if (row.Version != c.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if (row.Status != SupplierStatus.ACTIVE) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        if (!await db.Set<SupplierCategory>().AnyAsync(x => x.Id == c.CategoryId && x.TenantId == tenant && x.IsEnabled, token)) return null;
        var area = await db.Set<SupplierManagementArea>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.SupplierId == supplier && x.PropertyId == c.PropertyId, token);
        if (area is null) { area = new(tenant, supplier, c.PropertyId); db.Add(area); }
        else if (!area.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        var key = c.ServiceName.Trim().ToUpperInvariant();
        var service = await db.Set<ServiceEntity>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.SupplierId == supplier && x.CategoryId == c.CategoryId && x.NameKey == key, token);
        if (service is null) { service = new(tenant, supplier, c.CategoryId, c.ServiceName); db.Add(service); }
        else if (!service.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        if (!await db.Set<SupplierAreaService>().AnyAsync(x => x.TenantId == tenant && x.SupplierId == supplier && x.PropertyId == c.PropertyId && x.ServiceId == service.Id, token)) db.Add(new SupplierAreaService(tenant, supplier, c.PropertyId, service.Id));
        db.Entry(row).Property(x => x.IsEnabled).IsModified = true;
        var parent = await db.Properties.SingleAsync(x => x.Id == c.PropertyId, token); db.Entry(parent).Property(x => x.IsEnabled).IsModified = true;
        db.Add(new SupplierOperation(tenant, supplier, c.OperationId, actor.Issuer!, actor.UserId!, fingerprint, c.Version + 1));
        await db.SaveChangesAsync(token); await tx.CommitAsync(token); return new(Response(row), false, c.Version + 1);
    }
}
