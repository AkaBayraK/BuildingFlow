using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class PersonCenterMigrationTests
{
    [PostgresFact]public async Task ReceiptUpgradeReapplyCommonColumnsImmutabilityAndPopulatedRollbackGuard()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="center_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var command=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await command.ExecuteNonQueryAsync();
        try
        {
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,m=>m.EndsWith("_PersonCenterUnitOperations",StringComparison.Ordinal));Assert.True(index>0);var prior=migrations[index-1];await db.GetService<IMigrator>().MigrateAsync(prior);
            var t=new PersonTenant("Fixture");var p=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,100);var person=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Fixture","Person",null,null,null,null);db.AddRange(t,p,person,new PersonPropertyScope(t.Id,p.Id));await db.SaveChangesAsync();
            var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM persons p WHERE id={person.Id}").SingleAsync();await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM persons p WHERE id={person.Id}").SingleAsync());
            var columns=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name='person_unit_operations' AND is_nullable='NO'").ToArrayAsync();foreach(var col in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled"})Assert.Contains(col,columns);
            await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();var relation=new UnitOwnership(t.Id,p.Id,u.Id,person.Id,new(2026,1,1));db.Add(relation);await db.SaveChangesAsync();var receipt=new PersonUnitOperation(t.Id,p.Id,person.Id,relation.Id,"OWNER","fixture","fixture",Guid.NewGuid(),new string('A',64),relation.Version);db.Add(receipt);await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE person_unit_operations SET applied_version=2 WHERE id={receipt.Id}"));await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));Assert.True(await db.Set<PersonUnitOperation>().AnyAsync(r=>r.Id==receipt.Id));
        }
        finally{await using var command=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await command.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
