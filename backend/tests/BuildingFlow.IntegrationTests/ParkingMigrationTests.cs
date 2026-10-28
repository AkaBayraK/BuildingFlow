using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Araçlar/Otopark: upgrade and repeat apply, audit columns, scope FKs, active plate/open allocation uniqueness and Down guard.
public sealed class ParkingMigrationTests
{
    private static readonly string[] Tables = ["parking_policies", "parking_facilities", "parking_levels", "parking_spaces", "vehicles", "parking_allocations", "guest_vehicles", "parking_closures"];
    private static readonly string[] AuditColumns = ["id", "version", "created_at", "created_by", "modified_at", "modified_by", "is_deleted", "is_enabled"];

    [PostgresFact]
    public async Task UpgradeEnforcesScopeUniquenessAndDownGuard()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "parking_verify_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options);
            var migrations = db.Database.GetMigrations().ToList();
            var target = migrations.Single(x => x.EndsWith("_ParkingFoundation", StringComparison.Ordinal));
            var previous = migrations[migrations.IndexOf(target) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            Assert.Equal(migrations.Last(), (await db.Database.GetAppliedMigrationsAsync()).Last());
            LegacySchema.AssertCurrentModelMatchesMigrations();
            foreach (var table in Tables)
            {
                var fields = await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();
                foreach (var field in AuditColumns) Assert.Contains(field, fields);
            }

            var site = new Property("Otopark", "City", "District", "Address", PropertyType.SITE);
            var other = new Property("Diğer", "City", "District", "Address", PropertyType.SITE);
            var block = site.AddBuilding("A"); var unit = block.AddUnit("1", 1, 100);
            var foreignUnit = other.AddBuilding("B").AddUnit("1", 1, 100);
            var person = new Person(site.Id, "Ayşe", "Yılmaz");
            db.AddRange(site, other, person); await db.SaveChangesAsync();

            var policy = new ParkingPolicy(site.Id, 2, 2, 1, 24);
            var facility = new ParkingFacility(site.Id, block.Id, "A Blok Otoparkı");
            var level = new ParkingLevel(site.Id, facility.Id, "B1", -1);
            var space = new ParkingSpace(site.Id, facility.Id, level.Id, "A01", ParkingSpaceType.STANDARD);
            var vehicle = new Vehicle(site.Id, person.Id, unit.Id, "34ABC123", VehicleType.CAR, "BMW", null, null);
            db.AddRange(policy, facility, level, space, vehicle); await db.SaveChangesAsync();
            db.AddRange(new ParkingAllocation(site.Id, space, unit.Id, vehicle.Id, new(2026, 11, 1)),
                new GuestVehicle(policy, unit.Id, "34TST100", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(4), null, null));
            await db.SaveChangesAsync();

            await AssertRejected(db, new Vehicle(site.Id, person.Id, unit.Id, "34 abc 123", VehicleType.CAR, null, null, null));
            await AssertRejected(db, new Vehicle(site.Id, person.Id, foreignUnit.Id, "35GHI321", VehicleType.CAR, null, null, null));
            await AssertRejected(db, new ParkingAllocation(site.Id, space, unit.Id, null, new(2027, 1, 1)));
            await AssertRejected(db, new ParkingFacility(site.Id, null, "a blok otoparkı"));
            var rejected = new Vehicle(site.Id, person.Id, unit.Id, "06DEF456", VehicleType.CAR, null, null, null);
            rejected.Reject(DateTimeOffset.UtcNow, "Ruhsat eksik"); db.Add(rejected); await db.SaveChangesAsync();
            db.Add(new Vehicle(site.Id, person.Id, unit.Id, "06DEF456", VehicleType.CAR, null, null, null)); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE vehicles SET status='REJECTED' WHERE id={vehicle.Id}"));

            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous));
            Assert.Equal(target, (await db.Database.GetAppliedMigrationsAsync()).Last());
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync();
        }
    }

    [PostgresFact]
    public async Task ClosureUpgradePreservesExistingDataAndGuardsHistory()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "parking_closure_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            var options = new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options;
            await using var db = new BuildingFlowDbContext(options);
            var migrations = db.Database.GetMigrations().ToList(); var latest = migrations.Last(); var previous = migrations[migrations.FindIndex(x => x.EndsWith("_ParkingMaintenanceClosures")) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var p = new Property("Closure", "City", "District", "Address", PropertyType.SITE);
            var f = new ParkingFacility(p.Id, null, "Garage"); var space = new ParkingSpace(p.Id, f.Id, null, "C1", ParkingSpaceType.STANDARD);
            var other = new Property("Other", "City", "District", "Address", PropertyType.SITE);
            db.AddRange(p, other, f, space); await db.SaveChangesAsync();
            var before = await db.Database.SqlQuery<string>($"SELECT to_jsonb(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync();
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            var after = await db.Database.SqlQuery<string>($"SELECT to_jsonb(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync(); Assert.Equal(before, after);
            // Empty rollback/re-up is safe.
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.Database.MigrateAsync();
            var start = DateTimeOffset.UtcNow; var request = Guid.NewGuid();
            var closure = new ParkingClosure(p.Id, space.Id, request, start, start.AddHours(1), "Repair");
            db.Add(closure); await db.SaveChangesAsync();
            await AssertRejected(db, new ParkingClosure(other.Id, space.Id, Guid.NewGuid(), start, start.AddHours(1), "Wrong scope"));
            await AssertRejected(db, new ParkingClosure(p.Id, space.Id, request, start, start.AddHours(1), "Duplicate"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE parking_closures SET ends_at=starts_at WHERE id={closure.Id}"));
            closure.Cancel(start, "Done"); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous));
            // EF commits each completed migration separately. Empty retention Down can complete before closure Down refuses.
            Assert.EndsWith("_ParkingMaintenanceClosures", (await db.Database.GetAppliedMigrationsAsync()).Last());
            await db.Database.MigrateAsync();
            Assert.Equal(latest, (await db.Database.GetAppliedMigrationsAsync()).Last());
            Assert.Equal("Done", (await db.ParkingClosures.AsNoTracking().SingleAsync()).CancellationReason);
            LegacySchema.AssertCurrentModelMatchesMigrations();
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync();
        }
    }

    [PostgresFact]
    public async Task RetentionMigrationPreservesExistingRowsAndProtectsRedactionHistoryOnRollback()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema="parking_retention_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try{
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};
            var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
            await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToList();var previous=migrations[^2];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var p=new Property("Retention","City","District","Address",PropertyType.APARTMENT);var unit=p.AddUnit("1",1,100);
            var person=new Person(p.Id,"Test","Person",null,null);db.AddRange(p,person);await db.SaveChangesAsync();
            // Insert an old-schema row through SQL: current EF model already includes the new nullable field.
            var id=Guid.NewGuid();var request=Guid.NewGuid();var now=DateTimeOffset.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO vehicles (id,property_id,person_id,unit_id,request_id,plate,type,status,reviewed_at,review_reason,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled) VALUES ({id},{p.Id},{person.Id},{unit.Id},{request},'34OLD99','CAR','REJECTED',{now.AddDays(-31)},'old reason',1,{now},'fixture',{now},'fixture',false,true)");
            var before=await db.Database.SqlQuery<string>($"SELECT to_jsonb(v)::text AS \"Value\" FROM vehicles v WHERE id={id}").SingleAsync();
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();
            var after=await db.Database.SqlQuery<string>($"SELECT (to_jsonb(v)-'personal_data_redacted_at')::text AS \"Value\" FROM vehicles v WHERE id={id}").SingleAsync();Assert.Equal(before,after);
            await db.GetService<IMigrator>().MigrateAsync(previous);await db.Database.MigrateAsync();
            var v=await db.Vehicles.SingleAsync(v=>v.Id==id);Assert.Null(v.PersonalDataRedactedAt);Assert.True(v.Redact(now,30));await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));
            Assert.Equal(migrations.Last(),(await db.Database.GetAppliedMigrationsAsync()).Last());
            LegacySchema.AssertCurrentModelMatchesMigrations();
        }finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }

    private static async Task AssertRejected(BuildingFlowDbContext db, object entity)
    {
        db.Add(entity);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList().ForEach(e => e.State = EntityState.Detached);
    }

    private sealed class Actor : ICurrentUserService
    {
        public string? UserId => "parking-test";
        public string? Username => "fixture";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
    }
}
