using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BuildingFlow.Api;

public sealed record CreateQaDataBatchRequest(string? Kind, Guid RequestId, string? RunCode, int Blocks = 2, int UnitsPerBlock = 4, bool? WithFinance = null);
public sealed record QaDataBatchResponse(Guid Id, string Code, string Kind, string Environment, string? RunCode, string Status, int RecordCount,
    DateTimeOffset GeneratedAt, DateTimeOffset? CleanedAt, Guid? PropertyId, long Version);
public sealed record QaCleanupItem(int Order, string EntityType, int Count);
public sealed record QaCleanupPreview(Guid BatchId, string Code, string Kind, bool CanClean, IReadOnlyList<QaCleanupItem> Items, IReadOnlyList<string> Blockers, string Confirmation, long Version);
public sealed record QaCleanupRequest(long Version, string? Confirmation);

// Synthetic data for demos and tests. Cleanup only soft-deletes rows this batch registered, in dependency order and in
// one transaction; demo batches, finance history and rows added later by users block it instead of being touched.
public sealed class QaDataService(BuildingFlowDbContext db, IHostEnvironment environment, TimeProvider clock, DuesService dues, PaymentService payments)
{
    private static readonly string[] FirstNames = ["Ayşe", "Mehmet", "Zeynep", "Can", "Elif", "Burak", "Deniz", "Selin", "Emre", "Derya"];
    private static readonly string[] LastNames = ["Demo", "Örnek", "Deneme", "Sentetik"];
    private static readonly (string Type, int Order)[] Order = [("UnitOccupancy", 1), ("Person", 2), ("Unit", 3), ("Building", 4), ("Property", 5)];

    public async Task<IReadOnlyList<QaDataBatchResponse>> List(CancellationToken token)
    {
        var batches = await db.QaDataBatches.AsNoTracking().OrderByDescending(batch => batch.GeneratedAt).Take(50).ToListAsync(token);
        var ids = batches.Select(batch => batch.Id).ToList();
        var properties = await db.QaDataRecords.AsNoTracking().Where(record => ids.Contains(record.BatchId) && record.EntityType == "Property")
            .ToDictionaryAsync(record => record.BatchId, record => record.EntityId, token);
        return batches.Select(batch => Map(batch, properties.TryGetValue(batch.Id, out var id) ? id : null)).ToList();
    }

    public async Task<(QaDataBatchResponse Batch, bool Created)> Create(CreateQaDataBatchRequest request, CancellationToken token)
    {
        if (request.RequestId == Guid.Empty || !Enum.TryParse<QaDataKind>(request.Kind, false, out var kind) || !Enum.IsDefined(kind)
            || request.Blocks is < 1 or > 5 || request.UnitsPerBlock is < 1 or > 20 || request.RunCode is { Length: > 40 })
            throw new ArgumentException("Invalid QA data request.");
        if (await db.QaDataBatches.AsNoTracking().FirstOrDefaultAsync(batch => batch.RequestId == request.RequestId, token) is { } existing)
            return existing.Kind == kind ? (Map(existing, await PropertyOf(existing.Id, token)), false) : throw new HierarchyConflictException(ErrorCatalog.QaRequestReused.Create());
        var now = clock.GetUtcNow();
        var suffix = Convert.ToHexString(Guid.NewGuid().ToByteArray(), 0, 3).ToLowerInvariant();
        var batch = new QaDataBatch($"qa-data-{now.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}-{suffix}", kind, environment.EnvironmentName, request.RequestId, request.RunCode, now);
        var property = new Property($"{(kind == QaDataKind.DEMO ? "QA Demo" : "QA Test")} Sitesi {suffix}", "İstanbul", "Kadıköy", "Sentetik Cad. No:1", PropertyType.SITE);
        var records = new List<QaDataRecord> { new(batch.Id, "Property", property.Id, 5) };
        var people = new List<Person>(); var occupancies = new List<UnitOccupancy>();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        for (var b = 0; b < request.Blocks; b++)
        {
            var building = property.AddBuilding($"{(char)('A' + b)} Blok", (request.UnitsPerBlock + 1) / 2);
            records.Add(new(batch.Id, "Building", building.Id, 4));
            for (var u = 0; u < request.UnitsPerBlock; u++)
            {
                var unit = building.AddUnit($"{(char)('A' + b)}-{u + 1}", u / 2, 95 + u * 5);
                records.Add(new(batch.Id, "Unit", unit.Id, 3));
                var index = b * request.UnitsPerBlock + u;
                var owner = new Person(property.Id, FirstNames[index % FirstNames.Length], LastNames[index % LastNames.Length], null, $"demo.{suffix}.{index}@example.test");
                people.Add(owner); occupancies.Add(new UnitOccupancy(property.Id, unit.Id, owner.Id, OccupancyType.OWNER, today));
                if (u % 2 == 1)
                {
                    var tenant = new Person(property.Id, FirstNames[(index + 5) % FirstNames.Length], "Kiracı", null, $"demo.{suffix}.{index}.t@example.test");
                    people.Add(tenant); occupancies.Add(new UnitOccupancy(property.Id, unit.Id, tenant.Id, OccupancyType.TENANT, today));
                }
            }
        }
        records.AddRange(people.Select(person => new QaDataRecord(batch.Id, "Person", person.Id, 2)));
        records.AddRange(occupancies.Select(occupancy => new QaDataRecord(batch.Id, "UnitOccupancy", occupancy.Id, 1)));
        batch.Registered(records.Count);
        db.AddRange(batch, property); db.AddRange(people); db.AddRange(occupancies); db.AddRange(records);
        await db.SaveChangesAsync(token);
        // Finance rows are never deleted, so only protected DEMO batches get them (TEST stays cleanable).
        if (kind == QaDataKind.DEMO && request.WithFinance != false) await SeedFinance(batch, property, today, token);
        return (Map(batch, property.Id), true);
    }

