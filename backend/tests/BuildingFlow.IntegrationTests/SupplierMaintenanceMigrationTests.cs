using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierMaintenanceMigrationTests
{
    [PostgresFact] public async Task UpgradeRepeatEmptyRollbackAndPopulatedRollbackGuard()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!; var schema = "supplier_maintenance_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync(); await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", schema)).AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options); var migrations = db.Database.GetMigrations().ToList(); var target = migrations.Single(x => x.EndsWith("_SupplierMaintenanceSchedules", StringComparison.Ordinal)); var previous = migrations[migrations.IndexOf(target) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var t = new PersonTenant("Maintenance upgrade"); var p = new Property("Fixture", "City", "District", "Address"); var s = new Supplier(t.Id, "Upgrade supplier", SupplierKind.COMPANY); var cat = new SupplierCategory(t.Id, "Elevator"); var service = new BuildingFlow.Domain.SupplierService(t.Id, s.Id, cat.Id, "Inspection"); var area = new SupplierAreaService(t.Id, s.Id, p.Id, service.Id);
            db.AddRange(t, p, s, cat, service, area, new PersonPropertyScope(t.Id, p.Id), new SupplierManagementArea(t.Id, s.Id, p.Id)); await db.SaveChangesAsync();
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); LegacySchema.AssertCurrentModelMatchesMigrations();
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
            var schedule = new SupplierMaintenanceSchedule(t.Id, s.Id, p.Id, area.Id, null, "Inspection", null, MaintenanceType.INSPECTION, SupplierMaintenanceFrequency.MONTHLY, new(2027, 1, 4), new(2027, 2, 4), 7, SupplierNonWorkingDayRule.KEEP);
            var plan = new MaintenancePlan(p.Id, "Inspection", null, MaintenanceType.INSPECTION, new(2027, 1, 4), null, "Upgrade supplier", MaintenanceStatus.PLANNED);
            db.AddRange(schedule, plan, new SupplierMaintenanceOccurrence(schedule, 1, new(2027, 1, 4), new(2027, 1, 4), plan.Id)); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous)); Assert.True(await db.Set<SupplierMaintenanceSchedule>().AnyAsync(x => x.Id == schedule.Id));
            await using var columns = new NpgsqlCommand("SELECT count(*) FROM information_schema.columns WHERE table_schema=@schema AND table_name IN ('supplier_holidays','supplier_maintenance_schedules','supplier_maintenance_occurrences','supplier_maintenance_revisions') AND column_name IN ('id','version','created_at','created_by','modified_at','modified_by','is_deleted','is_enabled') AND is_nullable='NO'", admin); columns.Parameters.AddWithValue("schema", schema); Assert.Equal(32L, await columns.ExecuteScalarAsync());
        }
        finally { await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await drop.ExecuteNonQueryAsync(); }
    }
    private sealed class Actor : ICurrentUserService { public string? UserId => "fixture"; public string? Username => "fixture"; public bool IsAuthenticated => true; public bool IsInRole(string role) => role == "SUPER_ADMIN"; }
}
