using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        BaseEntityConfiguration.Configure(builder, "people");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.FirstName).HasColumnName("first_name").HasMaxLength(100);
        builder.Property(value => value.LastName).HasColumnName("last_name").HasMaxLength(100);
        builder.Property(value => value.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);
        builder.Property(value => value.Email).HasColumnName("email").HasMaxLength(254);
        builder.HasAlternateKey(value => new { value.PropertyId, value.Id });
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PropertyId, value.LastName, value.FirstName });
    }
}
internal sealed class UnitOccupancyConfiguration : IEntityTypeConfiguration<UnitOccupancy>
{
    public void Configure(EntityTypeBuilder<UnitOccupancy> builder)
    {
        BaseEntityConfiguration.Configure(builder, "unit_occupancies");
        builder.HasAlternateKey(value => new { value.PropertyId, value.UnitId, value.Id });
        builder.HasAlternateKey(value => new { value.PropertyId, value.UnitId, value.PersonId, value.Id });
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.PersonId).HasColumnName("person_id");
        builder.Property(value => value.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.StartDate).HasColumnName("start_date");
        builder.Property(value => value.EndDate).HasColumnName("end_date");
        builder.Property(value => value.IsPrimaryContact).HasColumnName("is_primary_contact");
        builder.HasOne<Person>().WithMany().HasForeignKey(value => new { value.PropertyId, value.PersonId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Unit>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PersonId, value.UnitId, value.Type }).IsUnique()
            .HasFilter("is_deleted = false AND end_date IS NULL");
        builder.HasIndex(value => value.UnitId).IsUnique()
            .HasFilter("is_deleted = false AND is_primary_contact = true AND end_date IS NULL");
        builder.HasIndex(value => new { value.PropertyId, value.StartDate });
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_occupancies_dates", "end_date IS NULL OR end_date >= start_date");
            table.HasCheckConstraint("ck_occupancies_type", "type IN ('OWNER','TENANT','RESIDENT')");
        });
    }
}
