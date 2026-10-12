using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class PropertyMigrationTests
{
    [PostgresFact]
    public async Task RenamePreservesFullHierarchyAndDowngradeRejectsLossyData()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="property_migration_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try {
            var connection=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};await using var db=new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(connection.ConnectionString,options=>options.MigrationsHistoryTable("__EFMigrationsHistory",schema)).Options);
            var migrations=db.Database.GetMigrations().ToArray();await db.GetService<IMigrator>().MigrateAsync(migrations.Single(value=>value.EndsWith("_SiteMembershipAccess",StringComparison.Ordinal)));
            var property=Guid.NewGuid();var building=Guid.NewGuid();var unit=Guid.NewGuid();var member=Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO sites VALUES ({property},{"Legacy"},{"City"},{"District"},{"Address"},{7L},now(),{"actor"},now(),{"actor"},{false},{true});
                INSERT INTO blocks VALUES ({building},{property},{"A"},{5L},now(),{"actor"},now(),{"actor"},{false},{true});
                INSERT INTO apartments VALUES ({unit},{building},{"1"},{-1},{120.5m},{3L},now(),{"actor"},now(),{"actor"},{false},{true});
                INSERT INTO site_memberships VALUES ({member},{property},{"user"},{2L},now(),{"actor"},now(),{"actor"},{false},{true});
                """);
            await db.Database.MigrateAsync();
            var saved=await db.Properties.AsNoTracking().SingleAsync();Assert.Equal(property,saved.Id);Assert.Equal(7,saved.Version);Assert.Equal(PropertyType.SITE,saved.Type);Assert.Equal("actor",saved.CreatedBy);
            var savedUnit=await db.Units.AsNoTracking().SingleAsync();Assert.Equal(unit,savedUnit.Id);Assert.Equal(building,savedUnit.BuildingId);Assert.Equal(property,savedUnit.PropertyId);Assert.Equal(3,savedUnit.Version);Assert.Equal(UnitType.APARTMENT,savedUnit.Type);Assert.Equal(120.5m,savedUnit.GrossSquareMeters);
            Assert.Equal(member,(await db.PropertyMemberships.AsNoTracking().SingleAsync()).Id);Assert.Equal(5,(await db.Buildings.AsNoTracking().SingleAsync()).Version);
            var legacy=migrations.Single(value=>value.EndsWith("_SiteMembershipAccess",StringComparison.Ordinal));
            await db.GetService<IMigrator>().MigrateAsync(legacy);await db.Database.MigrateAsync();
            Assert.Equal(saved.CreatedAt,(await db.Properties.AsNoTracking().SingleAsync()).CreatedAt);Assert.Equal(savedUnit.ModifiedAt,(await db.Units.AsNoTracking().SingleAsync()).ModifiedAt);
            var direct=Guid.NewGuid();await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO properties (id,name,city,district,address,type,floor_count,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
                VALUES ({direct},{"Standalone"},{"City"},{"District"},{"Address"},{"APARTMENT"},{8},{1L},now(),{"actor"},now(),{"actor"},{false},{true})
                """);
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(legacy));
            await db.Database.MigrateAsync();
            Assert.Equal(2,await db.Properties.CountAsync());Assert.Equal(migrations,await db.Database.GetAppliedMigrationsAsync());
            var person = Guid.NewGuid(); var occupancy = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO people (id,property_id,first_name,last_name,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
                VALUES ({person},{property},{"Migration"},{"History"},{1L},now(),{"actor"},now(),{"actor"},{false},{true});
                INSERT INTO unit_occupancies (id,property_id,person_id,unit_id,type,start_date,end_date,is_primary_contact,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
                VALUES ({occupancy},{property},{person},{unit},{"OWNER"},{new DateOnly(2026,10,1)},{new DateOnly(2026,10,2)},{false},{1L},now(),{"actor"},now(),{"actor"},{false},{true});
                """);
            var unified = migrations.Single(value=>value.EndsWith("_UnifiedPropertyModel",StringComparison.Ordinal));
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(unified));
            // Newer empty migrations can already have rolled back before the
            // older history guard refuses. Restore latest, then verify history.
            await db.Database.MigrateAsync();
            Assert.Equal(person,(await db.People.AsNoTracking().SingleAsync()).Id);
            Assert.Equal(occupancy,(await db.UnitOccupancies.AsNoTracking().SingleAsync()).Id);
            Assert.Equal(migrations,await db.Database.GetAppliedMigrationsAsync());
        } finally {await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
}
