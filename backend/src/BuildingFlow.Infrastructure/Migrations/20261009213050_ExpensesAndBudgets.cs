using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpensesAndBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    occurred_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    document_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_expenses", x => x.id);
                    table.CheckConstraint("ck_expenses_cancellation", "(is_cancelled = false AND cancellation_reason IS NULL AND cancelled_at IS NULL) OR (is_cancelled = true AND cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0 AND cancelled_at IS NOT NULL)");
                    table.CheckConstraint("ck_expenses_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_expenses_values", "amount > 0 AND currency = 'TRY' AND occurred_date >= DATE '2000-01-01' AND category IN ('MAINTENANCE','CLEANING','STAFF','ENERGY','SECURITY','INSURANCE','ADMINISTRATION','OTHER') AND length(trim(description)) > 0");
                    table.ForeignKey(
                        name: "FK_expenses_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "property_budgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    revision_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_property_budgets", x => x.id);
                    table.UniqueConstraint("AK_property_budgets_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_budgets_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_budgets_values", "amount >= 0 AND currency = 'TRY' AND period >= DATE '2000-01-01' AND period < DATE '9999-01-01' AND EXTRACT(DAY FROM period) = 1 AND category IN ('MAINTENANCE','CLEANING','STAFF','ENERGY','SECURITY','INSURANCE','ADMINISTRATION','OTHER') AND revision_count > 0");
                    table.ForeignKey(
                        name: "FK_property_budgets_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budget_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_budget_revisions", x => x.id);
                    table.CheckConstraint("ck_budget_revisions_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_budget_revisions_values", "amount >= 0 AND sequence > 0 AND length(trim(explanation)) > 0");
                    table.ForeignKey(
                        name: "FK_budget_revisions_property_budgets_property_id_budget_id",
                        columns: x => new { x.property_id, x.budget_id },
                        principalTable: "property_budgets",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_budget_revisions_budget_id_sequence",
                table: "budget_revisions",
                columns: new[] { "budget_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_budget_revisions_property_id_budget_id",
                table: "budget_revisions",
                columns: new[] { "property_id", "budget_id" });

            migrationBuilder.CreateIndex(
                name: "IX_budget_revisions_property_id_request_id",
                table: "budget_revisions",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expenses_property_id_occurred_date_category",
                table: "expenses",
                columns: new[] { "property_id", "occurred_date", "category" });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_property_id_request_id",
                table: "expenses",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_property_budgets_property_id_period_category",
                table: "property_budgets",
                columns: new[] { "property_id", "period", "category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Even cancelled expenses and old revisions are financial history.
            // Refuse the entire downgrade before dropping any table.
            migrationBuilder.Sql(@"DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM expenses) OR EXISTS (SELECT 1 FROM property_budgets) OR EXISTS (SELECT 1 FROM budget_revisions) THEN
                    RAISE EXCEPTION 'Expense or budget history exists; use a data-preserving forward migration.';
                END IF;
            END $$;");

            migrationBuilder.DropTable(
                name: "budget_revisions");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "property_budgets");
        }
    }
}
