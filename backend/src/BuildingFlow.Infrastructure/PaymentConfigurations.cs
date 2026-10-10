using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payments");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64);
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(value => value.PaidDate).HasColumnName("paid_date");
        builder.Property(value => value.Method).HasColumnName("method").HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.Reference).HasColumnName("reference").HasMaxLength(100);
        builder.Property(value => value.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(value => value.IsCancelled).HasColumnName("is_cancelled");
        builder.Property(value => value.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        builder.Property(value => value.CancelledAt).HasColumnName("cancelled_at");
        builder.HasOne<Unit>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId }).HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(value => new { value.PropertyId, value.UnitId, value.Id });
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.Reference }).IsUnique().HasFilter("method = 'BANK_TRANSFER' AND is_cancelled = false");
        builder.HasIndex(value => new { value.PropertyId, value.PaidDate });
        builder.HasMany(value => value.Allocations).WithOne().HasForeignKey(value => new { value.PropertyId, value.UnitId, value.PaymentId })
            .HasPrincipalKey(value => new { value.PropertyId, value.UnitId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payments_values", "amount > 0 AND currency = 'TRY' AND paid_date >= DATE '2000-01-01' AND method IN ('CASH','BANK_TRANSFER') AND (method <> 'BANK_TRANSFER' OR (reference IS NOT NULL AND length(trim(reference)) > 0 AND reference = upper(reference)))");
            table.HasCheckConstraint("ck_payments_cancellation", "(is_cancelled = false AND cancellation_reason IS NULL AND cancelled_at IS NULL) OR (is_cancelled = true AND cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0 AND cancelled_at IS NOT NULL)");
            table.HasCheckConstraint("ck_payments_history", "is_deleted = false AND is_enabled = true");
        });
    }
}
internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_allocations");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.PaymentId).HasColumnName("payment_id");
        builder.Property(value => value.ChargeId).HasColumnName("charge_id");
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.HasOne<DuesCharge>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId, value.ChargeId })
            .HasPrincipalKey(value => new { value.PropertyId, value.UnitId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PaymentId, value.ChargeId }).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.ChargeId });
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_allocations_values", "amount > 0");
            table.HasCheckConstraint("ck_allocations_history", "is_deleted = false AND is_enabled = true");
        });
    }
}
