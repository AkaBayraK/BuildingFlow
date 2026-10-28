using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class QaRunConfiguration : IEntityTypeConfiguration<QaRun>
{
    public void Configure(EntityTypeBuilder<QaRun> builder)
    {
        BaseEntityConfiguration.Configure(builder, "qa_runs");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40);
        builder.Property(x => x.ScenarioKey).HasColumnName("scenario_key").HasMaxLength(60);
        builder.Property(x => x.RequestId).HasColumnName("request_id");
        builder.Property(x => x.RetryOfId).HasColumnName("retry_of_id");
        builder.Property(x => x.Attempt).HasColumnName("attempt");
        builder.Property(x => x.TimeoutSeconds).HasColumnName("timeout_seconds");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(12);
        builder.Property(x => x.CancelRequested).HasColumnName("is_cancel_requested");
        builder.Property(x => x.QueuedAt).HasColumnName("queued_at");
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.FinishedAt).HasColumnName("finished_at");
        builder.Property(x => x.ResultCode).HasColumnName("result_code").HasMaxLength(20);
        builder.Property(x => x.PassedSteps).HasColumnName("passed_steps");
        builder.Property(x => x.FailedSteps).HasColumnName("failed_steps");
        builder.Ignore(x => x.IsActive);
        builder.Ignore(x => x.CanRetry);
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uidx_qa_runs_code");
        builder.HasIndex(x => x.RequestId).IsUnique().HasDatabaseName("uidx_qa_runs_request_id");
        builder.HasIndex(x => x.ScenarioKey).IsUnique().HasFilter("status IN ('QUEUED','RUNNING') AND is_deleted = false").HasDatabaseName("uidx_qa_runs_active_scenario");
        builder.HasIndex(x => new { x.Status, x.QueuedAt }).HasDatabaseName("idx_qa_runs_status_queued_at");
        builder.HasIndex(x => x.RetryOfId).HasDatabaseName("idx_qa_runs_retry_of_id");
        builder.HasOne<QaRun>().WithMany().HasForeignKey(x => x.RetryOfId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_qa_runs_retry_of");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_qa_runs_status", "status IN ('QUEUED','RUNNING','PASSED','FAILED','CANCELLED','TIMED_OUT')");
            t.HasCheckConstraint("ck_qa_runs_attempt", "attempt BETWEEN 1 AND 5");
            t.HasCheckConstraint("ck_qa_runs_timeout", "timeout_seconds BETWEEN 1 AND 3600");
        });
    }
}

internal sealed class QaRunStepConfiguration : IEntityTypeConfiguration<QaRunStep>
{
    public void Configure(EntityTypeBuilder<QaRunStep> builder)
    {
        BaseEntityConfiguration.Configure(builder, "qa_run_steps");
        builder.Property(x => x.RunId).HasColumnName("run_id");
        builder.Property(x => x.Sequence).HasColumnName("sequence");
        builder.Property(x => x.StepKey).HasColumnName("step_key").HasMaxLength(80);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Expected).HasColumnName("expected").HasMaxLength(300);
        builder.Property(x => x.Actual).HasColumnName("actual").HasMaxLength(300);
        builder.Property(x => x.DurationMs).HasColumnName("duration_ms");
        builder.HasIndex(x => new { x.RunId, x.Sequence }).IsUnique().HasDatabaseName("uidx_qa_run_steps_run_sequence");
        builder.HasOne<QaRun>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_qa_run_steps_run");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_qa_run_steps_status", "status IN ('PASSED','FAILED','SKIPPED')");
            t.HasCheckConstraint("ck_qa_run_steps_duration", "duration_ms >= 0");
        });
    }
}
