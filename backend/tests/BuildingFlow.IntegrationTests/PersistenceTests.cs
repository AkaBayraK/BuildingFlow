using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class PersistenceTests
{
    private static readonly (string Property, string Column, Type ClrType, string SqlType)[] RequiredColumns =
    [
        (nameof(BaseEntity.Id), "id", typeof(Guid), "uuid"),
        (nameof(BaseEntity.Version), "version", typeof(long), "bigint"),
        (nameof(BaseEntity.CreatedAt), "created_at", typeof(DateTimeOffset), "timestamp with time zone"),
        (nameof(BaseEntity.CreatedBy), "created_by", typeof(string), "character varying"),
        (nameof(BaseEntity.ModifiedAt), "modified_at", typeof(DateTimeOffset), "timestamp with time zone"),
        (nameof(BaseEntity.ModifiedBy), "modified_by", typeof(string), "character varying"),
        (nameof(BaseEntity.IsDeleted), "is_deleted", typeof(bool), "boolean"),
        (nameof(BaseEntity.IsEnabled), "is_enabled", typeof(bool), "boolean"),
    ];

    private static BuildingFlowDbContext Context(string connection) => new(
        new DbContextOptionsBuilder<BuildingFlowDbContext>()
            .UseNpgsql(connection)
            .AddInterceptors(new AuditInterceptor(new TestUser(), TimeProvider.System))
            .Options);

    [Fact]
    public void ModelHasExpectedRelationalMapping()
    {
        using var db = Context("Host=localhost;Database=model_only");
        var entities = db.Model.GetEntityTypes().Where(entity => entity.GetTableName() is not null).ToArray();
        Assert.NotEmpty(entities);
        foreach (var entity in entities)
        {
            Assert.True(typeof(BaseEntity).IsAssignableFrom(entity.ClrType), $"{entity.Name} must inherit BaseEntity.");
            var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            foreach (var expected in RequiredColumns)
            {
                var property = entity.FindProperty(expected.Property);
                Assert.NotNull(property);
                Assert.Equal(expected.Column, property.GetColumnName(table));
                Assert.False(property.IsNullable);
                Assert.Equal(expected.ClrType, property.ClrType);
            }
            Assert.Equal(nameof(BaseEntity.Id), Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
            Assert.True(entity.FindProperty(nameof(BaseEntity.Version))!.IsConcurrencyToken);
            Assert.NotEmpty(entity.GetDeclaredQueryFilters());
        }
        Assert.All(db.Model.GetEntityTypes().SelectMany(type => type.GetForeignKeys()),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [PostgresFact]
    public async Task EveryApplicationTableHasRequiredAuditColumns()
    {
        await using var db = Context(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!);
        await db.Database.MigrateAsync();
        await db.Database.OpenConnectionAsync();
        foreach (var entity in db.Model.GetEntityTypes().Where(entity => entity.GetTableName() is not null))
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT column_name, data_type, is_nullable
                FROM information_schema.columns
                WHERE table_schema = @schema AND table_name = @table
                """;
            var schema = command.CreateParameter();
            schema.ParameterName = "schema";
            schema.Value = entity.GetSchema() ?? "public";
            command.Parameters.Add(schema);
            var table = command.CreateParameter();
            table.ParameterName = "table";
            table.Value = entity.GetTableName()!;
            command.Parameters.Add(table);
            await using var reader = await command.ExecuteReaderAsync();
            Dictionary<string, (string Type, string Nullable)> columns = [];
            while (await reader.ReadAsync()) columns.Add(reader.GetString(0), (reader.GetString(1), reader.GetString(2)));
            foreach (var expected in RequiredColumns)
            {
                Assert.True(columns.TryGetValue(expected.Column, out var column), $"{table.Value} is missing {expected.Column}.");
                Assert.Equal(expected.SqlType, column.Type);
                Assert.Equal("NO", column.Nullable);
            }
        }
    }

    [PostgresFact]
    public async Task AuditSoftDeleteConcurrencyAndUniqueConstraints()
    {
        var connection = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        // Use a dedicated test database. Never reset a production/application database.
        await using var setup = Context(connection);
        await setup.Database.MigrateAsync();
        Assert.True(await setup.Database.CanConnectAsync());

        var site = new Property("Integration", "City", "District", "Address");
        var block = site.AddBuilding("A");
        var apartment = block.AddUnit("1", 0, 100);
        setup.Add(site);
        await setup.SaveChangesAsync();

        Assert.Equal(1, site.Version);
        Assert.Equal("test-user", site.CreatedBy);
        Assert.Equal(site.CreatedAt, site.ModifiedAt);
        Assert.Equal("test-user", apartment.CreatedBy);
        var createdAt = site.CreatedAt;

        await using var first = Context(connection);
        await using var second = Context(connection);
        var firstCopy = await first.Properties.SingleAsync(item => item.Id == site.Id);
        var staleCopy = await second.Properties.SingleAsync(item => item.Id == site.Id);
        firstCopy.SetEnabled(false);
        first.Entry(firstCopy).Property(item => item.CreatedBy).CurrentValue = "tampered";
        first.Entry(firstCopy).Property(item => item.CreatedAt).CurrentValue = createdAt.AddDays(1);
        await first.SaveChangesAsync();
        await first.Entry(firstCopy).ReloadAsync();

        Assert.Equal(2, firstCopy.Version);
        Assert.Equal(createdAt, firstCopy.CreatedAt);
        Assert.Equal("test-user", firstCopy.CreatedBy);
        Assert.True(firstCopy.ModifiedAt >= createdAt);
        staleCopy.SetEnabled(false);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        // Deliberately omit loading children: relational constraints must still prevent duplicates.
        await using var duplicate = Context(connection);
        var loadedSite = await duplicate.Properties.SingleAsync(item => item.Id == site.Id);
        loadedSite.AddBuilding("a");
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());

        await using var duplicateApartment = Context(connection);
        var loadedBlock = await duplicateApartment.Buildings.SingleAsync(item => item.Id == block.Id);
        loadedBlock.AddUnit("1", 1, 120);
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateApartment.SaveChangesAsync());

        first.Remove(firstCopy);
        await first.SaveChangesAsync();
        Assert.False(await first.Properties.AnyAsync(item => item.Id == site.Id));
        Assert.True(await first.Properties.IgnoreQueryFilters().AnyAsync(item => item.Id == site.Id && item.IsDeleted));
        // Soft-deleting a parent does not cascade and erase historical children.
        Assert.True(await first.Units.AnyAsync(item => item.Id == apartment.Id));
    }

    private sealed class TestUser : ICurrentUserService
    {
        public string? UserId => "test-user";
        public string? Username => "tester";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")))
        {
            Skip = "Set BUILDINGFLOW_TEST_DATABASE to a dedicated PostgreSQL test database.";
        }
    }
}
