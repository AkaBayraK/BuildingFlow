using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class CommercialConsentStoreTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly string Sha = new('b', 64);
    private const string Phone = "0555 123 45 67";

    [PostgresFact] public async Task NewestDecisionWinsAndHistoryIsKept()
    {
        var sender = await Sender();
        await factory.WithDb(async db =>
        {
            var store = new CommercialConsentStore(db, TimeProvider.System);
            Assert.Null(await store.CurrentAsync(sender, Phone, default));
            var approved = await store.RecordAsync(Approve(sender), default);
            Assert.Equal("APPROVED", approved.Status); Assert.Equal("+90********67", approved.RecipientMasked);
            await store.RecordAsync(new(sender, Phone, false, CommercialConsentStatus.REJECTED, CommercialConsentSource.SMS_REPLY, Guid.NewGuid()), default);
            Assert.Equal("REJECTED", (await store.CurrentAsync(sender, "+905551234567", default))!.Status);
            var history = await store.HistoryAsync(sender, Phone, 1, 10, default);
            Assert.Equal(2, history.TotalCount); Assert.Equal(["REJECTED", "APPROVED"], history.Items.Select(x => x.Status));
            Assert.Null(await store.CurrentAsync(sender, "+905559999999", default));
        });
    }

    [PostgresFact] public async Task OperationReplayReturnsSameRowAndDifferentContentConflicts()
    {
        var sender = await Sender();
        await factory.WithDb(async db =>
        {
            var store = new CommercialConsentStore(db, TimeProvider.System);
            var command = Approve(sender);
            var first = await store.RecordAsync(command, default);
            var replay = await store.RecordAsync(command with { Recipient = "+905551234567" }, default);
            Assert.True(replay.Replayed); Assert.Equal(first.RecordId, replay.RecordId);
            await Assert.ThrowsAsync<HierarchyConflictException>(() => store.RecordAsync(command with { Status = CommercialConsentStatus.REJECTED, TextVersion = null, TextSha256 = null }, default));
            Assert.Equal(1, await db.CommercialConsentRecords.CountAsync(x => x.SenderId == sender));
        });
    }

    [PostgresFact] public async Task ConcurrentDecisionsAreSerialisedPerSender()
    {
        var sender = await Sender();
        var tasks = Enumerable.Range(0, 6).Select(i => factory.WithDb(async db =>
        {
            var store = new CommercialConsentStore(db, TimeProvider.System);
            await store.RecordAsync(i % 2 == 0 ? Approve(sender) : new(sender, Phone, false, CommercialConsentStatus.REJECTED, CommercialConsentSource.WEB, Guid.NewGuid()), default);
        }));
        await Task.WhenAll(tasks);
        await factory.WithDb(async db =>
        {
            var rows = await db.CommercialConsentRecords.Where(x => x.SenderId == sender).OrderBy(x => x.OccurredAt).ThenBy(x => x.CreatedAt).ToListAsync();
            Assert.Equal(6, rows.Count); Assert.Equal(6, rows.Select(x => x.OccurredAt).Distinct().Count());
            var current = await new CommercialConsentStore(db, TimeProvider.System).CurrentAsync(sender, Phone, default);
            Assert.Equal(rows[^1].Id, current!.RecordId);
        });
    }

    [PostgresFact] public async Task DecisionsAreAppendOnlyButIysStateMayAdvance()
    {
        var sender = await Sender(iys: true);
        Guid id = default;
        await factory.WithDb(async db => id = (await new CommercialConsentStore(db, TimeProvider.System).RecordAsync(Approve(sender), default)).RecordId);
        await factory.WithDb(async db =>
        {
            var record = await db.CommercialConsentRecords.SingleAsync(x => x.Id == id);
            Assert.Equal(IysSyncStatus.PENDING, record.IysStatus);
            record.MarkIysSynced("IYS-REF-1", DateTimeOffset.UtcNow); await db.SaveChangesAsync();
        });
        await factory.WithDb(async db =>
        {
            var update = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"UPDATE commercial_consent_records SET status = 'REJECTED' WHERE id = {id}"));
            Assert.Contains("immutable", update.MessageText);
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"UPDATE commercial_consent_records SET is_deleted = true WHERE id = {id}"));
            var delete = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"DELETE FROM commercial_consent_records WHERE id = {id}"));
            Assert.Contains("append-only", delete.MessageText);
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"UPDATE commercial_consent_records SET text_sha256 = NULL WHERE id = {id}"));
            var stored = await db.CommercialConsentRecords.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal(CommercialConsentStatus.APPROVED, stored.Status); Assert.Equal("IYS-REF-1", stored.IysReference);
        });
    }

    [PostgresFact] public async Task DatabaseRejectsApprovalWithoutEvidenceAndSecondSenderPerScope()
    {
        var sender = await Sender();
        await factory.WithDb(async db =>
        {
            var property = await db.SmsLegalSenders.Where(x => x.Id == sender).Select(x => x.PropertyId).SingleAsync();
            db.SmsLegalSenders.Add(new SmsLegalSender(property, "İkinci", null, "c", "RET X", "4567", true, false, null));
            var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)duplicate.InnerException!).SqlState);
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.SmsLegalSenders.AddRange(Platform("A"), Platform("B"));
            var platform = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)platform.InnerException!).SqlState);
            await transaction.RollbackAsync(); db.ChangeTracker.Clear();
            var check = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"""
                INSERT INTO commercial_consent_records (id, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled, sender_id, channel, recipient,
                  is_recipient_verified, status, source, occurred_at, operation_id, evidence_sha256, iys_status)
                VALUES ({Guid.NewGuid()}, 0, now(), 'test', now(), 'test', false, true, {sender}, 'SMS', '+905551234567', false, 'APPROVED', 'SMS_REPLY', now(), {Guid.NewGuid()}, {Sha}, 'NOT_REQUIRED')
                """));
            Assert.Equal(PostgresErrorCodes.CheckViolation, check.SqlState);
        });
    }

    private static SmsLegalSender Platform(string title) => new(null, title, null, "c", "RET BF", "4567", true, false, null);
    private static CommercialConsentCommand Approve(Guid sender) =>
        new(sender, Phone, true, CommercialConsentStatus.APPROVED, CommercialConsentSource.WEB, Guid.NewGuid(), "user-1", null, 1, Sha);
    private async Task<Guid> Sender(bool iys = false)
    {
        var property = new Property("Consent " + Guid.NewGuid(), "City", "District", "Address", PropertyType.APARTMENT);
        var sender = new SmsLegalSender(property.Id, "Gül Sitesi Yönetimi", null, "yonetim@example.test", "RET GUL", "4567", true, iys, iys ? "GUL" : null);
        await factory.WithDb(async db => { db.Add(property); db.Add(sender); await db.SaveChangesAsync(); });
        return sender.Id;
    }
}

public sealed class CommercialConsentMigrationTests
{
    [PostgresFact] public async Task DownIsBlockedOnceConsentHistoryExists()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var config = new NpgsqlConnectionStringBuilder(original);
        if (config.Database is null || !config.Database.Contains("test", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Dedicated test database required.");
        var schema = "commercial_consent_migration_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            config.SearchPath = schema + ",public";
            await using var db = new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>()
                .UseNpgsql(config.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options);
            var migrations = db.Database.GetMigrations().ToList();
            var previous = migrations[migrations.FindIndex(v => v.EndsWith("_SmsCommercialConsents", StringComparison.Ordinal)) - 1];
            var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
            await db.Database.MigrateAsync();
            await migrator.MigrateAsync(previous); await db.Database.MigrateAsync();
            db.SmsLegalSenders.Add(new SmsLegalSender(null, "Platform", null, "c", "RET BF", "4567", true, false, null)); await db.SaveChangesAsync();
            var blocked = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(previous));
            Assert.Contains("must be preserved", blocked.MessageText);
            Assert.True(await db.SmsLegalSenders.AnyAsync());
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync();
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public string? UserId => "commercial-consent-migration";
        public string? Username => "fixture";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
    }
}
