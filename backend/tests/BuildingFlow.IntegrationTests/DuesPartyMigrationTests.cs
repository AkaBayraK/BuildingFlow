using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class DuesPartyMigrationTests
{
    [PostgresFact] public async Task LegacyDebtAuditCancellationAndAmountsSurviveUpgradeRepeatAndSafeDown()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="party_migration_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try
        {
            var connection=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};
            await using var db=new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(connection.ConnectionString,options=>options.MigrationsHistoryTable("__EFMigrationsHistory",schema)).Options);
            var previous=db.Database.GetMigrations().Single(m=>m.EndsWith("_PaymentsAndAllocations",StringComparison.Ordinal));await db.GetService<IMigrator>().MigrateAsync(previous);
            var upgraded=db.Database.GetMigrations().Single(m=>m.EndsWith("_DuesPartyHistoryAndCancellationReasons",StringComparison.Ordinal));
            // Fixture has the exact old schema: no person can truthfully be inferred for it.
            var property=Guid.NewGuid();var unit=Guid.NewGuid();var batch=Guid.NewGuid();var charge=Guid.NewGuid();var at=DateTimeOffset.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO properties (id,name,type,city,district,address,version,created_at,modified_at,created_by,modified_by,is_deleted,is_enabled) VALUES ({property},{"Existing"},{"APARTMENT"},{"City"},{"District"},{"Address"},{7L},{at},{at},{"original"},{"original"},{false},{true})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO units (id,property_id,number,type,floor,gross_square_meters,version,created_at,modified_at,created_by,modified_by,is_deleted,is_enabled) VALUES ({unit},{property},{"1"},{"APARTMENT"},{1},{100m},{2L},{at},{at},{"original"},{"original"},{false},{true})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dues_assessments (id,property_id,request_id,request_fingerprint,period,amount,currency,due_date,description,version,created_at,modified_at,created_by,modified_by,is_deleted,is_enabled) VALUES ({batch},{property},{Guid.NewGuid()},{new string('a',64)},{new DateOnly(2026,10,1)},{12.35m},{"TRY"},{new DateOnly(2026,10,31)},{"Legacy debt"},{1L},{at},{at},{"original"},{"original"},{false},{true})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dues_charges (id,property_id,assessment_id,unit_id,period,amount,currency,due_date,is_cancelled,cancellation_reason,cancelled_at,paid_amount,version,created_at,modified_at,created_by,modified_by,is_deleted,is_enabled) VALUES ({charge},{property},{batch},{unit},{new DateOnly(2026,10,1)},{12.35m},{"TRY"},{new DateOnly(2026,10,31)},{true},{"Original free text"},{at},{0m},{3L},{at},{at},{"original"},{"original"},{false},{true})");
            var before=await JsonCharge(db,false);
            await db.GetService<IMigrator>().MigrateAsync(upgraded);await db.GetService<IMigrator>().MigrateAsync(upgraded);
            Assert.Equal(before,await JsonCharge(db,true));var saved=await db.DuesCharges.AsNoTracking().Select(x=>new{x.PartiesRecorded,x.CancellationReasonId}).SingleAsync();
            Assert.False(saved.PartiesRecorded);Assert.Null(saved.CancellationReasonId);Assert.Empty(await db.DuesChargeParties.ToListAsync());Assert.Equal("UNIT",await db.DuesAssessments.Select(x=>x.DebtorType).SingleAsync());
            var reasons=await db.DuesCancellationReasons.ToListAsync();Assert.Equal(5,reasons.Count);Assert.All(reasons,r=>{Assert.Equal(1,r.Version);Assert.Equal("migration",r.CreatedBy);Assert.False(r.IsDeleted);Assert.True(r.IsEnabled);Assert.Equal(r.CreatedAt,r.ModifiedAt);});
            await db.GetService<IMigrator>().MigrateAsync(previous);await db.GetService<IMigrator>().MigrateAsync(upgraded);Assert.Equal(before,await JsonCharge(db,true));
            // Even a recorded empty-party snapshot is new history and must not be erased by Down.
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charges SET parties_recorded=true WHERE id={charge}");
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));
            Assert.Equal(db.Database.GetMigrations().TakeWhile(m=>string.CompareOrdinal(m,upgraded)<=0),await db.Database.GetAppliedMigrationsAsync());Assert.True(await db.DuesCharges.AsNoTracking().Select(x=>x.PartiesRecorded).SingleAsync());
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charges SET parties_recorded=false,cancellation_reason_id={BuildingFlow.Domain.DuesCancellationReason.OtherId},cancellation_reason_code='OTHER',cancellation_reason_name='Diğer' WHERE id={charge}");
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));
        }
        finally {await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private static async Task<string> JsonCharge(BuildingFlowDbContext db,bool upgraded)
    {
        await db.Database.OpenConnectionAsync();await using var command=db.Database.GetDbConnection().CreateCommand();
        command.CommandText=upgraded?"SELECT (to_jsonb(c)-'parties_recorded'-'cancellation_reason_id'-'cancellation_reason_code'-'cancellation_reason_name'-'cancelled_by_occupancy_id')::text FROM dues_charges c":"SELECT to_jsonb(c)::text FROM dues_charges c";
        return (string)(await command.ExecuteScalarAsync())!;
    }
}
