using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class QaDataBatchConfiguration : IEntityTypeConfiguration<QaDataBatch>
{
    public void Configure(EntityTypeBuilder<QaDataBatch> builder)
    {
        BaseEntityConfiguration.Configure(builder, "qa_data_batches");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40);
        builder.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Environment).HasColumnName("environment").HasMaxLength(40);
        builder.Property(x => x.RequestId).HasColumnName("request_id");
        builder.Property(x => x.RunCode).HasColumnName("run_code").HasMaxLength(40);
        builder.Property(x => x.GeneratedAt).HasColumnName("generated_at");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.CleanedAt).HasColumnName("cleaned_at");
        builder.Property(x => x.RecordCount).HasColumnName("record_count");
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uidx_qa_data_batches_code");
        builder.HasIndex(x => x.RequestId).IsUnique().HasDatabaseName("uidx_qa_data_batches_request_id");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_qa_data_batches_kind", "kind IN ('DEMO','TEST')");
            t.HasCheckConstraint("ck_qa_data_batches_status", "status IN ('ACTIVE','CLEANED')");
            t.HasCheckConstraint("ck_qa_data_batches_demo_kept", "kind = 'TEST' OR status = 'ACTIVE'");
        });
    }
}

internal sealed class QaDataRecordConfiguration : IEntityTypeConfiguration<QaDataRecord>
{
    public void Configure(EntityTypeBuilder<QaDataRecord> builder)
    {
        BaseEntityConfiguration.Configure(builder, "qa_data_records");
        builder.Property(x => x.BatchId).HasColumnName("batch_id");
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(40);
        builder.Property(x => x.EntityId).HasColumnName("entity_id");
        builder.Property(x => x.CleanupOrder).HasColumnName("cleanup_order");
        builder.HasIndex(x => new { x.BatchId, x.EntityType, x.EntityId }).IsUnique().HasDatabaseName("uidx_qa_data_records_batch_entity");
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("idx_qa_data_records_entity");
        builder.HasOne<QaDataBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_qa_data_records_batch");
        builder.ToTable(t => t.HasCheckConstraint("ck_qa_data_records_order", "cleanup_order BETWEEN 1 AND 100"));
    }
}
