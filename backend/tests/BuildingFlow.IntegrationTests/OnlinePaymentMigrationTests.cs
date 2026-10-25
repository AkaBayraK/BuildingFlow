using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class OnlinePaymentMigrationTests
{
    [PostgresFact] public async Task UpgradeBackfillsDuesKindKeepsPaymentsAndDownGuardsHistory()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="online_payment_migration_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try
        {
            var connection=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;
            var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(connection,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new AuditUser(),TimeProvider.System)).Options;
            await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,x=>x.EndsWith("_OnlinePayments"));Assert.True(index>0);
            var prior=migrations[index-1];await db.GetService<IMigrator>().MigrateAsync(prior);
            var property=Guid.NewGuid();var unit=Guid.NewGuid();var charge=Guid.NewGuid();var payment=Guid.NewGuid();
            await db.Database.ExecuteSqlRawAsync(LegacyRows(property,unit,charge,payment));
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();
            var migrated=await db.DuesCharges.SingleAsync(x=>x.Id==charge);
            Assert.Equal((ChargeKind.DUES,(ChargeCategory?)null,40m),(migrated.Kind,migrated.Category,migrated.PaidAmount));
            Assert.Equal(PaymentMethod.CASH,(await db.Payments.SingleAsync(x=>x.Id==payment)).Method);
            await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();
            var extra=new DuesAssessment(property,Guid.NewGuid(),new string('c',64),new DateOnly(2026,1,1),50m,"TRY",new DateOnly(2026,1,31),"Demirbaş","UNIT",ChargeKind.EXTRA,ChargeCategory.FIXTURE);
            extra.AddCharge(unit);db.Add(extra);await db.SaveChangesAsync();
            Assert.Equal(2,await db.DuesCharges.CountAsync(x=>x.UnitId==unit&&x.Period==new DateOnly(2026,1,1)&&!x.IsCancelled));
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));
            Assert.Equal(ChargeKind.EXTRA,(await db.DuesAssessments.SingleAsync(x=>x.Id==extra.Id)).Kind);
        }
        finally{await using var drop=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await drop.ExecuteNonQueryAsync();}
    }
    private static string LegacyRows(Guid property,Guid unit,Guid charge,Guid payment)
    {
        var assessment=Guid.NewGuid();const string audit="1, now(), 'fixture', now(), 'fixture', false, true";
        return $"""
            INSERT INTO properties (id, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled, name, type, city, district, address) VALUES ('{property}', {audit}, 'Legacy', 'APARTMENT', 'City', 'District', 'Address');
            INSERT INTO units (id, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled, property_id, number, type, floor, gross_square_meters) VALUES ('{unit}', {audit}, '{property}', '1', 'APARTMENT', 1, 100);
            INSERT INTO dues_assessments (id, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled, property_id, request_id, request_fingerprint, period, amount, currency, due_date, description) VALUES ('{assessment}', {audit}, '{property}', '{Guid.NewGuid()}', '{new string('a',64)}', DATE '2026-01-01', 100, 'TRY', DATE '2026-01-31', 'Dues');
            INSERT INTO dues_charges (id, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled, property_id, assessment_id, unit_id, period, amount, currency, due_date, is_cancelled, paid_amount) VALUES ('{charge}', {audit}, '{property}', '{assessment}', '{unit}', DATE '2026-01-01', 100, 'TRY', DATE '2026-01-31', false, 40);
            INSERT INTO payments (id, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled, property_id, unit_id, request_id, request_fingerprint, amount, currency, paid_date, method, description, is_cancelled) VALUES ('{payment}', {audit}, '{property}', '{unit}', '{Guid.NewGuid()}', '{new string('b',64)}', 40, 'TRY', DATE '2026-02-10', 'CASH', 'Cash', false);
            INSERT INTO payment_allocations (id, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled, property_id, unit_id, payment_id, charge_id, amount) VALUES ('{Guid.NewGuid()}', {audit}, '{property}', '{unit}', '{payment}', '{charge}', 40);
            """;
    }
    private sealed class AuditUser:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;}
}
