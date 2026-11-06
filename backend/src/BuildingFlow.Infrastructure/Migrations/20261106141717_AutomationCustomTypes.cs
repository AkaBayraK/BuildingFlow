using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutomationCustomTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uidx_automation_definitions_property_id_type",
                table: "automation_definitions");

            migrationBuilder.AlterColumn<Guid>(
                name: "person_id",
                table: "person_messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "unit_id",
                table: "notification_dispatches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "source_code",
                table: "notification_dispatches",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                table: "notification_dispatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "custom_trigger",
                table: "automation_definitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "custom_type_id",
                table: "automation_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "automation_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date_field = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    default_offsets = table.Column<int[]>(type: "integer[]", nullable: false),
                    day_of_month = table.Column<int>(type: "integer", nullable: true),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    recipient_scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    default_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    default_body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type_version = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_automation_types", x => x.id);
                    table.CheckConstraint("chk_automation_types_calendar", "(day_of_month IS NULL OR day_of_month BETWEEN 1 AND 28) AND (day_of_week IS NULL OR day_of_week BETWEEN 1 AND 7)");
                    table.CheckConstraint("chk_automation_types_trigger", "(trigger_kind = 'DATE_OFFSET') = (date_field IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_automation_types_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "automation_type_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type_version = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    snapshot = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
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
                    table.PrimaryKey("PK_automation_type_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_automation_type_revisions_automation_types_type_id",
                        column: x => x.type_id,
                        principalTable: "automation_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_automation_definitions_custom_type_id",
                table: "automation_definitions",
                column: "custom_type_id");

            migrationBuilder.CreateIndex(
                name: "uidx_automation_definitions_property_id_custom_type_id",
                table: "automation_definitions",
                columns: new[] { "property_id", "custom_type_id" },
                unique: true,
                filter: "is_deleted = false AND custom_type_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uidx_automation_definitions_property_id_type",
                table: "automation_definitions",
                columns: new[] { "property_id", "type" },
                unique: true,
                filter: "is_deleted = false AND type <> 'CUSTOM'");

            migrationBuilder.AddCheckConstraint(
                name: "chk_automation_definitions_custom",
                table: "automation_definitions",
                sql: "(type = 'CUSTOM') = (custom_type_id IS NOT NULL AND custom_trigger IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "uidx_automation_type_revisions_type_id_type_version",
                table: "automation_type_revisions",
                columns: new[] { "type_id", "type_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_automation_types_property_id_status",
                table: "automation_types",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uidx_automation_types_code",
                table: "automation_types",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "FK_automation_definitions_automation_types_custom_type_id",
                table: "automation_definitions",
                column: "custom_type_id",
                principalTable: "automation_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM automation_types) OR EXISTS (SELECT 1 FROM person_messages WHERE person_id IS NULL) OR EXISTS (SELECT 1 FROM notification_dispatches WHERE unit_id IS NULL) THEN RAISE EXCEPTION 'Custom automation history exists; remove it deliberately before rolling back.' USING ERRCODE = '23514'; END IF; END $$;");
            migrationBuilder.DropForeignKey(
                name: "FK_automation_definitions_automation_types_custom_type_id",
                table: "automation_definitions");

            migrationBuilder.DropTable(
                name: "automation_type_revisions");

            migrationBuilder.DropTable(
                name: "automation_types");

            migrationBuilder.DropIndex(
                name: "IX_automation_definitions_custom_type_id",
                table: "automation_definitions");

            migrationBuilder.DropIndex(
                name: "uidx_automation_definitions_property_id_custom_type_id",
                table: "automation_definitions");

            migrationBuilder.DropIndex(
                name: "uidx_automation_definitions_property_id_type",
                table: "automation_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "chk_automation_definitions_custom",
                table: "automation_definitions");

            migrationBuilder.DropColumn(
                name: "source_code",
                table: "notification_dispatches");

            migrationBuilder.DropColumn(
                name: "source_id",
                table: "notification_dispatches");

            migrationBuilder.DropColumn(
                name: "custom_trigger",
                table: "automation_definitions");

            migrationBuilder.DropColumn(
                name: "custom_type_id",
                table: "automation_definitions");

            migrationBuilder.AlterColumn<Guid>(
                name: "person_id",
                table: "person_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "unit_id",
                table: "notification_dispatches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "uidx_automation_definitions_property_id_type",
                table: "automation_definitions",
                columns: new[] { "property_id", "type" },
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
