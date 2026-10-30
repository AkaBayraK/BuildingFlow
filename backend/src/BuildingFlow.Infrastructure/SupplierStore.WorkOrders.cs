using System.Data;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    private IQueryable<SupplierWorkOrder> OrderRows(Guid tenant, Guid supplier) => db.Set<SupplierWorkOrder>().Where(x => x.TenantId == tenant && x.SupplierId == supplier);
    private SupplierWorkOrderResponse OrderResponse(SupplierWorkOrder o, string propertyName, string serviceName, int photoCount) => new(o.Id, o.TenantId, o.SupplierId, o.PropertyId, propertyName, o.AreaServiceId, serviceName, o.Title, o.Description, o.Source.ToString(), o.InquiryId, o.QuoteId, o.RequestId, o.MaintenancePlanId, o.SourceVersion, o.SourceSnapshot, o.Amount.HasValue ? Money(o.Amount.Value) : null, o.Currency, o.Status.ToString(), o.DueAt, o.DueAt < clock.GetUtcNow() && o.Status is SupplierWorkOrderStatus.ASSIGNED or SupplierWorkOrderStatus.ACCEPTED or SupplierWorkOrderStatus.IN_PROGRESS, o.AssignedAt, o.AcceptedAt, o.StartedAt, o.CompletedAt, o.VerifiedAt, o.ClosedAt, o.CompletionNote, o.DecisionReason, photoCount, o.Version, o.CreatedAt, o.ModifiedAt);
    private sealed class OrderData
    {
        public SupplierWorkOrder Record { get; init; } = null!;
        public string PropertyName { get; init; } = null!;
        public string ServiceName { get; init; } = null!;
        public int PhotoCount { get; init; }
    }
    private IQueryable<OrderData> LabeledOrders(Guid tenant, Guid supplier) =>
        from o in OrderRows(tenant, supplier).AsNoTracking()
        join p in db.Properties on o.PropertyId equals p.Id
        join a in db.Set<SupplierAreaService>() on new { o.TenantId, o.SupplierId, o.PropertyId, Id = o.AreaServiceId } equals new { a.TenantId, a.SupplierId, a.PropertyId, a.Id }
        join s in db.Set<BuildingFlow.Domain.SupplierService>() on new { a.TenantId, a.SupplierId, Id = a.ServiceId } equals new { s.TenantId, s.SupplierId, s.Id }
        select new OrderData { Record = o, PropertyName = p.Name, ServiceName = s.Name, PhotoCount = db.Set<SupplierWorkOrderPhoto>().Count(photo => photo.TenantId == tenant && photo.SupplierId == supplier && photo.PropertyId == o.PropertyId && photo.WorkOrderId == o.Id) };
    private async Task<SupplierWorkOrderResponse> OrderResponse(SupplierWorkOrder row, CancellationToken token)
    {
        var data = await LabeledOrders(row.TenantId, row.SupplierId).SingleAsync(x => x.Record.Id == row.Id, token);
        return OrderResponse(row, data.PropertyName, data.ServiceName, data.PhotoCount);
    }
    public async Task<PagedRows<SupplierWorkOrderSourceChoice>?> WorkOrderSources(Guid tenant, Guid supplier, Guid property, string source, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); if (source is not ("REQUEST" or "MAINTENANCE")) InvalidSupplier("source");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await AreaAllowed(tenant, supplier, property, token)) return null;
        var open = db.Set<SupplierWorkOrder>().Where(x => x.TenantId == tenant && x.PropertyId == property && x.Status != SupplierWorkOrderStatus.CLOSED && x.Status != SupplierWorkOrderStatus.REJECTED && x.Status != SupplierWorkOrderStatus.CANCELLED);
        SupplierWorkOrderSourceChoice[] items; int count;
        if (source == "REQUEST")
        {
            var rows = db.Set<MobileRequest>().AsNoTracking().Where(x => x.PropertyId == property && x.IsEnabled && (x.ManagementStatus == "RECORDED" || x.ManagementStatus == "IN_PROGRESS") && !open.Any(o => o.RequestId == x.Id));
            count = await rows.CountAsync(token); items = await rows.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierWorkOrderSourceChoice(x.Id, tenant, property, "REQUEST", x.Description, x.Version)).ToArrayAsync(token);
        }
        else
        {
            var rows = db.MaintenancePlans.AsNoTracking().Where(x => x.PropertyId == property && x.IsEnabled && (x.Status == MaintenanceStatus.PLANNED || x.Status == MaintenanceStatus.IN_PROGRESS) && !open.Any(o => o.MaintenancePlanId == x.Id));
            count = await rows.CountAsync(token); items = await rows.OrderBy(x => x.PlannedDate).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierWorkOrderSourceChoice(x.Id, tenant, property, "MAINTENANCE", x.Description == null ? x.Title : x.Title + "\n" + x.Description, x.Version)).ToArrayAsync(token);
        }
        await tx.CommitAsync(token); return new(items, page, pageSize, count);
    }
    public async Task<PagedRows<SupplierWorkOrderResponse>?> WorkOrders(Guid tenant, Guid supplier, string? status, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); SupplierWorkOrderStatus? filter = null;
        if (status is not null) { if (!Enum.TryParse<SupplierWorkOrderStatus>(status, out var parsed) || !Enum.IsDefined(parsed) || parsed.ToString() != status) InvalidSupplier("status"); filter = parsed; }
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token) || !await Rows(tenant).AnyAsync(x => x.Id == supplier, token)) return null;
        var scopes = PropertyScopes(tenant); var rows = LabeledOrders(tenant, supplier).Where(x => scopes.Any(s => s.PropertyId == x.Record.PropertyId));
        if (filter.HasValue) rows = rows.Where(x => x.Record.Status == filter.Value);
        var count = await rows.CountAsync(token); var values = await rows.OrderByDescending(x => x.Record.CreatedAt).ThenBy(x => x.Record.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(values.Select(x => OrderResponse(x.Record, x.PropertyName, x.ServiceName, x.PhotoCount)).ToArray(), page, pageSize, count);
    }
    public async Task<SupplierWorkOrderResponse?> WorkOrder(Guid tenant, Guid supplier, Guid id, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null; var row = await OrderRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
        if (row is null || !await AreaAllowed(tenant, supplier, row.PropertyId, token)) return null;
        var result = await OrderResponse(row, token); await tx.CommitAsync(token); return result;
    }
    private static object OrderSnapshot(SupplierWorkOrder o) => new { o.Title, o.Description, Source = o.Source.ToString(), o.SourceVersion, o.SourceSnapshot, o.RequestId, o.MaintenancePlanId, o.QuoteId, o.InquiryId, o.AreaServiceId, o.Amount, o.Currency, Status = o.Status.ToString(), o.DueAt, o.AssignedAt, o.AcceptedAt, o.StartedAt, o.CompletedAt, o.VerifiedAt, o.ClosedAt, o.CompletionNote, o.DecisionReason };
    private void OrderRevision(SupplierWorkOrder order, Guid operation, string fingerprint, string action, long applied, object? snapshot = null)
    {
        db.Add(new SupplierWorkOrderRevision(order, operation, actor.Issuer!, actor.UserId!, fingerprint, action, JsonSerializer.Serialize(snapshot ?? OrderSnapshot(order)), applied));
        if (db.Entry(order).State != EntityState.Added) db.Entry(order).Property(x => x.IsEnabled).IsModified = true;
    }
    private async Task<SupplierWorkOrderMutation?> OrderReplay(Guid tenant, Guid supplier, Guid operation, string fingerprint, Supplier parent, CancellationToken token)
    {
        var receipt = await db.Set<SupplierWorkOrderRevision>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.ActorIssuer == actor.Issuer && x.ActorSubject == actor.UserId && x.OperationId == operation, token); if (receipt is null) return null;
        if (receipt.SupplierId != supplier || receipt.Fingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        var order = await OrderRows(tenant, supplier).SingleOrDefaultAsync(x => x.Id == receipt.WorkOrderId && x.PropertyId == receipt.PropertyId, token);
        if (order is null || !await AreaAllowed(tenant, supplier, order.PropertyId, token)) return null;
        return new(await OrderResponse(order, token), Response(parent), true, receipt.AppliedVersion);
    }
    private async Task<(SupplierWorkOrder Order, Supplier Supplier, Property Property)?> LockOrder(Guid tenant, Guid supplier, Guid id, CancellationToken token)
    {
        if (!await Allowed(tenant, token)) return null;
        var property = await OrderRows(tenant, supplier).Where(x => x.Id == id).Select(x => (Guid?)x.PropertyId).SingleOrDefaultAsync(token); if (property is null) return null;
        var scope = await LockArea(tenant, supplier, property.Value, false, token); if (scope is null) return null;
        var row = await OrderRows(tenant, supplier).SingleOrDefaultAsync(x => x.Id == id, token); return row is null ? null : (row, scope.Value.Supplier, scope.Value.Property);
    }
    public async Task<SupplierWorkOrderMutation?> CreateWorkOrder(Guid tenant, Guid supplier, SupplierWorkOrderCreate c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.SupplierVersion < 1 || c.PropertyId == Guid.Empty || c.AreaServiceId == Guid.Empty || c.SourceId == Guid.Empty || c.SourceVersion < 1 || c.Source is not ("REQUEST" or "MAINTENANCE")) InvalidSupplier("source");
        if (!await Allowed(tenant, token)) return null;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        // Mobile management and offline request writes take this advisory lock before Property.
        if (c.Source == "REQUEST") await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"mobile-record:" + c.SourceId},0))", token);
        var scope = await LockArea(tenant, supplier, c.PropertyId, true, token); if (scope is null) return null;
        var fingerprint = Hash(new { action = "WORK_ORDER_CREATE", tenant, supplier, c.SupplierVersion, c.PropertyId, c.AreaServiceId, c.Source, c.SourceId, c.SourceVersion, title = c.Title?.Trim(), description = c.Description?.Trim(), dueAt = c.DueAt.HasValue ? (DateTimeOffset?)SupplierWorkOrder.UtcInstant(c.DueAt.Value) : null });
        var replay = await OrderReplay(tenant, supplier, c.OperationId, fingerprint, scope.Value.Supplier, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        Expected(c.SupplierVersion, scope.Value.Supplier.Version); if (!await ValidContractLinks(tenant, supplier, c.PropertyId, c.AreaServiceId, null, token)) return null;
        string snapshot;
        if (c.Source == "REQUEST")
        {
            var source = await db.Set<MobileRequest>().SingleOrDefaultAsync(x => x.Id == c.SourceId && x.PropertyId == c.PropertyId && x.IsEnabled, token); if (source is null) return null; Expected(c.SourceVersion, source.Version);
            if (source.ManagementStatus is not ("RECORDED" or "IN_PROGRESS")) throw new HierarchyConflictException(ErrorCatalog.SupplierWorkOrderState.Create()); snapshot = source.Description;
        }
        else
        {
            var source = await db.MaintenancePlans.SingleOrDefaultAsync(x => x.Id == c.SourceId && x.PropertyId == c.PropertyId && x.IsEnabled, token); if (source is null) return null; Expected(c.SourceVersion, source.Version);
            if (source.Status is not (MaintenanceStatus.PLANNED or MaintenanceStatus.IN_PROGRESS)) throw new HierarchyConflictException(ErrorCatalog.SupplierWorkOrderState.Create()); snapshot = source.Title + (source.Description is null ? "" : "\n" + source.Description);
        }
        if (await db.Set<SupplierWorkOrder>().AnyAsync(x => x.TenantId == tenant && x.PropertyId == c.PropertyId && x.Status != SupplierWorkOrderStatus.CLOSED && x.Status != SupplierWorkOrderStatus.REJECTED && x.Status != SupplierWorkOrderStatus.CANCELLED && (c.Source == "REQUEST" ? x.RequestId == c.SourceId : x.MaintenancePlanId == c.SourceId), token)) throw new HierarchyConflictException(ErrorCatalog.SupplierWorkOrderState.Create());
        SupplierWorkOrder order;
        try { order = new(tenant, supplier, c.PropertyId, c.AreaServiceId, Enum.Parse<SupplierWorkOrderSource>(c.Source), c.SourceId, c.SourceVersion, snapshot, c.Title!, c.Description!, c.DueAt); } catch (ArgumentException) { InvalidSupplier("workOrder"); throw; }
        db.Add(order); OrderRevision(order, c.OperationId, fingerprint, "CREATE", 1); TouchArea(scope.Value); await db.SaveChangesAsync(token);
        var result = new SupplierWorkOrderMutation(await OrderResponse(order, token), Response(scope.Value.Supplier), false, order.Version); await tx.CommitAsync(token); return result;
    }
    public async Task<SupplierWorkOrderMutation?> EditWorkOrder(Guid tenant, Guid supplier, Guid id, SupplierWorkOrderEdit c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1) InvalidSupplier("version");
        await using var tx = await db.Database.BeginTransactionAsync(token); var scope = await LockOrder(tenant, supplier, id, token); if (scope is null) return null; var order = scope.Value.Order;
        var fingerprint = Hash(new { action = "WORK_ORDER_EDIT", tenant, supplier, id, c.Version, description = c.Description?.Trim(), dueAt = c.DueAt.HasValue ? (DateTimeOffset?)SupplierWorkOrder.UtcInstant(c.DueAt.Value) : null });
        var replay = await OrderReplay(tenant, supplier, c.OperationId, fingerprint, scope.Value.Supplier, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        Expected(c.Version, order.Version); if (order.Status != SupplierWorkOrderStatus.DRAFT) throw new HierarchyConflictException(ErrorCatalog.SupplierWorkOrderState.Create());
        try { order.Edit(c.Description!, c.DueAt); } catch (ArgumentException) { InvalidSupplier("workOrder"); throw; }
        OrderRevision(order, c.OperationId, fingerprint, "EDIT", c.Version + 1); TouchArea((scope.Value.Supplier, scope.Value.Property)); await db.SaveChangesAsync(token);
        var result = new SupplierWorkOrderMutation(await OrderResponse(order, token), Response(scope.Value.Supplier), false, order.Version); await tx.CommitAsync(token); return result;
    }
    public async Task<SupplierWorkOrderMutation?> TransitionWorkOrder(Guid tenant, Guid supplier, Guid id, SupplierWorkOrderTransition c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1 || !Enum.TryParse<SupplierWorkOrderStatus>(c.Status, out var target) || !Enum.IsDefined(target) || target.ToString() != c.Status) { InvalidSupplier("status"); return null; }
        await using var tx = await db.Database.BeginTransactionAsync(token); var scope = await LockOrder(tenant, supplier, id, token); if (scope is null) return null; var order = scope.Value.Order;
        if (target is SupplierWorkOrderStatus.VERIFIED or SupplierWorkOrderStatus.CLOSED && (!(actor.IsInRole("SUPER_ADMIN") || actor.IsInRole("SITE_ADMIN")) || !(await management.EvaluateAsync(tenant, order.PropertyId, "SITE_ADMIN", token)).Allowed)) throw new UnauthorizedAccessException();
        var fingerprint = Hash(new { action = "WORK_ORDER_TRANSITION", tenant, supplier, id, c.Version, c.Status, reason = c.Reason?.Trim() });
        var replay = await OrderReplay(tenant, supplier, c.OperationId, fingerprint, scope.Value.Supplier, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        Expected(c.Version, order.Version); if (!SupplierWorkOrder.CanTransition(order.Status, target)) throw new HierarchyConflictException(ErrorCatalog.SupplierWorkOrderState.Create());
        if (target is SupplierWorkOrderStatus.ASSIGNED or SupplierWorkOrderStatus.ACCEPTED or SupplierWorkOrderStatus.IN_PROGRESS && (scope.Value.Supplier.Status != SupplierStatus.ACTIVE || !await ValidContractLinks(tenant, supplier, order.PropertyId, order.AreaServiceId, null, token))) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        if (target == SupplierWorkOrderStatus.ASSIGNED)
        {
            var sourceOpen = order.Source switch
            {
                SupplierWorkOrderSource.REQUEST => await db.Set<MobileRequest>().AnyAsync(x => x.Id == order.RequestId && x.PropertyId == order.PropertyId && x.IsEnabled && (x.ManagementStatus == "RECORDED" || x.ManagementStatus == "IN_PROGRESS"), token),
                SupplierWorkOrderSource.MAINTENANCE => await db.MaintenancePlans.AnyAsync(x => x.Id == order.MaintenancePlanId && x.PropertyId == order.PropertyId && x.IsEnabled && (x.Status == MaintenanceStatus.PLANNED || x.Status == MaintenanceStatus.IN_PROGRESS), token),
                _ => true
            };
            if (!sourceOpen) throw new HierarchyConflictException(ErrorCatalog.SupplierWorkOrderState.Create());
        }
        try { order.Transition(target, c.Reason!, clock.GetUtcNow()); } catch (ArgumentException) { InvalidSupplier("workOrder"); throw; }
        OrderRevision(order, c.OperationId, fingerprint, c.Status, c.Version + 1, new { WorkOrder = OrderSnapshot(order), Reason = c.Reason!.Trim() }); TouchArea((scope.Value.Supplier, scope.Value.Property)); await db.SaveChangesAsync(token);
        var result = new SupplierWorkOrderMutation(await OrderResponse(order, token), Response(scope.Value.Supplier), false, order.Version); await tx.CommitAsync(token); return result;
    }
    public async Task<PagedRows<SupplierContractHistory>?> WorkOrderHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null; var order = await OrderRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
        if (order is null || !await AreaAllowed(tenant, supplier, order.PropertyId, token)) return null;
        var rows = db.Set<SupplierWorkOrderRevision>().AsNoTracking().Where(x => x.TenantId == tenant && x.SupplierId == supplier && x.PropertyId == order.PropertyId && x.WorkOrderId == id);
        var count = await rows.CountAsync(token); var items = await rows.OrderByDescending(x => x.AppliedVersion).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierContractHistory(x.Id, x.Action, x.AppliedVersion, x.Snapshot, x.CreatedAt)).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, count);
    }
    public async Task<SupplierWorkOrderMutation?> AddWorkOrderNote(Guid tenant, Guid supplier, Guid id, SupplierWorkOrderNote c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1) InvalidSupplier("version");
        var note = MaintenanceText(c.Note, 2000, true, "note")!;
        await using var tx = await db.Database.BeginTransactionAsync(token); var scope = await LockOrder(tenant, supplier, id, token); if (scope is null) return null; var order = scope.Value.Order;
        var fingerprint = Hash(new { action = "WORK_ORDER_NOTE", tenant, supplier, id, c.Version, note });
        var replay = await OrderReplay(tenant, supplier, c.OperationId, fingerprint, scope.Value.Supplier, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        Expected(c.Version, order.Version);
        if (order.Status is SupplierWorkOrderStatus.CLOSED or SupplierWorkOrderStatus.REJECTED or SupplierWorkOrderStatus.CANCELLED) throw new HierarchyConflictException(ErrorCatalog.SupplierWorkOrderState.Create());
        OrderRevision(order, c.OperationId, fingerprint, "NOTE", c.Version + 1, new { WorkOrder = OrderSnapshot(order), Reason = note }); TouchArea((scope.Value.Supplier, scope.Value.Property)); await db.SaveChangesAsync(token);
        var result = new SupplierWorkOrderMutation(await OrderResponse(order, token), Response(scope.Value.Supplier), false, order.Version); await tx.CommitAsync(token); return result;
    }
    // Mobile managers work per property, across every supplier linked to it.
    public async Task<PagedRows<SupplierWorkOrderRow>?> PropertyWorkOrders(Guid tenant, Guid property, string? status, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); SupplierWorkOrderStatus? filter = status is null ? null : Parse<SupplierWorkOrderStatus>(status, "status");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token) || !await PropertyScopes(tenant).AnyAsync(s => s.PropertyId == property, token) || !(await management.EvaluateAsync(tenant, property, "MANAGER", token)).Allowed) return null;
        var rows = from o in db.Set<SupplierWorkOrder>().AsNoTracking().Where(o => o.TenantId == tenant && o.PropertyId == property)
                   join s in db.Set<Supplier>() on new { o.TenantId, Id = o.SupplierId } equals new { s.TenantId, s.Id }
                   join p in db.Properties on o.PropertyId equals p.Id
                   join a in db.Set<SupplierAreaService>() on new { o.TenantId, o.SupplierId, o.PropertyId, Id = o.AreaServiceId } equals new { a.TenantId, a.SupplierId, a.PropertyId, a.Id }
                   join v in db.Set<BuildingFlow.Domain.SupplierService>() on new { a.TenantId, a.SupplierId, Id = a.ServiceId } equals new { v.TenantId, v.SupplierId, v.Id }
                   where db.Set<SupplierManagementArea>().Any(m => m.TenantId == tenant && m.SupplierId == o.SupplierId && m.PropertyId == property && m.IsEnabled)
                   select new { Record = o, SupplierName = s.Name, PropertyName = p.Name, ServiceName = v.Name, PhotoCount = db.Set<SupplierWorkOrderPhoto>().Count(photo => photo.TenantId == tenant && photo.SupplierId == o.SupplierId && photo.PropertyId == property && photo.WorkOrderId == o.Id) };
        if (filter.HasValue) rows = rows.Where(x => x.Record.Status == filter.Value);
        var count = await rows.CountAsync(token); var items = await rows.OrderByDescending(x => x.Record.ModifiedAt).ThenBy(x => x.Record.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items.Select(x => new SupplierWorkOrderRow(OrderResponse(x.Record, x.PropertyName, x.ServiceName, x.PhotoCount), x.SupplierName)).ToArray(), page, pageSize, count);
    }
}
