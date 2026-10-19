using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class WebsiteMigrationTests
{
    [PostgresFact]public async Task UpgradeKeepsPropertiesAddsCommonColumnsGuardsPublicationsAndBlocksPopulatedRollback()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="website_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var command=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await command.ExecuteNonQueryAsync();
        try
        {
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,m=>m.EndsWith("_WebsiteCms",StringComparison.Ordinal));Assert.True(index>0);var prior=migrations[index-1];Assert.EndsWith("_PersonCenterUnitOperations",prior);var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync(prior);
            var p=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);db.Add(p);await db.SaveChangesAsync();
            var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync();await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync());
            foreach(var table in new[]{"website_sites","website_pages","website_sections","website_publications"}){var columns=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToArrayAsync();foreach(var col in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled"})Assert.Contains(col,columns);}
            // An empty module rolls back cleanly and re-applies.
            await migrator.MigrateAsync(prior);await db.Database.MigrateAsync();
            var site=new WebsiteSite(p.Id,Guid.NewGuid(),"fixture-site","Fixture");db.Add(site);await db.SaveChangesAsync();var publication=new WebsitePublication(p.Id,site.Id,Guid.NewGuid(),1,"{}",new string('a',64),DateTimeOffset.UtcNow,null,null);db.Add(publication);await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE website_publications SET snapshot={"{\"x\":1}"} WHERE id={publication.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM website_publications WHERE id={publication.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE website_sites SET primary_color={"#zzzzzz"} WHERE id={site.Id}"));
            Assert.Equal(1,await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE website_publications SET is_active=false WHERE id={publication.Id}"));
await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE website_publications SET is_active=true WHERE id={publication.Id}"));
            await Assert.ThrowsAsync<PostgresException>(()=>migrator.MigrateAsync(prior));Assert.Equal(migrations.Take(index+1),await db.Database.GetAppliedMigrationsAsync());Assert.True(await db.Set<WebsiteSite>().AnyAsync(s=>s.Id==site.Id));
        }
        finally{await using var command=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await command.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
