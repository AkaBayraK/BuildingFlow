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
