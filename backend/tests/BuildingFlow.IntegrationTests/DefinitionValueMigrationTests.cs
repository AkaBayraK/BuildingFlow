using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// TNM 02: upgrade, repeat, empty rollback and the populated-rollback guard on an isolated schema.
public sealed class DefinitionValueMigrationTests
{
    [PostgresFact] public async Task DefinitionValuesUpgradeRepeatEmptyRollbackAndPopulatedRollbackGuard()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!; var schema = "definition_values_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync(); await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", schema)).AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options); var migrations = db.Database.GetMigrations().ToList(); var target = migrations.Single(x => x.EndsWith("_DefinitionValues", StringComparison.Ordinal)); var previous = migrations[migrations.IndexOf(target) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); LegacySchema.AssertCurrentModelMatchesMigrations();
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
            var value = new DefinitionValue("REQUEST_CATEGORY", "ELEVATOR"); value.Set("Asansör", null, "wrench", "red", 3, true);
            db.Add(value); await db.SaveChangesAsync();
            var duplicate = new DefinitionValue("REQUEST_CATEGORY", "ELEVATOR"); duplicate.Set(null, null, null, null, 0, true); db.Add(duplicate);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            await using (var bad = new NpgsqlCommand($"UPDATE \"{schema}\".definition_values SET sort_order=1000", admin)) await Assert.ThrowsAsync<PostgresException>(() => bad.ExecuteNonQueryAsync());
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous)); Assert.True(await db.Set<DefinitionValue>().AnyAsync(x => x.Id == value.Id));
            await using var columns = new NpgsqlCommand("SELECT count(*) FROM information_schema.columns WHERE table_schema=@schema AND table_name='definition_values' AND column_name IN ('id','version','created_at','created_by','modified_at','modified_by','is_deleted','is_enabled') AND is_nullable='NO'", admin); columns.Parameters.AddWithValue("schema", schema); Assert.Equal(8L, await columns.ExecuteScalarAsync());
        }
        finally { await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await drop.ExecuteNonQueryAsync(); }
    }
    private sealed class Actor : ICurrentUserService { public string? UserId => "fixture"; public string? Username => "fixture"; public bool IsAuthenticated => true; public bool IsInRole(string role) => role == "SUPER_ADMIN"; }
}
