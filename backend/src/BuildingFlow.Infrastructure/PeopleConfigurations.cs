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
        builder.Property(v=>v.PersonKind).HasColumnName("person_kind").HasMaxLength(10);
        builder.Property(v=>v.Citizenship).HasColumnName("citizenship").HasMaxLength(10);
        builder.Property(v=>v.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        builder.Property(v=>v.IdentityNumber).HasColumnName("identity_number").HasMaxLength(50);
        builder.Property(v=>v.BirthDate).HasColumnName("birth_date");
        builder.Property(v=>v.Gender).HasColumnName("gender").HasMaxLength(20);
        builder.Property(v=>v.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(v=>v.LinkedUserId).HasColumnName("linked_user_id").HasMaxLength(200);
        builder.Property(v=>v.PersonCodeHash).HasColumnName("person_code_hash").HasMaxLength(64);
        builder.Property(v=>v.PersonCodeHint).HasColumnName("person_code_hint").HasMaxLength(2);
        builder.Property(v=>v.PersonCodeIssuedAt).HasColumnName("person_code_issued_at");
        builder.ToTable(t=>t.HasCheckConstraint("chk_people_person_code", "(person_code_hash IS NULL AND person_code_hint IS NULL AND person_code_issued_at IS NULL) OR (person_code_hash ~ '^[0-9a-f]{64}$' AND person_code_hint ~ '^[0-9]{2}$' AND person_code_issued_at IS NOT NULL)"));
        builder.HasIndex(v=>v.PersonCodeHash).IsUnique().HasDatabaseName("uidx_people_person_code_hash").HasFilter("person_code_hash IS NOT NULL");
        builder.ToTable(t=>t.HasCheckConstraint("ck_people_profile", "(person_kind IS NULL AND citizenship IS NULL AND country_code IS NULL AND identity_number IS NULL AND birth_date IS NULL AND gender IS NULL AND address IS NULL AND linked_user_id IS NULL) OR COALESCE((person_kind IN ('NATURAL','LEGAL') AND citizenship IN ('DOMESTIC','FOREIGN') AND country_code ~ '^[A-Z]{2}$' AND length(btrim(identity_number)) BETWEEN 1 AND 50 AND length(btrim(phone_number)) > 0 AND length(btrim(email)) > 0 AND ((citizenship = 'DOMESTIC' AND country_code = 'TR' AND identity_number ~ CASE WHEN person_kind = 'NATURAL' THEN '^[1-9][0-9]{10}$' ELSE '^[0-9]{10}$' END) OR (citizenship = 'FOREIGN' AND country_code <> 'TR' AND (person_kind = 'LEGAL' OR identity_number ~ '^[A-Za-z0-9-]{6,30}$'))) AND (person_kind = 'NATURAL' OR (birth_date IS NULL AND gender IS NULL)) AND (gender IS NULL OR gender IN ('FEMALE','MALE','OTHER','UNSPECIFIED')) AND (birth_date IS NULL OR birth_date >= DATE '1900-01-01')), FALSE)"));
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
