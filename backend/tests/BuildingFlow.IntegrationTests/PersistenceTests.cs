using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class PersistenceTests
{
    private static BuildingFlowDbContext Context(string connection) => new(
        new DbContextOptionsBuilder<BuildingFlowDbContext>()
            .UseNpgsql(connection)
            .AddInterceptors(new AuditInterceptor(new TestUser(), TimeProvider.System))
            .Options);

    [Fact]
    public void ModelHasExpectedRelationalMapping()
    {
        using var db = Context("Host=localhost;Database=model_only");
        var entity = db.Model.FindEntityType(typeof(Site))!;
        var table = StoreObjectIdentifier.Table("sites", null);

        Assert.Equal("created_at", entity.FindProperty(nameof(Site.CreatedAt))!.GetColumnName(table));
        Assert.True(entity.FindProperty(nameof(Site.Version))!.IsConcurrencyToken);
        Assert.NotEmpty(entity.GetDeclaredQueryFilters());
        Assert.All(db.Model.GetEntityTypes().SelectMany(type => type.GetForeignKeys()),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [PostgresFact]
    public async Task AuditSoftDeleteConcurrencyAndUniqueConstraints()
    {
        var connection = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        // Use a dedicated test database. Never reset a production/application database.
        await using var setup = Context(connection);
        await setup.Database.MigrateAsync();
        Assert.True(await setup.Database.CanConnectAsync());

        var site = new Site("Integration", "City", "District", "Address");
        var block = site.AddBlock("A");
        var apartment = block.AddApartment("1", 0, 100);
        setup.Add(site);
        await setup.SaveChangesAsync();

        Assert.Equal(1, site.Version);
        Assert.Equal("test-user", site.CreatedBy);
        Assert.Equal(site.CreatedAt, site.ModifiedAt);
        Assert.Equal("test-user", apartment.CreatedBy);
        var createdAt = site.CreatedAt;

        await using var first = Context(connection);
        await using var second = Context(connection);
        var firstCopy = await first.Sites.SingleAsync(item => item.Id == site.Id);
        var staleCopy = await second.Sites.SingleAsync(item => item.Id == site.Id);
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
        var loadedSite = await duplicate.Sites.SingleAsync(item => item.Id == site.Id);
        loadedSite.AddBlock("a");
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());

        await using var duplicateApartment = Context(connection);
        var loadedBlock = await duplicateApartment.Blocks.SingleAsync(item => item.Id == block.Id);
        loadedBlock.AddApartment("1", 1, 120);
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateApartment.SaveChangesAsync());

        first.Remove(firstCopy);
        await first.SaveChangesAsync();
        Assert.False(await first.Sites.AnyAsync(item => item.Id == site.Id));
        Assert.True(await first.Sites.IgnoreQueryFilters().AnyAsync(item => item.Id == site.Id && item.IsDeleted));
        // Soft-deleting a parent does not cascade and erase historical children.
        Assert.True(await first.Apartments.AnyAsync(item => item.Id == apartment.Id));
    }

    private sealed class TestUser : ICurrentUserService
    {
        public string? UserId => "test-user";
        public string? Username => "tester";
        public bool IsAuthenticated => true;
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
