using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssistantFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assistant_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    request_kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    prompt_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    tool_key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_assistant_audit_entries", x => x.id);
                    table.CheckConstraint("chk_assistant_audit_entries_duration", "duration_ms IS NULL OR duration_ms >= 0");
                    table.CheckConstraint("chk_assistant_audit_entries_outcome", "outcome IN ('STARTED','ANSWERED','UNSUPPORTED','FAILED')");
                    table.CheckConstraint("chk_assistant_audit_entries_request_kind", "request_kind IN ('PROMPT','FREE_TEXT')");
                    table.ForeignKey(
                        name: "fk_assistant_audit_entries_properties",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assistant_prompts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    tool_key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title_tr = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    title_en = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    text_tr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    text_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_assistant_prompts", x => x.id);
                    table.CheckConstraint("chk_assistant_prompts_category", "category IN ('OVERVIEW','FINANCE','OPERATIONS','REQUESTS')");
                    table.CheckConstraint("chk_assistant_prompts_sort_order", "sort_order BETWEEN 0 AND 999");
                    table.CheckConstraint("chk_assistant_prompts_tool_key", "tool_key IN ('DAILY_SUMMARY','DUES_COLLECTION','BUDGET_VARIANCE','UPCOMING_MAINTENANCE','OPEN_REQUESTS')");
                });

            migrationBuilder.InsertData(
                table: "assistant_prompts",
                columns: new[] { "id", "category", "code", "created_at", "created_by", "is_deleted", "is_enabled", "modified_at", "modified_by", "sort_order", "text_en", "text_tr", "title_en", "title_tr", "tool_key", "version" },
                values: new object[,]
                {
                    { new Guid("a1a55150-0001-4000-8000-000000000001"), "OVERVIEW", "TODAY_FOCUS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 10, "What should I check today? Give me the daily summary of this area.", "Bugün neye bakmalıyım? Yönetim alanının günlük özetini ver.", "What should I check today?", "Bugün neye bakmalıyım?", "DAILY_SUMMARY", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000002"), "OVERVIEW", "AREA_STATUS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 20, "How is this management area doing overall?", "Yönetim alanının genel durumu nasıl?", "Overall status", "Genel durum", "DAILY_SUMMARY", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000003"), "OVERVIEW", "OCCUPANCY", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 30, "How many units are occupied and how many are vacant?", "Kaç bölüm dolu, kaç bölüm boş?", "Occupancy", "Doluluk", "DAILY_SUMMARY", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000004"), "FINANCE", "OVERDUE_DUES", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 40, "How much dues are overdue and across how many charges?", "Geciken aidat toplamı ne kadar, kaç borç kaydı gecikmede?", "Overdue dues", "Geciken aidat", "DUES_COLLECTION", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000005"), "FINANCE", "MONTH_COLLECTION", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 50, "How is this month's dues collection going?", "Bu ayın aidat tahsilatı ne durumda?", "This month's collection", "Bu ayın tahsilatı", "DUES_COLLECTION", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000006"), "FINANCE", "YEAR_COLLECTION", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 60, "What is this year's dues collection rate?", "Bu yıl aidat tahsilat oranı nedir?", "Yearly collection", "Yıllık tahsilat", "DUES_COLLECTION", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000007"), "FINANCE", "BUDGET_OVERRUN", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 70, "Which expense categories are over budget?", "Bütçeye göre hangi gider kalemi aşıldı?", "Budget overrun", "Bütçe aşımı", "BUDGET_VARIANCE", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000008"), "FINANCE", "MONTH_BALANCE", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 80, "How do income and expenses compare this month?", "Bu ay gelir ve gider dengesi nasıl?", "Income and expenses", "Gelir-gider dengesi", "BUDGET_VARIANCE", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000009"), "OPERATIONS", "UPCOMING_MAINTENANCE", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 90, "How many maintenance plans are due within 30 days, and when is the first?", "30 gün içinde kaç planlı bakım var, ilki ne zaman?", "Upcoming maintenance", "Yaklaşan bakımlar", "UPCOMING_MAINTENANCE", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000010"), "OPERATIONS", "MAINTENANCE_TYPES", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 100, "What types of maintenance are coming up?", "Yaklaşan bakımlar hangi türlerde?", "Maintenance types", "Bakım türleri", "UPCOMING_MAINTENANCE", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000011"), "REQUESTS", "OPEN_REQUESTS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 110, "How many requests are open?", "Kaç açık talep var?", "Open requests", "Açık talepler", "OPEN_REQUESTS", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000012"), "REQUESTS", "STALE_REQUESTS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 120, "How long have the oldest open requests been waiting?", "En uzun süredir açık talepler ne kadar bekliyor?", "Waiting requests", "Bekleyen talepler", "OPEN_REQUESTS", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000013"), "REQUESTS", "REQUEST_CATEGORIES", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 130, "Which categories do open requests concentrate in?", "Açık talepler hangi kategorilerde yoğunlaşıyor?", "Request categories", "Talep kategorileri", "OPEN_REQUESTS", 1L }
                });

            migrationBuilder.CreateIndex(
                name: "idx_assistant_audit_entries_property_id",
                table: "assistant_audit_entries",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "idx_assistant_audit_entries_user_id_business_day",
                table: "assistant_audit_entries",
                columns: new[] { "user_id", "business_day" });

            migrationBuilder.CreateIndex(
                name: "uidx_assistant_prompts_code",
                table: "assistant_prompts",
                column: "code",
                unique: true,
                filter: "NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM assistant_audit_entries) OR EXISTS(SELECT 1 FROM assistant_prompts WHERE modified_by <> 'migration') THEN RAISE EXCEPTION 'Assistant audit or administrator-edited prompts exist; rollback would lose them'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "assistant_audit_entries");

            migrationBuilder.DropTable(
                name: "assistant_prompts");
        }
    }
}
