using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierQuoteMigrationTests
{
    [PostgresFact] public async Task UpgradeRepeatEmptyRollbackAndPopulatedRollbackPreserveScopeAndHistory()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!; var schema = "supplier_quotes_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync(); await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", schema)).AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options); var migrations = db.Database.GetMigrations().ToList(); var target = migrations.Single(x => x.EndsWith("_SupplierQuotes", StringComparison.Ordinal)); var previous = migrations[migrations.IndexOf(target) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); LegacySchema.AssertCurrentModelMatchesMigrations();
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.Database.MigrateAsync();
            var tenant = new PersonTenant("Quotes migration"); var property = new Property("Fixture", "City", "District", "Address"); var supplier = new Supplier(tenant.Id, "Supplier", SupplierKind.COMPANY); var cat = new SupplierCategory(tenant.Id, "Local"); var other = new SupplierCategory(tenant.Id, "Other");
            var service = new BuildingFlow.Domain.SupplierService(tenant.Id, supplier.Id, cat.Id, "Local"); var wrongService = new BuildingFlow.Domain.SupplierService(tenant.Id, supplier.Id, other.Id, "Other"); var area = new SupplierAreaService(tenant.Id, supplier.Id, property.Id, service.Id); var wrongArea = new SupplierAreaService(tenant.Id, supplier.Id, property.Id, wrongService.Id);
            db.AddRange(tenant, property, supplier, cat, other, service, wrongService, area, wrongArea, new PersonPropertyScope(tenant.Id, property.Id), new SupplierManagementArea(tenant.Id, supplier.Id, property.Id)); await db.SaveChangesAsync();
            var inquiry = new SupplierInquiry(tenant.Id, property.Id, cat.Id, "Scope", "Work scope", new(2027, 1, 1)); db.Add(inquiry); await db.SaveChangesAsync();
            db.Add(new SupplierInvitation(inquiry, supplier.Id, wrongArea.Id)); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            var invitation = new SupplierInvitation(inquiry, supplier.Id, area.Id); db.Add(invitation); await db.SaveChangesAsync();
            var quote = new SupplierQuote(invitation, 100m, 20m, "TRY", new(2027, 2, 1), "All parts", "issuer", "entrant"); db.Add(quote); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_quotes SET vat_amount=1, gross_amount=101 WHERE id={quote.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_invitations SET is_enabled=false WHERE id={invitation.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM supplier_inquiries WHERE id={inquiry.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous)); Assert.True(await db.Set<SupplierInquiry>().AnyAsync(x => x.Id == inquiry.Id));
            await using var columns = new NpgsqlCommand("SELECT count(*) FROM information_schema.columns WHERE table_schema=@schema AND table_name IN ('supplier_inquiries','supplier_invitations','supplier_quotes','supplier_work_orders','supplier_inquiry_revisions') AND column_name IN ('id','version','created_at','created_by','modified_at','modified_by','is_deleted','is_enabled') AND is_nullable='NO'", admin); columns.Parameters.AddWithValue("schema", schema); Assert.Equal(40L, await columns.ExecuteScalarAsync());
        }
        finally { await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await drop.ExecuteNonQueryAsync(); }
    }
    private sealed class Actor : ICurrentUserService { public string? UserId => "fixture"; public string? Username => "fixture"; public bool IsAuthenticated => true; public bool IsInRole(string role) => role == "SUPER_ADMIN"; }
}
