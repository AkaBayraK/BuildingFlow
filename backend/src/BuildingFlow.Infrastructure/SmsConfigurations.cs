using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SmsAccountConfiguration : IEntityTypeConfiguration<SmsAccount>
{
    public void Configure(EntityTypeBuilder<SmsAccount> b)
    {
        BaseEntityConfiguration.Configure(b, "sms_accounts");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.Provider).HasColumnName("provider").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.SenderHeader).HasColumnName("sender_header").HasMaxLength(11);
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(100);
        b.Property(x => x.ProtectedSecret).HasColumnName("protected_secret").HasMaxLength(1000);
        b.Property(x => x.ApiUrl).HasColumnName("api_url").HasMaxLength(300);
        b.Property(x => x.CreditBalance).HasColumnName("credit_balance");
        b.Property(x => x.LowCreditThreshold).HasColumnName("low_credit_threshold");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PropertyId).IsUnique().HasDatabaseName("uidx_sms_accounts_property_id").HasFilter("is_deleted = false");
        b.ToTable(t => t.HasCheckConstraint("chk_sms_accounts_credit", "credit_balance BETWEEN 0 AND 10000000 AND low_credit_threshold BETWEEN 0 AND 10000000"));
    }
}
internal sealed class SmsCreditTransactionConfiguration : IEntityTypeConfiguration<SmsCreditTransaction>
{
    public void Configure(EntityTypeBuilder<SmsCreditTransaction> b)
    {
        BaseEntityConfiguration.Configure(b, "sms_credit_transactions");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.AccountId).HasColumnName("account_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Quantity).HasColumnName("quantity");
        b.Property(x => x.BalanceAfter).HasColumnName("balance_after");
        b.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 4);
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        b.Property(x => x.ReferenceNo).HasColumnName("reference_no").HasMaxLength(100);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.MessageId).HasColumnName("message_id");
        b.HasOne<SmsAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.AccountId, x.CreatedAt }).HasDatabaseName("idx_sms_credit_transactions_account_id");
        b.ToTable(t => t.HasCheckConstraint("chk_sms_credit_transactions_quantity", "quantity <> 0 AND balance_after >= 0"));
    }
}
internal sealed class SmsTemplateConfiguration : IEntityTypeConfiguration<SmsTemplate>
{
    public void Configure(EntityTypeBuilder<SmsTemplate> b)
    {
        BaseEntityConfiguration.Configure(b, "sms_templates");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.TypeCode).HasColumnName("type_code").HasMaxLength(50);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.Content).HasColumnName("content").HasMaxLength(SmsTemplate.MaxContentLength);
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.TypeCode }).IsUnique().HasDatabaseName("uidx_sms_templates_property_id_type_code").HasFilter("is_deleted = false");
    }
}
internal sealed class SmsMessageConfiguration : IEntityTypeConfiguration<SmsMessage>
{
    public void Configure(EntityTypeBuilder<SmsMessage> b)
    {
        BaseEntityConfiguration.Configure(b, "sms_messages");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.AccountId).HasColumnName("account_id");
        b.Property(x => x.PersonId).HasColumnName("person_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.TypeCode).HasColumnName("type_code").HasMaxLength(50);
        b.Property(x => x.Purpose).HasColumnName("purpose").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PhoneMasked).HasColumnName("phone_masked").HasMaxLength(20);
        b.Property(x => x.MaskedBody).HasColumnName("masked_body").HasMaxLength(SmsTemplate.MaxContentLength * 2);
        b.Property(x => x.ProtectedPayload).HasColumnName("protected_payload").HasMaxLength(8000);
        b.Property(x => x.Segments).HasColumnName("segments");
        b.Property(x => x.CreditCost).HasColumnName("credit_cost");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Provider).HasColumnName("provider").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(100);
        b.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(50);
        b.Property(x => x.Attempts).HasColumnName("attempts");
        b.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
        b.Property(x => x.SentAt).HasColumnName("sent_at");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SmsAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.CreatedAt }).HasDatabaseName("idx_sms_messages_property_id_created_at");
        b.HasIndex(x => x.NextAttemptAt).HasDatabaseName("idx_sms_messages_next_attempt_at").HasFilter("status = 'QUEUED' AND is_deleted = false");
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique().HasDatabaseName("uidx_sms_messages_property_id_request_id").HasFilter("request_id IS NOT NULL");
        b.ToTable(t => {
            t.HasCheckConstraint("chk_sms_messages_status", "(status = 'QUEUED' AND sent_at IS NULL) OR (status = 'SENT' AND sent_at IS NOT NULL AND protected_payload IS NULL) OR (status = 'FAILED' AND protected_payload IS NULL AND error_code IS NOT NULL)");
            t.HasCheckConstraint("chk_sms_messages_counts", "segments BETWEEN 1 AND 10 AND credit_cost = segments AND attempts >= 0");
        });
    }
}
