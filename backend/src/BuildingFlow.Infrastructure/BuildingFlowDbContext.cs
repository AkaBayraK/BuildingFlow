using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingFlow.Infrastructure;

public sealed class BuildingFlowDbContext(DbContextOptions<BuildingFlowDbContext> options) : DbContext(options)
{
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Apartment> Apartments => Set<Apartment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BuildingFlowDbContext).Assembly);
    }
}
