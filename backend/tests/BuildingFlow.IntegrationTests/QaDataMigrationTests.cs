using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// QA test verisi kayıt defteri: upgrade/tekrar, audit kolonları, demo temizlenemez check'i, tekil kayıt ve Down guard.
public sealed class QaDataMigrationTests
{
    [PostgresFact]
    public async Task UpgradeEnforcesRegistryConstraintsAndDownGuard()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "qa_data_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options);
            var migrations = db.Database.GetMigrations().ToList();
            var previous = migrations[migrations.IndexOf(migrations.Single(x => x.EndsWith("_QaCenterData", StringComparison.Ordinal))) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            foreach (var table in new[] { "qa_data_batches", "qa_data_records" })
            {
                var columns = await db.Database.SqlQuery<string>($"SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();
                Assert.All(new[] { "id", "version", "created_at", "created_by", "modified_at", "modified_by", "is_deleted", "is_enabled" }, column => Assert.Contains(column, columns));
            }
            var batch = new QaDataBatch("qa-data-1", QaDataKind.DEMO, "Development", Guid.NewGuid(), null, DateTimeOffset.UtcNow);
            db.QaDataBatches.Add(batch); db.QaDataRecords.Add(new QaDataRecord(batch.Id, "Property", Guid.NewGuid(), 5)); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE qa_data_batches SET status='CLEANED' WHERE id={batch.Id}"));
            var duplicate = await db.QaDataRecords.AsNoTracking().FirstAsync();
            db.QaDataRecords.Add(new QaDataRecord(batch.Id, duplicate.EntityType, duplicate.EntityId, 5));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous));
            Assert.EndsWith("_QaCenterData", (await db.Database.GetAppliedMigrationsAsync()).Last());
            await db.Database.MigrateAsync();
            Assert.Equal(migrations.Last(), (await db.Database.GetAppliedMigrationsAsync()).Last());
        }
        finally { await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync(); }
    }

    private sealed class Actor : ICurrentUserService
    {
        public string? UserId => "qa-data-test";
        public string? Username => "fixture";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
    }
}
