using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SupplierInquiryConfiguration : IEntityTypeConfiguration<SupplierInquiry>
{
    public void Configure(EntityTypeBuilder<SupplierInquiry> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_inquiries");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.CategoryId).HasColumnName("category_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(4000);
        b.Property(x => x.Deadline).HasColumnName("deadline");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);
        b.HasAlternateKey(x => new { x.TenantId, x.PropertyId, x.Id });
        b.HasOne<PersonPropertyScope>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyId }).HasPrincipalKey(x => new { x.TenantId, x.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierCategory>().WithMany().HasForeignKey(x => new { x.TenantId, x.CategoryId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.PropertyId, x.CreatedAt });
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_inquiry_valid", "length(trim(title)) > 0 AND length(trim(description)) > 0 AND deadline BETWEEN DATE '2000-01-01' AND DATE '2100-12-31' AND status IN ('OPEN','AWARDED','CANCELLED') AND (status <> 'CANCELLED' OR cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0)"));
    }
}
internal sealed class SupplierInvitationConfiguration : IEntityTypeConfiguration<SupplierInvitation>
{
    public void Configure(EntityTypeBuilder<SupplierInvitation> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_invitations");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.InquiryId).HasColumnName("inquiry_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.AreaServiceId).HasColumnName("area_service_id");
        b.HasOne<SupplierInquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyId, x.InquiryId }).HasPrincipalKey(x => new { x.TenantId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.TenantId, x.PropertyId, x.InquiryId, x.SupplierId, x.Id });
        b.HasIndex(x => new { x.InquiryId, x.SupplierId }).IsUnique();
        b.HasOne<SupplierAreaService>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.AreaServiceId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
internal sealed class SupplierQuoteConfiguration : IEntityTypeConfiguration<SupplierQuote>
{
    public void Configure(EntityTypeBuilder<SupplierQuote> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_quotes");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.InquiryId).HasColumnName("inquiry_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.InvitationId).HasColumnName("invitation_id");
        b.Property(x => x.NetAmount).HasColumnName("net_amount").HasPrecision(18, 2);
        b.Property(x => x.VatRate).HasColumnName("vat_rate").HasPrecision(18, 2);
        b.Property(x => x.VatAmount).HasColumnName("vat_amount").HasPrecision(18, 2);
        b.Property(x => x.GrossAmount).HasColumnName("gross_amount").HasPrecision(18, 2);
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        b.Property(x => x.ValidUntil).HasColumnName("valid_until");
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
        b.Property(x => x.EnteredByIssuer).HasColumnName("entered_by_issuer").HasMaxLength(500);
        b.Property(x => x.EnteredBySubject).HasColumnName("entered_by_subject").HasMaxLength(200);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.DecisionReason).HasColumnName("decision_reason").HasMaxLength(1000);
        b.HasOne<SupplierInquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyId, x.InquiryId }).HasPrincipalKey(x => new { x.TenantId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.TenantId, x.PropertyId, x.InquiryId, x.SupplierId, x.Id });
        b.HasOne<SupplierInvitation>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyId, x.InquiryId, x.SupplierId, x.InvitationId }).HasPrincipalKey(x => new { x.TenantId, x.PropertyId, x.InquiryId, x.SupplierId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.InvitationId).IsUnique();
        b.HasIndex(x => x.InquiryId).IsUnique().HasFilter("status = 'APPROVED'");
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_quote_valid", "net_amount > 0 AND gross_amount <= 99999999.99 AND vat_rate BETWEEN 0 AND 100 AND vat_amount = round(net_amount * vat_rate / 100, 2) AND gross_amount = net_amount + vat_amount AND currency IN ('TRY','USD','EUR') AND valid_until BETWEEN DATE '2000-01-01' AND DATE '2100-12-31' AND length(trim(notes)) > 0 AND status IN ('PENDING','APPROVED','REJECTED') AND (status = 'PENDING' AND decision_reason IS NULL OR status <> 'PENDING' AND decision_reason IS NOT NULL AND length(trim(decision_reason)) > 0)"));
    }
}
internal sealed class SupplierInquiryRevisionConfiguration : IEntityTypeConfiguration<SupplierInquiryRevision>
{
    public void Configure(EntityTypeBuilder<SupplierInquiryRevision> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_inquiry_revisions");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.InquiryId).HasColumnName("inquiry_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);
        b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(20);
        b.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
        b.Property(x => x.AppliedVersion).HasColumnName("applied_version");
        b.HasOne<SupplierInquiry>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyId, x.InquiryId }).HasPrincipalKey(x => new { x.TenantId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.ActorIssuer, x.ActorSubject, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.InquiryId, x.AppliedVersion }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_inquiry_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','QUOTE','APPROVE','REJECT','CANCEL','CONVERT')"));
    }
}
