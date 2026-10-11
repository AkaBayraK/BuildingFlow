using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MetersAndReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_meters", x => x.id);
                    table.UniqueConstraint("AK_meters_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_meters_valid", "kind IN ('WATER','ELECTRICITY','GAS') AND length(trim(name))>0 AND length(request_fingerprint)=64");
                    table.ForeignKey(
                        name: "FK_meters_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meters_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meter_readings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reading_date = table.Column<DateOnly>(type: "date", nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
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
                    table.PrimaryKey("PK_meter_readings", x => x.id);
                    table.UniqueConstraint("AK_meter_readings_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_meter_readings_valid", "value>=0 AND revision_count>0 AND reading_date>='2000-01-01' AND is_enabled AND NOT is_deleted");
                    table.ForeignKey(
                        name: "FK_meter_readings_meters_property_id_meter_id",
                        columns: x => new { x.property_id, x.meter_id },
                        principalTable: "meters",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meter_reading_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reading_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_meter_reading_revisions", x => x.id);
                    table.CheckConstraint("ck_reading_revisions_valid", "value>=0 AND sequence>0 AND length(trim(reason))>0 AND length(request_fingerprint)=64 AND is_enabled AND NOT is_deleted");
                    table.ForeignKey(
                        name: "FK_meter_reading_revisions_meter_readings_property_id_reading_~",
                        columns: x => new { x.property_id, x.reading_id },
                        principalTable: "meter_readings",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_meter_reading_revisions_property_id_reading_id",
                table: "meter_reading_revisions",
                columns: new[] { "property_id", "reading_id" });

            migrationBuilder.CreateIndex(
                name: "IX_meter_reading_revisions_property_id_request_id",
                table: "meter_reading_revisions",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meter_reading_revisions_reading_id_sequence",
                table: "meter_reading_revisions",
                columns: new[] { "reading_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meter_readings_meter_id_reading_date",
                table: "meter_readings",
                columns: new[] { "meter_id", "reading_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meter_readings_property_id_meter_id",
                table: "meter_readings",
                columns: new[] { "property_id", "meter_id" });

            migrationBuilder.CreateIndex(
                name: "IX_meters_property_id_name",
                table: "meters",
                columns: new[] { "property_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meters_property_id_request_id",
                table: "meters",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meters_property_id_unit_id",
                table: "meters",
                columns: new[] { "property_id", "unit_id" });
            migrationBuilder.Sql("CREATE FUNCTION guard_meter_revision() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Reading revision is immutable'; END $$; CREATE TRIGGER meter_revision_immutable BEFORE UPDATE OR DELETE ON meter_reading_revisions FOR EACH ROW EXECUTE FUNCTION guard_meter_revision();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM meters) OR EXISTS(SELECT 1 FROM meter_readings) THEN RAISE EXCEPTION 'Meter history cannot be discarded'; END IF; END $$;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS meter_revision_immutable ON meter_reading_revisions; DROP FUNCTION IF EXISTS guard_meter_revision();");
            migrationBuilder.DropTable(
                name: "meter_reading_revisions");

            migrationBuilder.DropTable(
                name: "meter_readings");

            migrationBuilder.DropTable(
                name: "meters");
        }
    }
}
