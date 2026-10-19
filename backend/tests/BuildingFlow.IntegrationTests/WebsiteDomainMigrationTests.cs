using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class WebsiteDomainMigrationTests{
 [PostgresFact]public async Task UpgradeReplayEmptyRollbackAndHistoryGuard(){
 var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="webdomains_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var command=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await command.ExecuteNonQueryAsync();
 try{var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,x=>x.EndsWith("_WebsiteDomains",StringComparison.Ordinal));var prior=migrations[index-1];var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync(prior);var p=new Property("Fixture","City","District","Address");var site=new WebsiteSite(p.Id,Guid.NewGuid(),"listing-fixture","Fixture");db.AddRange(p,site);await db.SaveChangesAsync();var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync();await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync());await migrator.MigrateAsync(prior);await db.Database.MigrateAsync();
 var d=new WebsiteDomain(p.Id,site.Id,Guid.NewGuid(),"fixture.example.com","CUSTOM",new string('a',64));db.Add(d);await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>migrator.MigrateAsync(prior));Assert.Equal(migrations.Take(index+1),await db.Database.GetAppliedMigrationsAsync());Assert.True(await db.Set<WebsiteDomain>().AnyAsync(x=>x.Id==d.Id));
 }finally{await using var command=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await command.ExecuteNonQueryAsync();}
 }
 private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
