using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SupplierAlertPreferenceConfiguration : IEntityTypeConfiguration<SupplierAlertPreference>
{
    public void Configure(EntityTypeBuilder<SupplierAlertPreference> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_alert_preferences");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);
        b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);
        b.Property(x => x.ContractExpiry).HasColumnName("contract_expiry");
        b.Property(x => x.WorkOrderOverdue).HasColumnName("work_order_overdue");
        b.Property(x => x.MaintenanceDue).HasColumnName("maintenance_due");
        b.HasOne<PersonTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.ActorIssuer, x.ActorSubject }).IsUnique().HasFilter("NOT is_deleted");
    }
}
