using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsAndAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "paid_amount",
                table: "dues_charges",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_dues_charges_property_id_unit_id_id",
                table: "dues_charges",
                columns: new[] { "property_id", "unit_id", "id" });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    paid_date = table.Column<DateOnly>(type: "date", nullable: false),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.UniqueConstraint("AK_payments_property_id_unit_id_id", x => new { x.property_id, x.unit_id, x.id });
                    table.CheckConstraint("ck_payments_cancellation", "(is_cancelled = false AND cancellation_reason IS NULL AND cancelled_at IS NULL) OR (is_cancelled = true AND cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0 AND cancelled_at IS NOT NULL)");
                    table.CheckConstraint("ck_payments_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_payments_values", "amount > 0 AND currency = 'TRY' AND paid_date >= DATE '2000-01-01' AND method IN ('CASH','BANK_TRANSFER') AND (method <> 'BANK_TRANSFER' OR (reference IS NOT NULL AND length(trim(reference)) > 0 AND reference = upper(reference)))");
                    table.ForeignKey(
                        name: "FK_payments_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_allocations_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_allocations_values", "amount > 0");
                    table.ForeignKey(
                        name: "FK_payment_allocations_dues_charges_property_id_unit_id_charge~",
                        columns: x => new { x.property_id, x.unit_id, x.charge_id },
                        principalTable: "dues_charges",
                        principalColumns: new[] { "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_allocations_payments_property_id_unit_id_payment_id",
                        columns: x => new { x.property_id, x.unit_id, x.payment_id },
                        principalTable: "payments",
                        principalColumns: new[] { "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_charges_paid",
                table: "dues_charges",
                sql: "paid_amount >= 0 AND paid_amount <= amount AND (is_cancelled = false OR paid_amount = 0)");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_payment_id_charge_id",
                table: "payment_allocations",
                columns: new[] { "payment_id", "charge_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_property_id_charge_id",
                table: "payment_allocations",
                columns: new[] { "property_id", "charge_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_property_id_unit_id_charge_id",
                table: "payment_allocations",
                columns: new[] { "property_id", "unit_id", "charge_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_property_id_unit_id_payment_id",
                table: "payment_allocations",
                columns: new[] { "property_id", "unit_id", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_property_id_paid_date",
                table: "payments",
                columns: new[] { "property_id", "paid_date" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_property_id_reference",
                table: "payments",
                columns: new[] { "property_id", "reference" },
                unique: true,
                filter: "method = 'BANK_TRANSFER' AND is_cancelled = false");

            migrationBuilder.CreateIndex(
                name: "IX_payments_property_id_request_id",
                table: "payments",
                columns: new[] { "property_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$
                BEGIN
                    IF EXISTS (SELECT 1 FROM payments) OR EXISTS (SELECT 1 FROM payment_allocations)
                       OR EXISTS (SELECT 1 FROM dues_charges WHERE paid_amount <> 0) THEN
                        RAISE EXCEPTION 'Payment history exists; downgrade would lose financial records.';
                    END IF;
                END $guard$;
                """);

            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_dues_charges_property_id_unit_id_id",
                table: "dues_charges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_charges_paid",
                table: "dues_charges");

            migrationBuilder.DropColumn(
                name: "paid_amount",
                table: "dues_charges");
        }
    }
}
