using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DuesAndCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dues_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_dues_assessments", x => x.id);
                    table.UniqueConstraint("AK_dues_assessments_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_assessments_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_assessments_values", "amount > 0 AND currency = 'TRY' AND EXTRACT(DAY FROM period) = 1 AND period >= DATE '2000-01-01' AND due_date >= period");
                    table.ForeignKey(
                        name: "FK_dues_assessments_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dues_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_dues_charges", x => x.id);
                    table.CheckConstraint("ck_charges_cancellation", "(is_cancelled = false AND cancellation_reason IS NULL AND cancelled_at IS NULL) OR (is_cancelled = true AND cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0 AND cancelled_at IS NOT NULL)");
                    table.CheckConstraint("ck_charges_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_charges_values", "amount > 0 AND currency = 'TRY' AND EXTRACT(DAY FROM period) = 1 AND period >= DATE '2000-01-01' AND due_date >= period");
                    table.ForeignKey(
                        name: "FK_dues_charges_dues_assessments_property_id_assessment_id",
                        columns: x => new { x.property_id, x.assessment_id },
                        principalTable: "dues_assessments",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dues_charges_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dues_assessments_property_id_request_id",
                table: "dues_assessments",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dues_charges_property_id_assessment_id",
                table: "dues_charges",
                columns: new[] { "property_id", "assessment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dues_charges_property_id_due_date",
                table: "dues_charges",
                columns: new[] { "property_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_dues_charges_property_id_unit_id_period",
                table: "dues_charges",
                columns: new[] { "property_id", "unit_id", "period" },
                unique: true,
                filter: "is_cancelled = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM dues_assessments) OR EXISTS (SELECT 1 FROM dues_charges) THEN
                    RAISE EXCEPTION 'Dues history exists; refusing a lossy downgrade.';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "dues_charges");

            migrationBuilder.DropTable(
                name: "dues_assessments");
        }
    }
}
