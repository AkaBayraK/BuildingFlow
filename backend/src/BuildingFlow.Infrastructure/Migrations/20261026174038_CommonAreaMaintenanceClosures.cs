using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommonAreaMaintenanceClosures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "common_area_closures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    common_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    starts_at = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ends_at = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    notice = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_announcement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    release_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notification_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_common_area_closures", x => x.id);
                    table.CheckConstraint("ck_common_area_closure_valid", "starts_at < ends_at AND EXTRACT(SECOND FROM starts_at)=0 AND EXTRACT(SECOND FROM ends_at)=0 AND EXTRACT(MINUTE FROM starts_at)::int % 15=0 AND EXTRACT(MINUTE FROM ends_at)::int % 15=0 AND length(trim(notice))>0 AND notification_count>=0 AND ((released_at IS NULL AND release_reason IS NULL AND release_announcement_id IS NULL) OR (released_at IS NOT NULL AND release_reason IS NOT NULL AND length(trim(release_reason))>0 AND release_announcement_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_common_area_closures_announcements_property_id_announcement~",
                        columns: x => new { x.property_id, x.announcement_id },
                        principalTable: "announcements",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_common_area_closures_announcements_property_id_release_anno~",
                        columns: x => new { x.property_id, x.release_announcement_id },
                        principalTable: "announcements",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_common_area_closures_common_areas_property_id_common_area_id",
                        columns: x => new { x.property_id, x.common_area_id },
                        principalTable: "common_areas",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_common_area_closures_maintenance_plans_property_id_maintena~",
                        columns: x => new { x.property_id, x.maintenance_plan_id },
                        principalTable: "maintenance_plans",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_common_area_closures_property_id_announcement_id",
                table: "common_area_closures",
                columns: new[] { "property_id", "announcement_id" });

            migrationBuilder.CreateIndex(
                name: "IX_common_area_closures_property_id_common_area_id_date",
                table: "common_area_closures",
                columns: new[] { "property_id", "common_area_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_common_area_closures_property_id_common_area_id_maintenance~",
                table: "common_area_closures",
                columns: new[] { "property_id", "common_area_id", "maintenance_plan_id" },
                unique: true,
                filter: "released_at IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_common_area_closures_property_id_maintenance_plan_id",
                table: "common_area_closures",
                columns: new[] { "property_id", "maintenance_plan_id" });

            migrationBuilder.CreateIndex(
                name: "IX_common_area_closures_property_id_release_announcement_id",
                table: "common_area_closures",
                columns: new[] { "property_id", "release_announcement_id" });

            migrationBuilder.CreateIndex(
                name: "IX_common_area_closures_property_id_request_id",
                table: "common_area_closures",
                columns: new[] { "property_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                  IF EXISTS (SELECT 1 FROM common_area_closures) THEN
                    RAISE EXCEPTION 'Common area maintenance history exists; use a forward migration.';
                  END IF;
                END $guard$;
                """);
            migrationBuilder.DropTable(
                name: "common_area_closures");
        }
    }
}
