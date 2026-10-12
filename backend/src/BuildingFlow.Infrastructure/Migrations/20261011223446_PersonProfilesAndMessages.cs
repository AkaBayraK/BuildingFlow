using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersonProfilesAndMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "people",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "people",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "citizenship",
                table: "people",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "people",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "people",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identity_number",
                table: "people",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "linked_user_id",
                table: "people",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "person_kind",
                table: "people",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "person_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_person_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_person_messages_people_property_id_person_id",
                        columns: x => new { x.property_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_people_profile",
                table: "people",
                sql: "(person_kind IS NULL AND citizenship IS NULL AND country_code IS NULL AND identity_number IS NULL AND birth_date IS NULL AND gender IS NULL AND address IS NULL AND linked_user_id IS NULL) OR COALESCE((person_kind IN ('NATURAL','LEGAL') AND citizenship IN ('DOMESTIC','FOREIGN') AND country_code ~ '^[A-Z]{2}$' AND length(btrim(identity_number)) BETWEEN 1 AND 50 AND length(btrim(phone_number)) > 0 AND length(btrim(email)) > 0 AND ((citizenship = 'DOMESTIC' AND country_code = 'TR' AND identity_number ~ CASE WHEN person_kind = 'NATURAL' THEN '^[1-9][0-9]{10}$' ELSE '^[0-9]{10}$' END) OR (citizenship = 'FOREIGN' AND country_code <> 'TR' AND (person_kind = 'LEGAL' OR identity_number ~ '^[A-Za-z0-9-]{6,30}$'))) AND (person_kind = 'NATURAL' OR (birth_date IS NULL AND gender IS NULL)) AND (gender IS NULL OR gender IN ('FEMALE','MALE','OTHER','UNSPECIFIED')) AND (birth_date IS NULL OR birth_date >= DATE '1900-01-01')), FALSE)");

            migrationBuilder.CreateIndex(
                name: "IX_person_messages_property_id_person_id",
                table: "person_messages",
                columns: new[] { "property_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_person_messages_property_id_request_id",
                table: "person_messages",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_messages_recipient_user_id_created_at",
                table: "person_messages",
                columns: new[] { "recipient_user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM person_messages) OR EXISTS (SELECT 1 FROM people WHERE person_kind IS NOT NULL OR citizenship IS NOT NULL OR country_code IS NOT NULL OR identity_number IS NOT NULL OR birth_date IS NOT NULL OR gender IS NOT NULL OR address IS NOT NULL OR linked_user_id IS NOT NULL) THEN
                    RAISE EXCEPTION 'Person profile or message history prevents destructive downgrade';
                END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "person_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_people_profile",
                table: "people");

            migrationBuilder.DropColumn(
                name: "address",
                table: "people");

            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "people");

            migrationBuilder.DropColumn(
                name: "citizenship",
                table: "people");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "people");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "people");

            migrationBuilder.DropColumn(
                name: "identity_number",
                table: "people");

            migrationBuilder.DropColumn(
                name: "linked_user_id",
                table: "people");

            migrationBuilder.DropColumn(
                name: "person_kind",
                table: "people");
        }
    }
}
