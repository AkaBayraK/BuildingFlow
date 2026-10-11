using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildingFlow.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BuildingFlowDbContext>
{
    public BuildingFlowDbContext CreateDbContext(string[] args)
    {
        // Migration generation requires no credentials; database update needs an explicit connection.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Database=buildingflow";
        return new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>()
            .UseBuildingFlowNpgsql(connection).Options);
    }
}
