using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class PhoneOtpMigrationTests
{
    [PostgresFact] public async Task PhoneOtpUpgradeRepeatAndDownGuardKeepEvenSoftDeletedHistory()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="otp_migration_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try
        {
            var connection=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;
            var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(connection,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new AuditUser(),TimeProvider.System)).Options;
            await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,x=>x.EndsWith("_AccountPhoneOtp"));Assert.True(index>0);
            var prior=migrations[index-1];await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();await db.Database.MigrateAsync();
            Assert.Empty(await db.Set<PhoneOtpChallenge>().ToListAsync());
            await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();
            var challenge=new PhoneOtpChallenge("fixture-owner",new string('A',64),new string('B',64),DateTimeOffset.UtcNow);challenge.SetSecret(new string('C',64),"encrypted");db.Add(challenge);await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));
            db.Remove(challenge);await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));
            Assert.Single(await db.Set<PhoneOtpChallenge>().IgnoreQueryFilters().ToListAsync());
        }
        finally{await using var drop=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await drop.ExecuteNonQueryAsync();}
    }
    private sealed class AuditUser:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;}
}
