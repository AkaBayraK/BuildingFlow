using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class ManagementAssignmentMigrationTests
{
    [PostgresFact]public async Task UpgradeIsAdditiveWithCommonColumnsScopeChecksFiniteOverlapAndPopulatedDownGuard()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="management_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try
        {
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;await using var db=new BuildingFlowDbContext(options);
            var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,m=>m.EndsWith("_ManagementAssignments",StringComparison.Ordinal));Assert.True(index>0);var prior=migrations[index-1];await db.GetService<IMigrator>().MigrateAsync(prior);
            var property=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);var tenant=new PersonTenant("Fixture");var person=new CentralPerson(tenant.Id,"NATURAL","DOMESTIC","TR","12345678901","Fixture","Person",null,null,null,null);db.AddRange(property,tenant,person,new PersonPropertyScope(tenant.Id,property.Id));await db.SaveChangesAsync();var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM persons p WHERE id={person.Id}").SingleAsync();
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM persons p WHERE id={person.Id}").SingleAsync());
            foreach(var table in new[]{"management_assignments","management_assignment_events","management_assignment_operations"}){var columns=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToArrayAsync();foreach(var col in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled"})Assert.Contains(col,columns);}
            await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();var row=new ManagementAssignment(tenant.Id,property.Id,person.Id,"MANAGER",new(2026,1,1),new(2026,1,3));db.Add(row);await db.SaveChangesAsync();
            var overlap=new ManagementAssignment(tenant.Id,property.Id,person.Id,"MANAGER",new(2026,1,3),new(2026,1,4));db.Add(overlap);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.Entry(overlap).State=EntityState.Detached;
            var foreign=new PersonTenant("Foreign fixture");db.Add(foreign);await db.SaveChangesAsync();var invalid=new ManagementAssignment(foreign.Id,property.Id,person.Id,"MANAGER",new(2026,1,1));db.Add(invalid);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.Entry(invalid).State=EntityState.Detached;
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE management_assignments SET end_date=DATE '2026-01-04' WHERE id={row.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE management_assignments SET cancelled_at=now(),cancellation_reason=NULL WHERE id={row.Id}"));
            var cancelled=new ManagementAssignment(tenant.Id,property.Id,person.Id,"ACCOUNTANT",new(2026,1,1));db.Add(cancelled);await db.SaveChangesAsync();cancelled.Cancel(DateTimeOffset.UtcNow,"Revoked");await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE management_assignments SET end_date=DATE '2026-01-03' WHERE id={cancelled.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));Assert.Equal(migrations[index],(await db.Database.GetAppliedMigrationsAsync()).Last());Assert.Equal(2,await db.Set<ManagementAssignment>().CountAsync());
        }
        finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
