using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LatePenalties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payment_allocations_payment_id_charge_id",
                table: "payment_allocations");

            migrationBuilder.AddColumn<Guid>(
                name: "accrual_id",
                table: "payment_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "payment_allocations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PRINCIPAL");

            migrationBuilder.CreateTable(
                name: "late_penalty_accruals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_late_penalty_accruals", x => x.id);
                    table.UniqueConstraint("AK_late_penalty_accruals_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_late_penalty_accruals_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_late_penalty_accruals_status", "(status = 'ACTIVE' AND status_reason IS NULL AND status_changed_at IS NULL) OR (status <> 'ACTIVE' AND paid_amount = 0 AND status_reason IS NOT NULL AND length(trim(status_reason)) > 0 AND status_changed_at IS NOT NULL)");
                    table.CheckConstraint("ck_late_penalty_accruals_values", "amount > 0 AND paid_amount >= 0 AND paid_amount <= amount AND to_date >= from_date AND status IN ('ACTIVE','WAIVED','SUPERSEDED')");
                    table.ForeignKey(
                        name: "FK_late_penalty_accruals_dues_charges_property_id_unit_id_char~",
                        columns: x => new { x.property_id, x.unit_id, x.charge_id },
                        principalTable: "dues_charges",
                        principalColumns: new[] { "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "late_penalty_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    monthly_rate = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    legal_basis = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    legal_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    approval_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_late_penalty_policies", x => x.id);
                    table.CheckConstraint("ck_late_penalty_policies_approval", "(approval_status = 'APPROVED' AND approved_by IS NOT NULL AND approved_at IS NOT NULL) OR (approval_status IN ('DRAFT','CANCELLED') AND approved_by IS NULL AND approved_at IS NULL)");
                    table.CheckConstraint("ck_late_penalty_policies_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_late_penalty_policies_legal", "approval_status <> 'APPROVED' OR (monthly_rate = 5 AND legal_basis = 'KMK_20') OR (legal_basis <> 'KMK_20' AND approval_document_id IS NOT NULL)");
                    table.CheckConstraint("ck_late_penalty_policies_values", "monthly_rate > 0 AND monthly_rate <= 5 AND valid_from >= DATE '2000-01-01' AND (valid_to IS NULL OR valid_to >= valid_from) AND year = EXTRACT(YEAR FROM valid_from) AND calculation_method IN ('DAILY_30','DAILY_ACTUAL_MONTH') AND legal_basis IN ('KMK_20','MANAGEMENT_PLAN','GENERAL_ASSEMBLY','COURT_DECISION') AND length(trim(legal_reference)) > 0");
                    table.ForeignKey(
                        name: "FK_late_penalty_policies_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_late_penalty_policies_stored_documents_approval_document_id",
                        column: x => x.approval_document_id,
                        principalTable: "stored_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "late_penalty_switches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    is_applied = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_late_penalty_switches", x => x.id);
                    table.CheckConstraint("ck_late_penalty_switches_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_late_penalty_switches_values", "effective_from >= DATE '2000-01-01' AND length(trim(reason)) > 0");
                    table.ForeignKey(
                        name: "FK_late_penalty_switches_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "late_penalty_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accrual_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    principal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    monthly_rate = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(16,6)", precision: 16, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_late_penalty_segments", x => x.id);
                    table.CheckConstraint("ck_late_penalty_segments_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_late_penalty_segments_values", "days > 0 AND to_date >= from_date AND principal > 0 AND monthly_rate > 0 AND monthly_rate <= 5 AND amount >= 0");
                    table.ForeignKey(
                        name: "FK_late_penalty_segments_late_penalty_accruals_property_id_acc~",
                        columns: x => new { x.property_id, x.accrual_id },
                        principalTable: "late_penalty_accruals",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_late_penalty_segments_late_penalty_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "late_penalty_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_payment_id_accrual_id",
                table: "payment_allocations",
                columns: new[] { "payment_id", "accrual_id" },
                unique: true,
                filter: "accrual_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_payment_id_charge_id",
                table: "payment_allocations",
                columns: new[] { "payment_id", "charge_id" },
                unique: true,
                filter: "kind = 'PRINCIPAL'");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_property_id_accrual_id",
                table: "payment_allocations",
                columns: new[] { "property_id", "accrual_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_allocations_kind",
                table: "payment_allocations",
                sql: "(kind = 'PRINCIPAL' AND accrual_id IS NULL) OR (kind = 'PENALTY' AND accrual_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_accruals_charge_id_from_date",
                table: "late_penalty_accruals",
                columns: new[] { "charge_id", "from_date" },
                unique: true,
                filter: "status <> 'SUPERSEDED'");

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_accruals_property_id_status_to_date",
                table: "late_penalty_accruals",
                columns: new[] { "property_id", "status", "to_date" });

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_accruals_property_id_unit_id_charge_id",
                table: "late_penalty_accruals",
                columns: new[] { "property_id", "unit_id", "charge_id" });

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_policies_approval_document_id",
                table: "late_penalty_policies",
                column: "approval_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_policies_property_id_approval_status_valid_from",
                table: "late_penalty_policies",
                columns: new[] { "property_id", "approval_status", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_policies_property_id_request_id",
                table: "late_penalty_policies",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_segments_accrual_id_from_date",
                table: "late_penalty_segments",
                columns: new[] { "accrual_id", "from_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_segments_policy_id",
                table: "late_penalty_segments",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_segments_property_id_accrual_id",
                table: "late_penalty_segments",
                columns: new[] { "property_id", "accrual_id" });

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_switches_property_id_effective_from",
                table: "late_penalty_switches",
                columns: new[] { "property_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_late_penalty_switches_property_id_request_id",
                table: "late_penalty_switches",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_allocations_late_penalty_accruals_property_id_accru~",
                table: "payment_allocations",
                columns: new[] { "property_id", "accrual_id" },
                principalTable: "late_penalty_accruals",
                principalColumns: new[] { "property_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM late_penalty_switches) OR EXISTS (SELECT 1 FROM late_penalty_policies) OR EXISTS (SELECT 1 FROM late_penalty_accruals) OR EXISTS (SELECT 1 FROM payment_allocations WHERE kind <> 'PRINCIPAL') THEN RAISE EXCEPTION 'Late penalty financial history must be preserved.'; END IF; END $$;");
            migrationBuilder.DropForeignKey(
                name: "FK_payment_allocations_late_penalty_accruals_property_id_accru~",
                table: "payment_allocations");

            migrationBuilder.DropTable(
                name: "late_penalty_segments");

            migrationBuilder.DropTable(
                name: "late_penalty_switches");

            migrationBuilder.DropTable(
                name: "late_penalty_accruals");

            migrationBuilder.DropTable(
                name: "late_penalty_policies");

            migrationBuilder.DropIndex(
                name: "IX_payment_allocations_payment_id_accrual_id",
                table: "payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_payment_allocations_payment_id_charge_id",
                table: "payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_payment_allocations_property_id_accrual_id",
                table: "payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_allocations_kind",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "accrual_id",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "payment_allocations");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_payment_id_charge_id",
                table: "payment_allocations",
                columns: new[] { "payment_id", "charge_id" },
                unique: true);
        }
    }
}
