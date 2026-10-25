using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class LatePenaltySwitchConfiguration : IEntityTypeConfiguration<LatePenaltySwitch>
{
    public void Configure(EntityTypeBuilder<LatePenaltySwitch> builder)
    {
        BaseEntityConfiguration.Configure(builder, "late_penalty_switches");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(value => value.Apply).HasColumnName("is_applied");
        builder.Property(value => value.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PropertyId, value.EffectiveFrom }).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_late_penalty_switches_values", "effective_from >= DATE '2000-01-01' AND length(trim(reason)) > 0");
            table.HasCheckConstraint("ck_late_penalty_switches_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class LatePenaltyPolicyConfiguration : IEntityTypeConfiguration<LatePenaltyPolicy>
{
    public void Configure(EntityTypeBuilder<LatePenaltyPolicy> builder)
    {
        BaseEntityConfiguration.Configure(builder, "late_penalty_policies");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.Year).HasColumnName("year");
        builder.Property(value => value.ValidFrom).HasColumnName("valid_from");
        builder.Property(value => value.ValidTo).HasColumnName("valid_to");
        builder.Property(value => value.MonthlyRate).HasColumnName("monthly_rate").HasPrecision(7, 4);
        builder.Property(value => value.Method).HasColumnName("calculation_method").HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.LegalBasis).HasColumnName("legal_basis").HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.LegalReference).HasColumnName("legal_reference").HasMaxLength(500);
        builder.Property(value => value.ApprovalDocumentId).HasColumnName("approval_document_id");
        builder.Property(value => value.Status).HasColumnName("approval_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.ApprovedBy).HasColumnName("approved_by").HasMaxLength(200);
        builder.Property(value => value.ApprovedAt).HasColumnName("approved_at");
        builder.Property(value => value.StatusReason).HasColumnName("status_reason").HasMaxLength(500);
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredDocument>().WithMany().HasForeignKey(value => value.ApprovalDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.Status, value.ValidFrom });
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_late_penalty_policies_values",
                "monthly_rate > 0 AND monthly_rate <= 5 AND valid_from >= DATE '2000-01-01' AND (valid_to IS NULL OR valid_to >= valid_from) " +
                "AND year = EXTRACT(YEAR FROM valid_from) AND calculation_method IN ('DAILY_30','DAILY_ACTUAL_MONTH') " +
                "AND legal_basis IN ('KMK_20','MANAGEMENT_PLAN','GENERAL_ASSEMBLY','COURT_DECISION') AND length(trim(legal_reference)) > 0");
            table.HasCheckConstraint("ck_late_penalty_policies_approval",
                "(approval_status = 'APPROVED' AND approved_by IS NOT NULL AND approved_at IS NOT NULL) OR " +
                "(approval_status IN ('DRAFT','CANCELLED') AND approved_by IS NULL AND approved_at IS NULL)");
            table.HasCheckConstraint("ck_late_penalty_policies_legal",
                "approval_status <> 'APPROVED' OR (monthly_rate = 5 AND legal_basis = 'KMK_20') OR (legal_basis <> 'KMK_20' AND approval_document_id IS NOT NULL)");
            table.HasCheckConstraint("ck_late_penalty_policies_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class LatePenaltyAccrualConfiguration : IEntityTypeConfiguration<LatePenaltyAccrual>
{
    public void Configure(EntityTypeBuilder<LatePenaltyAccrual> builder)
    {
        BaseEntityConfiguration.Configure(builder, "late_penalty_accruals");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.ChargeId).HasColumnName("charge_id");
        builder.Property(value => value.FromDate).HasColumnName("from_date");
        builder.Property(value => value.ToDate).HasColumnName("to_date");
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.PaidAmount).HasColumnName("paid_amount").HasPrecision(10, 2);
        builder.Property(value => value.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.StatusReason).HasColumnName("status_reason").HasMaxLength(500);
        builder.Property(value => value.StatusChangedAt).HasColumnName("status_changed_at");
        builder.HasOne<DuesCharge>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId, value.ChargeId })
            .HasPrincipalKey(value => new { value.PropertyId, value.UnitId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(value => new { value.PropertyId, value.Id });
        builder.HasIndex(value => new { value.ChargeId, value.FromDate }).IsUnique().HasFilter("status <> 'SUPERSEDED'");
        builder.HasIndex(value => new { value.PropertyId, value.Status, value.ToDate });
        builder.HasMany(value => value.Segments).WithOne().HasForeignKey(value => new { value.PropertyId, value.AccrualId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Segments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_late_penalty_accruals_values",
                "amount > 0 AND paid_amount >= 0 AND paid_amount <= amount AND to_date >= from_date AND status IN ('ACTIVE','WAIVED','SUPERSEDED')");
            table.HasCheckConstraint("ck_late_penalty_accruals_status",
                "(status = 'ACTIVE' AND status_reason IS NULL AND status_changed_at IS NULL) OR " +
                "(status <> 'ACTIVE' AND paid_amount = 0 AND status_reason IS NOT NULL AND length(trim(status_reason)) > 0 AND status_changed_at IS NOT NULL)");
            table.HasCheckConstraint("ck_late_penalty_accruals_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class LatePenaltySegmentConfiguration : IEntityTypeConfiguration<LatePenaltySegment>
{
    public void Configure(EntityTypeBuilder<LatePenaltySegment> builder)
    {
        BaseEntityConfiguration.Configure(builder, "late_penalty_segments");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.AccrualId).HasColumnName("accrual_id");
        builder.Property(value => value.FromDate).HasColumnName("from_date");
        builder.Property(value => value.ToDate).HasColumnName("to_date");
        builder.Property(value => value.Days).HasColumnName("days");
        builder.Property(value => value.Principal).HasColumnName("principal").HasPrecision(10, 2);
        builder.Property(value => value.MonthlyRate).HasColumnName("monthly_rate").HasPrecision(7, 4);
        builder.Property(value => value.Method).HasColumnName("calculation_method").HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.PolicyId).HasColumnName("policy_id");
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(16, 6);
        builder.HasOne<LatePenaltyPolicy>().WithMany().HasForeignKey(value => value.PolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.AccrualId, value.FromDate }).IsUnique();
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_late_penalty_segments_values",
                "days > 0 AND to_date >= from_date AND principal > 0 AND monthly_rate > 0 AND monthly_rate <= 5 AND amount >= 0");
            table.HasCheckConstraint("ck_late_penalty_segments_history", "is_deleted = false AND is_enabled = true");
        });
    }
}
