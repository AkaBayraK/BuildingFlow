using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal static class FinanceMapping
{
    internal const string Categories = "('MAINTENANCE','CLEANING','STAFF','ENERGY','SECURITY','INSURANCE','ADMINISTRATION','OTHER')";
}
internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        BaseEntityConfiguration.Configure(builder, "expenses");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64);
        builder.Property(value => value.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(value => value.OccurredDate).HasColumnName("occurred_date");
        builder.Property(value => value.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(value => value.DocumentReference).HasColumnName("document_reference").HasMaxLength(100);
        builder.Property(value => value.IsCancelled).HasColumnName("is_cancelled");
        builder.Property(value => value.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        builder.Property(value => value.CancelledAt).HasColumnName("cancelled_at");
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.OccurredDate, value.Category });
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_expenses_values", "amount > 0 AND currency = 'TRY' AND occurred_date >= DATE '2000-01-01' AND category IN " + FinanceMapping.Categories + " AND length(trim(description)) > 0");
            table.HasCheckConstraint("ck_expenses_cancellation", "(is_cancelled = false AND cancellation_reason IS NULL AND cancelled_at IS NULL) OR (is_cancelled = true AND cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0 AND cancelled_at IS NOT NULL)");
            table.HasCheckConstraint("ck_expenses_history", "is_deleted = false AND is_enabled = true");
        });
    }
}
internal sealed class PropertyBudgetConfiguration : IEntityTypeConfiguration<PropertyBudget>
{
    public void Configure(EntityTypeBuilder<PropertyBudget> builder)
    {
        BaseEntityConfiguration.Configure(builder, "property_budgets");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.Period).HasColumnName("period");
        builder.Property(value => value.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(value => value.RevisionCount).HasColumnName("revision_count");
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(value => new { value.PropertyId, value.Id });
        builder.HasIndex(value => new { value.PropertyId, value.Period, value.Category }).IsUnique();
        builder.HasMany(value => value.Revisions).WithOne().HasForeignKey(value => new { value.PropertyId, value.BudgetId }).HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Revisions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_budgets_values", "amount >= 0 AND currency = 'TRY' AND period >= DATE '2000-01-01' AND period < DATE '9999-01-01' AND EXTRACT(DAY FROM period) = 1 AND category IN " + FinanceMapping.Categories + " AND revision_count > 0");
            table.HasCheckConstraint("ck_budgets_history", "is_deleted = false AND is_enabled = true");
        });
    }
}
internal sealed class BudgetRevisionConfiguration : IEntityTypeConfiguration<BudgetRevision>
{
    public void Configure(EntityTypeBuilder<BudgetRevision> builder)
    {
        BaseEntityConfiguration.Configure(builder, "budget_revisions");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.BudgetId).HasColumnName("budget_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64);
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.Sequence).HasColumnName("sequence");
        builder.Property(value => value.Explanation).HasColumnName("explanation").HasMaxLength(500);
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasIndex(value => new { value.BudgetId, value.Sequence }).IsUnique();
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_budget_revisions_values", "amount >= 0 AND sequence > 0 AND length(trim(explanation)) > 0");
            table.HasCheckConstraint("ck_budget_revisions_history", "is_deleted = false AND is_enabled = true");
        });
    }
}
