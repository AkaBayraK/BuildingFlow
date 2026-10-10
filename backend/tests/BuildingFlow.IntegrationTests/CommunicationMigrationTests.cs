using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommunicationMigrationTests
{
    [PostgresTheory][InlineData(false)][InlineData(true)]
    public async Task UpgradePreservesExistingRowsAndContentHistoryBlocksDestructiveDown(bool documentOnly)
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var config=new NpgsqlConnectionStringBuilder(original);if(config.Database is null||!config.Database.Contains("test",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Dedicated test database required.");
        var schema="communication_migration_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try{
            config.SearchPath=schema+",public";await using var db=new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(config.ConnectionString,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options);
            var previous=db.Database.GetMigrations().Single(v=>v.EndsWith("_ExpensesAndBudgets",StringComparison.Ordinal));var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync(previous);
            var p=new Property("Upgrade preserved","City","District","Address",PropertyType.APARTMENT);db.Add(p);await db.SaveChangesAsync();var id=p.Id;
            var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={id}").SingleAsync();await db.Database.MigrateAsync();await db.Database.MigrateAsync();
            Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={id}").SingleAsync());Assert.False(db.Database.HasPendingModelChanges());
            await migrator.MigrateAsync(previous);await db.Database.MigrateAsync();
            foreach(var table in new[]{"announcements","stored_documents"}){var columns=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();foreach(var c in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled"})Assert.Contains(c,columns);}
            if(documentOnly){var doc=new StoredDocument(id,Guid.NewGuid(),new string('A',64),null,ContentAudience.ALL_MEMBERS,"test.pdf","application/pdf",10,new string('B',64));db.Add(doc);await db.SaveChangesAsync();db.Remove(doc);await db.SaveChangesAsync();}
            else{var a=new Announcement(id,"Title","Content",ContentAudience.ALL_MEMBERS);db.Add(a);await db.SaveChangesAsync();var other=new Property("Other","City","District","Address",PropertyType.APARTMENT);db.Add(other);await db.SaveChangesAsync();var doc=new StoredDocument(id,Guid.NewGuid(),new string('A',64),a.Id,ContentAudience.ALL_MEMBERS,"test.pdf","application/pdf",10,new string('B',64));db.Add(doc);await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE stored_documents SET property_id={other.Id} WHERE id={doc.Id}"));await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE announcements SET status={"PUBLISHED"} WHERE id={a.Id}"));}
            await Assert.ThrowsAsync<PostgresException>(()=>migrator.MigrateAsync(previous));Assert.Equal(db.Database.GetMigrations(),await db.Database.GetAppliedMigrationsAsync());
        }finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService {public string? UserId=>"communication-migration";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
