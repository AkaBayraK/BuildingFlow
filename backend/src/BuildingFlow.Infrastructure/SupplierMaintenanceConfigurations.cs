using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SupplierHolidayConfiguration : IEntityTypeConfiguration<SupplierHoliday>
{
    public void Configure(EntityTypeBuilder<SupplierHoliday> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_holidays");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Date).HasColumnName("date");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        b.HasOne<PersonTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.Date }).IsUnique().HasFilter("NOT is_deleted");
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_holiday_valid", "length(trim(name)) > 0 AND date >= DATE '2000-01-01' AND date <= DATE '2100-12-31'"));
    }
}
internal sealed class SupplierMaintenanceScheduleConfiguration : IEntityTypeConfiguration<SupplierMaintenanceSchedule>
{
    public void Configure(EntityTypeBuilder<SupplierMaintenanceSchedule> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_maintenance_schedules");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.AreaServiceId).HasColumnName("area_service_id");
        b.Property(x => x.ContractId).HasColumnName("contract_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(MaintenancePlan.TitleLength);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(MaintenancePlan.DescriptionLength);
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Frequency).HasColumnName("frequency").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.StartDate).HasColumnName("start_date");
        b.Property(x => x.EndDate).HasColumnName("end_date");
        b.Property(x => x.ReminderDays).HasColumnName("reminder_days");
        b.Property(x => x.NonWorkingDayRule).HasColumnName("non_working_day_rule").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.EndReason).HasColumnName("end_reason").HasMaxLength(1000);
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id });
        b.HasOne<SupplierAreaService>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.AreaServiceId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierContract>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.ContractId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Status, x.StartDate });
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_maintenance_schedule_valid", "length(trim(title)) > 0 AND type IN ('MAINTENANCE','CLEANING','INSPECTION','DRILL','OTHER') AND frequency IN ('WEEKLY','MONTHLY','QUARTERLY','SEMIANNUAL','YEARLY') AND non_working_day_rule IN ('KEEP','NEXT_WORKING_DAY','PREVIOUS_WORKING_DAY') AND start_date >= DATE '2000-01-01' AND end_date <= DATE '2100-12-31' AND end_date >= start_date AND end_date - start_date <= 731 AND reminder_days BETWEEN 0 AND 60 AND status IN ('ACTIVE','ENDED') AND (status <> 'ENDED' OR end_reason IS NOT NULL AND length(trim(end_reason)) > 0)"));
    }
}
internal sealed class SupplierMaintenanceOccurrenceConfiguration : IEntityTypeConfiguration<SupplierMaintenanceOccurrence>
{
    public void Configure(EntityTypeBuilder<SupplierMaintenanceOccurrence> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_maintenance_occurrences");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.ScheduleId).HasColumnName("schedule_id");
        b.Property(x => x.Sequence).HasColumnName("sequence");
        b.Property(x => x.NominalDate).HasColumnName("nominal_date");
        b.Property(x => x.PlannedDate).HasColumnName("planned_date");
        b.Property(x => x.MaintenancePlanId).HasColumnName("maintenance_plan_id");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CompletedOn).HasColumnName("completed_on");
        b.Property(x => x.DelayDays).HasColumnName("delay_days");
        b.Property(x => x.CompletionNote).HasColumnName("completion_note").HasMaxLength(4000);
        b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
        b.Property(x => x.SkipReason).HasColumnName("skip_reason").HasMaxLength(1000);
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id });
        b.HasOne<SupplierMaintenanceSchedule>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.ScheduleId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MaintenancePlan>().WithMany().HasForeignKey(x => new { x.PropertyId, x.MaintenancePlanId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierWorkOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.WorkOrderId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ScheduleId, x.Sequence }).IsUnique();
        b.HasIndex(x => x.MaintenancePlanId).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.PropertyId, x.PlannedDate, x.Status });
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_maintenance_occurrence_valid", "sequence > 0 AND status IN ('PLANNED','COMPLETED','SKIPPED') AND (status = 'COMPLETED') = (completed_on IS NOT NULL AND delay_days IS NOT NULL AND delay_days >= 0 AND completion_note IS NOT NULL AND length(trim(completion_note)) > 0) AND (status = 'SKIPPED') = (skip_reason IS NOT NULL AND length(trim(skip_reason)) > 0) AND (work_order_id IS NULL OR status = 'COMPLETED')"));
    }
}
internal sealed class SupplierMaintenanceRevisionConfiguration : IEntityTypeConfiguration<SupplierMaintenanceRevision>
{
    public void Configure(EntityTypeBuilder<SupplierMaintenanceRevision> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_maintenance_revisions");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.ScheduleId).HasColumnName("schedule_id");
        b.Property(x => x.OccurrenceId).HasColumnName("occurrence_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);
        b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(20);
        b.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
        b.Property(x => x.AppliedVersion).HasColumnName("applied_version");
        b.HasOne<SupplierMaintenanceSchedule>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.ScheduleId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierMaintenanceOccurrence>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.OccurrenceId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.ActorIssuer, x.ActorSubject, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.ScheduleId, x.CreatedAt });
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_maintenance_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','END','COMPLETE','SKIP') AND (occurrence_id IS NULL) = (action IN ('CREATE','END'))"));
    }
}
