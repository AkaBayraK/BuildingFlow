using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class CommonAreaConfiguration : IEntityTypeConfiguration<CommonArea>
{
    public void Configure(EntityTypeBuilder<CommonArea> b)
    {
        BaseEntityConfiguration.Configure(b, "common_areas");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(150);
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.BuildingId).HasColumnName("building_id");
        b.Property(x => x.Visibility).HasColumnName("visibility").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Usage).HasColumnName("usage").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Capacity).HasColumnName("capacity");
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
        b.Property(x => x.Rules).HasColumnName("rules").HasMaxLength(8000);
        b.Property(x => x.OpensAt).HasColumnName("opens_at");
        b.Property(x => x.ClosesAt).HasColumnName("closes_at");
        b.Property(x => x.FeeAmount).HasColumnName("fee_amount").HasPrecision(10, 2);
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Building>().WithMany().HasForeignKey(x => new { x.PropertyId, x.BuildingId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique(); // Includes deleted rows: never reuse a create operation.
        b.HasIndex(x => new { x.PropertyId, x.Type, x.Name });
        b.ToTable(t => t.HasCheckConstraint("ck_common_areas_valid", "length(trim(name)) > 0 AND request_id <> '00000000-0000-0000-0000-000000000000'::uuid AND fingerprint ~ '^[0-9A-Fa-f]{64}$' AND type IN ('POOL','FITNESS','MEETING_ROOM','PLAYGROUND','BARBECUE','SPORTS_COURT','EVENT_HALL','OTHER') AND visibility IN ('PROPERTY','BUILDING') AND (visibility <> 'BUILDING' OR building_id IS NOT NULL) AND usage IN ('OPEN_USE','RESERVATION','APPROVAL') AND capacity BETWEEN 1 AND 10000 AND currency = 'TRY' AND fee_amount BETWEEN 0 AND 99999999.99 AND ((opens_at IS NULL AND closes_at IS NULL) OR (opens_at IS NOT NULL AND closes_at IS NOT NULL AND opens_at < closes_at AND EXTRACT(SECOND FROM opens_at) = 0 AND EXTRACT(SECOND FROM closes_at) = 0))"));
    }
}
