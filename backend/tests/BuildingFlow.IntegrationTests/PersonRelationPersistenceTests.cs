using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class PersonRelationPersistenceTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private sealed record Fixture(Guid Tenant,Guid Property,Guid Unit,Guid Person,Guid Other);
    private async Task<Fixture> Seed()
    {
        var tenant=new PersonTenant("Fixture tenant");var property=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);var unit=property.AddUnit("1",1,100);
        var person=new CentralPerson(tenant.Id,"NATURAL","DOMESTIC","TR","12345678901","Fixture","Person",null,null,null,null);var other=new CentralPerson(tenant.Id,"NATURAL","DOMESTIC","TR","12345678901","Other","Person",null,null,null,null);
        await factory.WithDb(async db=>{db.AddRange(tenant,property,person,other,new PersonPropertyScope(tenant.Id,property.Id));await db.SaveChangesAsync();});return new(tenant.Id,property.Id,unit.Id,person.Id,other.Id);
    }
    [PostgresFact]public async Task DatabaseRejectsFiniteOverlapAndCrossKindPrimaryContactAndKeepsEndedHistory()
    {
        var s=await Seed();await factory.WithDb(async db=>{var owner=new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,new(2026,1,1),new(2026,1,3),true);db.Add(owner);await db.SaveChangesAsync();
            var overlap=new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,new(2026,1,3),new(2026,1,4));db.Add(overlap);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.Entry(overlap).State=EntityState.Detached;
            var primary=new UnitTenancy(s.Tenant,s.Property,s.Unit,s.Other,new(2026,1,2),new(2026,1,3),true);db.Add(primary);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.Entry(primary).State=EntityState.Detached;
            var next=new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,new(2026,1,4));db.Add(next);await db.SaveChangesAsync();next.End(new(2026,1,4));await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_ownerships SET end_date=DATE '2026-01-05' WHERE id={next.Id}"));
        });
    }
    [PostgresFact]public async Task DatabaseUsesExactSharesAndRejectsForeignTenantCompositeFk()
    {
        var s=await Seed();await factory.WithDb(async db=>{var one=new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,new(2026,1,1),null,false,long.MaxValue-1,long.MaxValue);db.Add(one);await db.SaveChangesAsync();
            var excessive=new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Other,new(2026,1,1),null,false,2,long.MaxValue);db.Add(excessive);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.Entry(excessive).State=EntityState.Detached;
            db.Add(new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Other,new(2026,1,1),null,false,1,long.MaxValue));await db.SaveChangesAsync();
            var foreignTenant=new PersonTenant("Foreign fixture");db.Add(foreignTenant);await db.SaveChangesAsync();var foreign=new UnitResidency(foreignTenant.Id,s.Property,s.Unit,s.Person,new(2026,1,1));db.Add(foreign);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.Entry(foreign).State=EntityState.Detached;
        });
    }
    [PostgresFact]public async Task CompetingFinitePrimaryContactsAcrossTablesAllowOnlyOneCommit()
    {
        var s=await Seed();
        async Task<bool> Write(bool owner)
        {
            var success=false;await factory.WithDb(async db=>{BaseEntity row=owner?new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Person,new(2026,1,1),new(2026,1,2),true):new UnitTenancy(s.Tenant,s.Property,s.Unit,s.Other,new(2026,1,1),new(2026,1,2),true);db.Add(row);try{await db.SaveChangesAsync();success=true;}catch(DbUpdateException){db.Entry(row).State=EntityState.Detached;}});return success;
        }
        var results=await Task.WhenAll(Write(true),Write(false));Assert.Single(results,r=>r);
    }
    [PostgresFact]public async Task GuardMigrationReappliesPreservesLegacyJsonAndRejectsRollbackWithHistory()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="person_relation_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var c=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await c.ExecuteNonQueryAsync();
        try
        {
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;await using var db=new BuildingFlowDbContext(options);
            var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,m=>m.EndsWith("_PersonUnitRelations",StringComparison.Ordinal));Assert.True(index>0);var prior=migrations[index-1];await db.GetService<IMigrator>().MigrateAsync(prior);
            var property=new Property("Legacy fixture","City","District","Address");var person=new Person(property.Id,"Legacy","Person",null,null);db.AddRange(property,person);await db.SaveChangesAsync();var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM people p WHERE id={person.Id}").SingleAsync();
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM people p WHERE id={person.Id}").SingleAsync());
            foreach(var table in new[]{"person_property_scopes","legacy_person_maps","legacy_occupancy_maps","unit_ownerships","unit_tenancies","unit_residencies"}){var columns=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToArrayAsync();foreach(var column in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled"})Assert.Contains(column,columns);}
            await db.GetService<IMigrator>().MigrateAsync(prior);await db.Database.MigrateAsync();var tenant=new PersonTenant("Fixture");db.AddRange(tenant,new PersonPropertyScope(tenant.Id,property.Id));await db.SaveChangesAsync();await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));Assert.Equal(migrations[index],(await db.Database.GetAppliedMigrationsAsync()).Last());
        }
        finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
