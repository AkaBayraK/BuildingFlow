using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class ParkingPolicyConfiguration : IEntityTypeConfiguration<ParkingPolicy>
{
    public void Configure(EntityTypeBuilder<ParkingPolicy> b)
    {
        BaseEntityConfiguration.Configure(b, "parking_policies");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.MaxVehiclesPerUnit).HasColumnName("max_vehicles_per_unit");
        b.Property(x => x.MaxVehiclesPerPerson).HasColumnName("max_vehicles_per_person");
        b.Property(x => x.MaxSpacesPerUnit).HasColumnName("max_spaces_per_unit");
        b.Property(x => x.GuestMaxHours).HasColumnName("guest_max_hours");
        b.Property(x => x.RetentionDays).HasColumnName("retention_days");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PropertyId).IsUnique().HasFilter("is_deleted = false");
        b.ToTable(t => t.HasCheckConstraint("ck_parking_policies_valid",
            "max_vehicles_per_unit BETWEEN 1 AND 20 AND max_vehicles_per_person BETWEEN 1 AND 20 AND max_spaces_per_unit BETWEEN 0 AND 20 AND guest_max_hours BETWEEN 1 AND 168 AND retention_days BETWEEN 1 AND 365"));
    }
}

internal sealed class ParkingFacilityConfiguration : IEntityTypeConfiguration<ParkingFacility>
{
    public void Configure(EntityTypeBuilder<ParkingFacility> b)
    {
        BaseEntityConfiguration.Configure(b, "parking_facilities");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.BuildingId).HasColumnName("building_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(ParkingFacility.NameLength).HasColumnType("citext");
        b.Ignore(x => x.IsShared);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Building>().WithMany().HasForeignKey(x => new { x.PropertyId, x.BuildingId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.Name }).IsUnique().HasFilter("is_deleted = false");
        b.ToTable(t => t.HasCheckConstraint("ck_parking_facilities_name", "length(trim(name)) > 0"));
    }
}

internal sealed class ParkingLevelConfiguration : IEntityTypeConfiguration<ParkingLevel>
{
    public void Configure(EntityTypeBuilder<ParkingLevel> b)
    {
        BaseEntityConfiguration.Configure(b, "parking_levels");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.FacilityId).HasColumnName("facility_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(ParkingLevel.NameLength).HasColumnType("citext");
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.HasAlternateKey(x => new { x.FacilityId, x.Id });
        b.HasOne<ParkingFacility>().WithMany().HasForeignKey(x => new { x.PropertyId, x.FacilityId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.FacilityId, x.Name }).IsUnique().HasFilter("is_deleted = false");
        b.HasIndex(x => new { x.PropertyId, x.FacilityId });
        b.ToTable(t => t.HasCheckConstraint("ck_parking_levels_valid", "length(trim(name)) > 0 AND sort_order BETWEEN -10 AND 50"));
    }
}

internal sealed class ParkingSpaceConfiguration : IEntityTypeConfiguration<ParkingSpace>
{
    public void Configure(EntityTypeBuilder<ParkingSpace> b)
    {
        BaseEntityConfiguration.Configure(b, "parking_spaces");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.FacilityId).HasColumnName("facility_id");
        b.Property(x => x.LevelId).HasColumnName("level_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(ParkingSpace.CodeLength).HasColumnType("citext");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.IsAllocatable);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasOne<ParkingFacility>().WithMany().HasForeignKey(x => new { x.PropertyId, x.FacilityId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ParkingLevel>().WithMany().HasForeignKey(x => new { x.FacilityId, x.LevelId })
            .HasPrincipalKey(x => new { x.FacilityId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.FacilityId, x.Code }).IsUnique().HasFilter("is_deleted = false");
        b.ToTable(t => t.HasCheckConstraint("ck_parking_spaces_valid",
            "length(trim(code)) > 0 AND type IN ('STANDARD','ACCESSIBLE','VISITOR','EV_CHARGING','MOTORCYCLE')"));
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        BaseEntityConfiguration.Configure(b, "vehicles");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.PersonId).HasColumnName("person_id");
        b.Property(x => x.UnitId).HasColumnName("unit_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.PersonalDataRedactedAt).HasColumnName("personal_data_redacted_at");
        b.Property(x => x.Plate).HasColumnName("plate").HasMaxLength(VehiclePlate.MaxLength);
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(Vehicle.TextLength);
        b.Property(x => x.Model).HasColumnName("model").HasMaxLength(Vehicle.TextLength);
        b.Property(x => x.Color).HasColumnName("color").HasMaxLength(Vehicle.TextLength);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
        b.Property(x => x.ReviewReason).HasColumnName("review_reason").HasMaxLength(Vehicle.ReasonLength);
        b.Property(x => x.DeactivatedAt).HasColumnName("deactivated_at");
        b.Ignore(x => x.IsActive);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasOne<Person>().WithMany().HasForeignKey(x => new { x.PropertyId, x.PersonId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x => new { x.PropertyId, x.UnitId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.Plate }).IsUnique().HasFilter("is_deleted = false AND status IN ('PENDING','APPROVED')");
        b.HasIndex(x => new { x.PropertyId, x.UnitId });
        b.HasIndex(x => new { x.PropertyId, x.PersonId });
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_vehicles_valid",
            "plate ~ '^[A-Z0-9]{2,12}$' AND type IN ('CAR','MOTORCYCLE','VAN','OTHER') AND status IN ('PENDING','APPROVED','REJECTED','PASSIVE') " +
            "AND (status <> 'REJECTED' OR (reviewed_at IS NOT NULL AND review_reason IS NOT NULL)) AND (status <> 'APPROVED' OR reviewed_at IS NOT NULL) " +
            "AND (status <> 'PASSIVE' OR deactivated_at IS NOT NULL)"));
    }
}

