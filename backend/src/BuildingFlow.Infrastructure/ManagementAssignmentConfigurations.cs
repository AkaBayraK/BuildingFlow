using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class ManagementAssignmentConfiguration:IEntityTypeConfiguration<ManagementAssignment>
{
    public void Configure(EntityTypeBuilder<ManagementAssignment> b)
    {
        BaseEntityConfiguration.Configure(b,"management_assignments");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.PersonId).HasColumnName("person_id");b.Property(x=>x.Role).HasColumnName("role").HasMaxLength(20);b.Property(x=>x.StartDate).HasColumnName("start_date");b.Property(x=>x.EndDate).HasColumnName("end_date");b.Property(x=>x.EndReason).HasColumnName("end_reason").HasMaxLength(500);b.Property(x=>x.CancelledAt).HasColumnName("cancelled_at");b.Property(x=>x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        b.HasAlternateKey(x=>new{x.TenantId,x.PropertyId,x.Id});
        b.HasOne<PersonPropertyScope>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CentralPerson>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PersonId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.PropertyId,x.PersonId,x.Role}).IsUnique().HasFilter("is_deleted=false AND cancelled_at IS NULL AND end_date IS NULL");b.HasIndex(x=>new{x.TenantId,x.PropertyId,x.StartDate});
        b.ToTable(t=>{t.HasCheckConstraint("ck_management_role","role IN ('SITE_ADMIN','MANAGER','ACCOUNTANT')");t.HasCheckConstraint("ck_management_dates","start_date>DATE '0001-01-01' AND (end_date IS NULL OR end_date>=start_date)");t.HasCheckConstraint("ck_management_closure","(end_reason IS NULL OR (end_date IS NOT NULL AND length(btrim(end_reason)) BETWEEN 1 AND 500)) AND ((cancelled_at IS NULL AND cancellation_reason IS NULL) OR (cancelled_at IS NOT NULL AND cancellation_reason IS NOT NULL AND length(btrim(cancellation_reason)) BETWEEN 1 AND 500))");});
    }
}
internal sealed class ManagementAssignmentEventConfiguration:IEntityTypeConfiguration<ManagementAssignmentEvent>
{
    public void Configure(EntityTypeBuilder<ManagementAssignmentEvent> b)
    {
        BaseEntityConfiguration.Configure(b,"management_assignment_events");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.AssignmentId).HasColumnName("assignment_id");b.Property(x=>x.Action).HasColumnName("action").HasMaxLength(20);b.Property(x=>x.Reason).HasColumnName("reason").HasMaxLength(500);b.Property(x=>x.AppliedVersion).HasColumnName("applied_version");b.HasOne<ManagementAssignment>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.AssignmentId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.PropertyId,x.AssignmentId,x.AppliedVersion}).IsUnique();b.ToTable(t=>t.HasCheckConstraint("ck_management_event","action IN ('CREATED','ENDED','CANCELLED') AND length(btrim(reason)) BETWEEN 1 AND 500 AND applied_version>0"));
    }
}
internal sealed class ManagementAssignmentOperationConfiguration:IEntityTypeConfiguration<ManagementAssignmentOperation>
{
    public void Configure(EntityTypeBuilder<ManagementAssignmentOperation> b)
    {
        BaseEntityConfiguration.Configure(b,"management_assignment_operations");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.AssignmentId).HasColumnName("assignment_id");b.Property(x=>x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);b.Property(x=>x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);b.Property(x=>x.OperationId).HasColumnName("operation_id");b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);b.Property(x=>x.AppliedVersion).HasColumnName("applied_version");b.HasIndex(x=>new{x.TenantId,x.ActorIssuer,x.ActorSubject,x.OperationId}).IsUnique();b.HasOne<ManagementAssignment>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.AssignmentId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.ToTable(t=>t.HasCheckConstraint("ck_management_operation","applied_version>0 AND length(fingerprint)=64"));
    }
}
