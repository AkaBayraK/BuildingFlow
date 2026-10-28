using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommonAreaMigrationTests
{
    [PostgresFact] public async Task UpgradePreservesExistingRowsRepeatEmptyRollbackAndHistoryGuard()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "common_area_verify_" + Guid.NewGuid().ToString("N"); await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseBuildingFlowNpgsql(cs.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options); var migrations = db.Database.GetMigrations().ToList();
            var target = migrations.Single(m => m.EndsWith("_CommonAreaDefinitions", StringComparison.Ordinal)); var previous = migrations[migrations.IndexOf(target) - 1]; var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(previous);
            var p = new Property("Preserve", "City", "District", "Address", PropertyType.SITE); var b = p.AddBuilding("A");
            var foreign = new Property("Foreign", "City", "District", "Address", PropertyType.SITE); var wrong = foreign.AddBuilding("B"); db.AddRange(p, b, foreign, wrong); await db.SaveChangesAsync();
            var before = await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync();
            await migrator.MigrateAsync(target); await migrator.MigrateAsync(target); LegacySchema.AssertCurrentModelMatchesMigrations();
            Assert.Equal(before, await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync());
            await migrator.MigrateAsync(previous); await migrator.MigrateAsync(target);
            var area = new CommonArea(p.Id, Guid.NewGuid(), new string('A', 64), "Area", CommonAreaType.POOL, b.Id, CommonAreaVisibility.BUILDING, CommonAreaUsage.OPEN_USE, 20, null, null, null, null, 0); db.Add(area); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_areas SET building_id={wrong.Id} WHERE id={area.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_areas SET capacity=0 WHERE id={area.Id}"));
            db.Remove(area); await db.SaveChangesAsync(); // Deleted history also blocks Down.
            await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(previous));
            Assert.Equal(target, (await db.Database.GetAppliedMigrationsAsync()).Last());
            Assert.Equal(1, await db.Set<CommonArea>().IgnoreQueryFilters().CountAsync());
            Assert.Equal(before, await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync());
        }
        finally { await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync(); }
    }
    [PostgresFact] public async Task ReservationMigrationUpgradesRepeatsRollsBackEmptyAndGuardsHistory()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "common_reservation_verify_" + Guid.NewGuid().ToString("N"); await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseBuildingFlowNpgsql(cs.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options); var migrations = db.Database.GetMigrations().ToList();
            var target = migrations.Single(m => m.EndsWith("_CommonAreaReservations", StringComparison.Ordinal)); var previous = migrations[migrations.IndexOf(target) - 1]; var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(previous);
            var p = new Property("Preserve", "City", "District", "Address", PropertyType.APARTMENT); var unit = p.AddUnit("1", 0, 80);
            var foreign = new Property("Foreign", "City", "District", "Address", PropertyType.APARTMENT); var wrong = foreign.AddUnit("1", 0, 80);
            var area = new CommonArea(p.Id, Guid.NewGuid(), new string('A', 64), "Area", CommonAreaType.SPORTS_COURT, null, CommonAreaVisibility.PROPERTY, CommonAreaUsage.RESERVATION, 14, null, null, null, null, 0);
            db.AddRange(p, unit, foreign, wrong); await db.SaveChangesAsync(); db.Add(area); await db.SaveChangesAsync();
            var before = await db.Database.SqlQuery<string>($"SELECT row_to_json(a)::text AS \"Value\" FROM common_areas a WHERE id={area.Id}").SingleAsync();
            await migrator.MigrateAsync(target); await migrator.MigrateAsync(target); LegacySchema.AssertCurrentModelMatchesMigrations();
            Assert.Equal(before, await db.Database.SqlQuery<string>($"SELECT row_to_json(a)::text AS \"Value\" FROM common_areas a WHERE id={area.Id}").SingleAsync());
            await migrator.MigrateAsync(previous); await migrator.MigrateAsync(target);
            var booking = new CommonAreaReservation(p.Id, area.Id, Guid.NewGuid(), new string('B', 64), unit.Id, new DateOnly(2030, 1, 1), new(10, 0), new(11, 0), 4, null);
            db.Add(booking); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_area_reservations SET unit_id={wrong.Id} WHERE id={booking.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_area_reservations SET ends_at='09:00' WHERE id={booking.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_area_reservations SET status='CANCELLED' WHERE id={booking.Id}"));
            booking.Cancel("Test", DateTimeOffset.UtcNow); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(previous));
            Assert.Equal(target, (await db.Database.GetAppliedMigrationsAsync()).Last());
            Assert.Equal(1, await db.Set<CommonAreaReservation>().CountAsync());
        }
        finally { await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync(); }
    }
    private sealed class Actor : ICurrentUserService { public string? UserId => "common-test"; public string? Username => "fixture"; public bool IsAuthenticated => true; public bool IsInRole(string role) => false; }
}
