using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutomationNotificationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "automation_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: true),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: true),
                    send_hour = table.Column<int>(type: "integer", nullable: false),
                    quiet_start_hour = table.Column<int>(type: "integer", nullable: false),
                    quiet_end_hour = table.Column<int>(type: "integer", nullable: false),
                    offset_days = table.Column<int[]>(type: "integer[]", nullable: false),
                    is_in_app_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_sms_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    title_template = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body_template = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    rule_version = table.Column<int>(type: "integer", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_automation_definitions", x => x.id);
                    table.CheckConstraint("chk_automation_definitions_channels", "is_in_app_enabled OR is_sms_enabled OR is_email_enabled");
                    table.CheckConstraint("chk_automation_definitions_dates", "ends_on IS NULL OR starts_on IS NULL OR ends_on >= starts_on");
                    table.CheckConstraint("chk_automation_definitions_hours", "send_hour BETWEEN 0 AND 23 AND quiet_start_hour BETWEEN 0 AND 23 AND quiet_end_hour BETWEEN 0 AND 23");
                    table.ForeignKey(
                        name: "FK_automation_definitions_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "automation_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_version = table.Column<int>(type: "integer", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    trigger = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    evaluated = table.Column<int>(type: "integer", nullable: false),
                    dispatched = table.Column<int>(type: "integer", nullable: false),
                    skipped = table.Column<int>(type: "integer", nullable: false),
                    error_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_automation_executions", x => x.id);
                    table.ForeignKey(
                        name: "FK_automation_executions_automation_definitions_definition_id",
                        column: x => x.definition_id,
                        principalTable: "automation_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "automation_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_version = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_automation_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_automation_revisions_automation_definitions_definition_id",
                        column: x => x.definition_id,
                        principalTable: "automation_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_dispatches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_version = table.Column<int>(type: "integer", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    dedup_key = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    offset_days = table.Column<int>(type: "integer", nullable: true),
                    period = table.Column<DateOnly>(type: "date", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    recipient_masked = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    person_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sms_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_notification_dispatches", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_dispatches_automation_definitions_definition_id",
                        column: x => x.definition_id,
                        principalTable: "automation_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notification_dispatches_automation_executions_execution_id",
                        column: x => x.execution_id,
                        principalTable: "automation_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notification_dispatches_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_automation_definitions_status",
                table: "automation_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uidx_automation_definitions_property_id_type",
                table: "automation_definitions",
                columns: new[] { "property_id", "type" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "idx_automation_executions_property_id_started_at",
                table: "automation_executions",
                columns: new[] { "property_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "uidx_automation_executions_definition_id_business_date",
                table: "automation_executions",
                columns: new[] { "definition_id", "business_date" },
                unique: true,
                filter: "trigger = 'SCHEDULE' AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uidx_automation_revisions_definition_id_rule_version",
                table: "automation_revisions",
                columns: new[] { "definition_id", "rule_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_dispatches_definition_id",
                table: "notification_dispatches",
                column: "definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_dispatches_execution_id",
                table: "notification_dispatches",
                column: "execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_dispatches_property_id_unit_id",
                table: "notification_dispatches",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "idx_notification_dispatches_property_id_created_at",
                table: "notification_dispatches",
                columns: new[] { "property_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_notification_dispatches_property_id_payment_id",
                table: "notification_dispatches",
                columns: new[] { "property_id", "payment_id" },
                filter: "payment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_notification_dispatches_sms_message_id",
                table: "notification_dispatches",
                column: "sms_message_id",
                filter: "sms_message_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uidx_notification_dispatches_dedup_key",
                table: "notification_dispatches",
                column: "dedup_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM notification_dispatches) OR EXISTS (SELECT 1 FROM automation_executions) THEN RAISE EXCEPTION 'Automation history exists; remove it deliberately before rolling back.' USING ERRCODE = '23514'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "automation_revisions");

            migrationBuilder.DropTable(
                name: "notification_dispatches");

            migrationBuilder.DropTable(
                name: "automation_executions");

            migrationBuilder.DropTable(
                name: "automation_definitions");
        }
    }
}
