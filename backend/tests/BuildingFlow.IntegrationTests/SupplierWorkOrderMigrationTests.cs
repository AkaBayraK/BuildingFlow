using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierWorkOrderMigrationTests
{
    [PostgresFact] public async Task ExistingQuoteDraftUpgradePreservesAuditAndEmptyRollbackButProtectsNewHistory()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!; var schema = "supplier_work_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync(); await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", schema)).AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options); var migrations = db.Database.GetMigrations().ToList(); var target = migrations.Single(x => x.EndsWith("_SupplierWorkOrders", StringComparison.Ordinal)); var previous = migrations[migrations.IndexOf(target) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var t = new PersonTenant("Legacy quote draft"); var p = new Property("Fixture", "City", "District", "Address"); var s = new Supplier(t.Id, "Legacy supplier", SupplierKind.COMPANY); var cat = new SupplierCategory(t.Id, "Maintenance"); var service = new BuildingFlow.Domain.SupplierService(t.Id, s.Id, cat.Id, "Elevator"); var area = new SupplierAreaService(t.Id, s.Id, p.Id, service.Id);
            var inquiry = new SupplierInquiry(t.Id, p.Id, cat.Id, "Annual", "Original quote scope", new(2027, 1, 1)); var invitation = new SupplierInvitation(inquiry, s.Id, area.Id);
            db.AddRange(t, p, s, cat, service, area, inquiry, invitation, new PersonPropertyScope(t.Id, p.Id), new SupplierManagementArea(t.Id, s.Id, p.Id)); await db.SaveChangesAsync();
            var quote = new SupplierQuote(invitation, 100m, 20m, "TRY", new(2027, 2, 1), "Parts included", "issuer", "entrant"); db.Add(quote); await db.SaveChangesAsync();
            quote.Decide(true, "Approved fixture", "issuer", "reviewer", new(2027, 1, 1)); inquiry.Award(); await db.SaveChangesAsync();
            var id = Guid.NewGuid(); var stamp = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO supplier_work_orders(id,tenant_id,property_id,supplier_id,area_service_id,inquiry_id,quote_id,title,amount,currency,status,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
                VALUES({id},{t.Id},{p.Id},{s.Id},{area.Id},{inquiry.Id},{quote.Id},'Annual',120.00,'TRY','DRAFT',1,{stamp},'legacy-fixture',{stamp},'legacy-fixture',false,true)
                """);
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); LegacySchema.AssertCurrentModelMatchesMigrations();
            var order = await db.Set<SupplierWorkOrder>().SingleAsync(x => x.Id == id); Assert.Equal(SupplierWorkOrderSource.QUOTE, order.Source); Assert.Equal(inquiry.Description, order.Description); Assert.Equal(inquiry.Description, order.SourceSnapshot); Assert.Equal(1, order.Version); Assert.Equal(stamp, order.CreatedAt); Assert.Equal("legacy-fixture", order.CreatedBy); Assert.Equal(120m, order.Amount);
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
            order = await db.Set<SupplierWorkOrder>().SingleAsync(x => x.Id == id); Assert.Equal(1, order.Version);
            db.Add(new SupplierWorkOrderRevision(order, Guid.NewGuid(), "fixture", "fixture", new string('A', 64), "CREATE", "{}", 1)); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous)); Assert.True(await db.Set<SupplierWorkOrder>().AnyAsync(x => x.Id == id));
            await using var columns = new NpgsqlCommand("SELECT count(*) FROM information_schema.columns WHERE table_schema=@schema AND table_name IN ('supplier_work_order_photos','supplier_work_order_revisions') AND column_name IN ('id','version','created_at','created_by','modified_at','modified_by','is_deleted','is_enabled') AND is_nullable='NO'", admin); columns.Parameters.AddWithValue("schema", schema); Assert.Equal(16L, await columns.ExecuteScalarAsync());
        }
        finally { await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await drop.ExecuteNonQueryAsync(); }
    }
    private sealed class Actor : ICurrentUserService { public string? UserId => "fixture"; public string? Username => "fixture"; public bool IsAuthenticated => true; public bool IsInRole(string role) => role == "SUPER_ADMIN"; }
}
