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

internal static class BuildingFeatureConfiguration
{
    public const string HeatingTypes = "'NONE','STOVE','GAS_STOVE','FLOOR_HEATER','CENTRAL','CENTRAL_METERED','COMBI','UNDERFLOOR','AIR_CONDITIONING','OTHER'";
    public static void Configure<T>(EntityTypeBuilder<T> builder, string table) where T : BaseEntity
    {
        builder.Property<int?>("ConstructionYear").HasColumnName("construction_year");
        builder.Property<HeatingType?>("HeatingType").HasColumnName("heating_type").HasConversion<string>().HasMaxLength(30);
        builder.Property<bool?>("HasElevator").HasColumnName("has_elevator");
        builder.Property<ParkingType?>("ParkingType").HasColumnName("parking_type").HasConversion<string>().HasMaxLength(20);
        builder.ToTable(value =>
        {
            value.HasCheckConstraint($"ck_{table}_construction_year", "construction_year IS NULL OR construction_year BETWEEN 1800 AND 9999");
            value.HasCheckConstraint($"ck_{table}_heating_type", $"heating_type IS NULL OR heating_type IN ({HeatingTypes})");
            value.HasCheckConstraint($"ck_{table}_parking_type", "parking_type IS NULL OR parking_type IN ('OPEN','CLOSED','OPEN_AND_CLOSED','NONE')");
        });
    }
}

internal sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        BaseEntityConfiguration.Configure(builder, "properties");
        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(entity => entity.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.FloorCount).HasColumnName("floor_count");
        builder.ToTable(table => { table.HasCheckConstraint("ck_properties_type", "type IN ('APARTMENT','SITE','RESIDENCE','MIXED')"); table.HasCheckConstraint("ck_properties_floor_count", "floor_count IS NULL OR (type = 'APARTMENT' AND floor_count > 0)"); });
        builder.HasMany(entity => entity.Units).WithOne().HasForeignKey(entity => entity.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Units).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(entity => entity.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(entity => entity.District).HasColumnName("district").HasMaxLength(100);
        builder.Property(entity => entity.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(entity => entity.Neighborhood).HasColumnName("neighborhood").HasMaxLength(100);
        builder.Ignore(entity => entity.Features);
        BuildingFeatureConfiguration.Configure(builder, "properties");
        builder.ToTable(table => table.HasCheckConstraint("ck_properties_building_features", "type = 'APARTMENT' OR (construction_year IS NULL AND heating_type IS NULL AND has_elevator IS NULL AND parking_type IS NULL)"));
        builder.HasMany(entity => entity.Buildings).WithOne().HasForeignKey(entity => entity.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Buildings).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        BaseEntityConfiguration.Configure(builder, "buildings");
        builder.Property(entity => entity.PropertyId).HasColumnName("property_id");
        builder.Property(entity => entity.FloorCount).HasColumnName("floor_count");
        builder.Ignore(entity => entity.Features);
        BuildingFeatureConfiguration.Configure(builder, "buildings");
        builder.HasAlternateKey(entity => new { entity.PropertyId, entity.Id });
        builder.ToTable(table => table.HasCheckConstraint("ck_buildings_floor_count", "floor_count IS NULL OR floor_count > 0"));
        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(100).HasColumnType("citext");
        builder.HasIndex(entity => new { entity.PropertyId, entity.Name }).IsUnique().HasFilter("is_deleted = false");
        builder.HasMany(entity => entity.Units).WithOne().HasForeignKey(entity => new { entity.PropertyId, entity.BuildingId })
            .HasPrincipalKey(entity => new { entity.PropertyId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(entity => entity.Units).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        BaseEntityConfiguration.Configure(builder, "units");
        builder.Property(entity => entity.BuildingId).HasColumnName("building_id");
        builder.Property(entity => entity.PropertyId).HasColumnName("property_id");
        builder.Property(entity => entity.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.Number).HasColumnName("number").HasMaxLength(30).HasColumnType("citext");
        builder.Property(entity => entity.Floor).HasColumnName("floor");
        builder.Property(entity => entity.GrossSquareMeters).HasColumnName("gross_square_meters").HasPrecision(10, 2);
        builder.Ignore(entity => entity.Details);
        builder.Property(entity => entity.NetSquareMeters).HasColumnName("net_square_meters").HasPrecision(10, 2);
        builder.Property(entity => entity.RoomLayout).HasColumnName("room_layout").HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.FloorPosition).HasColumnName("floor_position").HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.IsMezzanine).HasColumnName("is_mezzanine");
        builder.Property(entity => entity.BathroomCount).HasColumnName("bathroom_count");
        builder.Property(entity => entity.KitchenType).HasColumnName("kitchen_type").HasConversion<string>().HasMaxLength(10);
        builder.Property(entity => entity.HasBalcony).HasColumnName("has_balcony");
        builder.Property(entity => entity.HeatingType).HasColumnName("heating_type").HasConversion<string>().HasMaxLength(30);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_units_net_area", "net_square_meters IS NULL OR (net_square_meters > 0 AND net_square_meters <= gross_square_meters)");
            table.HasCheckConstraint("ck_units_room_layout", "room_layout IS NULL OR room_layout IN ('STUDIO','R1_1','R1_5_1','R2_0','R2_1','R2_5_1','R3_1','R3_5_1','R4_1','R4_5_1','R5_1','R5_2','R6_PLUS')");
            table.HasCheckConstraint("ck_units_floor_position", "floor_position IS NULL OR (floor_position IN ('BELOW_ENTRANCE','BASEMENT') AND floor < 0) OR (floor_position IN ('GROUND','GARDEN','ENTRANCE','HIGH_ENTRANCE') AND floor = 0) OR (floor_position IN ('REGULAR','ROOF') AND floor >= 1)");
            table.HasCheckConstraint("ck_units_bathroom_count", "bathroom_count IS NULL OR bathroom_count BETWEEN 0 AND 6");
            table.HasCheckConstraint("ck_units_kitchen_type", "kitchen_type IS NULL OR kitchen_type IN ('OPEN','CLOSED')");
            table.HasCheckConstraint("ck_units_heating_type", $"heating_type IS NULL OR heating_type IN ({BuildingFeatureConfiguration.HeatingTypes})");
        });
        builder.HasIndex(entity => new { entity.BuildingId, entity.Number }).IsUnique().HasFilter("is_deleted = false AND building_id IS NOT NULL");
        builder.HasIndex(entity => new { entity.PropertyId, entity.Number }).IsUnique().HasFilter("is_deleted = false AND building_id IS NULL");
        builder.ToTable(table => { table.HasCheckConstraint("ck_units_positive_area", "gross_square_meters > 0"); table.HasCheckConstraint("ck_units_type", "type IN ('APARTMENT','OFFICE','SHOP','VILLA','WAREHOUSE','WORKSHOP')"); });
    }
}

internal sealed class PropertyMembershipConfiguration : IEntityTypeConfiguration<PropertyMembership>
{
    public void Configure(EntityTypeBuilder<PropertyMembership> builder)
    {
        BaseEntityConfiguration.Configure(builder, "property_memberships");
        builder.Property(member => member.PropertyId).HasColumnName("property_id");
        builder.Property(member => member.UserId).HasColumnName("user_id").HasMaxLength(200);
        builder.HasOne<Property>().WithMany().HasForeignKey(member => member.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(member => new { member.PropertyId, member.UserId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(member => member.UserId);
    }
}
