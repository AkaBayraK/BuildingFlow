using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class PersonProfileMigrationTests
{
 [PostgresFact] public async Task UpgradePreservesLegacyUnknownsAndSchemaRejectsLossyDownAndWrongScope()
 {
  var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="person_verify_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var cmd=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await cmd.ExecuteNonQueryAsync();
  try {
   var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;
   var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
   await using var current=new BuildingFlowDbContext(options);var migrations=current.Database.GetMigrations().ToArray();var prior=migrations[^2];var latest=migrations[^1];await current.GetService<IMigrator>().MigrateAsync(prior);
   var legacyOptions=new DbContextOptionsBuilder<BuildingFlowDbContext>().BeforeApartmentDetails().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
   Guid id,property;await using(var legacy=new BuildingFlowDbContext(legacyOptions)){var p=new Property("Legacy","City","District","Address",PropertyType.APARTMENT);var person=new Person(p.Id,"Legacy","Example");legacy.AddRange(p,person);await legacy.SaveChangesAsync();id=person.Id;property=p.Id;}
   await current.Database.MigrateAsync();await current.Database.MigrateAsync();var saved=await current.People.SingleAsync(p=>p.Id==id);Assert.Null(saved.PersonKind);Assert.Null(saved.IdentityNumber);Assert.Null(saved.PhoneNumber);Assert.Equal(1,saved.Version);Assert.Equal("fixture",saved.CreatedBy);
   LegacySchema.AssertCurrentModelMatchesMigrations();foreach(var entity in current.Model.GetEntityTypes().Where(e=>typeof(BaseEntity).IsAssignableFrom(e.ClrType))){var table=entity.GetTableName()!;var fields=await current.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();foreach(var field in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled"})Assert.Contains(field,fields);}
   await current.GetService<IMigrator>().MigrateAsync(prior);await current.Database.MigrateAsync();current.ChangeTracker.Clear();saved=await current.People.SingleAsync(p=>p.Id==id);saved.SetProfile("NATURAL","DOMESTIC","TR","12345678901",new(1990,1,1),"MALE","Address",null);saved.Update("Legacy","Example","+905551234567","fixture@example.test",true);await current.SaveChangesAsync();
   var message=new PersonMessage(property,id,Guid.NewGuid(),new string('a',64),"fixture","Title","Body");current.Add(message);await current.SaveChangesAsync();var other=new Property("Other","City","District","Address",PropertyType.APARTMENT);current.Add(other);await current.SaveChangesAsync();
   await Assert.ThrowsAsync<PostgresException>(()=>current.Database.ExecuteSqlInterpolatedAsync($"UPDATE person_messages SET property_id={other.Id} WHERE id={message.Id}"));
   await Assert.ThrowsAsync<PostgresException>(()=>current.GetService<IMigrator>().MigrateAsync(prior));Assert.Equal(latest,(await current.Database.GetAppliedMigrationsAsync()).Last());Assert.Equal("12345678901",(await current.People.AsNoTracking().SingleAsync(p=>p.Id==id)).IdentityNumber);
  } finally {await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
 }
 private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
