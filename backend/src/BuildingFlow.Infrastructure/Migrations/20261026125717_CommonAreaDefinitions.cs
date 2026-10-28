using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommonAreaDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "common_areas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    rules = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    opens_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    closes_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    fee_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
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
                    table.PrimaryKey("PK_common_areas", x => x.id);
                    table.UniqueConstraint("AK_common_areas_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_common_areas_valid", "length(trim(name)) > 0 AND request_id <> '00000000-0000-0000-0000-000000000000'::uuid AND fingerprint ~ '^[0-9A-Fa-f]{64}$' AND type IN ('POOL','FITNESS','MEETING_ROOM','PLAYGROUND','BARBECUE','SPORTS_COURT','EVENT_HALL','OTHER') AND visibility IN ('PROPERTY','BUILDING') AND (visibility <> 'BUILDING' OR building_id IS NOT NULL) AND usage IN ('OPEN_USE','RESERVATION','APPROVAL') AND capacity BETWEEN 1 AND 10000 AND currency = 'TRY' AND fee_amount BETWEEN 0 AND 99999999.99 AND ((opens_at IS NULL AND closes_at IS NULL) OR (opens_at IS NOT NULL AND closes_at IS NOT NULL AND opens_at < closes_at AND EXTRACT(SECOND FROM opens_at) = 0 AND EXTRACT(SECOND FROM closes_at) = 0))");
                    table.ForeignKey(
                        name: "FK_common_areas_buildings_property_id_building_id",
                        columns: x => new { x.property_id, x.building_id },
                        principalTable: "buildings",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_common_areas_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_common_areas_property_id_building_id",
                table: "common_areas",
                columns: new[] { "property_id", "building_id" });

            migrationBuilder.CreateIndex(
                name: "IX_common_areas_property_id_request_id",
                table: "common_areas",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_common_areas_property_id_type_name",
                table: "common_areas",
                columns: new[] { "property_id", "type", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                    IF EXISTS (SELECT 1 FROM common_areas) THEN
                        RAISE EXCEPTION 'Common area history exists; use a forward migration or isolated restore.';
                    END IF;
                END $guard$;
                """);
            migrationBuilder.DropTable(
                name: "common_areas");
        }
    }
}
