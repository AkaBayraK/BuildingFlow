using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Upgrade keeps existing announcements (backfilled GENERAL), repeat is a no-op, audit columns exist and Down refuses to drop plans.
public sealed class PlanningMigrationTests
{
    [PostgresFact] public async Task UpgradeBackfillsCategoryRepeatAuditAndDownGuard()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="planning_verify_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try{
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
            await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToList();var target=migrations.Single(x=>x.EndsWith("_PlanningAndAnnouncementCategories",StringComparison.Ordinal));var previous=migrations[migrations.IndexOf(target)-1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var property=new Property("Preserved","City","District","Address",PropertyType.APARTMENT);db.Add(property);await db.SaveChangesAsync();var p=property.Id;
            var a=Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO announcements (id,property_id,title,content,audience,status,published_at,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled) VALUES ({a},{p},'Old','Body','ALL_MEMBERS','DRAFT',NULL,1,now(),'fixture',now(),'fixture',false,true)");
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();
            Assert.Equal(migrations.Last(),(await db.Database.GetAppliedMigrationsAsync()).Last());
            var old=await db.Announcements.AsNoTracking().SingleAsync(x=>x.Id==a);Assert.Equal(AnnouncementCategory.GENERAL,old.Category);Assert.Equal(1,old.Version);
            LegacySchema.AssertCurrentModelMatchesMigrations();
            var fields=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name='maintenance_plans' AND is_nullable='NO'").ToListAsync();
            foreach(var field in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled","property_id","title","type","planned_date","status"})Assert.Contains(field,fields);
            db.Add(new MaintenancePlan(p,"Bakım",null,MaintenanceType.MAINTENANCE,new(2026,11,12),null,null,MaintenanceStatus.PLANNED));await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));
            Assert.Equal(target,(await db.Database.GetAppliedMigrationsAsync()).Last());
        }
        finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"planning-test";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
