using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    number_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    corrects_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recorded_by_issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    recorded_by_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reconciled_on = table.Column<DateOnly>(type: "date", nullable: true),
                    reconciliation_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_supplier_invoices", x => x.id);
                    table.UniqueConstraint("AK_supplier_invoices_tenant_id_supplier_id_property_id_id", x => new { x.tenant_id, x.supplier_id, x.property_id, x.id });
                    table.CheckConstraint("ck_supplier_invoice_amounts", "net_amount > 0 AND vat_rate BETWEEN 0 AND 100 AND vat_amount >= 0 AND gross_amount = net_amount + vat_amount AND gross_amount <= 99999999.99 AND currency = 'TRY'");
                    table.CheckConstraint("ck_supplier_invoice_lifecycle", "(status NOT IN ('APPROVED','PAID','RECONCILED') OR expense_id IS NOT NULL) AND (status NOT IN ('PAID','RECONCILED')) = (paid_on IS NULL AND payment_reference IS NULL) AND (paid_on IS NULL OR paid_on >= issue_date) AND (status <> 'RECONCILED') = (reconciled_on IS NULL AND reconciliation_note IS NULL) AND (reconciled_on IS NULL OR reconciled_on >= paid_on) AND (status = 'CANCELLED') = (cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0)");
                    table.CheckConstraint("ck_supplier_invoice_valid", "length(trim(number)) > 0 AND length(number_key) > 0 AND length(trim(description)) > 0 AND issue_date >= DATE '2000-01-01' AND due_date >= issue_date AND due_date <= DATE '2100-12-31' AND category IN ('MAINTENANCE','CLEANING','STAFF','ENERGY','SECURITY','INSURANCE','ADMINISTRATION','OTHER') AND status IN ('RECORDED','APPROVED','PAID','RECONCILED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_supplier_invoices_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invoices_supplier_contracts_tenant_id_supplier_id_~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.contract_id },
                        principalTable: "supplier_contracts",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invoices_supplier_invoices_tenant_id_supplier_id_p~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.corrects_invoice_id },
                        principalTable: "supplier_invoices",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invoices_supplier_management_areas_tenant_id_suppl~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id },
                        principalTable: "supplier_management_areas",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invoices_supplier_work_orders_tenant_id_supplier_i~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.work_order_id },
                        principalTable: "supplier_work_orders",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_invoice_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    applied_version = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_supplier_invoice_revisions", x => x.id);
                    table.CheckConstraint("ck_supplier_invoice_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','APPROVE','PAY','RECONCILE','CANCEL')");
                    table.ForeignKey(
                        name: "FK_supplier_invoice_revisions_supplier_invoices_tenant_id_supp~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.invoice_id },
                        principalTable: "supplier_invoices",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_revisions_invoice_id_applied_version",
                table: "supplier_invoice_revisions",
                columns: new[] { "invoice_id", "applied_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_revisions_tenant_id_actor_issuer_actor_sub~",
                table: "supplier_invoice_revisions",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_revisions_tenant_id_supplier_id_property_i~",
                table: "supplier_invoice_revisions",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_corrects_invoice_id",
                table: "supplier_invoices",
                column: "corrects_invoice_id",
                unique: true,
                filter: "corrects_invoice_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_expense_id",
                table: "supplier_invoices",
                column: "expense_id",
                unique: true,
                filter: "expense_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_tenant_id_supplier_id_number_key",
                table: "supplier_invoices",
                columns: new[] { "tenant_id", "supplier_id", "number_key" },
                unique: true,
                filter: "status <> 'CANCELLED' AND NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_tenant_id_supplier_id_property_id_contrac~",
                table: "supplier_invoices",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_tenant_id_supplier_id_property_id_correct~",
                table: "supplier_invoices",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "corrects_invoice_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_tenant_id_supplier_id_property_id_status_~",
                table: "supplier_invoices",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "status", "issue_date" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_tenant_id_supplier_id_property_id_work_or~",
                table: "supplier_invoices",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "work_order_id" });
            migrationBuilder.Sql("""
                CREATE FUNCTION guard_supplier_invoice() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP='DELETE' OR NEW.is_deleted OR NOT NEW.is_enabled OR
                       (NEW.tenant_id,NEW.supplier_id,NEW.property_id,NEW.number,NEW.number_key,NEW.issue_date,NEW.due_date,NEW.net_amount,NEW.vat_rate,NEW.vat_amount,NEW.gross_amount,NEW.currency,NEW.category,NEW.description,NEW.work_order_id,NEW.contract_id,NEW.corrects_invoice_id,NEW.recorded_by_issuer,NEW.recorded_by_subject)
                       IS DISTINCT FROM (OLD.tenant_id,OLD.supplier_id,OLD.property_id,OLD.number,OLD.number_key,OLD.issue_date,OLD.due_date,OLD.net_amount,OLD.vat_rate,OLD.vat_amount,OLD.gross_amount,OLD.currency,OLD.category,OLD.description,OLD.work_order_id,OLD.contract_id,OLD.corrects_invoice_id,OLD.recorded_by_issuer,OLD.recorded_by_subject) THEN
                        RAISE EXCEPTION 'Supplier invoice values are immutable' USING ERRCODE='23514';
                    END IF;
                    IF NEW.status <> OLD.status AND NOT (
                        (OLD.status='RECORDED' AND NEW.status IN ('APPROVED','CANCELLED')) OR
                        (OLD.status='APPROVED' AND NEW.status IN ('PAID','CANCELLED')) OR
                        (OLD.status='PAID' AND NEW.status='RECONCILED')
                    ) THEN RAISE EXCEPTION 'Invalid supplier invoice transition' USING ERRCODE='23514'; END IF;
                    IF (NEW.expense_id IS DISTINCT FROM OLD.expense_id AND NOT (OLD.status='RECORDED' AND NEW.status='APPROVED')) OR
                       ((NEW.paid_on,NEW.payment_reference) IS DISTINCT FROM (OLD.paid_on,OLD.payment_reference) AND NOT (OLD.status='APPROVED' AND NEW.status='PAID')) OR
                       ((NEW.reconciled_on,NEW.reconciliation_note) IS DISTINCT FROM (OLD.reconciled_on,OLD.reconciliation_note) AND NOT (OLD.status='PAID' AND NEW.status='RECONCILED')) OR
                       (NEW.cancellation_reason IS DISTINCT FROM OLD.cancellation_reason AND NOT (OLD.status<>'CANCELLED' AND NEW.status='CANCELLED')) THEN
                        RAISE EXCEPTION 'Supplier invoice history cannot be rewritten' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_invoice_guard BEFORE UPDATE OR DELETE ON supplier_invoices FOR EACH ROW EXECUTE FUNCTION guard_supplier_invoice();
                CREATE TRIGGER supplier_invoice_revision_immutable BEFORE UPDATE OR DELETE ON supplier_invoice_revisions FOR EACH ROW EXECUTE FUNCTION reject_supplier_inquiry_revision_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM supplier_invoices) THEN RAISE EXCEPTION 'Supplier invoices exist; rollback would lose accounting history'; END IF;
                END $$;
                DROP FUNCTION guard_supplier_invoice() CASCADE;
                DROP TRIGGER supplier_invoice_revision_immutable ON supplier_invoice_revisions;
                """);
            migrationBuilder.DropTable(
                name: "supplier_invoice_revisions");

            migrationBuilder.DropTable(
                name: "supplier_invoices");
        }
    }
}
