using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SupplierContractConfiguration : IEntityTypeConfiguration<SupplierContract>
{
    public void Configure(EntityTypeBuilder<SupplierContract> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_contracts");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.AreaServiceId).HasColumnName("area_service_id");
        b.Property(x => x.DocumentId).HasColumnName("document_id");
        b.Property(x => x.PreviousContractId).HasColumnName("previous_contract_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        b.Property(x => x.StartDate).HasColumnName("start_date");
        b.Property(x => x.EndDate).HasColumnName("end_date");
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        b.Property(x => x.ReminderDays).HasColumnName("reminder_days");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>();
        b.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id });
        b.HasOne<SupplierAreaService>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.AreaServiceId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierDocument>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.DocumentId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierContract>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.PreviousContractId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PreviousContractId).IsUnique().HasFilter("previous_contract_id IS NOT NULL AND NOT is_deleted");
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.EndDate });
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_contract_valid", "length(trim(title)) > 0 AND start_date >= DATE '2000-01-01' AND end_date <= DATE '2100-12-31' AND end_date >= start_date AND amount > 0 AND amount <= 99999999.99 AND currency = 'TRY' AND reminder_days BETWEEN 0 AND 365 AND status IN ('DRAFT','ACTIVE','CANCELLED') AND (status <> 'CANCELLED' OR cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0)"));
    }
}
internal sealed class SupplierContractRevisionConfiguration : IEntityTypeConfiguration<SupplierContractRevision>
{
    public void Configure(EntityTypeBuilder<SupplierContractRevision> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_contract_revisions");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.ContractId).HasColumnName("contract_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);
        b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(20);
        b.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
        b.Property(x => x.AppliedVersion).HasColumnName("applied_version");
        b.HasOne<SupplierContract>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.ContractId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.ActorIssuer, x.ActorSubject, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.ContractId, x.AppliedVersion }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_contract_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','EDIT','ACTIVATE','CANCEL')"));
    }
}
