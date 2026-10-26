using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SmsLegalSenderConfiguration : IEntityTypeConfiguration<SmsLegalSender>
{
    public void Configure(EntityTypeBuilder<SmsLegalSender> b)
    {
        BaseEntityConfiguration.Configure(b, "sms_legal_senders");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        b.Property(x => x.MersisNumber).HasColumnName("mersis_number").HasMaxLength(16);
        b.Property(x => x.ContactInformation).HasColumnName("contact_information").HasMaxLength(200);
        b.Property(x => x.OptOutKeyword).HasColumnName("opt_out_keyword").HasMaxLength(20);
        b.Property(x => x.OptOutShortCode).HasColumnName("opt_out_short_code").HasMaxLength(15);
        b.Property(x => x.OptOutEnabled).HasColumnName("is_opt_out_enabled");
        b.Property(x => x.IysIntegrationEnabled).HasColumnName("is_iys_integration_enabled");
        b.Property(x => x.IysBrandCode).HasColumnName("iys_brand_code").HasMaxLength(20);
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PropertyId).IsUnique().HasDatabaseName("uidx_sms_legal_senders_property_id").HasFilter("is_deleted = false AND property_id IS NOT NULL");
        b.HasIndex(x => x.IsDeleted).IsUnique().HasDatabaseName("uidx_sms_legal_senders_platform").HasFilter("is_deleted = false AND property_id IS NULL");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("chk_sms_legal_senders_mersis", "mersis_number IS NULL OR mersis_number ~ '^[0-9]{16}$'");
            t.HasCheckConstraint("chk_sms_legal_senders_iys", "NOT is_iys_integration_enabled OR iys_brand_code IS NOT NULL");
        });
    }
}
internal sealed class CommercialConsentRecordConfiguration : IEntityTypeConfiguration<CommercialConsentRecord>
{
    public void Configure(EntityTypeBuilder<CommercialConsentRecord> b)
    {
        BaseEntityConfiguration.Configure(b, "commercial_consent_records");
        b.Property(x => x.SenderId).HasColumnName("sender_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Recipient).HasColumnName("recipient").HasMaxLength(16);
        b.Property(x => x.RecipientVerified).HasColumnName("is_recipient_verified");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        b.Property(x => x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
        b.Property(x => x.PersonId).HasColumnName("person_id");
        b.Property(x => x.TextVersion).HasColumnName("text_version");
        b.Property(x => x.TextSha256).HasColumnName("text_sha256").HasMaxLength(64);
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.EvidenceSha256).HasColumnName("evidence_sha256").HasMaxLength(64);
        b.Property(x => x.IysStatus).HasColumnName("iys_status").HasConversion<string>().HasMaxLength(15);
        b.Property(x => x.IysReference).HasColumnName("iys_reference").HasMaxLength(100);
        b.Property(x => x.IysSyncedAt).HasColumnName("iys_synced_at");
        b.Property(x => x.IysErrorCode).HasColumnName("iys_error_code").HasMaxLength(50);
        b.HasOne<SmsLegalSender>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OperationId).IsUnique().HasDatabaseName("uidx_commercial_consent_records_operation_id");
        b.HasIndex(x => new { x.SenderId, x.Channel, x.Recipient, x.OccurredAt }).HasDatabaseName("idx_commercial_consent_records_sender_id_recipient");
        b.HasIndex(x => x.IysStatus).HasDatabaseName("idx_commercial_consent_records_iys_status").HasFilter("iys_status IN ('PENDING', 'FAILED')");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("chk_commercial_consent_records_approval",
                "status <> 'APPROVED' OR (is_recipient_verified AND text_version > 0 AND text_sha256 IS NOT NULL AND source NOT IN ('SMS_REPLY', 'PROVIDER_CALLBACK'))");
            t.HasCheckConstraint("chk_commercial_consent_records_iys", "(iys_status = 'SYNCED') = (iys_reference IS NOT NULL AND iys_synced_at IS NOT NULL)");
        });
    }
}
internal sealed class SmsOperationalPreferenceConfiguration : IEntityTypeConfiguration<SmsOperationalPreference>
{
    public void Configure(EntityTypeBuilder<SmsOperationalPreference> b)
    {
        BaseEntityConfiguration.Configure(b, "sms_operational_preferences");
        b.Property(x => x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
        b.Property(x => x.Recipient).HasColumnName("recipient").HasMaxLength(16);
        b.Property(x => x.OperationalEnabled).HasColumnName("is_operational_enabled");
        b.HasIndex(x => x.OwnerUserId).IsUnique().HasDatabaseName("uidx_sms_operational_preferences_owner_user_id").HasFilter("is_deleted = false");
        b.HasIndex(x => x.Recipient).HasDatabaseName("idx_sms_operational_preferences_recipient").HasFilter("is_deleted = false AND NOT is_operational_enabled");
    }
}
