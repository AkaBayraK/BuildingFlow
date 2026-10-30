using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
{
    public void Configure(EntityTypeBuilder<SupplierInvoice> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_invoices");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Number).HasColumnName("number").HasMaxLength(50);
        b.Property(x => x.NumberKey).HasColumnName("number_key").HasMaxLength(50);
        b.Property(x => x.IssueDate).HasColumnName("issue_date");
        b.Property(x => x.DueDate).HasColumnName("due_date");
        b.Property(x => x.NetAmount).HasColumnName("net_amount").HasPrecision(18, 2);
        b.Property(x => x.VatRate).HasColumnName("vat_rate").HasPrecision(5, 2);
        b.Property(x => x.VatAmount).HasColumnName("vat_amount").HasPrecision(18, 2);
        b.Property(x => x.GrossAmount).HasColumnName("gross_amount").HasPrecision(18, 2);
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        b.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(400);
        b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
        b.Property(x => x.ContractId).HasColumnName("contract_id");
        b.Property(x => x.CorrectsInvoiceId).HasColumnName("corrects_invoice_id");
        b.Property(x => x.ExpenseId).HasColumnName("expense_id");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.RecordedByIssuer).HasColumnName("recorded_by_issuer").HasMaxLength(500);
        b.Property(x => x.RecordedBySubject).HasColumnName("recorded_by_subject").HasMaxLength(200);
        b.Property(x => x.PaidOn).HasColumnName("paid_on");
        b.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(100);
        b.Property(x => x.ReconciledOn).HasColumnName("reconciled_on");
        b.Property(x => x.ReconciliationNote).HasColumnName("reconciliation_note").HasMaxLength(1000);
        b.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);
        b.HasAlternateKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id });
        b.HasOne<SupplierManagementArea>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierWorkOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.WorkOrderId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierContract>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.ContractId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierInvoice>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.CorrectsInvoiceId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Expense>().WithMany().HasForeignKey(x => x.ExpenseId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.NumberKey }).IsUnique().HasFilter("status <> 'CANCELLED' AND NOT is_deleted");
        b.HasIndex(x => x.CorrectsInvoiceId).IsUnique().HasFilter("corrects_invoice_id IS NOT NULL");
        b.HasIndex(x => x.ExpenseId).IsUnique().HasFilter("expense_id IS NOT NULL");
        b.HasIndex(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Status, x.IssueDate });
        b.ToTable(t => {
            t.HasCheckConstraint("ck_supplier_invoice_amounts", "net_amount > 0 AND vat_rate BETWEEN 0 AND 100 AND vat_amount >= 0 AND gross_amount = net_amount + vat_amount AND gross_amount <= 99999999.99 AND currency = 'TRY'");
            t.HasCheckConstraint("ck_supplier_invoice_valid", "length(trim(number)) > 0 AND length(number_key) > 0 AND length(trim(description)) > 0 AND issue_date >= DATE '2000-01-01' AND due_date >= issue_date AND due_date <= DATE '2100-12-31' AND category IN ('MAINTENANCE','CLEANING','STAFF','ENERGY','SECURITY','INSURANCE','ADMINISTRATION','OTHER') AND status IN ('RECORDED','APPROVED','PAID','RECONCILED','CANCELLED')");
            t.HasCheckConstraint("ck_supplier_invoice_lifecycle", "(status NOT IN ('APPROVED','PAID','RECONCILED') OR expense_id IS NOT NULL) AND (status NOT IN ('PAID','RECONCILED')) = (paid_on IS NULL AND payment_reference IS NULL) AND (paid_on IS NULL OR paid_on >= issue_date) AND (status <> 'RECONCILED') = (reconciled_on IS NULL AND reconciliation_note IS NULL) AND (reconciled_on IS NULL OR reconciled_on >= paid_on) AND (status = 'CANCELLED') = (cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0)");
        });
    }
}
internal sealed class SupplierInvoiceRevisionConfiguration : IEntityTypeConfiguration<SupplierInvoiceRevision>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceRevision> b)
    {
        BaseEntityConfiguration.Configure(b, "supplier_invoice_revisions");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);
        b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(20);
        b.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
        b.Property(x => x.AppliedVersion).HasColumnName("applied_version");
        b.HasOne<SupplierInvoice>().WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.InvoiceId }).HasPrincipalKey(x => new { x.TenantId, x.SupplierId, x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.ActorIssuer, x.ActorSubject, x.OperationId }).IsUnique();
        b.HasIndex(x => new { x.InvoiceId, x.AppliedVersion }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_supplier_invoice_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','APPROVE','PAY','RECONCILE','CANCEL')"));
    }
}
