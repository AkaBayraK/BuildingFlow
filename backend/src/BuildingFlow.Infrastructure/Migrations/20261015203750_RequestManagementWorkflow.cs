using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RequestManagementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_management_history",
                table: "mobile_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "management_response",
                table: "mobile_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "management_status",
                table: "mobile_requests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "RECORDED");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_mobile_requests_property_id_id",
                table: "mobile_requests",
                columns: new[] { "property_id", "id" });

            migrationBuilder.CreateTable(
                name: "request_management_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    to_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    response = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    actor_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expected_version = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_request_management_events", x => x.id);
                    table.CheckConstraint("ck_request_event_response", "length(trim(response)) BETWEEN 1 AND 2000 AND expected_version > 0");
                    table.CheckConstraint("ck_request_event_transition", "(from_status='RECORDED' AND to_status IN ('IN_PROGRESS','REJECTED')) OR (from_status='IN_PROGRESS' AND to_status IN ('RESOLVED','REJECTED')) OR (from_status IN ('RESOLVED','REJECTED') AND to_status='RECORDED')");
                    table.ForeignKey(
                        name: "FK_request_management_events_mobile_requests_property_id_reque~",
                        columns: x => new { x.property_id, x.request_id },
                        principalTable: "mobile_requests",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mobile_requests_property_id_management_status_modified_at",
                table: "mobile_requests",
                columns: new[] { "property_id", "management_status", "modified_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_request_management_status",
                table: "mobile_requests",
                sql: "management_status IN ('RECORDED','IN_PROGRESS','RESOLVED','REJECTED')");

            migrationBuilder.CreateIndex(
                name: "IX_request_management_events_actor_user_id_operation_id",
                table: "request_management_events",
                columns: new[] { "actor_user_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_management_events_property_id_request_id_created_at",
                table: "request_management_events",
                columns: new[] { "property_id", "request_id", "created_at" });
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_request_history_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Request management history is immutable'; END $$;
                CREATE TRIGGER request_history_immutable BEFORE UPDATE OR DELETE ON request_management_events
                FOR EACH ROW EXECUTE FUNCTION reject_request_history_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM request_management_events) OR EXISTS
                     (SELECT 1 FROM mobile_requests WHERE has_management_history OR management_status <> 'RECORDED' OR management_response IS NOT NULL)
                  THEN RAISE EXCEPTION 'Request management history prevents rollback'; END IF;
                END $$;
                DROP TRIGGER request_history_immutable ON request_management_events;
                DROP FUNCTION reject_request_history_mutation();
                """);
            migrationBuilder.DropTable(
                name: "request_management_events");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_mobile_requests_property_id_id",
                table: "mobile_requests");

            migrationBuilder.DropIndex(
                name: "IX_mobile_requests_property_id_management_status_modified_at",
                table: "mobile_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_request_management_status",
                table: "mobile_requests");

            migrationBuilder.DropColumn(
                name: "has_management_history",
                table: "mobile_requests");

            migrationBuilder.DropColumn(
                name: "management_response",
                table: "mobile_requests");

            migrationBuilder.DropColumn(
                name: "management_status",
                table: "mobile_requests");
        }
    }
}
