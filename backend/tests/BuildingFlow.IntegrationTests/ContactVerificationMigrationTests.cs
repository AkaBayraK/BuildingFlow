using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class ContactVerificationMigrationTests
{
    [PostgresFact] public async Task UpgradeDoesNotInventProofAndDownCannotDeleteEvenSoftDeletedHistory()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema="contact_migration_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try
        {
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;
            var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new AuditActor(),TimeProvider.System)).Options;
            await using var db=new BuildingFlowDbContext(options);
            var migrations=db.Database.GetMigrations().ToArray();var target=Array.FindIndex(migrations,m=>m.EndsWith("_AccountPhoneVerificationFoundation"));
            Assert.True(target>0);var prior=migrations[target-1];
            await db.GetService<IMigrator>().MigrateAsync(prior);
            var profile=new MobileProfile("legacy-contact");profile.Edit("Legacy","OWNER","05551234567",null);
            db.Add(profile);await db.SaveChangesAsync();
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();
            Assert.Empty(await db.Set<AccountPhoneVerification>().ToListAsync());
            Assert.Equal("05551234567",(await db.Set<MobileProfile>().SingleAsync()).Phone);
            // Empty additive schema can be rolled back and reapplied without affecting the old profile.
            await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();
            var proof=new AccountPhoneVerification("legacy-contact");proof.Stage(profile.Phone);db.Add(proof);await db.SaveChangesAsync();
            Assert.Null(proof.VerifiedAt);Assert.Equal("fixture-contact",proof.CreatedBy);
            Assert.True(proof.Version>0);Assert.True(proof.IsEnabled);Assert.False(proof.IsDeleted);
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync("UPDATE account_phone_verifications SET verified_at=now()"));
            var duplicate=new AccountPhoneVerification("legacy-contact");db.Add(duplicate);
            await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.Entry(duplicate).State=EntityState.Detached;
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));
            db.Remove(proof);await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));
            Assert.Single(await db.Set<AccountPhoneVerification>().IgnoreQueryFilters().ToListAsync());
        }
        finally {await using var drop=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await drop.ExecuteNonQueryAsync();}
    }
    private sealed class AuditActor:ICurrentUserService
    {
        public string? UserId=>"fixture-contact";public string? Username=>"fixture-contact";
        public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;
    }
}
