using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QaCenterRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "qa_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    scenario_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retry_of_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempt = table.Column<int>(type: "integer", nullable: false),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    is_cancel_requested = table.Column<bool>(type: "boolean", nullable: false),
                    queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    result_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    passed_steps = table.Column<int>(type: "integer", nullable: false),
                    failed_steps = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_qa_runs", x => x.id);
                    table.CheckConstraint("ck_qa_runs_attempt", "attempt BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_qa_runs_status", "status IN ('QUEUED','RUNNING','PASSED','FAILED','CANCELLED','TIMED_OUT')");
                    table.CheckConstraint("ck_qa_runs_timeout", "timeout_seconds BETWEEN 1 AND 3600");
                    table.ForeignKey(
                        name: "fk_qa_runs_retry_of",
                        column: x => x.retry_of_id,
                        principalTable: "qa_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qa_run_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    step_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    expected = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    actual = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_qa_run_steps", x => x.id);
                    table.CheckConstraint("ck_qa_run_steps_duration", "duration_ms >= 0");
                    table.CheckConstraint("ck_qa_run_steps_status", "status IN ('PASSED','FAILED','SKIPPED')");
                    table.ForeignKey(
                        name: "fk_qa_run_steps_run",
                        column: x => x.run_id,
                        principalTable: "qa_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uidx_qa_run_steps_run_sequence",
                table: "qa_run_steps",
                columns: new[] { "run_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_qa_runs_retry_of_id",
                table: "qa_runs",
                column: "retry_of_id");

            migrationBuilder.CreateIndex(
                name: "idx_qa_runs_status_queued_at",
                table: "qa_runs",
                columns: new[] { "status", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "uidx_qa_runs_active_scenario",
                table: "qa_runs",
                column: "scenario_key",
                unique: true,
                filter: "status IN ('QUEUED','RUNNING') AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uidx_qa_runs_code",
                table: "qa_runs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_qa_runs_request_id",
                table: "qa_runs",
                column: "request_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM qa_runs) THEN
                        RAISE EXCEPTION 'QA run history exists; rollback would lose test evidence.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropTable(
                name: "qa_run_steps");

            migrationBuilder.DropTable(
                name: "qa_runs");
        }
    }
}
