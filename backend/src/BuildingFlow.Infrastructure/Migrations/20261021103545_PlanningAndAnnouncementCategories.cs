using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <summary>Planlama &amp; Bakımlar table and announcement categories; existing announcements are backfilled to GENERAL.</summary>
    public partial class PlanningAndAnnouncementCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "announcements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "GENERAL");

            migrationBuilder.CreateTable(
                name: "maintenance_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    planned_date = table.Column<DateOnly>(type: "date", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    responsible = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_maintenance_plans", x => x.id);
                    table.UniqueConstraint("AK_maintenance_plans_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_maintenance_plans_valid", "type IN ('MAINTENANCE','CLEANING','INSPECTION','DRILL','OTHER') AND status IN ('PLANNED','IN_PROGRESS','COMPLETED','CANCELLED') AND length(trim(title)) > 0 AND planned_date BETWEEN DATE '2000-01-01' AND DATE '2100-12-31'");
                    table.ForeignKey(
                        name: "FK_maintenance_plans_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_announcements_category",
                table: "announcements",
                sql: "category IN ('GENERAL','MEETING','DUES','MAINTENANCE','URGENT','OTHER')");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_plans_property_id_planned_date",
                table: "maintenance_plans",
                columns: new[] { "property_id", "planned_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Plans and chosen categories are user data; Down refuses to discard them instead of silently losing history.
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM maintenance_plans) OR EXISTS(SELECT 1 FROM announcements WHERE category <> 'GENERAL') THEN RAISE EXCEPTION 'Planning history or announcement categories cannot be discarded'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "maintenance_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_announcements_category",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "category",
                table: "announcements");
        }
    }
}
