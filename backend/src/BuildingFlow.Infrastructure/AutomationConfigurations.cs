using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class AutomationDefinitionConfiguration : IEntityTypeConfiguration<AutomationDefinition>
{
    public void Configure(EntityTypeBuilder<AutomationDefinition> b)
    {
        BaseEntityConfiguration.Configure(b, "automation_definitions");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.StartsOn).HasColumnName("starts_on");
        b.Property(x => x.EndsOn).HasColumnName("ends_on");
        b.Property(x => x.SendHour).HasColumnName("send_hour");
        b.Property(x => x.QuietStartHour).HasColumnName("quiet_start_hour");
        b.Property(x => x.QuietEndHour).HasColumnName("quiet_end_hour");
        b.Property(x => x.OffsetDays).HasColumnName("offset_days");
        b.Property(x => x.InAppEnabled).HasColumnName("is_in_app_enabled");
        b.Property(x => x.SmsEnabled).HasColumnName("is_sms_enabled");
        b.Property(x => x.EmailEnabled).HasColumnName("is_email_enabled");
        b.Property(x => x.TitleTemplate).HasColumnName("title_template").HasMaxLength(AutomationDefinition.MaxTitle);
        b.Property(x => x.BodyTemplate).HasColumnName("body_template").HasMaxLength(AutomationDefinition.MaxBody);
        b.Property(x => x.RuleVersion).HasColumnName("rule_version");
        b.Property(x => x.ActivatedAt).HasColumnName("activated_at");
        b.Property(x => x.CustomTypeId).HasColumnName("custom_type_id");
        b.Property(x => x.CustomTrigger).HasColumnName("custom_trigger").HasConversion<string>().HasMaxLength(20);
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AutomationTypeDefinition>().WithMany().HasForeignKey(x => x.CustomTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.Type }).IsUnique().HasDatabaseName("uidx_automation_definitions_property_id_type").HasFilter("is_deleted = false AND type <> 'CUSTOM'");
        b.HasIndex(x => new { x.PropertyId, x.CustomTypeId }).IsUnique().HasDatabaseName("uidx_automation_definitions_property_id_custom_type_id").HasFilter("is_deleted = false AND custom_type_id IS NOT NULL");
        b.HasIndex(x => x.Status).HasDatabaseName("idx_automation_definitions_status");
        b.ToTable(t => {
            t.HasCheckConstraint("chk_automation_definitions_hours", "send_hour BETWEEN 0 AND 23 AND quiet_start_hour BETWEEN 0 AND 23 AND quiet_end_hour BETWEEN 0 AND 23");
            t.HasCheckConstraint("chk_automation_definitions_channels", "is_in_app_enabled OR is_sms_enabled OR is_email_enabled");
            t.HasCheckConstraint("chk_automation_definitions_dates", "ends_on IS NULL OR starts_on IS NULL OR ends_on >= starts_on");
            t.HasCheckConstraint("chk_automation_definitions_custom", "(type = 'CUSTOM') = (custom_type_id IS NOT NULL AND custom_trigger IS NOT NULL)");
        });
    }
}

internal sealed class AutomationRevisionConfiguration : IEntityTypeConfiguration<AutomationRevision>
{
    public void Configure(EntityTypeBuilder<AutomationRevision> b)
    {
        BaseEntityConfiguration.Configure(b, "automation_revisions");
        b.Property(x => x.DefinitionId).HasColumnName("definition_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.RuleVersion).HasColumnName("rule_version");
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(20);
        b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        b.Property(x => x.Snapshot).HasColumnName("snapshot").HasMaxLength(8000);
        b.HasOne<AutomationDefinition>().WithMany().HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.DefinitionId, x.RuleVersion }).IsUnique().HasDatabaseName("uidx_automation_revisions_definition_id_rule_version");
    }
}

internal sealed class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecution>
{
    public void Configure(EntityTypeBuilder<AutomationExecution> b)
    {
        BaseEntityConfiguration.Configure(b, "automation_executions");
        b.Property(x => x.DefinitionId).HasColumnName("definition_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.RuleVersion).HasColumnName("rule_version");
        b.Property(x => x.BusinessDate).HasColumnName("business_date");
        b.Property(x => x.Trigger).HasColumnName("trigger").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Attempts).HasColumnName("attempts");
        b.Property(x => x.StartedAt).HasColumnName("started_at");
        b.Property(x => x.FinishedAt).HasColumnName("finished_at");
        b.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
        b.Property(x => x.Evaluated).HasColumnName("evaluated");
        b.Property(x => x.Dispatched).HasColumnName("dispatched");
        b.Property(x => x.Skipped).HasColumnName("skipped");
        b.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(50);
        b.HasOne<AutomationDefinition>().WithMany().HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.DefinitionId, x.BusinessDate }).IsUnique().HasDatabaseName("uidx_automation_executions_definition_id_business_date")
            .HasFilter("trigger = 'SCHEDULE' AND is_deleted = false");
        b.HasIndex(x => new { x.PropertyId, x.StartedAt }).HasDatabaseName("idx_automation_executions_property_id_started_at");
    }
}

