using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DuesPartyHistoryAndCancellationReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_unit_occupancies_property_id_unit_id",
                table: "unit_occupancies");

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason_code",
                table: "dues_charges",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancellation_reason_id",
                table: "dues_charges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason_name",
                table: "dues_charges",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_occupancy_id",
                table: "dues_charges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "parties_recorded",
                table: "dues_charges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "debtor_type",
                table: "dues_assessments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "UNIT");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_unit_occupancies_property_id_unit_id_id",
                table: "unit_occupancies",
                columns: new[] { "property_id", "unit_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_unit_occupancies_property_id_unit_id_person_id_id",
                table: "unit_occupancies",
                columns: new[] { "property_id", "unit_id", "person_id", "id" });

            migrationBuilder.CreateTable(
                name: "dues_cancellation_reasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requires_explanation = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_dues_cancellation_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dues_charge_parties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occupancy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_name = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_debtor = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_dues_charge_parties", x => x.id);
                    table.CheckConstraint("ck_charge_parties_history", "is_deleted = false AND is_enabled = true");
                    table.CheckConstraint("ck_charge_parties_values", "length(trim(person_name)) > 0 AND type IN ('OWNER','TENANT','RESIDENT') AND (end_date IS NULL OR end_date >= start_date) AND (is_debtor = false OR type IN ('OWNER','TENANT'))");
                    table.ForeignKey(
                        name: "FK_dues_charge_parties_dues_charges_property_id_unit_id_charge~",
                        columns: x => new { x.property_id, x.unit_id, x.charge_id },
                        principalTable: "dues_charges",
                        principalColumns: new[] { "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dues_charge_parties_people_property_id_person_id",
                        columns: x => new { x.property_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dues_charge_parties_unit_occupancies_property_id_unit_id_pe~",
                        columns: x => new { x.property_id, x.unit_id, x.person_id, x.occupancy_id },
                        principalTable: "unit_occupancies",
                        principalColumns: new[] { "property_id", "unit_id", "person_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "dues_cancellation_reasons",
                columns: new[] { "id", "code", "created_at", "created_by", "is_deleted", "is_enabled", "modified_at", "modified_by", "name", "requires_explanation", "version" },
                values: new object[,]
                {
                    { new Guid("9c74d000-6001-4000-8000-000000000001"), "DUPLICATE_ASSESSMENT", new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", "Mükerrer tahakkuk", false, 1L },
                    { new Guid("9c74d000-6001-4000-8000-000000000002"), "WRONG_AMOUNT", new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", "Yanlış tutar", false, 1L },
                    { new Guid("9c74d000-6001-4000-8000-000000000003"), "WRONG_PERIOD", new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", "Yanlış dönem", false, 1L },
                    { new Guid("9c74d000-6001-4000-8000-000000000004"), "TENANT_MOVED_OUT", new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", "Kiracı taşınması", false, 1L },
                    { new Guid("9c74d000-6001-4000-8000-000000000005"), "OTHER", new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", "Diğer", true, 1L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_dues_charges_cancellation_reason_id",
                table: "dues_charges",
                column: "cancellation_reason_id");

            migrationBuilder.CreateIndex(
                name: "IX_dues_charges_property_id_unit_id_cancelled_by_occupancy_id",
                table: "dues_charges",
                columns: new[] { "property_id", "unit_id", "cancelled_by_occupancy_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_charges_reason_metadata",
                table: "dues_charges",
                sql: "(cancellation_reason_id IS NULL AND cancellation_reason_code IS NULL AND cancellation_reason_name IS NULL AND cancelled_by_occupancy_id IS NULL) OR (is_cancelled = true AND cancellation_reason_id IS NOT NULL AND cancellation_reason_code IS NOT NULL AND cancellation_reason_name IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_assessments_debtor",
                table: "dues_assessments",
                sql: "debtor_type IN ('UNIT','TENANT','OWNER')");

            migrationBuilder.CreateIndex(
                name: "IX_dues_cancellation_reasons_code",
                table: "dues_cancellation_reasons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dues_charge_parties_charge_id",
                table: "dues_charge_parties",
                column: "charge_id",
                unique: true,
                filter: "is_debtor = true");

            migrationBuilder.CreateIndex(
                name: "IX_dues_charge_parties_charge_id_occupancy_id",
                table: "dues_charge_parties",
                columns: new[] { "charge_id", "occupancy_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dues_charge_parties_property_id_person_id",
                table: "dues_charge_parties",
                columns: new[] { "property_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dues_charge_parties_property_id_unit_id_charge_id",
                table: "dues_charge_parties",
                columns: new[] { "property_id", "unit_id", "charge_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dues_charge_parties_property_id_unit_id_person_id_occupancy~",
                table: "dues_charge_parties",
                columns: new[] { "property_id", "unit_id", "person_id", "occupancy_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_dues_charges_dues_cancellation_reasons_cancellation_reason_~",
                table: "dues_charges",
                column: "cancellation_reason_id",
                principalTable: "dues_cancellation_reasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dues_charges_unit_occupancies_property_id_unit_id_cancelled~",
                table: "dues_charges",
                columns: new[] { "property_id", "unit_id", "cancelled_by_occupancy_id" },
                principalTable: "unit_occupancies",
                principalColumns: new[] { "property_id", "unit_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Downgrade cannot discard recorded party/reason history. Empty seed-only installs are safe.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM dues_charge_parties)
                       OR EXISTS (SELECT 1 FROM dues_charges WHERE parties_recorded OR cancellation_reason_id IS NOT NULL)
                       OR EXISTS (SELECT 1 FROM dues_assessments WHERE debtor_type <> 'UNIT') THEN
                        RAISE EXCEPTION 'Dues party history exists; downgrade would lose financial history.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_dues_charges_dues_cancellation_reasons_cancellation_reason_~",
                table: "dues_charges");

            migrationBuilder.DropForeignKey(
                name: "FK_dues_charges_unit_occupancies_property_id_unit_id_cancelled~",
                table: "dues_charges");

            migrationBuilder.DropTable(
                name: "dues_cancellation_reasons");

            migrationBuilder.DropTable(
                name: "dues_charge_parties");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_unit_occupancies_property_id_unit_id_id",
                table: "unit_occupancies");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_unit_occupancies_property_id_unit_id_person_id_id",
                table: "unit_occupancies");

            migrationBuilder.DropIndex(
                name: "IX_dues_charges_cancellation_reason_id",
                table: "dues_charges");

            migrationBuilder.DropIndex(
                name: "IX_dues_charges_property_id_unit_id_cancelled_by_occupancy_id",
                table: "dues_charges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_charges_reason_metadata",
                table: "dues_charges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_assessments_debtor",
                table: "dues_assessments");

            migrationBuilder.DropColumn(
                name: "cancellation_reason_code",
                table: "dues_charges");

            migrationBuilder.DropColumn(
                name: "cancellation_reason_id",
                table: "dues_charges");

            migrationBuilder.DropColumn(
                name: "cancellation_reason_name",
                table: "dues_charges");

            migrationBuilder.DropColumn(
                name: "cancelled_by_occupancy_id",
                table: "dues_charges");

            migrationBuilder.DropColumn(
                name: "parties_recorded",
                table: "dues_charges");

            migrationBuilder.DropColumn(
                name: "debtor_type",
                table: "dues_assessments");

            migrationBuilder.CreateIndex(
                name: "IX_unit_occupancies_property_id_unit_id",
                table: "unit_occupancies",
                columns: new[] { "property_id", "unit_id" });
        }
    }
}
