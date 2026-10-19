using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CentralPersonMigrationTests {
 [PostgresFact]public async Task AdditiveUpgradePreservesLegacyPeopleAndRejectsPopulatedRollback(){
  var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="central_person_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var c=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await c.ExecuteNonQueryAsync();
  try{
   var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;await using var db=new BuildingFlowDbContext(options);
   var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,m=>m.EndsWith("_CentralPersonBackend",StringComparison.Ordinal));Assert.True(index>0);var prior=migrations[index-1];await db.GetService<IMigrator>().MigrateAsync(prior);
   var property=new Property("Legacy fixture","City","District","Address");var person=new Person(property.Id,"Legacy","Example","+905551234567","fixture@example.test");db.AddRange(property,person);await db.SaveChangesAsync();var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM people p WHERE id={person.Id}").SingleAsync();
   await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();var after=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM people p WHERE id={person.Id}").SingleAsync();Assert.Equal(before,after);Assert.False(await db.Set<CentralPerson>().AnyAsync());
   foreach(var table in new[]{"person_tenants","person_user_accounts","person_tenant_memberships","persons","person_account_links","person_operation_receipts"}){var fields=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();foreach(var field in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled"})Assert.Contains(field,fields);}
   await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM people p WHERE id={person.Id}").SingleAsync());
   var tenant=new PersonTenant("New fixture");db.Add(tenant);await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));Assert.Equal(migrations[index],(await db.Database.GetAppliedMigrationsAsync()).Last());Assert.Equal(1,await db.Set<PersonTenant>().CountAsync());
  }finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
 }
 private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
