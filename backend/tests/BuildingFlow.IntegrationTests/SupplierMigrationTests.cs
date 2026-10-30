using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierMigrationTests
{
    [PostgresFact] public async Task UpgradeRepeatCompositeScopeImmutableReceiptAndDownProtection()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "supplier_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", schema)).AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options);
            var migrations = db.Database.GetMigrations().ToList(); var target = migrations.Single(x => x.EndsWith("_SupplierFoundation", StringComparison.Ordinal)); var previous = migrations[migrations.IndexOf(target) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            LegacySchema.AssertCurrentModelMatchesMigrations();
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.Database.MigrateAsync();
            var a = new PersonTenant("A"); var b = new PersonTenant("B"); var property = new Property("Fixture", "City", "District", "Address");
            var supplier = new Supplier(a.Id, "Acme", SupplierKind.COMPANY); var category = new SupplierCategory(b.Id, "Foreign category");
            db.AddRange(a, b, property, supplier, category, new PersonPropertyScope(a.Id, property.Id)); await db.SaveChangesAsync();
            db.Add(new SupplierService(a.Id, supplier.Id, category.Id, "Foreign service"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            db.Add(new SupplierManagementArea(b.Id, supplier.Id, property.Id)); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            var localCategory = new SupplierCategory(a.Id, "Local category"); var service = new BuildingFlow.Domain.SupplierService(a.Id, supplier.Id, localCategory.Id, "Local service");
            db.AddRange(localCategory, service, new SupplierManagementArea(a.Id, supplier.Id, property.Id), new SupplierAreaService(a.Id, supplier.Id, property.Id, service.Id)); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(target));
            var receipt = new SupplierOperation(a.Id, supplier.Id, Guid.NewGuid(), "issuer", "actor", new string('A', 64), 1); db.Add(receipt); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_operations SET fingerprint={new string('B', 64)} WHERE id={receipt.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous));
            Assert.True(await db.Set<Supplier>().AnyAsync(s => s.Id == supplier.Id));
        }
        finally { await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await drop.ExecuteNonQueryAsync(); }
    }
    private sealed class Actor : BuildingFlow.Application.ICurrentUserService
    {
        public string? Username => "supplier-migration-test"; public string? UserId => "supplier-migration-test"; public bool IsAuthenticated => true;
        public bool IsInRole(string role) => role == "SUPER_ADMIN";
    }
}
