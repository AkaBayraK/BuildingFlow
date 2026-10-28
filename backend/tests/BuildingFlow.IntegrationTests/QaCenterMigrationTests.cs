using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Test Merkezi: upgrade and repeat apply, audit columns, active-scenario/step uniqueness, status checks and Down guard.
public sealed class QaCenterMigrationTests
{
    private static readonly string[] AuditColumns = ["id", "version", "created_at", "created_by", "modified_at", "modified_by", "is_deleted", "is_enabled"];

    [PostgresFact]
    public async Task UpgradeEnforcesConstraintsAndDownGuard()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "qa_center_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options);
            var migrations = db.Database.GetMigrations().ToList();
            var previous = migrations[migrations.IndexOf(migrations.Single(x => x.EndsWith("_QaCenterRuns", StringComparison.Ordinal))) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            Assert.Equal(migrations.Last(), (await db.Database.GetAppliedMigrationsAsync()).Last());
            foreach (var table in new[] { "qa_runs", "qa_run_steps" })
            {
                var columns = await db.Database.SqlQuery<string>($"SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();
                Assert.All(AuditColumns, column => Assert.Contains(column, columns));
            }
            var now = DateTimeOffset.UtcNow;
            var run = new QaRun("qa-migration-1", "db.migrations", Guid.NewGuid(), 60, now);
            db.QaRuns.Add(run); await db.SaveChangesAsync();
            await AssertRejected(db, new QaRun("qa-migration-2", "db.migrations", Guid.NewGuid(), 60, now));
            await AssertRejected(db, new QaRun("qa-migration-1", "api.authorization", Guid.NewGuid(), 60, now));
            run.Start(now); db.QaRunSteps.Add(new QaRunStep(run.Id, 1, "database.connect", QaStepStatus.PASSED, "true", "true", 1)); await db.SaveChangesAsync();
            await AssertRejected(db, new QaRunStep(run.Id, 1, "duplicate", QaStepStatus.PASSED, "a", "b", 1));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE qa_runs SET status='DONE' WHERE id={run.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous));
            // EF commits each completed migration separately: later empty Downs may complete before this Down refuses.
            Assert.EndsWith("_QaCenterRuns", (await db.Database.GetAppliedMigrationsAsync()).Last());
            await db.Database.MigrateAsync();
            Assert.Equal(migrations.Last(), (await db.Database.GetAppliedMigrationsAsync()).Last());
            LegacySchema.AssertCurrentModelMatchesMigrations();
        }
        finally { await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync(); }
    }

    private static async Task AssertRejected(BuildingFlowDbContext db, object entity)
    {
        db.Add(entity);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList().ForEach(e => e.State = EntityState.Detached);
    }

    private sealed class Actor : ICurrentUserService
    {
        public string? UserId => "qa-center-test";
        public string? Username => "fixture";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
    }
}