    private async Task SeedFinance(QaDataBatch batch, Property property, DateOnly today, CancellationToken token)
    {
        var units = property.Buildings.SelectMany(building => building.Units).OrderBy(unit => unit.Number, StringComparer.Ordinal).ToList();
        var period = new DateOnly(today.Year, today.Month, 1);
        var assessment = await dues.CreateAsync(property.Id, new CreateAssessmentRequest(Guid.NewGuid(), units.Select(unit => unit.Id).ToList(), period.ToString("yyyy-MM"),
            "1500.00", "TRY", period.AddDays(14), "Demo aidat"), token) ?? throw new InvalidOperationException("Demo dues were not created.");
        var records = new List<QaDataRecord> { new(batch.Id, "DuesAssessment", assessment.Id, 90) };
        var charges = await db.DuesCharges.AsNoTracking().Where(charge => charge.AssessmentId == assessment.Id).OrderBy(charge => charge.UnitId).ToListAsync(token);
        foreach (var charge in charges.Where((_, index) => index % 2 == 0))
        {
            var paid = await payments.CreateAsync(property.Id, new CreatePaymentRequest(Guid.NewGuid(), charge.UnitId, "1500.00", "TRY", today, "CASH", null, "Demo tahsilat",
                [new AllocationRequest(charge.Id, "1500.00", charge.Version)]), token) ?? throw new InvalidOperationException("Demo payment was not created.");
            records.Add(new(batch.Id, "Payment", paid.Id, 91));
        }
        var tracked = await db.QaDataBatches.SingleAsync(value => value.Id == batch.Id, token);
        tracked.Registered(tracked.RecordCount + records.Count);
        db.QaDataRecords.AddRange(records);
        await db.SaveChangesAsync(token);
    }

    public async Task<QaCleanupPreview?> Preview(Guid id, CancellationToken token)
    {
        var batch = await db.QaDataBatches.AsNoTracking().FirstOrDefaultAsync(value => value.Id == id, token);
        if (batch is null) return null;
        var records = await db.QaDataRecords.AsNoTracking().Where(record => record.BatchId == id).ToListAsync(token);
        var items = Order.Select(o => new QaCleanupItem(o.Order, o.Type, records.Count(r => r.EntityType == o.Type))).Where(item => item.Count > 0).ToList();
        var blockers = new List<string>();
        if (batch.Kind == QaDataKind.DEMO) blockers.Add("DEMO_PROTECTED");
        if (batch.Status != QaDataBatchStatus.ACTIVE) blockers.Add("ALREADY_CLEANED");
        if (records.FirstOrDefault(r => r.EntityType == "Property") is { } property && batch.Status == QaDataBatchStatus.ACTIVE)
        {
            var p = property.EntityId;
            if (await db.DuesCharges.IgnoreQueryFilters().AnyAsync(x => x.PropertyId == p, token) || await db.Payments.IgnoreQueryFilters().AnyAsync(x => x.PropertyId == p, token)
                || await db.Expenses.IgnoreQueryFilters().AnyAsync(x => x.PropertyId == p, token))
                blockers.Add("FINANCE_HISTORY");
            var known = records.Select(r => r.EntityId).ToHashSet();
            if (await db.Units.Where(x => x.PropertyId == p).Select(x => x.Id).AnyAsync(x => !known.Contains(x), token)
                || await db.People.Where(x => x.PropertyId == p).Select(x => x.Id).AnyAsync(x => !known.Contains(x), token)
                || await db.UnitOccupancies.Where(x => x.PropertyId == p).Select(x => x.Id).AnyAsync(x => !known.Contains(x), token))
                blockers.Add("UNREGISTERED_ROWS");
        }
        return new QaCleanupPreview(batch.Id, batch.Code, batch.Kind.ToString(), blockers.Count == 0, items, blockers, "TEMIZLE " + batch.Code, batch.Version);
    }

