using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class AutoMigrationTests
{
    [PostgresFact]
    public Task FreshDatabaseIsMigratedOnStartup() => WithSchema(async (connection, schema) =>
    {
        await StartApi(connection, true);
        await using var db = Context(connection, schema);
        Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal("integer", await ColumnType(db, schema, "units", "bathroom_count"));
    });

    [PostgresFact]
    public Task PendingMigrationIsAppliedOnStartupAndDataIsKept() => WithSchema(async (connection, schema) =>
    {
        await using var db = Context(connection, schema);
        var migrations = db.Database.GetMigrations().ToArray();
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        await db.Database.ExecuteSqlRawAsync(InsertProperty);
        Assert.Single(await db.Database.GetPendingMigrationsAsync());
        await StartApi(connection, true);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(1, await db.Properties.CountAsync());
    });

    [PostgresFact]
    public Task HandAddedColumnsAreAlignedInsteadOfFailing() => WithSchema(async (connection, schema) =>
    {
        await using var db = Context(connection, schema);
        await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().ToArray()[^2]);
        await db.Database.ExecuteSqlRawAsync(InsertProperty);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE properties ADD COLUMN construction_year bigint DEFAULT 0, ADD COLUMN neighborhood varchar(200) NOT NULL DEFAULT 'Moda'");
        await db.Database.ExecuteSqlRawAsync("UPDATE properties SET construction_year = NULL");
        await StartApi(connection, true);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal("integer", await ColumnType(db, schema, "properties", "construction_year"));
        Assert.Equal("character varying(100)|YES|", await db.Database.SqlQuery<string>($"""
            SELECT format_type(a.atttypid, a.atttypmod) || '|' || CASE WHEN a.attnotnull THEN 'NO' ELSE 'YES' END || '|' || coalesce(pg_get_expr(d.adbin, d.adrelid), '') AS "Value"
            FROM pg_attribute a JOIN pg_class c ON c.oid = a.attrelid JOIN pg_namespace n ON n.oid = c.relnamespace
            LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
            WHERE n.nspname = {schema} AND c.relname = 'properties' AND a.attname = 'neighborhood'
            """).SingleAsync());
        Assert.Equal("Moda", (await db.Properties.AsNoTracking().SingleAsync()).Neighborhood);
    });

    [PostgresFact]
    public Task IncompatibleHandAddedDataStopsStartupWithoutPartialSchema() => WithSchema(async (connection, schema) =>
    {
        await using var db = Context(connection, schema);
        var migrations = db.Database.GetMigrations().ToArray();
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        await db.Database.ExecuteSqlRawAsync(InsertProperty);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE properties ADD COLUMN neighborhood varchar(200); UPDATE properties SET neighborhood = repeat('x', 150)");
        await Assert.ThrowsAnyAsync<Exception>(() => StartApi(connection, true));
        Assert.Equal(migrations[^1], Assert.Single(await db.Database.GetPendingMigrationsAsync()));
        Assert.Null(await ColumnType(db, schema, "units", "bathroom_count"));
    });

    [PostgresFact]
    public Task DisabledAutoMigrationLeavesDatabaseUntouched() => WithSchema(async (connection, schema) =>
    {
        await StartApi(connection, false);
        await using var db = Context(connection, schema);
        Assert.Null(await ColumnType(db, schema, "properties", "id"));
    });

    [PostgresFact]
    public Task ConcurrentMigrationWaitsForAdvisoryLock() => WithSchema(async (connection, schema) =>
    {
        await using var holder = new NpgsqlConnection(connection);
        await holder.OpenAsync();
        await using (var acquire = new NpgsqlCommand($"SELECT pg_advisory_lock({DatabaseMigration.AdvisoryLockKey})", holder))
            await acquire.ExecuteNonQueryAsync();
        await using var db = Context(connection, schema);
        var migration = DatabaseMigration.ApplyPendingAsync(db, NullLogger.Instance);
        await Task.Delay(1500);
        Assert.False(migration.IsCompleted);
        await using (var release = new NpgsqlCommand($"SELECT pg_advisory_unlock({DatabaseMigration.AdvisoryLockKey})", holder))
            await release.ExecuteNonQueryAsync();
        Assert.Equal(db.Database.GetMigrations().Count(), (await migration).Count);
        Assert.Empty(await DatabaseMigration.ApplyPendingAsync(db, NullLogger.Instance));
    });

    private const string InsertProperty = """
        INSERT INTO properties (id,name,city,district,address,type,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
        VALUES ('00000000-0000-4000-8000-0000000000aa','Auto migrate','City','District','Address','APARTMENT',1,now(),'test',now(),'test',false,true)
        """;

    private static BuildingFlowDbContext Context(string connection, string schema) =>
        new(new DbContextOptionsBuilder<BuildingFlowDbContext>().UseBuildingFlowNpgsql(connection).Options);

    private static Task<string?> ColumnType(BuildingFlowDbContext db, string schema, string table, string column) =>
        db.Database.SqlQuery<string>($"""
            SELECT data_type AS "Value" FROM information_schema.columns
            WHERE table_schema = {schema} AND table_name = {table} AND column_name = {column}
            """).SingleOrDefaultAsync();

    private static async Task StartApi(string connection, bool autoMigrate)
    {
        await using var factory = new StartupFactory(connection, autoMigrate);
        using var client = factory.CreateClient();
        Assert.True((await client.GetAsync("/health/live")).IsSuccessStatusCode);
    }

    // Each test owns a throwaway database so the default public schema behaves exactly like a real deployment.
    private static async Task WithSchema(Func<string, string, Task> test)
    {
        var original = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!);
        if (original.Database is null || !original.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use a dedicated test database.");
        var database = original.Database + "_auto_" + Guid.NewGuid().ToString("N")[..12];
        await using var admin = new NpgsqlConnection(original.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            await test(new NpgsqlConnectionStringBuilder(original.ConnectionString) { Database = database, Pooling = false }.ConnectionString, "public");
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    private sealed class StartupFactory(string connection, bool autoMigrate) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connection,
                ["Database:AutoMigrate"] = autoMigrate ? "true" : "false",
                ["Authentication:Authority"] = "http://localhost:8080/realms/buildingflow",
                ["Authentication:Audience"] = "buildingflow-api",
                ["Authentication:MetadataAddress"] = ""
            }));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
    }
}
