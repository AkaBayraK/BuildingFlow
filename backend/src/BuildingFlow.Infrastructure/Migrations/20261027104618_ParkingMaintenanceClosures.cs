using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParkingMaintenanceClosures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parking_closures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_parking_closures", x => x.id);
                    table.CheckConstraint("ck_parking_closures_valid", "ends_at > starts_at AND length(trim(reason)) > 0 AND ((cancelled_at IS NULL AND cancellation_reason IS NULL) OR (cancelled_at IS NOT NULL AND length(trim(cancellation_reason)) > 0))");
                    table.ForeignKey(
                        name: "FK_parking_closures_parking_spaces_property_id_space_id",
                        columns: x => new { x.property_id, x.space_id },
                        principalTable: "parking_spaces",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_parking_closures_property_id_request_id",
                table: "parking_closures",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parking_closures_property_id_space_id_starts_at",
                table: "parking_closures",
                columns: new[] { "property_id", "space_id", "starts_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM parking_closures) THEN
                        RAISE EXCEPTION 'Parking closure history exists; rollback would lose data.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "parking_closures");
        }
    }
}
