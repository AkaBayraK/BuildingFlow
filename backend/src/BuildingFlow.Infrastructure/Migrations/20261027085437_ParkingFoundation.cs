using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParkingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parking_facilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_parking_facilities", x => x.id);
                    table.UniqueConstraint("AK_parking_facilities_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_parking_facilities_name", "length(trim(name)) > 0");
                    table.ForeignKey(
                        name: "FK_parking_facilities_buildings_property_id_building_id",
                        columns: x => new { x.property_id, x.building_id },
                        principalTable: "buildings",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parking_facilities_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parking_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_vehicles_per_unit = table.Column<int>(type: "integer", nullable: false),
                    max_vehicles_per_person = table.Column<int>(type: "integer", nullable: false),
                    max_spaces_per_unit = table.Column<int>(type: "integer", nullable: false),
                    guest_max_hours = table.Column<int>(type: "integer", nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_parking_policies", x => x.id);
                    table.CheckConstraint("ck_parking_policies_valid", "max_vehicles_per_unit BETWEEN 1 AND 20 AND max_vehicles_per_person BETWEEN 1 AND 20 AND max_spaces_per_unit BETWEEN 0 AND 20 AND guest_max_hours BETWEEN 1 AND 168 AND retention_days BETWEEN 1 AND 365");
                    table.ForeignKey(
                        name: "FK_parking_policies_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_vehicles", x => x.id);
                    table.UniqueConstraint("AK_vehicles_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_vehicles_valid", "plate ~ '^[A-Z0-9]{2,12}$' AND type IN ('CAR','MOTORCYCLE','VAN','OTHER') AND status IN ('PENDING','APPROVED','REJECTED','PASSIVE') AND (status <> 'REJECTED' OR (reviewed_at IS NOT NULL AND review_reason IS NOT NULL)) AND (status <> 'APPROVED' OR reviewed_at IS NOT NULL) AND (status <> 'PASSIVE' OR deactivated_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_vehicles_people_property_id_person_id",
                        columns: x => new { x.property_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicles_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parking_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 50, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_parking_levels", x => x.id);
                    table.UniqueConstraint("AK_parking_levels_facility_id_id", x => new { x.facility_id, x.id });
                    table.CheckConstraint("ck_parking_levels_valid", "length(trim(name)) > 0 AND sort_order BETWEEN -10 AND 50");
                    table.ForeignKey(
                        name: "FK_parking_levels_parking_facilities_property_id_facility_id",
                        columns: x => new { x.property_id, x.facility_id },
                        principalTable: "parking_facilities",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parking_spaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "citext", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_parking_spaces", x => x.id);
                    table.UniqueConstraint("AK_parking_spaces_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_parking_spaces_valid", "length(trim(code)) > 0 AND type IN ('STANDARD','ACCESSIBLE','VISITOR','EV_CHARGING','MOTORCYCLE')");
                    table.ForeignKey(
                        name: "FK_parking_spaces_parking_facilities_property_id_facility_id",
                        columns: x => new { x.property_id, x.facility_id },
                        principalTable: "parking_facilities",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parking_spaces_parking_levels_facility_id_level_id",
                        columns: x => new { x.facility_id, x.level_id },
                        principalTable: "parking_levels",
                        principalColumns: new[] { "facility_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plate = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    guest_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    arrived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    departed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_guest_vehicles", x => x.id);
                    table.CheckConstraint("ck_guest_vehicles_valid", "plate ~ '^[A-Z0-9]{2,12}$' AND ends_at > starts_at AND status IN ('PLANNED','ARRIVED','DEPARTED','CANCELLED') AND (status <> 'ARRIVED' OR arrived_at IS NOT NULL) AND (status <> 'DEPARTED' OR (arrived_at IS NOT NULL AND departed_at >= arrived_at)) AND (status <> 'CANCELLED' OR cancelled_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_guest_vehicles_parking_spaces_property_id_space_id",
                        columns: x => new { x.property_id, x.space_id },
                        principalTable: "parking_spaces",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_guest_vehicles_units_property_id_host_unit_id",
                        columns: x => new { x.property_id, x.host_unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parking_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    space_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_parking_allocations", x => x.id);
                    table.CheckConstraint("ck_parking_allocations_dates", "end_date IS NULL OR end_date >= start_date");
                    table.ForeignKey(
                        name: "FK_parking_allocations_parking_spaces_property_id_space_id",
                        columns: x => new { x.property_id, x.space_id },
                        principalTable: "parking_spaces",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parking_allocations_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parking_allocations_vehicles_property_id_vehicle_id",
                        columns: x => new { x.property_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guest_vehicles_property_id_host_unit_id",
                table: "guest_vehicles",
                columns: new[] { "property_id", "host_unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_guest_vehicles_property_id_space_id",
                table: "guest_vehicles",
                columns: new[] { "property_id", "space_id" });

            migrationBuilder.CreateIndex(
                name: "IX_guest_vehicles_property_id_starts_at",
                table: "guest_vehicles",
                columns: new[] { "property_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "IX_guest_vehicles_space_id_starts_at",
                table: "guest_vehicles",
                columns: new[] { "space_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_allocations_property_id_space_id",
                table: "parking_allocations",
                columns: new[] { "property_id", "space_id" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_allocations_property_id_unit_id",
                table: "parking_allocations",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_allocations_property_id_vehicle_id",
                table: "parking_allocations",
                columns: new[] { "property_id", "vehicle_id" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_allocations_space_id",
                table: "parking_allocations",
                column: "space_id",
                unique: true,
                filter: "is_deleted = false AND end_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_parking_facilities_property_id_building_id",
                table: "parking_facilities",
                columns: new[] { "property_id", "building_id" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_facilities_property_id_name",
                table: "parking_facilities",
                columns: new[] { "property_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_parking_levels_facility_id_name",
                table: "parking_levels",
                columns: new[] { "facility_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_parking_levels_property_id_facility_id",
                table: "parking_levels",
                columns: new[] { "property_id", "facility_id" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_policies_property_id",
                table: "parking_policies",
                column: "property_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_parking_spaces_facility_id_code",
                table: "parking_spaces",
                columns: new[] { "facility_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_parking_spaces_facility_id_level_id",
                table: "parking_spaces",
                columns: new[] { "facility_id", "level_id" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_spaces_property_id_facility_id",
                table: "parking_spaces",
                columns: new[] { "property_id", "facility_id" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_property_id_person_id",
                table: "vehicles",
                columns: new[] { "property_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_property_id_plate",
                table: "vehicles",
                columns: new[] { "property_id", "plate" },
                unique: true,
                filter: "is_deleted = false AND status IN ('PENDING','APPROVED')");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_property_id_unit_id",
                table: "vehicles",
                columns: new[] { "property_id", "unit_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Vehicle, allocation and guest history is personal user data; Down refuses to drop it silently.
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM vehicles) OR EXISTS(SELECT 1 FROM parking_allocations) OR EXISTS(SELECT 1 FROM guest_vehicles) OR EXISTS(SELECT 1 FROM parking_spaces) OR EXISTS(SELECT 1 FROM parking_policies) THEN RAISE EXCEPTION 'Parking data cannot be discarded'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "guest_vehicles");

            migrationBuilder.DropTable(
                name: "parking_allocations");

            migrationBuilder.DropTable(
                name: "parking_policies");

            migrationBuilder.DropTable(
                name: "parking_spaces");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "parking_levels");

            migrationBuilder.DropTable(
                name: "parking_facilities");
        }
    }
}
