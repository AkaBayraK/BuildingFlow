using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApartmentAndUnitDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            EnsureNullableColumn(migrationBuilder, "units", "bathroom_count", "integer");

            EnsureNullableColumn(migrationBuilder, "units", "floor_position", "character varying(20)");

            EnsureNullableColumn(migrationBuilder, "units", "has_balcony", "boolean");

            EnsureNullableColumn(migrationBuilder, "units", "heating_type", "character varying(30)");

            EnsureNullableColumn(migrationBuilder, "units", "is_mezzanine", "boolean");

            EnsureNullableColumn(migrationBuilder, "units", "kitchen_type", "character varying(10)");

            EnsureNullableColumn(migrationBuilder, "units", "net_square_meters", "numeric(10,2)");

            EnsureNullableColumn(migrationBuilder, "units", "room_layout", "character varying(20)");

            EnsureNullableColumn(migrationBuilder, "properties", "construction_year", "integer");

            EnsureNullableColumn(migrationBuilder, "properties", "has_elevator", "boolean");

            EnsureNullableColumn(migrationBuilder, "properties", "heating_type", "character varying(30)");

            EnsureNullableColumn(migrationBuilder, "properties", "neighborhood", "character varying(100)");

            EnsureNullableColumn(migrationBuilder, "properties", "parking_type", "character varying(20)");

            EnsureNullableColumn(migrationBuilder, "buildings", "construction_year", "integer");

            EnsureNullableColumn(migrationBuilder, "buildings", "has_elevator", "boolean");

            EnsureNullableColumn(migrationBuilder, "buildings", "heating_type", "character varying(30)");

            EnsureNullableColumn(migrationBuilder, "buildings", "parking_type", "character varying(20)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_units_bathroom_count",
                table: "units",
                sql: "bathroom_count IS NULL OR bathroom_count BETWEEN 0 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "ck_units_floor_position",
                table: "units",
                sql: "floor_position IS NULL OR (floor_position IN ('BELOW_ENTRANCE','BASEMENT') AND floor < 0) OR (floor_position IN ('GROUND','GARDEN','ENTRANCE','HIGH_ENTRANCE') AND floor = 0) OR (floor_position IN ('REGULAR','ROOF') AND floor >= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_units_heating_type",
                table: "units",
                sql: "heating_type IS NULL OR heating_type IN ('NONE','STOVE','GAS_STOVE','FLOOR_HEATER','CENTRAL','CENTRAL_METERED','COMBI','UNDERFLOOR','AIR_CONDITIONING','OTHER')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_units_kitchen_type",
                table: "units",
                sql: "kitchen_type IS NULL OR kitchen_type IN ('OPEN','CLOSED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_units_net_area",
                table: "units",
                sql: "net_square_meters IS NULL OR (net_square_meters > 0 AND net_square_meters <= gross_square_meters)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_units_room_layout",
                table: "units",
                sql: "room_layout IS NULL OR room_layout IN ('STUDIO','R1_1','R1_5_1','R2_0','R2_1','R2_5_1','R3_1','R3_5_1','R4_1','R4_5_1','R5_1','R5_2','R6_PLUS')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_properties_building_features",
                table: "properties",
                sql: "type = 'APARTMENT' OR (construction_year IS NULL AND heating_type IS NULL AND has_elevator IS NULL AND parking_type IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_properties_construction_year",
                table: "properties",
                sql: "construction_year IS NULL OR construction_year BETWEEN 1800 AND 9999");

            migrationBuilder.AddCheckConstraint(
                name: "ck_properties_heating_type",
                table: "properties",
                sql: "heating_type IS NULL OR heating_type IN ('NONE','STOVE','GAS_STOVE','FLOOR_HEATER','CENTRAL','CENTRAL_METERED','COMBI','UNDERFLOOR','AIR_CONDITIONING','OTHER')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_properties_parking_type",
                table: "properties",
                sql: "parking_type IS NULL OR parking_type IN ('OPEN','CLOSED','OPEN_AND_CLOSED','NONE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_buildings_construction_year",
                table: "buildings",
                sql: "construction_year IS NULL OR construction_year BETWEEN 1800 AND 9999");

            migrationBuilder.AddCheckConstraint(
                name: "ck_buildings_heating_type",
                table: "buildings",
                sql: "heating_type IS NULL OR heating_type IN ('NONE','STOVE','GAS_STOVE','FLOOR_HEATER','CENTRAL','CENTRAL_METERED','COMBI','UNDERFLOOR','AIR_CONDITIONING','OTHER')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_buildings_parking_type",
                table: "buildings",
                sql: "parking_type IS NULL OR parking_type IN ('OPEN','CLOSED','OPEN_AND_CLOSED','NONE')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM properties WHERE neighborhood IS NOT NULL OR construction_year IS NOT NULL OR heating_type IS NOT NULL OR has_elevator IS NOT NULL OR parking_type IS NOT NULL) OR EXISTS(SELECT 1 FROM buildings WHERE construction_year IS NOT NULL OR heating_type IS NOT NULL OR has_elevator IS NOT NULL OR parking_type IS NOT NULL) OR EXISTS(SELECT 1 FROM units WHERE net_square_meters IS NOT NULL OR room_layout IS NOT NULL OR floor_position IS NOT NULL OR is_mezzanine IS NOT NULL OR bathroom_count IS NOT NULL OR kitchen_type IS NOT NULL OR has_balcony IS NOT NULL OR heating_type IS NOT NULL) THEN RAISE EXCEPTION 'Apartment and unit details cannot be discarded'; END IF; END $$;");

            migrationBuilder.DropCheckConstraint(
                name: "ck_units_bathroom_count",
                table: "units");

            migrationBuilder.DropCheckConstraint(
                name: "ck_units_floor_position",
                table: "units");

            migrationBuilder.DropCheckConstraint(
                name: "ck_units_heating_type",
                table: "units");

            migrationBuilder.DropCheckConstraint(
                name: "ck_units_kitchen_type",
                table: "units");

            migrationBuilder.DropCheckConstraint(
                name: "ck_units_net_area",
                table: "units");

            migrationBuilder.DropCheckConstraint(
                name: "ck_units_room_layout",
                table: "units");

            migrationBuilder.DropCheckConstraint(
                name: "ck_properties_building_features",
                table: "properties");

            migrationBuilder.DropCheckConstraint(
                name: "ck_properties_construction_year",
                table: "properties");

            migrationBuilder.DropCheckConstraint(
                name: "ck_properties_heating_type",
                table: "properties");

            migrationBuilder.DropCheckConstraint(
                name: "ck_properties_parking_type",
                table: "properties");

            migrationBuilder.DropCheckConstraint(
                name: "ck_buildings_construction_year",
                table: "buildings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_buildings_heating_type",
                table: "buildings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_buildings_parking_type",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "bathroom_count",
                table: "units");

            migrationBuilder.DropColumn(
                name: "floor_position",
                table: "units");

            migrationBuilder.DropColumn(
                name: "has_balcony",
                table: "units");

            migrationBuilder.DropColumn(
                name: "heating_type",
                table: "units");

            migrationBuilder.DropColumn(
                name: "is_mezzanine",
                table: "units");

            migrationBuilder.DropColumn(
                name: "kitchen_type",
                table: "units");

            migrationBuilder.DropColumn(
                name: "net_square_meters",
                table: "units");

            migrationBuilder.DropColumn(
                name: "room_layout",
                table: "units");

            migrationBuilder.DropColumn(
                name: "construction_year",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "has_elevator",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "heating_type",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "neighborhood",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "parking_type",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "construction_year",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "has_elevator",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "heating_type",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "parking_type",
                table: "buildings");
        }

        // Columns may already exist when they were added by hand to a local database; align them instead of failing.
        private static void EnsureNullableColumn(MigrationBuilder migrationBuilder, string table, string column, string type) =>
            migrationBuilder.Sql($"ALTER TABLE {table} ADD COLUMN IF NOT EXISTS {column} {type}; ALTER TABLE {table} ALTER COLUMN {column} TYPE {type}, ALTER COLUMN {column} DROP DEFAULT, ALTER COLUMN {column} DROP NOT NULL;");
    }
}
