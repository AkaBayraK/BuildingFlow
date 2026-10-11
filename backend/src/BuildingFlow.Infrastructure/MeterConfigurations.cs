using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class MeterConfiguration : IEntityTypeConfiguration<Meter>
{
    public void Configure(EntityTypeBuilder<Meter> b)
    {
        BaseEntityConfiguration.Configure(b, "meters");
        b.Property(x => x.PropertyId).HasColumnName("property_id"); b.Property(x => x.UnitId).HasColumnName("unit_id");
        b.Property(x => x.RequestId).HasColumnName("request_id"); b.Property(x => x.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).HasColumnType("citext"); b.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x => new { x.PropertyId, x.UnitId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique(); b.HasIndex(x => new { x.PropertyId, x.Name }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_meters_valid", "kind IN ('WATER','ELECTRICITY','GAS') AND length(trim(name))>0 AND length(request_fingerprint)=64"));
    }
}
internal sealed class MeterReadingConfiguration : IEntityTypeConfiguration<MeterReading>
{
    public void Configure(EntityTypeBuilder<MeterReading> b)
    {
        BaseEntityConfiguration.Configure(b, "meter_readings");
        b.Property(x => x.PropertyId).HasColumnName("property_id"); b.Property(x => x.MeterId).HasColumnName("meter_id"); b.Property(x => x.ReadingDate).HasColumnName("reading_date");
        b.Property(x => x.Value).HasColumnName("value").HasPrecision(12,3); b.Property(x => x.RevisionCount).HasColumnName("revision_count");
        b.HasAlternateKey(x => new { x.PropertyId, x.Id }); b.HasIndex(x => new { x.MeterId, x.ReadingDate }).IsUnique();
        b.HasOne<Meter>().WithMany().HasForeignKey(x => new { x.PropertyId, x.MeterId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("ck_meter_readings_valid", "value>=0 AND revision_count>0 AND reading_date>='2000-01-01' AND is_enabled AND NOT is_deleted"));
    }
}
internal sealed class MeterReadingRevisionConfiguration : IEntityTypeConfiguration<MeterReadingRevision>
{
    public void Configure(EntityTypeBuilder<MeterReadingRevision> b)
    {
        BaseEntityConfiguration.Configure(b, "meter_reading_revisions");
        b.Property(x => x.PropertyId).HasColumnName("property_id"); b.Property(x => x.ReadingId).HasColumnName("reading_id"); b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64); b.Property(x => x.Value).HasColumnName("value").HasPrecision(12,3);
        b.Property(x => x.Sequence).HasColumnName("sequence"); b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        b.HasOne<MeterReading>().WithMany().HasForeignKey(x => new { x.PropertyId, x.ReadingId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique(); b.HasIndex(x => new { x.ReadingId, x.Sequence }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_reading_revisions_valid", "value>=0 AND sequence>0 AND length(trim(reason))>0 AND length(request_fingerprint)=64 AND is_enabled AND NOT is_deleted"));
    }
}
