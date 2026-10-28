using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QaCenterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "qa_data_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    environment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    cleaned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    record_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_qa_data_batches", x => x.id);
                    table.CheckConstraint("ck_qa_data_batches_demo_kept", "kind = 'TEST' OR status = 'ACTIVE'");
                    table.CheckConstraint("ck_qa_data_batches_kind", "kind IN ('DEMO','TEST')");
                    table.CheckConstraint("ck_qa_data_batches_status", "status IN ('ACTIVE','CLEANED')");
                });

            migrationBuilder.CreateTable(
                name: "qa_data_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cleanup_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_qa_data_records", x => x.id);
                    table.CheckConstraint("ck_qa_data_records_order", "cleanup_order BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "fk_qa_data_records_batch",
                        column: x => x.batch_id,
                        principalTable: "qa_data_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uidx_qa_data_batches_code",
                table: "qa_data_batches",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_qa_data_batches_request_id",
                table: "qa_data_batches",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_qa_data_records_entity",
                table: "qa_data_records",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "uidx_qa_data_records_batch_entity",
                table: "qa_data_records",
                columns: new[] { "batch_id", "entity_type", "entity_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM qa_data_batches) THEN
                        RAISE EXCEPTION 'QA data batches exist; rollback would lose the cleanup registry.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropTable(
                name: "qa_data_records");

            migrationBuilder.DropTable(
                name: "qa_data_batches");
        }
    }
}
