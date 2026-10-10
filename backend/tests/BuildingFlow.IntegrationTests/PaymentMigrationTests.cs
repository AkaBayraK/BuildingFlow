using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class PaymentMigrationTests
{
    [PostgresFact] public async Task UpgradeBackfillsOldDebtSafeEmptyDownAndHistoryGuardProtectAmountsAndCompositeScope()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="payments_migration_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try
        {
            var connection=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};
            await using var db=new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(connection.ConnectionString,options=>options.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options);
            var previous=db.Database.GetMigrations().Single(value=>value.EndsWith("_DuesAndCharges",StringComparison.Ordinal));
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var property=Guid.NewGuid();var unit=Guid.NewGuid();var batch=Guid.NewGuid();var charge=Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO properties (id,name,type,city,district,address,version,created_at,modified_at,created_by,modified_by,is_deleted,is_enabled) VALUES ({property},{"Existing"},{"APARTMENT"},{"City"},{"District"},{"Address"},{1L},{DateTimeOffset.UtcNow},{DateTimeOffset.UtcNow},{"fixture"},{"fixture"},{false},{true})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO units (id,property_id,number,type,floor,gross_square_meters,version,created_at,modified_at,created_by,modified_by,is_deleted,is_enabled) VALUES ({unit},{property},{"1"},{"APARTMENT"},{1},{100m},{1L},{DateTimeOffset.UtcNow},{DateTimeOffset.UtcNow},{"fixture"},{"fixture"},{false},{true})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dues_assessments (id,property_id,request_id,request_fingerprint,period,amount,currency,due_date,description,version,created_at,modified_at,created_by,modified_by,is_deleted,is_enabled) VALUES ({batch},{property},{Guid.NewGuid()},{new string('a',64)},{new DateOnly(2026,10,1)},{12.35m},{"TRY"},{new DateOnly(2026,10,31)},{"Old debt"},{1L},{DateTimeOffset.UtcNow},{DateTimeOffset.UtcNow},{"fixture"},{"fixture"},{false},{true})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dues_charges (id,property_id,assessment_id,unit_id,period,amount,currency,due_date,is_cancelled,version,created_at,modified_at,created_by,modified_by,is_deleted,is_enabled) VALUES ({charge},{property},{batch},{unit},{new DateOnly(2026,10,1)},{12.35m},{"TRY"},{new DateOnly(2026,10,31)},{false},{1L},{DateTimeOffset.UtcNow},{DateTimeOffset.UtcNow},{"fixture"},{"fixture"},{false},{true})");
            await db.Database.MigrateAsync();var saved=await db.DuesCharges.SingleAsync();var created=saved.CreatedAt;Assert.Equal(0m,saved.PaidAmount);Assert.Equal(1,saved.Version);Assert.Equal(12.35m,saved.Balance);
            await db.GetService<IMigrator>().MigrateAsync(previous);await db.Database.MigrateAsync();await db.Database.MigrateAsync();
            var payment=new Payment(property,unit,Guid.NewGuid(),new string('b',64),3.10m,"TRY",new(2026,10,9),PaymentMethod.CASH,null,"Cash");payment.Allocate(saved,3.10m);payment.ValidateComplete();db.Add(payment);await db.SaveChangesAsync();
            Assert.Equal(created,saved.CreatedAt);Assert.Equal(2,saved.Version);
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));
            // A newer empty module may downgrade before the payment history guard stops its Down.
            await db.Database.MigrateAsync();
            Assert.Equal(3.10m,(await db.Payments.AsNoTracking().SingleAsync()).Amount);Assert.Single(await db.PaymentAllocations.ToListAsync());Assert.Equal(db.Database.GetMigrations(),await db.Database.GetAppliedMigrationsAsync());
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dues_charges SET paid_amount={13m} WHERE id={charge}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE payments SET is_deleted={true} WHERE id={payment.Id}"));
            var other= new Property("Other","City","District","Address",PropertyType.APARTMENT);var otherUnit=other.AddUnit("1",1,100);var otherBatch=new DuesAssessment(other.Id,Guid.NewGuid(),new string('a',64),new(2026,10,1),1m,"TRY",new(2026,10,31),"Other");var otherCharge=otherBatch.AddCharge(otherUnit.Id);db.Add(other);db.Add(otherBatch);await db.SaveChangesAsync();
            var part=Assert.Single(payment.Allocations);
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE payment_allocations SET charge_id={otherCharge.Id} WHERE id={part.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE payment_allocations SET property_id={other.Id},unit_id={otherUnit.Id},charge_id={otherCharge.Id} WHERE id={part.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE payment_allocations SET amount={0m} WHERE id={part.Id}"));
        }
        finally {await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService
    {public string? UserId=>"payment-migration";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
