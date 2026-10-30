using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SupplierWorkOrderConfiguration : IEntityTypeConfiguration<SupplierWorkOrder>
{
    public void Configure(EntityTypeBuilder<SupplierWorkOrder> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_work_orders");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.AreaServiceId).HasColumnName("area_service_id");
        b.Property(x => x.InquiryId).HasColumnName("inquiry_id");
        b.Property(x => x.QuoteId).HasColumnName("quote_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.MaintenancePlanId).HasColumnName("maintenance_plan_id");
        b.Property(x => x.SourceVersion).HasColumnName("source_version");
        b.Property(x => x.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.SourceSnapshot).HasColumnName("source_snapshot").HasMaxLength(4000);
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(4000);
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.DueAt).HasColumnName("due_at");
        b.Property(x => x.AssignedAt).HasColumnName("assigned_at");
        b.Property(x => x.AcceptedAt).HasColumnName("accepted_at");
        b.Property(x => x.StartedAt).HasColumnName("started_at");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");
        b.Property(x => x.VerifiedAt).HasColumnName("verified_at");
        b.Property(x => x.ClosedAt).HasColumnName("closed_at");
        b.Property(x => x.CompletionNote).HasColumnName("completion_note").HasMaxLength(4000);
        b.Property(x => x.DecisionReason).HasColumnName("decision_reason").HasMaxLength(1000);
        b.Ignore(x => x.AllowsPhotos);
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id });
        b.HasOne<SupplierInquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyId, x.InquiryId }).HasPrincipalKey(x => new { x.TenantId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierQuote>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyId, x.InquiryId, x.SupplierId, x.QuoteId }).HasPrincipalKey(x => new { x.TenantId, x.PropertyId, x.InquiryId, x.SupplierId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierAreaService>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.AreaServiceId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MobileRequest>().WithMany().HasForeignKey(x => new { x.PropertyId, x.RequestId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MaintenancePlan>().WithMany().HasForeignKey(x => new { x.PropertyId, x.MaintenancePlanId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.QuoteId).IsUnique().HasFilter("quote_id IS NOT NULL");
        b.HasIndex(x => x.RequestId).IsUnique().HasFilter("request_id IS NOT NULL AND status NOT IN ('CLOSED','REJECTED','CANCELLED')");
        b.HasIndex(x => x.MaintenancePlanId).IsUnique().HasFilter("maintenance_plan_id IS NOT NULL AND status NOT IN ('CLOSED','REJECTED','CANCELLED')");
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Status, x.CreatedAt });
        b.ToTable(t => {
            t.HasCheckConstraint("ck_supplier_work_order_source", "(source = 'QUOTE' AND quote_id IS NOT NULL AND inquiry_id IS NOT NULL AND request_id IS NULL AND maintenance_plan_id IS NULL AND source_version IS NULL AND amount IS NOT NULL AND currency IS NOT NULL) OR (source = 'REQUEST' AND request_id IS NOT NULL AND source_version IS NOT NULL AND source_version > 0 AND inquiry_id IS NULL AND quote_id IS NULL AND maintenance_plan_id IS NULL AND amount IS NULL AND currency IS NULL) OR (source = 'MAINTENANCE' AND maintenance_plan_id IS NOT NULL AND source_version IS NOT NULL AND source_version > 0 AND inquiry_id IS NULL AND quote_id IS NULL AND request_id IS NULL AND amount IS NULL AND currency IS NULL)");
            t.HasCheckConstraint("ck_supplier_work_order_valid", "length(trim(title)) > 0 AND length(trim(description)) > 0 AND length(trim(source_snapshot)) > 0 AND (amount IS NULL OR amount > 0 AND amount <= 99999999.99) AND (currency IS NULL OR currency IN ('TRY','USD','EUR')) AND status IN ('DRAFT','ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED','CANCELLED') AND (due_at IS NULL OR due_at >= TIMESTAMPTZ '2000-01-01' AND due_at < TIMESTAMPTZ '2101-01-01')");
            t.HasCheckConstraint("ck_supplier_work_order_lifecycle", "(status NOT IN ('ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED') OR assigned_at IS NOT NULL AND due_at IS NOT NULL) AND (status NOT IN ('ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED') OR accepted_at IS NOT NULL) AND (status NOT IN ('IN_PROGRESS','COMPLETED','VERIFIED','CLOSED') OR started_at IS NOT NULL) AND (status NOT IN ('COMPLETED','VERIFIED','CLOSED') OR completed_at IS NOT NULL AND completion_note IS NOT NULL AND length(trim(completion_note)) > 0) AND (status NOT IN ('VERIFIED','CLOSED') OR verified_at IS NOT NULL) AND (status <> 'CLOSED' OR closed_at IS NOT NULL) AND (status NOT IN ('REJECTED','CANCELLED') OR decision_reason IS NOT NULL AND length(trim(decision_reason)) > 0) AND (assigned_at IS NULL OR due_at IS NOT NULL AND due_at > assigned_at) AND (accepted_at IS NULL OR assigned_at IS NOT NULL AND accepted_at >= assigned_at) AND (started_at IS NULL OR accepted_at IS NOT NULL AND started_at >= accepted_at) AND (completed_at IS NULL OR started_at IS NOT NULL AND completed_at >= started_at) AND (verified_at IS NULL OR completed_at IS NOT NULL AND verified_at >= completed_at) AND (closed_at IS NULL OR verified_at IS NOT NULL AND closed_at >= verified_at)");
        });
    }
}
internal sealed class SupplierWorkOrderPhotoConfiguration : IEntityTypeConfiguration<SupplierWorkOrderPhoto>
{
    public void Configure(EntityTypeBuilder<SupplierWorkOrderPhoto> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_work_order_photos");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
        b.Property(x => x.DocumentId).HasColumnName("document_id");
        b.Property(x => x.Caption).HasColumnName("caption").HasMaxLength(1000);
        b.HasOne<SupplierWorkOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.WorkOrderId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierDocument>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.DocumentId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.WorkOrderId, x.DocumentId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_work_order_photo_caption", "length(trim(caption)) > 0"));
    }
}
internal sealed class SupplierWorkOrderRevisionConfiguration : IEntityTypeConfiguration<SupplierWorkOrderRevision>
{
    public void Configure(EntityTypeBuilder<SupplierWorkOrderRevision> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_work_order_revisions");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);
        b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(20);
        b.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
        b.Property(x => x.AppliedVersion).HasColumnName("applied_version");
        b.HasOne<SupplierWorkOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.WorkOrderId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.ActorIssuer, x.ActorSubject, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.WorkOrderId, x.AppliedVersion }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_work_order_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','EDIT','ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED','CANCELLED','PHOTO','NOTE')"));
    }
}