internal sealed class ParkingAllocationConfiguration : IEntityTypeConfiguration<ParkingAllocation>
{
    public void Configure(EntityTypeBuilder<ParkingAllocation> b)
    {
        BaseEntityConfiguration.Configure(b, "parking_allocations");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.SpaceId).HasColumnName("space_id");
        b.Property(x => x.UnitId).HasColumnName("unit_id");
        b.Property(x => x.VehicleId).HasColumnName("vehicle_id");
        b.Property(x => x.StartDate).HasColumnName("start_date");
        b.Property(x => x.EndDate).HasColumnName("end_date");
        b.HasOne<ParkingSpace>().WithMany().HasForeignKey(x => new { x.PropertyId, x.SpaceId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x => new { x.PropertyId, x.UnitId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Vehicle>().WithMany().HasForeignKey(x => new { x.PropertyId, x.VehicleId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.SpaceId).IsUnique().HasFilter("is_deleted = false AND end_date IS NULL");
        b.HasIndex(x => new { x.PropertyId, x.UnitId });
        b.ToTable(t => t.HasCheckConstraint("ck_parking_allocations_dates", "end_date IS NULL OR end_date >= start_date"));
    }
}

internal sealed class GuestVehicleConfiguration : IEntityTypeConfiguration<GuestVehicle>
{
    public void Configure(EntityTypeBuilder<GuestVehicle> b)
    {
        BaseEntityConfiguration.Configure(b, "guest_vehicles");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.HostUnitId).HasColumnName("host_unit_id");
        b.Property(x => x.SpaceId).HasColumnName("space_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.PersonalDataRedactedAt).HasColumnName("personal_data_redacted_at");
        b.Property(x => x.Plate).HasColumnName("plate").HasMaxLength(VehiclePlate.MaxLength);
        b.Property(x => x.GuestName).HasColumnName("guest_name").HasMaxLength(GuestVehicle.NameLength);
        b.Property(x => x.Note).HasColumnName("note").HasMaxLength(GuestVehicle.NoteLength);
        b.Property(x => x.StartsAt).HasColumnName("starts_at");
        b.Property(x => x.EndsAt).HasColumnName("ends_at");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ArrivedAt).HasColumnName("arrived_at");
        b.Property(x => x.DepartedAt).HasColumnName("departed_at");
        b.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        b.HasOne<Unit>().WithMany().HasForeignKey(x => new { x.PropertyId, x.HostUnitId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ParkingSpace>().WithMany().HasForeignKey(x => new { x.PropertyId, x.SpaceId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.StartsAt });
        b.HasIndex(x => new { x.SpaceId, x.StartsAt });
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_guest_vehicles_valid",
            "plate ~ '^[A-Z0-9]{2,12}$' AND ends_at > starts_at AND status IN ('PLANNED','ARRIVED','DEPARTED','CANCELLED') " +
            "AND (status <> 'ARRIVED' OR arrived_at IS NOT NULL) AND (status <> 'DEPARTED' OR (arrived_at IS NOT NULL AND departed_at >= arrived_at)) " +
            "AND (status <> 'CANCELLED' OR cancelled_at IS NOT NULL)"));
    }
}

internal sealed class ParkingClosureConfiguration : IEntityTypeConfiguration<ParkingClosure>
{
    public void Configure(EntityTypeBuilder<ParkingClosure> b)
    {
        BaseEntityConfiguration.Configure(b, "parking_closures");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.SpaceId).HasColumnName("space_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.StartsAt).HasColumnName("starts_at");
        b.Property(x => x.EndsAt).HasColumnName("ends_at");
        b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        b.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        b.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        b.HasOne<ParkingSpace>().WithMany().HasForeignKey(x => new { x.PropertyId, x.SpaceId })
            .HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique();
        b.HasIndex(x => new { x.PropertyId, x.SpaceId, x.StartsAt });
        b.ToTable(t => t.HasCheckConstraint("ck_parking_closures_valid",
            "ends_at > starts_at AND length(trim(reason)) > 0 AND ((cancelled_at IS NULL AND cancellation_reason IS NULL) OR (cancelled_at IS NOT NULL AND length(trim(cancellation_reason)) > 0))"));
    }
}
