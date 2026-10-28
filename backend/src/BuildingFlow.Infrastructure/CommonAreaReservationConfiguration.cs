using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class CommonAreaReservationConfiguration : IEntityTypeConfiguration<CommonAreaReservation>
{
    public void Configure(EntityTypeBuilder<CommonAreaReservation> b)
    {
        BaseEntityConfiguration.Configure(b, "common_area_reservations");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.CommonAreaId).HasColumnName("common_area_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.UnitId).HasColumnName("unit_id");
        b.Property(x => x.Date).HasColumnName("reservation_date");
        b.Property(x => x.StartsAt).HasColumnName("starts_at");
        b.Property(x => x.EndsAt).HasColumnName("ends_at");
        b.Property(x => x.PartySize).HasColumnName("party_size");
        b.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CancelReason).HasColumnName("cancel_reason").HasMaxLength(500);
        b.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        b.HasOne<CommonArea>().WithMany().HasForeignKey(x => new { x.PropertyId, x.CommonAreaId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x => new { x.PropertyId, x.UnitId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique(); // Includes cancelled/deleted rows: never reuse a create operation.
        b.HasIndex(x => new { x.PropertyId, x.CommonAreaId, x.Date, x.StartsAt });
        b.ToTable(t => t.HasCheckConstraint("ck_common_area_reservations_valid", "request_id <> '00000000-0000-0000-0000-000000000000'::uuid AND fingerprint ~ '^[0-9A-Fa-f]{64}$' AND starts_at < ends_at AND EXTRACT(EPOCH FROM starts_at)::bigint % 900 = 0 AND EXTRACT(EPOCH FROM ends_at)::bigint % 900 = 0 AND party_size BETWEEN 1 AND 10000 AND status IN ('CONFIRMED','CANCELLED') AND ((status = 'CONFIRMED' AND cancel_reason IS NULL AND cancelled_at IS NULL) OR (status = 'CANCELLED' AND length(trim(cancel_reason)) > 0 AND cancelled_at IS NOT NULL))"));
    }
}
