using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Upgrade keeps existing data, repeat is a no-op, every survey table has the audit columns and Down refuses to drop surveys.
public sealed class SurveyMigrationTests
{
    [PostgresFact] public async Task UpgradeRepeatAuditColumnsAndDownGuard()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="survey_verify_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try{
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
            await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToList();var target=migrations.Single(x=>x.EndsWith("_Surveys",StringComparison.Ordinal));var previous=migrations[migrations.IndexOf(target)-1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var property=new Property("Preserved","City","District","Address",PropertyType.APARTMENT);db.Add(property);await db.SaveChangesAsync();
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();
            Assert.Equal(migrations.Last(),(await db.Database.GetAppliedMigrationsAsync()).Last());Assert.True(await db.Properties.AnyAsync(p=>p.Id==property.Id));
            LegacySchema.AssertCurrentModelMatchesMigrations();
            foreach(var table in new[]{"surveys","survey_questions","survey_options","survey_participations","survey_responses","survey_answers"}){
                var fields=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();
                foreach(var field in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled","property_id"})Assert.Contains(field,fields);}
            // An empty survey schema can be rolled back; once a survey exists Down refuses.
            await db.GetService<IMigrator>().MigrateAsync(previous);await db.Database.MigrateAsync();
            db.Add(new Survey(property.Id,"Anket",null,SurveyCategory.GENERAL,new(2026,11,1),new(2026,11,15),SurveyAudience.ALL_RESIDENTS,SurveyResponseMode.PER_UNIT,false));await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));
            Assert.Equal(target,(await db.Database.GetAppliedMigrationsAsync()).Single(x=>x==target));Assert.Equal(1,await db.Surveys.CountAsync());
        }
        finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"survey-test";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
