using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class PaymentProviderConfiguration : IEntityTypeConfiguration<PaymentProvider>
{
    public void Configure(EntityTypeBuilder<PaymentProvider> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_providers");
        builder.Property(value => value.Code).HasColumnName("code").HasMaxLength(30);
        builder.HasIndex(value => value.Code).IsUnique().HasDatabaseName("uidx_payment_providers_code");
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_providers_code", "code = upper(code) AND length(trim(code)) > 0");
            table.HasCheckConstraint("ck_payment_providers_history", "is_deleted = false");
        });
    }
}

internal sealed class PropertyPaymentSettingsConfiguration : IEntityTypeConfiguration<PropertyPaymentSettings>
{
    public void Configure(EntityTypeBuilder<PropertyPaymentSettings> builder)
    {
        BaseEntityConfiguration.Configure(builder, "property_payment_settings");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.OnlineEnabled).HasColumnName("is_online_enabled");
        builder.Property(value => value.PartialAllowed).HasColumnName("is_partial_allowed");
        builder.Property(value => value.ShowIbanToResidents).HasColumnName("is_iban_visible_to_residents");
        builder.Property(value => value.RequireDualApproval).HasColumnName("is_dual_approval_required");
        builder.Property(value => value.ReceiptPrefix).HasColumnName("receipt_prefix").HasMaxLength(10);
        builder.Property(value => value.CommissionPolicy).HasColumnName("commission_policy").HasConversion<string>().HasMaxLength(20);
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => value.PropertyId).IsUnique().HasDatabaseName("uidx_property_payment_settings_property_id");
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_property_payment_settings_values", "commission_policy = 'ABSORBED' AND receipt_prefix ~ '^[A-Z0-9]{2,10}$'");
            table.HasCheckConstraint("ck_property_payment_settings_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class PaymentBankAccountConfiguration : IEntityTypeConfiguration<PaymentBankAccount>
{
    public void Configure(EntityTypeBuilder<PaymentBankAccount> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_bank_accounts");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.BuildingId).HasColumnName("building_id");
        builder.Property(value => value.BankName).HasColumnName("bank_name").HasMaxLength(100);
        builder.Property(value => value.AccountHolder).HasColumnName("account_holder").HasMaxLength(200);
        builder.Property(value => value.Iban).HasColumnName("iban").HasMaxLength(34);
        builder.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(value => value.Purpose).HasColumnName("purpose").HasConversion<string>().HasMaxLength(10);
        builder.Property(value => value.IsDefault).HasColumnName("is_default");
        builder.Property(value => value.ValidFrom).HasColumnName("valid_from");
        builder.Property(value => value.ValidTo).HasColumnName("valid_to");
        builder.Property(value => value.Status).HasColumnName("account_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.DecidedBy).HasColumnName("decided_by").HasMaxLength(200);
        builder.Property(value => value.DecidedAt).HasColumnName("decided_at");
        builder.Property(value => value.StatusReason).HasColumnName("status_reason").HasMaxLength(500);
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Building>().WithMany().HasForeignKey(value => new { value.PropertyId, value.BuildingId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(value => new { value.PropertyId, value.Id });
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.Iban }).IsUnique().HasDatabaseName("uidx_payment_bank_accounts_open_iban")
            .HasFilter("account_status IN ('PENDING_APPROVAL','ACTIVE')");
        builder.HasIndex(value => value.PropertyId, "uidx_payment_bank_accounts_default_property").IsUnique().HasFilter("is_default = true AND building_id IS NULL");
        builder.HasIndex(value => new { value.PropertyId, value.BuildingId }, "uidx_payment_bank_accounts_default_building").IsUnique().HasFilter("is_default = true AND building_id IS NOT NULL");
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_bank_accounts_values",
                "currency = 'TRY' AND iban ~ '^TR[0-9]{24}$' AND purpose IN ('ALL','DUES','EXTRA') AND valid_from >= DATE '2000-01-01' " +
                "AND (valid_to IS NULL OR valid_to >= valid_from) AND length(trim(bank_name)) > 0 AND length(trim(account_holder)) > 0");
            table.HasCheckConstraint("ck_payment_bank_accounts_status",
                "account_status IN ('PENDING_APPROVAL','ACTIVE','REJECTED','CLOSED') AND (is_default = false OR account_status = 'ACTIVE') " +
                "AND (account_status = 'PENDING_APPROVAL' OR decided_by IS NOT NULL OR account_status = 'CLOSED')");
            table.HasCheckConstraint("ck_payment_bank_accounts_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class PaymentMerchantAccountConfiguration : IEntityTypeConfiguration<PaymentMerchantAccount>
{
    public void Configure(EntityTypeBuilder<PaymentMerchantAccount> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_merchant_accounts");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.BuildingId).HasColumnName("building_id");
        builder.Property(value => value.ProviderCode).HasColumnName("provider_code").HasMaxLength(30);
        builder.Property(value => value.Label).HasColumnName("label").HasMaxLength(100);
        builder.Property(value => value.MerchantId).HasColumnName("merchant_id").HasMaxLength(100);
        builder.Property(value => value.SecretReference).HasColumnName("secret_reference").HasMaxLength(100);
        builder.Property(value => value.Environment).HasColumnName("environment").HasConversion<string>().HasMaxLength(10);
        builder.Property(value => value.IsDefault).HasColumnName("is_default");
        builder.Property(value => value.LastCheckedAt).HasColumnName("last_checked_at");
        builder.Property(value => value.LastCheckSucceeded).HasColumnName("last_check_succeeded");
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Building>().WithMany().HasForeignKey(value => new { value.PropertyId, value.BuildingId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(value => new { value.PropertyId, value.Id });
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasIndex(value => value.PropertyId, "uidx_payment_merchant_accounts_default_property").IsUnique().HasFilter("is_default = true AND building_id IS NULL");
        builder.HasIndex(value => new { value.PropertyId, value.BuildingId }, "uidx_payment_merchant_accounts_default_building").IsUnique().HasFilter("is_default = true AND building_id IS NOT NULL");
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_merchant_accounts_values",
                "environment = 'SANDBOX' AND provider_code = upper(provider_code) AND secret_reference ~ '^[A-Za-z0-9_:-]{1,100}$' " +
                "AND length(trim(merchant_id)) > 0 AND length(trim(label)) > 0 AND (is_default = false OR is_enabled = true)");
            table.HasCheckConstraint("ck_payment_merchant_accounts_history", "is_deleted = false");
        });
    }
}

internal sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_intents");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64);
        builder.Property(value => value.PayerUserId).HasColumnName("payer_user_id").HasMaxLength(200);
        builder.Property(value => value.PayerRole).HasColumnName("payer_role").HasMaxLength(10);
        builder.Property(value => value.MerchantAccountId).HasColumnName("merchant_account_id");
        builder.Property(value => value.ProviderCode).HasColumnName("provider_code").HasMaxLength(30);
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(value => value.Status).HasColumnName("intent_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.ExpiresAt).HasColumnName("expires_at");
        builder.Property(value => value.StatusChangedAt).HasColumnName("status_changed_at");
        builder.Property(value => value.ProviderReference).HasColumnName("provider_reference").HasMaxLength(100);
        builder.Property(value => value.CheckoutUrl).HasColumnName("checkout_url").HasMaxLength(1000);
        builder.Property(value => value.ProviderTransactionId).HasColumnName("provider_transaction_id").HasMaxLength(100);
        builder.Property(value => value.PaidDate).HasColumnName("paid_date");
        builder.Property(value => value.FeeAmount).HasColumnName("fee_amount").HasPrecision(10, 2);
        builder.Property(value => value.PaymentId).HasColumnName("payment_id");
        builder.Property(value => value.FailureCode).HasColumnName("failure_code").HasMaxLength(50);
        builder.HasOne<Unit>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentMerchantAccount>().WithMany().HasForeignKey(value => new { value.PropertyId, value.MerchantAccountId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId, value.PaymentId })
            .HasPrincipalKey(value => new { value.PropertyId, value.UnitId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(value => new { value.PropertyId, value.UnitId, value.Id });
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasIndex(value => new { value.ProviderCode, value.ProviderReference }).IsUnique().HasFilter("provider_reference IS NOT NULL");
        builder.HasIndex(value => value.PaymentId).IsUnique().HasFilter("payment_id IS NOT NULL");
        builder.HasIndex(value => new { value.Status, value.ExpiresAt });
        builder.HasIndex(value => new { value.PropertyId, value.CreatedAt });
        builder.HasMany(value => value.Lines).WithOne().HasForeignKey(value => new { value.PropertyId, value.UnitId, value.IntentId })
            .HasPrincipalKey(value => new { value.PropertyId, value.UnitId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_intents_values",
                "amount > 0 AND currency = 'TRY' AND payer_role IN ('OWNER','TENANT') " +
                "AND intent_status IN ('PENDING','SUCCEEDED','REVIEW','FAILED','EXPIRED','CANCELLED','REFUNDED','CHARGED_BACK')");
            table.HasCheckConstraint("ck_payment_intents_success",
                "(intent_status IN ('PENDING','FAILED','EXPIRED','CANCELLED') AND paid_date IS NULL AND payment_id IS NULL) OR " +
                "(intent_status NOT IN ('PENDING','FAILED','EXPIRED','CANCELLED') AND provider_transaction_id IS NOT NULL AND paid_date IS NOT NULL " +
                "AND fee_amount >= 0 AND (intent_status <> 'SUCCEEDED' OR payment_id IS NOT NULL))");
            table.HasCheckConstraint("ck_payment_intents_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class PaymentIntentLineConfiguration : IEntityTypeConfiguration<PaymentIntentLine>
{
    public void Configure(EntityTypeBuilder<PaymentIntentLine> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_intent_lines");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.IntentId).HasColumnName("intent_id");
        builder.Property(value => value.ChargeId).HasColumnName("charge_id");
        builder.Property(value => value.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.ChargeVersion).HasColumnName("charge_version");
        builder.HasOne<DuesCharge>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId, value.ChargeId })
            .HasPrincipalKey(value => new { value.PropertyId, value.UnitId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.IntentId, value.ChargeId, value.Kind }).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.ChargeId });
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_intent_lines_values", "amount > 0 AND kind IN ('PRINCIPAL','PENALTY') AND charge_version > 0");
            table.HasCheckConstraint("ck_payment_intent_lines_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class PaymentProviderEventConfiguration : IEntityTypeConfiguration<PaymentProviderEvent>
{
    public void Configure(EntityTypeBuilder<PaymentProviderEvent> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_provider_events");
        builder.Property(value => value.ProviderCode).HasColumnName("provider_code").HasMaxLength(30);
        builder.Property(value => value.EventId).HasColumnName("event_id").HasMaxLength(100);
        builder.Property(value => value.EventType).HasColumnName("event_type").HasMaxLength(40);
        builder.Property(value => value.ProviderReference).HasColumnName("provider_reference").HasMaxLength(100);
        builder.Property(value => value.OccurredAt).HasColumnName("occurred_at");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.IntentId).HasColumnName("intent_id");
        builder.Property(value => value.Outcome).HasColumnName("outcome").HasMaxLength(40);
        builder.Property(value => value.ProcessedAt).HasColumnName("processed_at");
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentIntent>().WithMany().HasForeignKey(value => value.IntentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.ProviderCode, value.EventId }).IsUnique().HasDatabaseName("uidx_payment_provider_events_event");
        builder.HasIndex(value => value.IntentId);
        builder.ToTable(table => table.HasCheckConstraint("ck_payment_provider_events_history", "is_deleted = false AND is_enabled = true"));
    }
}

internal sealed class PaymentReceiptConfiguration : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_receipts");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.PaymentId).HasColumnName("payment_id");
        builder.Property(value => value.Year).HasColumnName("receipt_year");
        builder.Property(value => value.Sequence).HasColumnName("sequence_number");
        builder.Property(value => value.Number).HasColumnName("receipt_number").HasMaxLength(30);
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(value => value.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => value.PaymentId).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.Year, value.Sequence }).IsUnique();
        builder.HasIndex(value => new { value.PropertyId, value.Number }).IsUnique();
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_receipts_values", "receipt_year >= 2000 AND sequence_number > 0");
            table.HasCheckConstraint("ck_payment_receipts_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class PaymentLedgerEntryConfiguration : IEntityTypeConfiguration<PaymentLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PaymentLedgerEntry> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_ledger_entries");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.UnitId).HasColumnName("unit_id");
        builder.Property(value => value.Type).HasColumnName("entry_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(12, 2);
        builder.Property(value => value.OccurredOn).HasColumnName("occurred_on");
        builder.Property(value => value.Method).HasColumnName("method").HasMaxLength(30);
        builder.Property(value => value.PaymentId).HasColumnName("payment_id");
        builder.Property(value => value.IntentId).HasColumnName("intent_id");
        builder.Property(value => value.RefundId).HasColumnName("refund_id");
        builder.HasOne<Unit>().WithMany().HasForeignKey(value => new { value.PropertyId, value.UnitId })
            .HasPrincipalKey(value => new { value.PropertyId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(value => value.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentIntent>().WithMany().HasForeignKey(value => value.IntentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentRefund>().WithMany().HasForeignKey(value => value.RefundId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PropertyId, value.OccurredOn });
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_ledger_entries_values",
                "occurred_on >= DATE '2000-01-01' AND ((entry_type IN ('COLLECTION','UNALLOCATED_RECEIPT') AND amount > 0) OR " +
                "(entry_type IN ('COLLECTION_REVERSAL','ONLINE_REFUND','CHARGEBACK') AND amount < 0))");
            table.HasCheckConstraint("ck_payment_ledger_entries_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_refunds");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.IntentId).HasColumnName("intent_id");
        builder.Property(value => value.RequestId).HasColumnName("request_id");
        builder.Property(value => value.Amount).HasColumnName("amount").HasPrecision(10, 2);
        builder.Property(value => value.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(value => value.Status).HasColumnName("refund_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.DecidedBy).HasColumnName("decided_by").HasMaxLength(200);
        builder.Property(value => value.DecidedAt).HasColumnName("decided_at");
        builder.Property(value => value.DecisionNote).HasColumnName("decision_note").HasMaxLength(500);
        builder.Property(value => value.ProviderRefundId).HasColumnName("provider_refund_id").HasMaxLength(100);
        builder.Property(value => value.FailureCode).HasColumnName("failure_code").HasMaxLength(50);
        builder.HasOne<PaymentIntent>().WithMany().HasForeignKey(value => value.IntentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.PropertyId, value.RequestId }).IsUnique();
        builder.HasIndex(value => value.IntentId).IsUnique().HasDatabaseName("uidx_payment_refunds_open_intent")
            .HasFilter("refund_status IN ('REQUESTED','SUCCEEDED')");
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_refunds_values", "amount > 0 AND length(trim(reason)) > 0 AND refund_status IN ('REQUESTED','REJECTED','SUCCEEDED','FAILED')");
            table.HasCheckConstraint("ck_payment_refunds_decision",
                "(refund_status = 'REQUESTED' AND decided_by IS NULL) OR (refund_status <> 'REQUESTED' AND decided_by IS NOT NULL AND decided_at IS NOT NULL)");
            table.HasCheckConstraint("ck_payment_refunds_history", "is_deleted = false AND is_enabled = true");
        });
    }
}

internal sealed class PaymentReconciliationItemConfiguration : IEntityTypeConfiguration<PaymentReconciliationItem>
{
    public void Configure(EntityTypeBuilder<PaymentReconciliationItem> builder)
    {
        BaseEntityConfiguration.Configure(builder, "payment_reconciliation_items");
        builder.Property(value => value.PropertyId).HasColumnName("property_id");
        builder.Property(value => value.IntentId).HasColumnName("intent_id");
        builder.Property(value => value.Type).HasColumnName("issue_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.Status).HasColumnName("issue_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.Detail).HasColumnName("detail").HasMaxLength(500);
        builder.Property(value => value.ResolvedBy).HasColumnName("resolved_by").HasMaxLength(200);
        builder.Property(value => value.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(value => value.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(500);
        builder.HasOne<Property>().WithMany().HasForeignKey(value => value.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentIntent>().WithMany().HasForeignKey(value => value.IntentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.IntentId, value.Type }).IsUnique().HasDatabaseName("uidx_payment_reconciliation_items_open")
            .HasFilter("issue_status = 'OPEN' AND intent_id IS NOT NULL");
        builder.HasIndex(value => new { value.PropertyId, value.Status });
        builder.ToTable(table => {
            table.HasCheckConstraint("ck_payment_reconciliation_items_values",
                "issue_type IN ('DEBT_CHANGED','CHARGEBACK_UNREVERSED','PROVIDER_MISMATCH','REFUND_UNCONFIRMED') AND " +
                "((issue_status = 'OPEN' AND resolved_by IS NULL) OR (issue_status = 'RESOLVED' AND resolved_by IS NOT NULL AND resolution_note IS NOT NULL))");
            table.HasCheckConstraint("ck_payment_reconciliation_items_history", "is_deleted = false AND is_enabled = true");
        });
    }
}
