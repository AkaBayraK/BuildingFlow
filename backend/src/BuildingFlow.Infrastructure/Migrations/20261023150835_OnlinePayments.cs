using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OnlinePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_payments_values",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_dues_charges_property_id_unit_id_period",
                table: "dues_charges");

            migrationBuilder.AddColumn<Guid>(
                name: "bank_account_id",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "dues_charges",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "dues_charges",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "DUES");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "dues_assessments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "dues_assessments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "DUES");

            migrationBuilder.CreateTable(
                name: "payment_bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_holder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    purpose = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    account_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decided_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_bank_accounts", x => x.id);
                    table.UniqueConstraint("AK_payment_bank_accounts_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_payment_bank_accounts_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_payment_bank_accounts_status", "account_status IN ('PENDING_APPROVAL','ACTIVE','REJECTED','CLOSED') AND (is_default = false OR account_status = 'ACTIVE') AND (account_status = 'PENDING_APPROVAL' OR decided_by IS NOT NULL OR account_status = 'CLOSED')");
                    table.CheckConstraint("ck_payment_bank_accounts_values", "currency = 'TRY' AND iban ~ '^TR[0-9]{24}$' AND purpose IN ('ALL','DUES','EXTRA') AND valid_from >= DATE '2000-01-01' AND (valid_to IS NULL OR valid_to >= valid_from) AND length(trim(bank_name)) > 0 AND length(trim(account_holder)) > 0");
                    table.ForeignKey(
                        name: "FK_payment_bank_accounts_buildings_property_id_building_id",
                        columns: x => new { x.property_id, x.building_id },
                        principalTable: "buildings",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_bank_accounts_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_merchant_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    merchant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    secret_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    environment = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_check_succeeded = table.Column<bool>(type: "boolean", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_merchant_accounts", x => x.id);
                    table.UniqueConstraint("AK_payment_merchant_accounts_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_payment_merchant_accounts_history", "is_deleted = false");
                    table.CheckConstraint("ck_payment_merchant_accounts_values", "environment = 'SANDBOX' AND provider_code = upper(provider_code) AND secret_reference ~ '^[A-Za-z0-9_:-]{1,100}$' AND length(trim(merchant_id)) > 0 AND length(trim(label)) > 0 AND (is_default = false OR is_enabled = true)");
                    table.ForeignKey(
                        name: "FK_payment_merchant_accounts_buildings_property_id_building_id",
                        columns: x => new { x.property_id, x.building_id },
                        principalTable: "buildings",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_merchant_accounts_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_providers", x => x.id);
                    table.CheckConstraint("ck_payment_providers_code", "code = upper(code) AND length(trim(code)) > 0");
                    table.CheckConstraint("ck_payment_providers_history", "is_deleted = false");
                });

            migrationBuilder.CreateTable(
                name: "payment_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_year = table.Column<int>(type: "integer", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_receipts", x => x.id);
                    table.CheckConstraint("ck_payment_receipts_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_payment_receipts_values", "receipt_year >= 2000 AND sequence_number > 0");
                    table.ForeignKey(
                        name: "FK_payment_receipts_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_receipts_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "property_payment_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_online_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_partial_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    is_iban_visible_to_residents = table.Column<bool>(type: "boolean", nullable: false),
                    is_dual_approval_required = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    commission_policy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_payment_settings", x => x.id);
                    table.CheckConstraint("ck_property_payment_settings_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_property_payment_settings_values", "commission_policy = 'ABSORBED' AND receipt_prefix ~ '^[A-Z0-9]{2,10}$'");
                    table.ForeignKey(
                        name: "FK_property_payment_settings_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payer_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payer_role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    merchant_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    intent_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    checkout_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    provider_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    paid_date = table.Column<DateOnly>(type: "date", nullable: true),
                    fee_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_intents", x => x.id);
                    table.UniqueConstraint("AK_payment_intents_property_id_unit_id_id", x => new { x.property_id, x.unit_id, x.id });
                    table.CheckConstraint("ck_payment_intents_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_payment_intents_success", "(intent_status IN ('PENDING','FAILED','EXPIRED','CANCELLED') AND paid_date IS NULL AND payment_id IS NULL) OR (intent_status NOT IN ('PENDING','FAILED','EXPIRED','CANCELLED') AND provider_transaction_id IS NOT NULL AND paid_date IS NOT NULL AND fee_amount >= 0 AND (intent_status <> 'SUCCEEDED' OR payment_id IS NOT NULL))");
                    table.CheckConstraint("ck_payment_intents_values", "amount > 0 AND currency = 'TRY' AND payer_role IN ('OWNER','TENANT') AND intent_status IN ('PENDING','SUCCEEDED','REVIEW','FAILED','EXPIRED','CANCELLED','REFUNDED','CHARGED_BACK')");
                    table.ForeignKey(
                        name: "FK_payment_intents_payment_merchant_accounts_property_id_merch~",
                        columns: x => new { x.property_id, x.merchant_account_id },
                        principalTable: "payment_merchant_accounts",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_intents_payments_property_id_unit_id_payment_id",
                        columns: x => new { x.property_id, x.unit_id, x.payment_id },
                        principalTable: "payments",
                        principalColumns: new[] { "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_intents_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_intent_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    charge_version = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_intent_lines", x => x.id);
                    table.CheckConstraint("ck_payment_intent_lines_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_payment_intent_lines_values", "amount > 0 AND kind IN ('PRINCIPAL','PENALTY') AND charge_version > 0");
                    table.ForeignKey(
                        name: "FK_payment_intent_lines_dues_charges_property_id_unit_id_charg~",
                        columns: x => new { x.property_id, x.unit_id, x.charge_id },
                        principalTable: "dues_charges",
                        principalColumns: new[] { "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_intent_lines_payment_intents_property_id_unit_id_in~",
                        columns: x => new { x.property_id, x.unit_id, x.intent_id },
                        principalTable: "payment_intents",
                        principalColumns: new[] { "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_provider_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_provider_events", x => x.id);
                    table.CheckConstraint("ck_payment_provider_events_history", "is_deleted = false AND is_enabled = true");
                    table.ForeignKey(
                        name: "FK_payment_provider_events_payment_intents_intent_id",
                        column: x => x.intent_id,
                        principalTable: "payment_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_provider_events_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_reconciliation_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issue_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    issue_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    resolved_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_reconciliation_items", x => x.id);
                    table.CheckConstraint("ck_payment_reconciliation_items_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_payment_reconciliation_items_values", "issue_type IN ('DEBT_CHANGED','CHARGEBACK_UNREVERSED','PROVIDER_MISMATCH','REFUND_UNCONFIRMED') AND ((issue_status = 'OPEN' AND resolved_by IS NULL) OR (issue_status = 'RESOLVED' AND resolved_by IS NOT NULL AND resolution_note IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_payment_reconciliation_items_payment_intents_intent_id",
                        column: x => x.intent_id,
                        principalTable: "payment_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_reconciliation_items_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    refund_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decided_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    provider_refund_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_refunds", x => x.id);
                    table.CheckConstraint("ck_payment_refunds_decision", "(refund_status = 'REQUESTED' AND decided_by IS NULL) OR (refund_status <> 'REQUESTED' AND decided_by IS NOT NULL AND decided_at IS NOT NULL)");
                    table.CheckConstraint("ck_payment_refunds_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_payment_refunds_values", "amount > 0 AND length(trim(reason)) > 0 AND refund_status IN ('REQUESTED','REJECTED','SUCCEEDED','FAILED')");
                    table.ForeignKey(
                        name: "FK_payment_refunds_payment_intents_intent_id",
                        column: x => x.intent_id,
                        principalTable: "payment_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_refunds_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_ledger_entries", x => x.id);
                    table.CheckConstraint("ck_payment_ledger_entries_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_payment_ledger_entries_values", "occurred_on >= DATE '2000-01-01' AND ((entry_type IN ('COLLECTION','UNALLOCATED_RECEIPT') AND amount > 0) OR (entry_type IN ('COLLECTION_REVERSAL','ONLINE_REFUND','CHARGEBACK') AND amount < 0))");
                    table.ForeignKey(
                        name: "FK_payment_ledger_entries_payment_intents_intent_id",
                        column: x => x.intent_id,
                        principalTable: "payment_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_ledger_entries_payment_refunds_refund_id",
                        column: x => x.refund_id,
                        principalTable: "payment_refunds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_ledger_entries_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_ledger_entries_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_property_id_bank_account_id",
                table: "payments",
                columns: new[] { "property_id", "bank_account_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_payments_values",
                table: "payments",
                sql: "amount > 0 AND currency = 'TRY' AND paid_date >= DATE '2000-01-01' AND method IN ('CASH','BANK_TRANSFER','ONLINE_CARD') AND (method <> 'BANK_TRANSFER' OR (reference IS NOT NULL AND length(trim(reference)) > 0 AND reference = upper(reference))) AND (method <> 'ONLINE_CARD' OR (reference IS NOT NULL AND length(trim(reference)) > 0)) AND (bank_account_id IS NULL OR method = 'BANK_TRANSFER')");

            migrationBuilder.CreateIndex(
                name: "IX_dues_charges_property_id_unit_id_period",
                table: "dues_charges",
                columns: new[] { "property_id", "unit_id", "period" },
                unique: true,
                filter: "is_cancelled = false AND kind = 'DUES'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charges_kind",
                table: "dues_charges",
                sql: "(kind = 'DUES' AND category IS NULL) OR (kind = 'EXTRA' AND category IN ('FIXTURE','REPAIR','HEATING','UTILITY','INSURANCE','OTHER'))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_assessments_kind",
                table: "dues_assessments",
                sql: "(kind = 'DUES' AND category IS NULL) OR (kind = 'EXTRA' AND category IN ('FIXTURE','REPAIR','HEATING','UTILITY','INSURANCE','OTHER'))");

            migrationBuilder.CreateIndex(
                name: "IX_payment_bank_accounts_property_id_request_id",
                table: "payment_bank_accounts",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_payment_bank_accounts_default_building",
                table: "payment_bank_accounts",
                columns: new[] { "property_id", "building_id" },
                unique: true,
                filter: "is_default = true AND building_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uidx_payment_bank_accounts_default_property",
                table: "payment_bank_accounts",
                column: "property_id",
                unique: true,
                filter: "is_default = true AND building_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uidx_payment_bank_accounts_open_iban",
                table: "payment_bank_accounts",
                columns: new[] { "property_id", "iban" },
                unique: true,
                filter: "account_status IN ('PENDING_APPROVAL','ACTIVE')");

            migrationBuilder.CreateIndex(
                name: "IX_payment_intent_lines_intent_id_charge_id_kind",
                table: "payment_intent_lines",
                columns: new[] { "intent_id", "charge_id", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_intent_lines_property_id_charge_id",
                table: "payment_intent_lines",
                columns: new[] { "property_id", "charge_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intent_lines_property_id_unit_id_charge_id",
                table: "payment_intent_lines",
                columns: new[] { "property_id", "unit_id", "charge_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intent_lines_property_id_unit_id_intent_id",
                table: "payment_intent_lines",
                columns: new[] { "property_id", "unit_id", "intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_intent_status_expires_at",
                table: "payment_intents",
                columns: new[] { "intent_status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_payment_id",
                table: "payment_intents",
                column: "payment_id",
                unique: true,
                filter: "payment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_property_id_created_at",
                table: "payment_intents",
                columns: new[] { "property_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_property_id_merchant_account_id",
                table: "payment_intents",
                columns: new[] { "property_id", "merchant_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_property_id_request_id",
                table: "payment_intents",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_property_id_unit_id_payment_id",
                table: "payment_intents",
                columns: new[] { "property_id", "unit_id", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_provider_code_provider_reference",
                table: "payment_intents",
                columns: new[] { "provider_code", "provider_reference" },
                unique: true,
                filter: "provider_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_ledger_entries_intent_id",
                table: "payment_ledger_entries",
                column: "intent_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_ledger_entries_payment_id",
                table: "payment_ledger_entries",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_ledger_entries_property_id_occurred_on",
                table: "payment_ledger_entries",
                columns: new[] { "property_id", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_ledger_entries_property_id_unit_id",
                table: "payment_ledger_entries",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_ledger_entries_refund_id",
                table: "payment_ledger_entries",
                column: "refund_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_merchant_accounts_property_id_request_id",
                table: "payment_merchant_accounts",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_payment_merchant_accounts_default_building",
                table: "payment_merchant_accounts",
                columns: new[] { "property_id", "building_id" },
                unique: true,
                filter: "is_default = true AND building_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uidx_payment_merchant_accounts_default_property",
                table: "payment_merchant_accounts",
                column: "property_id",
                unique: true,
                filter: "is_default = true AND building_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_provider_events_intent_id",
                table: "payment_provider_events",
                column: "intent_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_provider_events_property_id",
                table: "payment_provider_events",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "uidx_payment_provider_events_event",
                table: "payment_provider_events",
                columns: new[] { "provider_code", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_payment_providers_code",
                table: "payment_providers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_receipts_payment_id",
                table: "payment_receipts",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_receipts_property_id_receipt_number",
                table: "payment_receipts",
                columns: new[] { "property_id", "receipt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_receipts_property_id_receipt_year_sequence_number",
                table: "payment_receipts",
                columns: new[] { "property_id", "receipt_year", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_reconciliation_items_property_id_issue_status",
                table: "payment_reconciliation_items",
                columns: new[] { "property_id", "issue_status" });

            migrationBuilder.CreateIndex(
                name: "uidx_payment_reconciliation_items_open",
                table: "payment_reconciliation_items",
                columns: new[] { "intent_id", "issue_type" },
                unique: true,
                filter: "issue_status = 'OPEN' AND intent_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_refunds_property_id_request_id",
                table: "payment_refunds",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_payment_refunds_open_intent",
                table: "payment_refunds",
                column: "intent_id",
                unique: true,
                filter: "refund_status IN ('REQUESTED','SUCCEEDED')");

            migrationBuilder.CreateIndex(
                name: "uidx_property_payment_settings_property_id",
                table: "property_payment_settings",
                column: "property_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_payment_bank_accounts_property_id_bank_account_id",
                table: "payments",
                columns: new[] { "property_id", "bank_account_id" },
                principalTable: "payment_bank_accounts",
                principalColumns: new[] { "property_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM dues_assessments WHERE kind <> 'DUES') OR EXISTS (SELECT 1 FROM payments WHERE method = 'ONLINE_CARD' OR bank_account_id IS NOT NULL) " +
                "OR EXISTS (SELECT 1 FROM payment_receipts) OR EXISTS (SELECT 1 FROM payment_ledger_entries) OR EXISTS (SELECT 1 FROM payment_intents) " +
                "OR EXISTS (SELECT 1 FROM payment_bank_accounts) OR EXISTS (SELECT 1 FROM payment_merchant_accounts) OR EXISTS (SELECT 1 FROM payment_provider_events) " +
                "OR EXISTS (SELECT 1 FROM property_payment_settings) OR EXISTS (SELECT 1 FROM payment_providers) " +
                "THEN RAISE EXCEPTION 'Online payment and extra charge history must be preserved.'; END IF; END $$;");
            migrationBuilder.DropForeignKey(
                name: "FK_payments_payment_bank_accounts_property_id_bank_account_id",
                table: "payments");

            migrationBuilder.DropTable(
                name: "payment_bank_accounts");

            migrationBuilder.DropTable(
                name: "payment_intent_lines");

            migrationBuilder.DropTable(
                name: "payment_ledger_entries");

            migrationBuilder.DropTable(
                name: "payment_provider_events");

            migrationBuilder.DropTable(
                name: "payment_providers");

            migrationBuilder.DropTable(
                name: "payment_receipts");

            migrationBuilder.DropTable(
                name: "payment_reconciliation_items");

            migrationBuilder.DropTable(
                name: "property_payment_settings");

            migrationBuilder.DropTable(
                name: "payment_refunds");

            migrationBuilder.DropTable(
                name: "payment_intents");

            migrationBuilder.DropTable(
                name: "payment_merchant_accounts");

            migrationBuilder.DropIndex(
                name: "IX_payments_property_id_bank_account_id",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payments_values",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_dues_charges_property_id_unit_id_period",
                table: "dues_charges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_charges_kind",
                table: "dues_charges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_assessments_kind",
                table: "dues_assessments");

            migrationBuilder.DropColumn(
                name: "bank_account_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "category",
                table: "dues_charges");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "dues_charges");

            migrationBuilder.DropColumn(
                name: "category",
                table: "dues_assessments");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "dues_assessments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payments_values",
                table: "payments",
                sql: "amount > 0 AND currency = 'TRY' AND paid_date >= DATE '2000-01-01' AND method IN ('CASH','BANK_TRANSFER') AND (method <> 'BANK_TRANSFER' OR (reference IS NOT NULL AND length(trim(reference)) > 0 AND reference = upper(reference)))");

            migrationBuilder.CreateIndex(
                name: "IX_dues_charges_property_id_unit_id_period",
                table: "dues_charges",
                columns: new[] { "property_id", "unit_id", "period" },
                unique: true,
                filter: "is_cancelled = false");
        }
    }
}
