using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class PaymentStore(BuildingFlowDbContext db, TimeProvider clock) : IPaymentStore
{
    private IQueryable<Property> Properties(AccessScope access)
    {
        var parents = db.Properties.Where(value => !value.IsDeleted);
        return access.ReadAll ? parents : parents.Where(value => db.PropertyMemberships.Any(member =>
            !member.IsDeleted && member.PropertyId == value.Id && member.UserId == access.UserId && member.IsEnabled));
    }
    private IQueryable<Payment> Payments(Guid property, AccessScope access) => db.Payments.AsNoTracking().Where(value =>
        !value.IsDeleted && value.PropertyId == property && Properties(access).Any(parent => parent.Id == property));
    private async Task<Property?> LockProperty(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        return await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token);
    }
    private void Touch(BaseEntity entity) => db.Entry(entity).Property(value => value.IsEnabled).IsModified = true;
    private static void Active(Property property)
    {
        if (!property.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.PaymentsInactiveProperty.Create());
    }
    private static string Fingerprint(PaymentCommand command)
    {
        var content = JsonSerializer.Serialize(new {
            command.UnitId, amount = DuesService.Money(command.Amount), date = command.PaidDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            method = command.Method.ToString(), command.Reference, command.Description,
            allocations = command.Allocations.OrderBy(value => value.ChargeId).Select(value => new { value.ChargeId, amount = DuesService.Money(value.Amount), value.Version })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
    public async Task<PaymentResponse?> CreateAsync(Guid property, PaymentCommand command, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, access, token); if (parent is null) return null;
        var fingerprint = Fingerprint(command);
        var previous = await db.Payments.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == command.RequestId, token);
        if (previous is not null)
        {
            if (previous.RequestFingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.PaymentsErrorRequestConflict.Create());
            await transaction.CommitAsync(token);
            var replay = await GetAsync(property, previous.Id, access, token); return replay is null ? null : replay with { Replayed = true };
        }
        Active(parent);
        if (command.PaidDate > BusinessDate.Today(clock)) throw new RequestValidationException(new() { ["paidDate"] = [ErrorCatalog.PaymentsErrorFutureDate.Create()] });
        // Historical charges of disabled/deleted units can be settled. Property scope remains explicit.
        if (!await db.Units.IgnoreQueryFilters().AnyAsync(value => value.PropertyId == property && value.Id == command.UnitId, token)) return null;
        var ids = command.Allocations.Select(value => value.ChargeId).ToArray();
        var charges = await db.DuesCharges.Where(value => value.PropertyId == property && value.UnitId == command.UnitId && ids.Contains(value.Id)).ToListAsync(token);
        if (charges.Count != ids.Length) return null;
        foreach (var allocation in command.Allocations)
        {
            var charge = charges.Single(value => value.Id == allocation.ChargeId);
            if (charge.Version != allocation.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
            if (charge.IsCancelled || allocation.Amount > charge.Balance) throw new HierarchyConflictException(ErrorCatalog.PaymentsErrorBalance.Create());
        }
        var payment = new Payment(property, command.UnitId, command.RequestId, fingerprint, command.Amount, "TRY", command.PaidDate, command.Method, command.Reference, command.Description);
        foreach (var allocation in command.Allocations) payment.Allocate(charges.Single(value => value.Id == allocation.ChargeId), allocation.Amount);
        payment.ValidateComplete(); db.Payments.Add(payment); Touch(parent);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new HierarchyConflictException(ErrorCatalog.PaymentsErrorUnique.Create()); }
        await transaction.CommitAsync(token);
        return await GetAsync(property, payment.Id, access, token);
    }
    private sealed class Row
    {
        public required Payment Payment { get; init; }
        public required string UnitNumber { get; init; }
        public Guid? BuildingId { get; init; }
        public string? BuildingName { get; init; }
    }
    private IQueryable<Row> Rows(IQueryable<Payment> payments, Guid property) =>
        from payment in payments
        join unit in db.Units.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on payment.UnitId equals unit.Id
        join building in db.Buildings.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on unit.BuildingId equals (Guid?)building.Id into parents
        from building in parents.DefaultIfEmpty()
        select new Row { Payment = payment, UnitNumber = unit.Number, BuildingId = unit.BuildingId, BuildingName = building == null ? null : building.Name };
    private sealed record AllocationRow(Guid PaymentId, Guid Id, Guid ChargeId, DateOnly Period, decimal Amount);
    private async Task<List<AllocationRow>> Allocations(Guid property, Guid[] payments, CancellationToken token) => await (
        from allocation in db.PaymentAllocations.AsNoTracking().Where(value => value.PropertyId == property && payments.Contains(value.PaymentId))
        join charge in db.DuesCharges.AsNoTracking().Where(value => value.PropertyId == property) on allocation.ChargeId equals charge.Id
        select new AllocationRow(allocation.PaymentId, allocation.Id, allocation.ChargeId, charge.Period, allocation.Amount)).ToListAsync(token);
    private static PaymentResponse Response(Row row, IEnumerable<AllocationRow> allocations)
    {
        var value = row.Payment;
        return new(value.Id, value.PropertyId, value.UnitId, row.BuildingId, row.UnitNumber, row.BuildingName, value.RequestId,
            DuesService.Money(value.Amount), value.Currency, value.PaidDate, value.Method.ToString(), value.Reference, value.Description,
            value.IsCancelled ? "CANCELLED" : "RECORDED", value.Version, value.CreatedAt, value.CancellationReason, value.CancelledAt,
            allocations.Where(part => part.PaymentId == value.Id).OrderBy(part => part.Period).ThenBy(part => part.ChargeId)
                .Select(part => new PaymentAllocationResponse(part.Id, part.ChargeId, part.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture), DuesService.Money(part.Amount))).ToArray(), false);
    }
    public async Task<PaymentResponse?> GetAsync(Guid property, Guid payment, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        var row = await Rows(Payments(property, access).Where(value => value.Id == payment), property).SingleOrDefaultAsync(token);
        if (row is null) return null;
        var parts = await Allocations(property, [payment], token);
        await transaction.CommitAsync(token); return Response(row, parts);
    }
    public async Task<PaymentPage?> ListAsync(Guid property, PaymentQuery query, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        var payments = Payments(property, access);
        if (query.UnitId is not null) payments = payments.Where(value => value.UnitId == query.UnitId);
        if (query.ChargeId is not null) payments = payments.Where(value => db.PaymentAllocations.Any(part => part.PropertyId == property && part.PaymentId == value.Id && part.ChargeId == query.ChargeId));
        if (query.From is not null) payments = payments.Where(value => value.PaidDate >= query.From);
        if (query.To is not null) payments = payments.Where(value => value.PaidDate <= query.To);
        if (query.Status == "RECORDED") payments = payments.Where(value => !value.IsCancelled);
        if (query.Status == "CANCELLED") payments = payments.Where(value => value.IsCancelled);
        if (!string.IsNullOrEmpty(query.Method)) { var method = Enum.Parse<PaymentMethod>(query.Method); payments = payments.Where(value => value.Method == method); }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            payments = payments.Where(value => EF.Functions.ILike(value.Description, pattern, "\\") || (value.Reference != null && EF.Functions.ILike(value.Reference, pattern, "\\")) ||
                db.Units.IgnoreQueryFilters().Any(unit => unit.PropertyId == property && unit.Id == value.UnitId && (EF.Functions.ILike(unit.Number, pattern, "\\") ||
                    db.Buildings.IgnoreQueryFilters().Any(building => building.PropertyId == property && building.Id == unit.BuildingId && EF.Functions.ILike(building.Name, pattern, "\\")))));
        }
        var count = await payments.CountAsync(token);
        var sums = await payments.GroupBy(_ => 1).Select(group => new {
            Recorded = group.Sum(value => value.IsCancelled ? 0m : value.Amount), Cancelled = group.Sum(value => value.IsCancelled ? value.Amount : 0m),
            RecordedCount = group.Count(value => !value.IsCancelled), CancelledCount = group.Count(value => value.IsCancelled)
        }).SingleOrDefaultAsync(token);
        var rows = Rows(payments, property); var desc = query.SortDirection == "desc";
        var ordered = query.SortBy switch {
            "amount" => desc ? rows.OrderByDescending(value => value.Payment.Amount) : rows.OrderBy(value => value.Payment.Amount),
            "createdAt" => desc ? rows.OrderByDescending(value => value.Payment.CreatedAt) : rows.OrderBy(value => value.Payment.CreatedAt),
            _ => desc ? rows.OrderByDescending(value => value.Payment.PaidDate) : rows.OrderBy(value => value.Payment.PaidDate)
        };
        var items = await ordered.ThenBy(value => value.Payment.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        var parts = await Allocations(property, items.Select(value => value.Payment.Id).ToArray(), token);
        await transaction.CommitAsync(token);
        return new(items.Select(value => Response(value, parts)).ToArray(), query.Page, query.PageSize, count,
            new(DuesService.Money(sums?.Recorded ?? 0m), DuesService.Money(sums?.Cancelled ?? 0m), sums?.RecordedCount ?? 0, sums?.CancelledCount ?? 0, "TRY"));
    }
    public async Task<PaymentResponse?> CancelAsync(Guid property, Guid payment, CancelChargeRequest request, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, access, token); if (parent is null) return null;
        Active(parent);
        var value = await db.Payments.Include(item => item.Allocations).SingleOrDefaultAsync(item => item.PropertyId == property && item.Id == payment, token);
        if (value is null) return null;
        if (value.Version != request.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if (value.IsCancelled) throw new HierarchyConflictException(ErrorCatalog.PaymentsErrorCancelled.Create());
        var ids = value.Allocations.Select(part => part.ChargeId).ToArray();
        var charges = await db.DuesCharges.Where(charge => charge.PropertyId == property && charge.UnitId == value.UnitId && ids.Contains(charge.Id)).ToListAsync(token);
        foreach (var part in value.Allocations) charges.Single(charge => charge.Id == part.ChargeId).ReversePayment(part.Amount);
        value.Cancel(request.Reason!, clock.GetUtcNow()); Touch(parent);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return await GetAsync(property, payment, access, token);
    }
}
