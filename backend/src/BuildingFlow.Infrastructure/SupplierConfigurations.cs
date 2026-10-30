using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        BaseEntityConfiguration.Configure(b, "suppliers");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        b.Property(x => x.NameKey).HasColumnName("name_key").HasMaxLength(200);
        b.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.StatusReason).HasColumnName("status_reason").HasMaxLength(1000);
        b.HasOne<PersonTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.TenantId, x.Id });
        b.HasIndex(x => new { x.TenantId, x.NameKey }).IsUnique().HasFilter("NOT is_deleted");
        b.ToTable(t => t.HasCheckConstraint("ck_suppliers_valid", "length(trim(name)) > 0 AND kind IN ('COMPANY','INDIVIDUAL') AND status IN ('DRAFT','ACTIVE','INACTIVE','BLOCKED') AND is_enabled = (status IN ('DRAFT','ACTIVE')) AND (status NOT IN ('INACTIVE','BLOCKED') OR length(trim(status_reason)) > 0 AND status_reason IS NOT NULL)"));
    }
}
internal sealed class SupplierCategoryConfiguration : IEntityTypeConfiguration<SupplierCategory>
{
    public void Configure(EntityTypeBuilder<SupplierCategory> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_categories");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.NameKey).HasColumnName("name_key").HasMaxLength(100);
        b.HasOne<PersonTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.TenantId, x.Id });
        b.HasIndex(x => new { x.TenantId, x.NameKey }).IsUnique().HasFilter("NOT is_deleted");
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_categories_name", "length(trim(name)) > 0"));
    }
}
internal sealed class SupplierServiceConfiguration : IEntityTypeConfiguration<SupplierService>
{
    public void Configure(EntityTypeBuilder<SupplierService> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_services");
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.Id });
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.CategoryId).HasColumnName("category_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        b.Property(x => x.NameKey).HasColumnName("name_key").HasMaxLength(200);
        b.HasOne<PersonTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierCategory>().WithMany().HasForeignKey(x => new { x.TenantId, x.CategoryId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.CategoryId, x.NameKey }).IsUnique().HasFilter("NOT is_deleted");
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_services_name", "length(trim(name)) > 0"));
    }
}
internal sealed class SupplierManagementAreaConfiguration : IEntityTypeConfiguration<SupplierManagementArea>
{
    public void Configure(EntityTypeBuilder<SupplierManagementArea> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_management_areas");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.HasOne<PersonTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PersonPropertyScope>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyId }).HasPrincipalKey(x => new { x.TenantId, x.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.PropertyId });
    }
}
internal sealed class SupplierContactConfiguration : IEntityTypeConfiguration<SupplierContact>
{
    public void Configure(EntityTypeBuilder<SupplierContact> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_contacts");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(254);
        b.HasOne<PersonTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_contacts_name", "length(trim(name)) > 0"));
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_contacts_channel", "phone IS NOT NULL OR email IS NOT NULL"));
    }
}
internal sealed class SupplierDocumentConfiguration : IEntityTypeConfiguration<SupplierDocument>
{
    public void Configure(EntityTypeBuilder<SupplierDocument> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_documents");
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id });
        b.Property(x => x.PreviousDocumentId).HasColumnName("previous_document_id");
        b.Property(x => x.RevisionNumber).HasColumnName("revision_number");
        b.HasOne<SupplierDocument>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.PreviousDocumentId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PreviousDocumentId).IsUnique().HasFilter("previous_document_id IS NOT NULL");
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_document_revision", "(previous_document_id IS NULL AND revision_number = 1) OR (previous_document_id IS NOT NULL AND revision_number > 1)"));
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.DocumentId).HasColumnName("document_id");
        b.Property(x => x.ExpiresOn).HasColumnName("expires_on");
        b.HasOne<PersonTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierManagementArea>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StoredDocument>().WithMany().HasForeignKey(x => new { x.PropertyId, x.DocumentId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.DocumentId }).IsUnique().HasFilter("NOT is_deleted");
    }
}
internal sealed class SupplierOperationConfiguration : IEntityTypeConfiguration<SupplierOperation>
{
    public void Configure(EntityTypeBuilder<SupplierOperation> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_operations");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);
        b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.AppliedVersion).HasColumnName("applied_version");
        b.HasIndex(x => new { x.TenantId, x.ActorIssuer, x.ActorSubject, x.OperationId }).IsUnique();
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_operations_valid", "operation_id <> '00000000-0000-0000-0000-000000000000' AND applied_version > 0 AND length(fingerprint) = 64"));
    }
}
internal sealed class SupplierAreaServiceConfiguration : IEntityTypeConfiguration<SupplierAreaService>
{
    public void Configure(EntityTypeBuilder<SupplierAreaService> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_area_services");
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id });
        b.Property(x => x.TenantId).HasColumnName("tenant_id"); b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id"); b.Property(x => x.ServiceId).HasColumnName("service_id");
        b.HasOne<SupplierManagementArea>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierService>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.ServiceId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.ServiceId }).IsUnique().HasFilter("NOT is_deleted");
    }
}
