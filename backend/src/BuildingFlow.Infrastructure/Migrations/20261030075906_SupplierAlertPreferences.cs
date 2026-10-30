using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierAlertPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_alert_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contract_expiry = table.Column<bool>(type: "boolean", nullable: false),
                    work_order_overdue = table.Column<bool>(type: "boolean", nullable: false),
                    maintenance_due = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_supplier_alert_preferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_alert_preferences_person_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "person_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_alert_preferences_tenant_id_actor_issuer_actor_sub~",
                table: "supplier_alert_preferences",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject" },
                unique: true,
                filter: "NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM supplier_alert_preferences) THEN RAISE EXCEPTION 'Alert preferences exist; rollback would lose user choices'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "supplier_alert_preferences");
        }
    }
}
