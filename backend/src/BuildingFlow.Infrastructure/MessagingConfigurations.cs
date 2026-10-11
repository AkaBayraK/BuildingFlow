using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        BaseEntityConfiguration.Configure(b, "outbox_messages");
        b.Property(x => x.MessageId).HasColumnName("message_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        b.Property(x => x.Type).HasColumnName("type").HasMaxLength(50);
        b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        b.Property(x => x.Payload).HasColumnName("payload");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Attempts).HasColumnName("attempts");
        b.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(50);
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.MessageId).IsUnique();
        b.HasIndex(x => new { x.NextAttemptAt, x.CreatedAt }).HasFilter("status = 'PENDING' AND is_deleted = false");
        b.ToTable(t => {
            t.HasCheckConstraint("ck_outbox_messages_type", "type IN ('email.requested.v1','sms.requested.v1','notification.requested.v1')");
            t.HasCheckConstraint("ck_outbox_messages_payload", "length(payload) BETWEEN 1 AND 65536 AND attempts >= 0");
            t.HasCheckConstraint("ck_outbox_messages_status", "(status = 'PENDING' AND published_at IS NULL) OR (status = 'PUBLISHED' AND published_at IS NOT NULL)");
        });
    }
}
internal sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> b)
    {
        BaseEntityConfiguration.Configure(b, "processed_messages");
        b.Property(x => x.Consumer).HasColumnName("consumer").HasMaxLength(100);
        b.Property(x => x.MessageId).HasColumnName("message_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Type).HasColumnName("type").HasMaxLength(50);
        b.Property(x => x.PayloadSha256).HasColumnName("payload_sha256").HasMaxLength(64);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Consumer, x.MessageId }).IsUnique();
        b.HasIndex(x => x.PropertyId);
        b.ToTable(t => t.HasCheckConstraint("ck_processed_messages_status", "status IN ('PROCESSED','SKIPPED') AND length(payload_sha256) = 64"));
    }
}
