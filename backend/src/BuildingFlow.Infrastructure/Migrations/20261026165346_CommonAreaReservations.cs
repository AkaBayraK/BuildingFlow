using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommonAreaReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "common_area_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    common_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    starts_at = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ends_at = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_common_area_reservations", x => x.id);
                    table.CheckConstraint("ck_common_area_reservations_valid", "request_id <> '00000000-0000-0000-0000-000000000000'::uuid AND fingerprint ~ '^[0-9A-Fa-f]{64}$' AND starts_at < ends_at AND EXTRACT(EPOCH FROM starts_at)::bigint % 900 = 0 AND EXTRACT(EPOCH FROM ends_at)::bigint % 900 = 0 AND party_size BETWEEN 1 AND 10000 AND status IN ('CONFIRMED','CANCELLED') AND ((status = 'CONFIRMED' AND cancel_reason IS NULL AND cancelled_at IS NULL) OR (status = 'CANCELLED' AND length(trim(cancel_reason)) > 0 AND cancelled_at IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_common_area_reservations_common_areas_property_id_common_ar~",
                        columns: x => new { x.property_id, x.common_area_id },
                        principalTable: "common_areas",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_common_area_reservations_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_common_area_reservations_property_id_common_area_id_reserva~",
                table: "common_area_reservations",
                columns: new[] { "property_id", "common_area_id", "reservation_date", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "IX_common_area_reservations_property_id_request_id",
                table: "common_area_reservations",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_common_area_reservations_property_id_unit_id",
                table: "common_area_reservations",
                columns: new[] { "property_id", "unit_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                  IF EXISTS (SELECT 1 FROM common_area_reservations) THEN
                    RAISE EXCEPTION 'Common area reservation history exists; use a forward migration.';
                  END IF;
                END $guard$;
                """);
            migrationBuilder.DropTable(
                name: "common_area_reservations");
        }
    }
}
