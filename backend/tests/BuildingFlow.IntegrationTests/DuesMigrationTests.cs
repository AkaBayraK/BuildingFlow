using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class DuesMigrationTests
{
    [PostgresFact] public async Task UpgradeRepeatAndLossyDowngradePreserveFinancialAndExistingData()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema="dues_migration_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try
        {
            var connection=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};
            await using var db=new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>()
                .UseNpgsql(connection.ConnectionString,options=>options.MigrationsHistoryTable("__EFMigrationsHistory",schema))
                .AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options);
            var previous=db.Database.GetMigrations().Single(value=>value.EndsWith("_PeopleAndUnitOccupancies",StringComparison.Ordinal));
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var property=new Property("Existing","City","District","Address",PropertyType.APARTMENT);
            var unit=property.AddUnit("1",0,100);db.Add(property);await db.SaveChangesAsync();
            var created=property.CreatedAt;var version=property.Version;
            await db.Database.MigrateAsync();
            var batch=new DuesAssessment(property.Id,Guid.NewGuid(),new string('a',64),new(2026,10,1),10.25m,"TRY",new(2026,10,10),"Migration");
            var charge=batch.AddCharge(unit.Id);db.Add(batch);await db.SaveChangesAsync();
            await db.Database.MigrateAsync();
            var saved=await db.Properties.AsNoTracking().SingleAsync();
            Assert.Equal(created,saved.CreatedAt);Assert.Equal(version,saved.Version);Assert.Equal(unit.Id,(await db.Units.AsNoTracking().SingleAsync()).Id);
            Assert.Equal(batch.Id,(await db.DuesAssessments.AsNoTracking().SingleAsync()).Id);Assert.Equal(10.25m,(await db.DuesCharges.AsNoTracking().SingleAsync()).Amount);
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));
            // Newer empty-module migrations may downgrade before the dues-history
            // guard refuses its own Down. Restore today's model before querying it.
            Assert.Contains(db.Database.GetMigrations().Single(value=>value.EndsWith("_DuesAndCharges",StringComparison.Ordinal)),await db.Database.GetAppliedMigrationsAsync());
            await db.Database.MigrateAsync();
            Assert.Equal(charge.Id,(await db.DuesCharges.AsNoTracking().SingleAsync()).Id);
            Assert.Equal(db.Database.GetMigrations(),await db.Database.GetAppliedMigrationsAsync());
            var otherProperty=new Property("Other","City","District","Address",PropertyType.APARTMENT);
            var otherUnit=otherProperty.AddUnit("1",0,100);db.Add(otherProperty);await db.SaveChangesAsync();
            // A real unit in another Property must also fail: testing only a missing
            // UUID would not distinguish a composite FK from a plain unit FK.
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charges SET unit_id={otherUnit.Id} WHERE id={charge.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charges SET property_id={otherProperty.Id},unit_id={otherUnit.Id} WHERE id={charge.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charges SET unit_id={Guid.NewGuid()} WHERE id={charge.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charges SET amount={0m} WHERE id={charge.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charges SET is_cancelled={true} WHERE id={charge.Id}"));
        }
        finally
        {
            await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();
        }
    }
    private sealed class Actor : ICurrentUserService
    {
        public string? UserId=>"dues-migration";public string? Username=>"dues-migration";public bool IsAuthenticated=>true;
        public bool IsInRole(string role)=>false;
    }
}
