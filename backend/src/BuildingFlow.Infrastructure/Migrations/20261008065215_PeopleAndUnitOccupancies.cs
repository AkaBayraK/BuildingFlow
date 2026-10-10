using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PeopleAndUnitOccupancies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_units_property_id_id",
                table: "units",
                columns: new[] { "property_id", "id" });

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
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
                    table.PrimaryKey("PK_people", x => x.id);
                    table.UniqueConstraint("AK_people_property_id_id", x => new { x.property_id, x.id });
                    table.ForeignKey(
                        name: "FK_people_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_occupancies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary_contact = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_unit_occupancies", x => x.id);
                    table.CheckConstraint("ck_occupancies_dates", "end_date IS NULL OR end_date >= start_date");
                    table.CheckConstraint("ck_occupancies_type", "type IN ('OWNER','TENANT','RESIDENT')");
                    table.ForeignKey(
                        name: "FK_unit_occupancies_people_property_id_person_id",
                        columns: x => new { x.property_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_occupancies_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_people_property_id_last_name_first_name",
                table: "people",
                columns: new[] { "property_id", "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_occupancies_person_id_unit_id_type",
                table: "unit_occupancies",
                columns: new[] { "person_id", "unit_id", "type" },
                unique: true,
                filter: "is_deleted = false AND end_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_unit_occupancies_property_id_person_id",
                table: "unit_occupancies",
                columns: new[] { "property_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_occupancies_property_id_start_date",
                table: "unit_occupancies",
                columns: new[] { "property_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_occupancies_property_id_unit_id",
                table: "unit_occupancies",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_occupancies_unit_id",
                table: "unit_occupancies",
                column: "unit_id",
                unique: true,
                filter: "is_deleted = false AND is_primary_contact = true AND end_date IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM people) OR EXISTS (SELECT 1 FROM unit_occupancies) THEN
                    RAISE EXCEPTION 'People/occupancy history exists; refusing a lossy downgrade.';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "unit_occupancies");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_units_property_id_id",
                table: "units");
        }
    }
}