internal sealed class NotificationDispatchConfiguration : IEntityTypeConfiguration<NotificationDispatch>
{
    public void Configure(EntityTypeBuilder<NotificationDispatch> b)
    {
        BaseEntityConfiguration.Configure(b, "notification_dispatches");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.DefinitionId).HasColumnName("definition_id");
        b.Property(x => x.ExecutionId).HasColumnName("execution_id");
        b.Property(x => x.RuleVersion).HasColumnName("rule_version");
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.DedupKey).HasColumnName("dedup_key").HasMaxLength(64).IsFixedLength();
        b.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(50);
        b.Property(x => x.UnitId).HasColumnName("unit_id");
        b.Property(x => x.ChargeId).HasColumnName("charge_id");
        b.Property(x => x.SourceCode).HasColumnName("source_code").HasMaxLength(40);
        b.Property(x => x.SourceId).HasColumnName("source_id");
        b.Property(x => x.PaymentId).HasColumnName("payment_id");
        b.Property(x => x.PersonId).HasColumnName("person_id");
        b.Property(x => x.OffsetDays).HasColumnName("offset_days");
        b.Property(x => x.Period).HasColumnName("period");
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(12, 2);
        b.Property(x => x.RecipientMasked).HasColumnName("recipient_masked").HasMaxLength(100);
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        b.Property(x => x.PersonMessageId).HasColumnName("person_message_id");
        b.Property(x => x.SmsMessageId).HasColumnName("sms_message_id");
        b.Property(x => x.OutboxMessageId).HasColumnName("outbox_message_id");
        b.HasOne<AutomationExecution>().WithMany().HasForeignKey(x => x.ExecutionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AutomationDefinition>().WithMany().HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x => new { x.PropertyId, x.UnitId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.DedupKey).IsUnique().HasDatabaseName("uidx_notification_dispatches_dedup_key");
        b.HasIndex(x => new { x.PropertyId, x.CreatedAt }).HasDatabaseName("idx_notification_dispatches_property_id_created_at");
        b.HasIndex(x => x.SmsMessageId).HasDatabaseName("idx_notification_dispatches_sms_message_id").HasFilter("sms_message_id IS NOT NULL");
        b.HasIndex(x => new { x.PropertyId, x.PaymentId }).HasDatabaseName("idx_notification_dispatches_property_id_payment_id").HasFilter("payment_id IS NOT NULL");
    }
}

internal sealed class AutomationTypeDefinitionConfiguration : IEntityTypeConfiguration<AutomationTypeDefinition>
{
    public void Configure(EntityTypeBuilder<AutomationTypeDefinition> b)
    {
        BaseEntityConfiguration.Configure(b, "automation_types");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(60);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(40);
        b.Property(x => x.SourceCode).HasColumnName("source_code").HasMaxLength(40);
        b.Property(x => x.Conditions).HasColumnName("conditions").HasColumnType("jsonb");
        b.Property(x => x.TriggerKind).HasColumnName("trigger_kind").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.DateField).HasColumnName("date_field").HasMaxLength(40);
        b.Property(x => x.DefaultOffsets).HasColumnName("default_offsets");
        b.Property(x => x.DayOfMonth).HasColumnName("day_of_month");
        b.Property(x => x.DayOfWeek).HasColumnName("day_of_week");
        b.Property(x => x.RecipientScopes).HasColumnName("recipient_scopes");
        b.Property(x => x.DefaultTitle).HasColumnName("default_title").HasMaxLength(AutomationDefinition.MaxTitle);
        b.Property(x => x.DefaultBody).HasColumnName("default_body").HasMaxLength(AutomationDefinition.MaxBody);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.TypeVersion).HasColumnName("type_version");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uidx_automation_types_code").HasFilter("is_deleted = false");
        b.HasIndex(x => new { x.PropertyId, x.Status }).HasDatabaseName("idx_automation_types_property_id_status");
        b.ToTable(t => {
            t.HasCheckConstraint("chk_automation_types_calendar", "(day_of_month IS NULL OR day_of_month BETWEEN 1 AND 28) AND (day_of_week IS NULL OR day_of_week BETWEEN 1 AND 7)");
            t.HasCheckConstraint("chk_automation_types_trigger", "(trigger_kind = 'DATE_OFFSET') = (date_field IS NOT NULL)");
        });
    }
}

internal sealed class AutomationTypeRevisionConfiguration : IEntityTypeConfiguration<AutomationTypeRevision>
{
    public void Configure(EntityTypeBuilder<AutomationTypeRevision> b)
    {
        BaseEntityConfiguration.Configure(b, "automation_type_revisions");
        b.Property(x => x.TypeId).HasColumnName("type_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.TypeVersion).HasColumnName("type_version");
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(20);
        b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        b.Property(x => x.Snapshot).HasColumnName("snapshot").HasMaxLength(8000);
        b.HasOne<AutomationTypeDefinition>().WithMany().HasForeignKey(x => x.TypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TypeId, x.TypeVersion }).IsUnique().HasDatabaseName("uidx_automation_type_revisions_type_id_type_version");
    }
}
