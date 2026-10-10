using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class DuesAssessmentConfiguration : IEntityTypeConfiguration<DuesAssessment>
{
    public void Configure(EntityTypeBuilder<DuesAssessment> builder)
    {
        BaseEntityConfiguration.Configure(builder, "dues_assessments");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64);
        builder.Property(value => value.Period).HasColumnName("period");
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(value => value.DueDate).HasColumnName("due_date");
        builder.Property(value => value.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(value => value.DebtorType).HasColumnName("debtor_type").HasMaxLength(10).HasDefaultValue("UNIT");
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(value => new { value.PropertyId, value.Id });
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasMany(value => value.Charges).WithOne().HasForeignKey(value => new { value.PropertyId, value.AssessmentId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Charges).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_assessments_values", "amount > 0 AND currency = 'TRY' AND EXTRACT(DAY FROM period) = 1 AND period >= DATE '2000-01-01' AND due_date >= period");
            table.HasCheckConstraint("ck_assessments_history", "is_deleted = false AND is_enabled = true");
            table.HasCheckConstraint("ck_assessments_debtor", "debtor_type IN ('UNIT','TENANT','OWNER')");
        });
    }
}
internal sealed class DuesChargeConfiguration : IEntityTypeConfiguration<DuesCharge>
{
    public void Configure(EntityTypeBuilder<DuesCharge> builder)
    {
        BaseEntityConfiguration.Configure(builder, "dues_charges");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.AssessmentId).HasColumnName("assessment_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.Period).HasColumnName("period");
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(value => value.DueDate).HasColumnName("due_date");
        builder.Property(value => value.IsCancelled).HasColumnName("is_cancelled");
        builder.Property(value => value.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        builder.Property(value => value.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(value => value.PartiesRecorded).HasColumnName("parties_recorded").HasDefaultValue(false);
        builder.Property(value => value.CancellationReasonId).HasColumnName("cancellation_reason_id");
        builder.Property(value => value.CancellationReasonCode).HasColumnName("cancellation_reason_code").HasMaxLength(50);
        builder.Property(value => value.CancellationReasonName).HasColumnName("cancellation_reason_name").HasMaxLength(100);
        builder.Property(value => value.CancelledByOccupancyId).HasColumnName("cancelled_by_occupancy_id");
        builder.HasOne<DuesCancellationReason>().WithMany().HasForeignKey(value => value.CancellationReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOccupancy>().WithMany().HasForeignKey(value => new {value.PropertyId,value.UnitId,value.CancelledByOccupancyId})
            .HasPrincipalKey(value => new {value.PropertyId,value.UnitId,value.Id}).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(value => value.Parties).WithOne().HasForeignKey(value => new {value.PropertyId,value.UnitId,value.ChargeId})
            .HasPrincipalKey(value => new {value.PropertyId,value.UnitId,value.Id}).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Parties).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(value => value.PaidAmount).HasColumnName("paid_amount").HasPrecision(10, 2).HasDefaultValue(0m);
        builder.HasAlternateKey(value => new { value.PropertyId, value.UnitId, value.Id });
        builder.Ignore(value => value.Balance);
        builder.HasOne<Unit>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PropertyId, value.UnitId, value.Period }).IsUnique().HasFilter("is_cancelled = false");
        builder.HasIndex(value => new { value.PropertyId, value.DueDate });
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_charges_values", "amount > 0 AND currency = 'TRY' AND EXTRACT(DAY FROM period) = 1 AND period >= DATE '2000-01-01' AND due_date >= period");
            table.HasCheckConstraint("ck_charges_cancellation", "(is_cancelled = false AND cancellation_reason IS NULL AND cancelled_at IS NULL) OR (is_cancelled = true AND cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0 AND cancelled_at IS NOT NULL)");
            table.HasCheckConstraint("ck_charges_paid", "paid_amount >= 0 AND paid_amount <= amount AND (is_cancelled = false OR paid_amount = 0)");
            table.HasCheckConstraint("ck_charges_history", "is_deleted = false AND is_enabled = true");
            table.HasCheckConstraint("ck_charges_reason_metadata", "(cancellation_reason_id IS NULL AND cancellation_reason_code IS NULL AND cancellation_reason_name IS NULL AND cancelled_by_occupancy_id IS NULL) OR (is_cancelled = true AND cancellation_reason_id IS NOT NULL AND cancellation_reason_code IS NOT NULL AND cancellation_reason_name IS NOT NULL)");
        });
    }
}

