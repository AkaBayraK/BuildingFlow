using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingFlow.Infrastructure;

internal static class BaseEntityConfiguration
{
    public static void Configure<T>(EntityTypeBuilder<T> builder, string table) where T : BaseEntity
    {
        builder.ToTable(table);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entity => entity.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(entity => entity.CreatedAt).HasColumnName("created_at");
        builder.Property(entity => entity.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        builder.Property(entity => entity.ModifiedAt).HasColumnName("modified_at");
        builder.Property(entity => entity.ModifiedBy).HasColumnName("modified_by").HasMaxLength(200);
        builder.Property(entity => entity.IsDeleted).HasColumnName("is_deleted");
        builder.Property(entity => entity.IsEnabled).HasColumnName("is_enabled");
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        BaseEntityConfiguration.Configure(builder, "sites");
        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(entity => entity.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(entity => entity.District).HasColumnName("district").HasMaxLength(100);
        builder.Property(entity => entity.Address).HasColumnName("address").HasMaxLength(500);
        builder.HasMany(entity => entity.Blocks).WithOne().HasForeignKey(entity => entity.SiteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Blocks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        BaseEntityConfiguration.Configure(builder, "blocks");
        builder.Property(entity => entity.SiteId).HasColumnName("site_id");
        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(100).HasColumnType("citext");
        builder.HasIndex(entity => new { entity.SiteId, entity.Name }).IsUnique().HasFilter("is_deleted = false");
        builder.HasMany(entity => entity.Apartments).WithOne().HasForeignKey(entity => entity.BlockId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Apartments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        BaseEntityConfiguration.Configure(builder, "apartments");
        builder.Property(entity => entity.BlockId).HasColumnName("block_id");
        builder.Property(entity => entity.Number).HasColumnName("number").HasMaxLength(30).HasColumnType("citext");
        builder.Property(entity => entity.Floor).HasColumnName("floor");
        builder.Property(entity => entity.GrossSquareMeters).HasColumnName("gross_square_meters").HasPrecision(10, 2);
        builder.HasIndex(entity => new { entity.BlockId, entity.Number }).IsUnique().HasFilter("is_deleted = false");
        builder.ToTable(table => table.HasCheckConstraint("ck_apartments_positive_area", "gross_square_meters > 0"));
    }
}
