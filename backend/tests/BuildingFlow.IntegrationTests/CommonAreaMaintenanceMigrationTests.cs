using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommonAreaMaintenanceMigrationTests
{
    [PostgresFact]public async Task UpgradeRepeatSchemaScopeChecksEmptyDownAndReleasedHistoryGuard()
    {
        var connection=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="closure_verify_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(connection);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try{
            var cs=new NpgsqlConnectionStringBuilder(connection){SearchPath=schema+",public"};var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseBuildingFlowNpgsql(cs.ConnectionString,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
            await using var db=new BuildingFlowDbContext(options);var list=db.Database.GetMigrations().ToList();var target=list.Single(m=>m.EndsWith("_CommonAreaMaintenanceClosures",StringComparison.Ordinal));var previous=list[list.IndexOf(target)-1];var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync(previous);
            var p=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);db.Add(p);await db.SaveChangesAsync();
            var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync();
            await migrator.MigrateAsync(target);await migrator.MigrateAsync(target);await migrator.MigrateAsync(previous);await migrator.MigrateAsync(target);LegacySchema.AssertCurrentModelMatchesMigrations();
            Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync());
            var a=new CommonArea(p.Id,Guid.NewGuid(),new string('A',64),"Area",CommonAreaType.OTHER,null,CommonAreaVisibility.PROPERTY,CommonAreaUsage.RESERVATION,20,null,null,null,null,0);
            var plan=new MaintenancePlan(p.Id,"Plan",null,MaintenanceType.MAINTENANCE,new(2026,11,1),null,null,MaintenanceStatus.PLANNED);var notice=new Announcement(p.Id,"Notice","Body",ContentAudience.ALL_MEMBERS);notice.Publish(DateTimeOffset.UtcNow);db.AddRange(a,plan,notice);await db.SaveChangesAsync();
            var c=new CommonAreaClosure(p.Id,a.Id,plan.Id,Guid.NewGuid(),new string('B',64),new(2026,11,1),new(10,0),new(12,0),"Notice",notice.Id);db.Add(c);await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_area_closures SET ends_at=starts_at WHERE id={c.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_area_closures SET maintenance_plan_id={Guid.NewGuid()} WHERE id={c.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_area_closures SET released_at=now(), release_announcement_id={notice.Id}, release_reason=NULL WHERE id={c.Id}"));
            db.Remove(c);await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>migrator.MigrateAsync(previous));Assert.Equal(target,(await db.Database.GetAppliedMigrationsAsync()).Last());
        }finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
