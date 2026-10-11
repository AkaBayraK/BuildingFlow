using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class MigrationTests
{
    [Fact]
    public void CurrentModelHasNoUnrecordedSchemaChanges()
    {
        using var db = new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [PostgresFact]
    public Task FreshInstallationAndRepeatedApplicationKeepExistingData() => VerifyMigrationSequence(false);

    [PostgresFact]
    public Task UpgradeFromInitialFoundationPreservesExistingData() => VerifyMigrationSequence(true);

    [PostgresFact]
    public async Task IdempotentScriptAppliesTwiceOnlyWithTerminatedRawSql()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "migration_script_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" }.ConnectionString;
            string Script(bool terminated)
            {
                var options = new DbContextOptionsBuilder<BuildingFlowDbContext>();
                if (terminated) options.UseBuildingFlowNpgsql(connection, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema));
                else options.UseNpgsql(connection, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema));
                using var context = new BuildingFlowDbContext(options.Options);
                return context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            }
            await using var target = new NpgsqlConnection(connection);
            await target.OpenAsync();
            var unpatched = await Assert.ThrowsAsync<PostgresException>(async () => { await using var c = new NpgsqlCommand(Script(false), target); await c.ExecuteNonQueryAsync(); });
            Assert.Equal(PostgresErrorCodes.SyntaxError, unpatched.SqlState);
            await using (var rollback = new NpgsqlCommand("ROLLBACK", target))
                await rollback.ExecuteNonQueryAsync();
            var script = Script(true);
            for (var pass = 0; pass < 2; pass++)
            {
                await using var apply = new NpgsqlCommand(script, target);
                await apply.ExecuteNonQueryAsync();
            }
            await using var db = new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>()
                .UseBuildingFlowNpgsql(connection, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options);
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(0, await db.Properties.CountAsync());
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin);
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    private static async Task VerifyMigrationSequence(bool upgrade)
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "migration_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            await using var db = new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>()
                .UseNpgsql(connection.ConnectionString, options => options.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .Options);
            var migrations = db.Database.GetMigrations().ToArray();
            Assert.True(migrations.Length >= 2);
            if (upgrade)
            {
                await db.GetService<IMigrator>().MigrateAsync(migrations[0]);
                Assert.Equal(migrations.Take(1), await db.Database.GetAppliedMigrationsAsync());
            }
            else await db.Database.MigrateAsync();

            var id = Guid.NewGuid();
            var name = "Migration preservation test";
            var actor = "migration-test";
            var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            // The old table name is used only before the rename migration.
            if(upgrade) await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO sites (id,name,city,district,address,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
                VALUES ({id},{name},{"City"},{"District"},{"Address"},{7L},{timestamp},{actor},{timestamp},{actor},{false},{false})
                """);
            else await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO properties (id,name,city,district,address,type,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
                VALUES ({id},{name},{"City"},{"District"},{"Address"},{"SITE"},{7L},{timestamp},{actor},{timestamp},{actor},{false},{false})
                """);
            await db.Database.MigrateAsync();
            Assert.Equal(migrations, await db.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            var site = await db.Properties.AsNoTracking().SingleAsync(item => item.Id == id);
            Assert.Equal(name, site.Name);
            Assert.Equal(7, site.Version);
            Assert.Equal(actor, site.CreatedBy);
            Assert.Equal(actor, site.ModifiedBy);
            Assert.Equal(timestamp, site.CreatedAt);
            Assert.Equal(timestamp, site.ModifiedAt);
            Assert.False(site.IsEnabled);
            Assert.Empty(await db.PropertyMemberships.ToListAsync());

            // Applying the same sequence again must neither duplicate history nor alter records.
            await db.Database.MigrateAsync();
            Assert.Equal(migrations, await db.Database.GetAppliedMigrationsAsync());
            Assert.Equal(1, await db.Properties.CountAsync());
            var repeated = await db.Properties.AsNoTracking().SingleAsync(item => item.Id == id);
            Assert.Equal(site.Name, repeated.Name);
            Assert.Equal(site.Version, repeated.Version);
            Assert.Equal(site.ModifiedAt, repeated.ModifiedAt);
        }
        finally
        {
            // Only the schema created by this test is removed, in the dedicated test database.
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin);
            await cleanup.ExecuteNonQueryAsync();
        }
    }
}