internal sealed class DuesChargePartyConfiguration : IEntityTypeConfiguration<DuesChargeParty>
{
    public void Configure(EntityTypeBuilder<DuesChargeParty> builder)
    {
        BaseEntityConfiguration.Configure(builder, "dues_charge_parties");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.ChargeId).HasColumnName("charge_id");
        builder.Property(value => value.PersonId).HasColumnName("person_id");
        builder.Property(value => value.OccupancyId).HasColumnName("occupancy_id");
        builder.Property(value => value.PersonName).HasColumnName("person_name").HasMaxLength(201);
        builder.Property(value => value.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.StartDate).HasColumnName("start_date");
        builder.Property(value => value.EndDate).HasColumnName("end_date");
        builder.Property(value => value.IsDebtor).HasColumnName("is_debtor");
        builder.HasOne<Person>().WithMany().HasForeignKey(value => new {value.PropertyId,value.PersonId}).HasPrincipalKey(value => new {value.PropertyId,value.Id}).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOccupancy>().WithMany().HasForeignKey(value => new {value.PropertyId,value.UnitId,value.PersonId,value.OccupancyId})
            .HasPrincipalKey(value => new {value.PropertyId,value.UnitId,value.PersonId,value.Id}).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new {value.ChargeId,value.OccupancyId}).IsUnique();
        builder.HasIndex(value => value.ChargeId).IsUnique().HasFilter("is_debtor = true");
        builder.HasIndex(value => new {value.PropertyId,value.PersonId});
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_charge_parties_values", "length(trim(person_name)) > 0 AND type IN ('OWNER','TENANT','RESIDENT') AND (end_date IS NULL OR end_date >= start_date) AND (is_debtor = false OR type IN ('OWNER','TENANT'))");
            table.HasCheckConstraint("ck_charge_parties_history", "is_deleted = false AND is_enabled = true");
        });
    }
}
internal sealed class DuesCancellationReasonConfiguration : IEntityTypeConfiguration<DuesCancellationReason>
{
    public void Configure(EntityTypeBuilder<DuesCancellationReason> builder)
    {
        BaseEntityConfiguration.Configure(builder, "dues_cancellation_reasons");
        builder.Property(value => value.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(value => value.Name).HasColumnName("name").HasMaxLength(100);
        builder.Property(value => value.RequiresExplanation).HasColumnName("requires_explanation");
        builder.HasIndex(value => value.Code).IsUnique();
        var at = new DateTimeOffset(2026,10,9,0,0,0,TimeSpan.Zero);
        var entries = new[] {("DUPLICATE_ASSESSMENT","Mükerrer tahakkuk"),("WRONG_AMOUNT","Yanlış tutar"),("WRONG_PERIOD","Yanlış dönem"),("TENANT_MOVED_OUT","Kiracı taşınması"),("OTHER","Diğer")};
        builder.HasData(entries.Select((entry,index) => new {
            Id = Guid.Parse($"9c74d000-6001-4000-8000-{index+1:D12}"), Code = entry.Item1, Name = entry.Item2,
            RequiresExplanation = entry.Item1 == "OTHER", Version = 1L, CreatedAt = at, ModifiedAt = at,
            CreatedBy = "migration", ModifiedBy = "migration", IsDeleted = false, IsEnabled = true
        }));
    }
}
