using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class RequestManagementMigrationTests {
 [PostgresFact]public async Task UpgradePreservesLegacyRequestsRollbackAndCompositeHistoryAreProtected(){
  var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="request_verify_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var command=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await command.ExecuteNonQueryAsync();
  try {
   var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;
   var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
   await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,m=>m.EndsWith("_RequestManagementWorkflow",StringComparison.Ordinal));Assert.True(index>0);var prior=migrations[index-1];await db.GetService<IMigrator>().MigrateAsync(prior);
   var property=new Property("Legacy fixture","City","District","Address");db.Add(property);await db.SaveChangesAsync();var id=Guid.NewGuid();
   await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO mobile_requests (id,property_id,context_role,owner_user_id,description,category,priority,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled) VALUES ({id},{property.Id},'MANAGER','fixture','Legacy leak','FAULT','HIGH',1,now(),'fixture',now(),'fixture',false,true)");
   var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(r)::text AS \"Value\" FROM mobile_requests r WHERE id={id}").SingleAsync();
   await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();
   var after=await db.Database.SqlQuery<string>($"SELECT (to_jsonb(r)-'management_status'-'management_response'-'has_management_history')::text AS \"Value\" FROM mobile_requests r WHERE id={id}").SingleAsync();
   Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(System.Text.Json.Nodes.JsonNode.Parse(before),System.Text.Json.Nodes.JsonNode.Parse(after)));
   var r=await db.Set<MobileRequest>().SingleAsync(x=>x.Id==id);Assert.Equal("RECORDED",r.ManagementStatus);Assert.Null(r.ManagementResponse);Assert.False(r.HasManagementHistory);
   await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();db.ChangeTracker.Clear();
   r=await db.Set<MobileRequest>().SingleAsync(x=>x.Id==id);r.Manage("IN_PROGRESS","Investigating");var history=new RequestManagementEvent(property.Id,id,"RECORDED","IN_PROGRESS","Investigating","fixture",Guid.NewGuid(),new string('a',64),r.Version);db.Add(history);await db.SaveChangesAsync();
   await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM request_management_events WHERE id={history.Id}"));
   var other=new Property("Other fixture","City","District","Address");db.Add(other);await db.SaveChangesAsync();var bad=new RequestManagementEvent(other.Id,id,"RECORDED","IN_PROGRESS","Invalid scope","fixture",Guid.NewGuid(),new string('b',64),1);db.Add(bad);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.Entry(bad).State=EntityState.Detached;
   await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));Assert.Equal(migrations[index],(await db.Database.GetAppliedMigrationsAsync()).Last());Assert.Equal(1,await db.Set<RequestManagementEvent>().CountAsync());
  }finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
 }
 private sealed class Actor:ICurrentUserService {public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
