using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace BuildingFlow.IntegrationTests;

/// <summary>
/// Upgrade tests write rows at an older migration point; columns added by later migrations do not exist there yet.
/// </summary>
internal static class LegacySchema
{
    public static DbContextOptionsBuilder<BuildingFlowDbContext> BeforeApartmentDetails(this DbContextOptionsBuilder<BuildingFlowDbContext> options) =>
        options.ReplaceService<IModelCustomizer, WithoutApartmentDetails>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

    public static void AssertCurrentModelMatchesMigrations()
    {
        using var current = new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql("Host=localhost;Database=model_only").Options);
        Assert.False(current.Database.HasPendingModelChanges());
    }

    private sealed class WithoutApartmentDetails(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            foreach (var name in new[] { "Neighborhood", "ConstructionYear", "HeatingType", "HasElevator", "ParkingType" })
                modelBuilder.Entity<Property>().Ignore(name);
            foreach (var name in new[] { "ConstructionYear", "HeatingType", "HasElevator", "ParkingType" })
                modelBuilder.Entity<Building>().Ignore(name);
            foreach (var name in new[] { "NetSquareMeters", "RoomLayout", "FloorPosition", "IsMezzanine", "BathroomCount", "KitchenType", "HasBalcony", "HeatingType" })
                modelBuilder.Entity<Unit>().Ignore(name);
        }
    }
}