    public async Task<QaDataBatchResponse?> Clean(Guid id, QaCleanupRequest request, CancellationToken token)
    {
        var preview = await Preview(id, token);
        if (preview is null) return null;
        if (preview.Kind == nameof(QaDataKind.DEMO)) throw new HierarchyConflictException(ErrorCatalog.QaDataProtected.Create());
        if (preview.Version != request.Version || preview.Blockers.Contains("ALREADY_CLEANED")) throw new HierarchyConflictException(ErrorCatalog.QaDataState.Create());
        if (!string.Equals(request.Confirmation, preview.Confirmation, StringComparison.Ordinal)) throw new HierarchyConflictException(ErrorCatalog.QaDataConfirm.Create());
        if (!preview.CanClean) throw new HierarchyConflictException(ErrorCatalog.QaDataBlocked.Create());
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var batch = await db.QaDataBatches.SingleAsync(value => value.Id == id, token);
        var records = await db.QaDataRecords.Where(record => record.BatchId == id).OrderBy(record => record.CleanupOrder).ToListAsync(token);
        foreach (var group in records.GroupBy(record => record.CleanupOrder).OrderBy(group => group.Key))
        {
            var ids = group.Select(record => record.EntityId).ToList();
            object[] rows = group.First().EntityType switch
            {
                "UnitOccupancy" => [.. await db.UnitOccupancies.Where(x => ids.Contains(x.Id)).ToListAsync(token)],
                "Person" => [.. await db.People.Where(x => ids.Contains(x.Id)).ToListAsync(token)],
                "Unit" => [.. await db.Units.Where(x => ids.Contains(x.Id)).ToListAsync(token)],
                "Building" => [.. await db.Buildings.Where(x => ids.Contains(x.Id)).ToListAsync(token)],
                "Property" => [.. await db.Properties.Where(x => ids.Contains(x.Id)).ToListAsync(token)],
                _ => throw new InvalidOperationException("Unknown QA data entity.")
            };
            db.RemoveRange(rows);
            await db.SaveChangesAsync(token);
        }
        batch.MarkCleaned(clock.GetUtcNow());
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return Map(batch, records.FirstOrDefault(r => r.EntityType == "Property")?.EntityId);
    }

    private async Task<Guid?> PropertyOf(Guid batchId, CancellationToken token) =>
        await db.QaDataRecords.AsNoTracking().Where(r => r.BatchId == batchId && r.EntityType == "Property").Select(r => (Guid?)r.EntityId).FirstOrDefaultAsync(token);

    private static QaDataBatchResponse Map(QaDataBatch batch, Guid? propertyId) => new(batch.Id, batch.Code, batch.Kind.ToString(), batch.Environment, batch.RunCode,
        batch.Status.ToString(), batch.RecordCount, batch.GeneratedAt, batch.CleanedAt, propertyId, batch.Version);
}

internal static class QaDataEndpoints
{
    public static void MapQaDataEndpoints(this RouteGroupBuilder root)
    {
        var data = root.MapGroup("/data/batches");
        data.MapGet("", async (QaDataService service, CancellationToken token) => Results.Ok(await service.List(token)));
        data.MapPost("", async (CreateQaDataBatchRequest request, QaDataService service, CancellationToken token) =>
            await service.Create(request, token) is var (batch, created) && created ? Results.Created($"/api/v1/qa-center/data/batches/{batch.Id}", batch) : Results.Ok(batch));
        data.MapGet("/{batchId:guid}/cleanup-preview", async (Guid batchId, QaDataService service, CancellationToken token) =>
            await service.Preview(batchId, token) is { } preview ? Results.Ok(preview) : Results.NotFound());
        data.MapPost("/{batchId:guid}/cleanup", async (Guid batchId, QaCleanupRequest request, QaDataService service, CancellationToken token) =>
            await service.Clean(batchId, request, token) is { } batch ? Results.Ok(batch) : Results.NotFound());
    }
}
