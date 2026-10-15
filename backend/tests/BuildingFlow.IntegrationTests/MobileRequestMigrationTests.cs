using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MobileRequestMigrationTests
{
 [PostgresFact] public async Task UpgradeBackfillsLegacyRequestsAndDownRejectsCategoryOrPhotoLoss()
 {
  var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="request_verify_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var cmd=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await cmd.ExecuteNonQueryAsync();
  try {
   var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;
   var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
   await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToArray();var target=Array.FindIndex(migrations,m=>m.EndsWith("_MobileRequestCategoriesAndPhotos",StringComparison.Ordinal));var prior=migrations[target-1];
   await db.GetService<IMigrator>().MigrateAsync(prior);
   var legacy=new Property("Legacy","City","District","Address",PropertyType.APARTMENT);var otherProperty=new Property("Other","City","District","Address",PropertyType.APARTMENT);db.AddRange(legacy,otherProperty);await db.SaveChangesAsync();db.ChangeTracker.Clear();
   var property=legacy.Id;var other=otherProperty.Id;var request=Guid.NewGuid();
   await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO mobile_requests(id,property_id,unit_id,context_role,owner_user_id,description,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled) VALUES ({request},{property},NULL,'MANAGER','fixture','Legacy request',1,now(),'fixture',now(),'fixture',false,true)");
   await db.Database.MigrateAsync();await db.Database.MigrateAsync();
   var saved=await db.Set<MobileRequest>().SingleAsync(x=>x.Id==request);Assert.Equal("OTHER",saved.Category);Assert.Equal("NORMAL",saved.Priority);Assert.Equal("Legacy request",saved.Description);Assert.Equal(1,saved.Version);
   await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE mobile_requests SET category='FIRE' WHERE id={request}"));
   await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE mobile_requests SET priority='low' WHERE id={request}"));
   // Default-only rows may be downgraded; chosen categories and photo metadata may not.
   await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();db.ChangeTracker.Clear();
   saved=await db.Set<MobileRequest>().SingleAsync(x=>x.Id==request);saved.Edit("Legacy request","ELEVATOR","URGENT");await db.SaveChangesAsync();
   await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));
   saved.Edit("Legacy request","OTHER","NORMAL");await db.SaveChangesAsync();
   var photo=new MobileRequestPhoto(request,other,"fixture",Guid.NewGuid(),new string('a',64),new string('b',64),10);db.Add(photo);
   await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.ChangeTracker.Clear();
   db.Add(new MobileRequestPhoto(request,property,"fixture",Guid.NewGuid(),new string('a',64),new string('b',64),10));await db.SaveChangesAsync();
   await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));Assert.Equal(migrations[target],(await db.Database.GetAppliedMigrationsAsync()).Last());
  } finally {await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
 }
 private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
