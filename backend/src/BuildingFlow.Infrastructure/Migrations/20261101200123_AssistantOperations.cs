using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssistantOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_assistant_prompts_category",
                table: "assistant_prompts");

            migrationBuilder.DropCheckConstraint(
                name: "chk_assistant_prompts_tool_key",
                table: "assistant_prompts");

            migrationBuilder.DropCheckConstraint(
                name: "chk_assistant_audit_entries_request_kind",
                table: "assistant_audit_entries");

            migrationBuilder.InsertData(
                table: "assistant_prompts",
                columns: new[] { "id", "category", "code", "created_at", "created_by", "is_deleted", "is_enabled", "modified_at", "modified_by", "sort_order", "text_en", "text_tr", "title_en", "title_tr", "tool_key", "version" },
                values: new object[,]
                {
                    { new Guid("a1a55150-0001-4000-8000-000000000014"), "REQUESTS", "OLDEST_REQUESTS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 140, "Which requests have been open the longest?", "En uzun süredir açık olan talepler hangileri?", "Longest waiting", "En uzun bekleyenler", "REQUEST_BACKLOG", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000015"), "REQUESTS", "REQUEST_STATUS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 150, "How are requests distributed by status?", "Talepler durumlarına göre nasıl dağılıyor?", "Request statuses", "Talep durumları", "REQUEST_BACKLOG", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000016"), "OPERATIONS", "OVERDUE_WORK_ORDERS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 160, "Which supplier work orders are overdue?", "Hangi tedarikçi iş emirleri gecikiyor?", "Overdue work orders", "Geciken iş emirleri", "SUPPLIER_WATCH", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000017"), "OPERATIONS", "EXPIRING_CONTRACTS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 170, "Which supplier contracts are about to expire?", "Süresi dolmak üzere olan tedarikçi sözleşmeleri hangileri?", "Expiring contracts", "Biten sözleşmeler", "SUPPLIER_WATCH", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000018"), "FINANCE", "PENDING_INVOICES", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 180, "What supplier invoices are waiting for approval or payment?", "Ödeme veya onay bekleyen tedarikçi faturaları ne durumda?", "Pending invoices", "Bekleyen faturalar", "SUPPLIER_WATCH", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000019"), "COMMUNICATION", "SURVEY_RESULTS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 190, "Summarise the latest survey's results.", "Son anketin sonuçlarını özetle.", "Survey results", "Anket sonuçları", "SURVEY_SUMMARY", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000020"), "COMMUNICATION", "SURVEY_PARTICIPATION", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 200, "What was the participation rate of the latest survey?", "Son ankete katılım oranı nedir?", "Survey participation", "Anket katılımı", "SURVEY_SUMMARY", 1L }
                });

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_prompts_category",
                table: "assistant_prompts",
                sql: "category IN ('OVERVIEW','FINANCE','OPERATIONS','REQUESTS','COMMUNICATION')");

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_prompts_tool_key",
                table: "assistant_prompts",
                sql: "tool_key IN ('DAILY_SUMMARY','DUES_COLLECTION','BUDGET_VARIANCE','UPCOMING_MAINTENANCE','OPEN_REQUESTS','REQUEST_BACKLOG','SUPPLIER_WATCH','SURVEY_SUMMARY')");

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_audit_entries_request_kind",
                table: "assistant_audit_entries",
                sql: "request_kind IN ('PROMPT','FREE_TEXT','DRAFT')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM assistant_audit_entries WHERE request_kind = 'DRAFT') OR EXISTS(SELECT 1 FROM assistant_prompts WHERE (tool_key IN ('REQUEST_BACKLOG','SUPPLIER_WATCH','SURVEY_SUMMARY') OR category = 'COMMUNICATION') AND modified_by <> 'migration') THEN RAISE EXCEPTION 'Assistant drafts or administrator prompts for FAZ 2 tools exist; rollback would violate the narrower constraints'; END IF; END $$;");
            migrationBuilder.DropCheckConstraint(
                name: "chk_assistant_prompts_category",
                table: "assistant_prompts");

            migrationBuilder.DropCheckConstraint(
                name: "chk_assistant_prompts_tool_key",
                table: "assistant_prompts");

            migrationBuilder.DropCheckConstraint(
                name: "chk_assistant_audit_entries_request_kind",
                table: "assistant_audit_entries");

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000015"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000016"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000017"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000018"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000019"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000020"));

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_prompts_category",
                table: "assistant_prompts",
                sql: "category IN ('OVERVIEW','FINANCE','OPERATIONS','REQUESTS')");

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_prompts_tool_key",
                table: "assistant_prompts",
                sql: "tool_key IN ('DAILY_SUMMARY','DUES_COLLECTION','BUDGET_VARIANCE','UPCOMING_MAINTENANCE','OPEN_REQUESTS')");

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_audit_entries_request_kind",
                table: "assistant_audit_entries",
                sql: "request_kind IN ('PROMPT','FREE_TEXT')");
        }
    }
}
