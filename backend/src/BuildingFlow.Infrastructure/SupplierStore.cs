using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Infrastructure;

public sealed partial class SupplierStore(BuildingFlowDbContext db, ICurrentUserService actor, ManagementAccess management, IFileStorage storage, TimeProvider clock, ILogger<SupplierStore> logger) : ISupplierStore, ISupplierRelationsStore, ISupplierDocumentsStore, ISupplierQuoteStore, ISupplierWorkOrderStore, ISupplierMaintenanceStore, ISupplierInvoiceStore
{
    private async Task<bool> Allowed(Guid tenant, CancellationToken token)
    {
        if (!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || string.IsNullOrWhiteSpace(actor.Issuer) || !new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" }.Any(actor.IsInRole)) throw new UnauthorizedAccessException();
        if (!await db.Set<PersonTenant>().AnyAsync(x => x.Id == tenant && x.IsEnabled, token)) return false;
        if (actor.IsInRole("SUPER_ADMIN")) return true;
        return await db.Set<PersonTenantMembership>().AnyAsync(m => m.TenantId == tenant && m.IsEnabled && db.Set<PersonUserAccount>().Any(a => a.Id == m.UserAccountId && a.IsEnabled && a.Issuer == actor.Issuer && a.Subject == actor.UserId), token)
            && await management.TenantPeopleAllowedAsync(tenant, token);
    }
    public async Task<PagedRows<SupplierWorkspace>> Workspaces(int page, int pageSize, CancellationToken token)
    {
        if (page is < 1 or > 1000000 || pageSize is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] });
        var tenants = management.PersonTenants().AsNoTracking();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        var total = await tenants.CountAsync(token);
        var items = await tenants.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierWorkspace(x.Id, x.Name)).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, total);
    }
    private IQueryable<Supplier> Rows(Guid tenant) => db.Set<Supplier>().Where(x => x.TenantId == tenant);
    private static SupplierResponse Response(Supplier x) => new(x.Id, x.TenantId, x.Name, x.Kind.ToString(), x.Description, x.Status.ToString(), x.StatusReason, x.IsEnabled, x.Version, x.CreatedAt, x.ModifiedAt);
    public async Task<PagedRows<SupplierResponse>?> List(Guid tenant, SupplierQuery q, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null;
        var rows = Rows(tenant).AsNoTracking();
        if (q.Status is not null) { var status = Enum.Parse<SupplierStatus>(q.Status); rows = rows.Where(x => x.Status == status); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = "%" + q.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            rows = rows.Where(x => EF.Functions.ILike(x.Name, pattern, "\\"));
        }
        var total = await rows.CountAsync(token);
        var desc = q.SortDirection == "desc";
        var sorted = q.SortBy == "createdAt" ? (desc ? rows.OrderByDescending(x => x.CreatedAt) : rows.OrderBy(x => x.CreatedAt)) : (desc ? rows.OrderByDescending(x => x.NameKey) : rows.OrderBy(x => x.NameKey));
        var items = await sorted.ThenBy(x => x.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items.Select(Response).ToArray(), q.Page, q.PageSize, total);
    }
    public async Task<SupplierResponse?> Get(Guid tenant, Guid id, CancellationToken token)
    { if (!await Allowed(tenant, token)) return null; var row = await Rows(tenant).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token); return row is null ? null : Response(row); }
    public async Task<SupplierMutation?> Save(Guid tenant, Guid? id, SupplierRequest c, CancellationToken token)
    {
        if (!await Allowed(tenant, token)) return null;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM person_tenants WHERE id = {tenant} FOR UPDATE", token);
        if (!await Allowed(tenant, token)) return null;
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { tenant, id, c.Name, c.Kind, c.Description, c.Version, c.Status, c.StatusReason }))));
        var prior = await db.Set<SupplierOperation>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.ActorIssuer == actor.Issuer && x.ActorSubject == actor.UserId && x.OperationId == c.OperationId, token);
        if (prior is not null)
        {
            if (prior.Fingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            var current = await Rows(tenant).AsNoTracking().SingleOrDefaultAsync(x => x.Id == prior.SupplierId, token);
            if (current is null) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            await tx.CommitAsync(token); return new(Response(current), true, prior.AppliedVersion);
        }
        var key = c.Name!.ToUpperInvariant();
        if (await Rows(tenant).AnyAsync(x => x.Id != id && x.NameKey == key, token)) throw new HierarchyConflictException(ErrorCatalog.SupplierDuplicate.Create());
        Supplier row;
        if (id.HasValue)
        {
            row = (await Rows(tenant).SingleOrDefaultAsync(x => x.Id == id, token))!;
            if (row is null) return null;
            if (row.Version != c.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            row.Edit(c.Name, Enum.Parse<SupplierKind>(c.Kind!), c.Description);
        }
        else { row = new(tenant, c.Name, Enum.Parse<SupplierKind>(c.Kind!), c.Description); db.Add(row); }
        row.ChangeStatus(Enum.Parse<SupplierStatus>(c.Status), c.StatusReason);
        // A no-op still consumes the supplied version and receives immutable operation evidence.
        if (id.HasValue) db.Entry(row).Property(x => x.IsEnabled).IsModified = true;
        var applied = id.HasValue ? c.Version + 1 : 1;
        db.Add(new SupplierOperation(tenant, row.Id, c.OperationId, actor.Issuer!, actor.UserId!, fingerprint, applied));
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(Response(row), false, applied);
    }
}
