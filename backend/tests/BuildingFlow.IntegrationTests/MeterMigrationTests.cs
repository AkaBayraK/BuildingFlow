using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MeterMigrationTests
{
    [PostgresFact] public async Task UpgradeRepeatAuditCompositeFkImmutableHistoryAndDownGuard()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="meter_verify_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try{var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToArray();var target=migrations.Last();var previous=migrations[^2];await db.GetService<IMigrator>().MigrateAsync(previous);var p=new Property("Preserved","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,80);var other=new Property("Other","City","District","Address",PropertyType.APARTMENT);db.AddRange(p,other);await db.SaveChangesAsync();var version=p.Version;await db.Database.MigrateAsync();await db.Database.MigrateAsync();Assert.Equal(version,(await db.Properties.AsNoTracking().SingleAsync(x=>x.Id==p.Id)).Version);Assert.False(db.Database.HasPendingModelChanges());foreach(var entity in db.Model.GetEntityTypes().Where(e=>typeof(BaseEntity).IsAssignableFrom(e.ClrType))){var table=entity.GetTableName()!;var fields=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();foreach(var field in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled"})Assert.Contains(field,fields);}
        var meter=new Meter(p.Id,u.Id,Guid.NewGuid(),new string('a',64),"Water",MeterKind.WATER);db.Add(meter);await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE meters SET property_id={other.Id} WHERE id={meter.Id}"));var r=new MeterReading(p.Id,meter.Id,new(2026,10,1));db.Add(r);db.Add(r.Revise(Guid.NewGuid(),new string('a',64),0,"Baseline"));await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE meter_readings SET value={-1m} WHERE id={r.Id}"));await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE meter_readings SET is_deleted=true WHERE id={r.Id}"));await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));Assert.Equal(target,(await db.Database.GetAppliedMigrationsAsync()).Last());Assert.Single(await db.MeterReadingRevisions.ToListAsync());}
        finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"meter-test";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
