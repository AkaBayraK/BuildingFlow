using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// FAZ12: the whole supplier module rolls back to the release before it and reinstalls cleanly on an empty schema.
public sealed class SupplierMigrationChainTests
{
    [PostgresFact] public async Task AllSupplierMigrationsRollBackAndReapplyOnEmptySchema()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!; var schema = "supplier_chain_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync(); await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", schema)).AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options); var migrations = db.Database.GetMigrations().ToList();
            var first = migrations.IndexOf(migrations.Single(x => x.EndsWith("_SupplierFoundation", StringComparison.Ordinal))); var before = migrations[first - 1];
            Assert.Equal(9, migrations.Skip(first).Count(x => x.Contains("_Supplier", StringComparison.Ordinal)));
            async Task<long> Tables() { await using var q = new NpgsqlCommand("SELECT count(*) FROM information_schema.tables WHERE table_schema=@s AND table_name LIKE 'supplier%'", admin); q.Parameters.AddWithValue("s", schema); return (long)(await q.ExecuteScalarAsync())!; }
            await db.Database.MigrateAsync(); var installed = await Tables(); Assert.True(installed >= 24, $"{installed} supplier tables");
            await db.GetService<IMigrator>().MigrateAsync(before); Assert.Equal(0L, await Tables());
            await db.Database.MigrateAsync(); Assert.Equal(installed, await Tables()); LegacySchema.AssertCurrentModelMatchesMigrations();
        }
        finally { await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await drop.ExecuteNonQueryAsync(); }
    }
    private sealed class Actor : ICurrentUserService { public string? UserId => "fixture"; public string? Username => "fixture"; public bool IsAuthenticated => true; public bool IsInRole(string role) => role == "SUPER_ADMIN"; }
}
