using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssistantResident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_consent_types_feature",
                table: "consent_types");

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
                    { new Guid("a1a55150-0001-4000-8000-000000000021"), "RESIDENT", "MY_BALANCE", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 210, "What is my total balance and overdue amount for this unit?", "Bu bölüm için toplam borcum ve geciken tutarım ne kadar?", "What do I owe?", "Borcum ne kadar?", "RESIDENT_BALANCE", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000022"), "RESIDENT", "MY_NEXT_DUE", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 220, "When is my next payment due and how much is it?", "Bir sonraki ödemem ne zaman ve ne kadar?", "My next due date", "Son ödeme tarihim", "RESIDENT_BALANCE", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000023"), "RESIDENT", "MY_PAYMENTS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 230, "What were my recent payments?", "Son ödemelerim nelerdi?", "My recent payments", "Son ödemelerim", "RESIDENT_BALANCE", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000024"), "RESIDENT", "MY_REQUESTS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 240, "What is the status of my requests?", "Taleplerimin durumu nedir?", "My requests", "Taleplerim", "RESIDENT_REQUESTS", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000025"), "RESIDENT", "MY_ANNOUNCEMENTS", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 250, "What do the latest announcements say?", "Son duyurularda ne var?", "Latest announcements", "Son duyurular", "RESIDENT_NOTICES", 1L },
                    { new Guid("a1a55150-0001-4000-8000-000000000026"), "RESIDENT", "MY_UNREAD", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, true, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", 260, "How many unread announcements do I have?", "Kaç okunmamış duyurum var?", "Unread announcements", "Okunmamış duyurular", "RESIDENT_NOTICES", 1L }
                });

            migrationBuilder.AddCheckConstraint(
                name: "chk_consent_types_feature",
                table: "consent_types",
                sql: "feature IS NULL OR (is_system AND feature IN ('PROFILE_PHOTO','REQUEST_PHOTO','AI_ASSISTANT'))");

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_prompts_category",
                table: "assistant_prompts",
                sql: "category IN ('OVERVIEW','FINANCE','OPERATIONS','REQUESTS','COMMUNICATION','RESIDENT')");

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_prompts_tool_key",
                table: "assistant_prompts",
                sql: "tool_key IN ('DAILY_SUMMARY','DUES_COLLECTION','BUDGET_VARIANCE','UPCOMING_MAINTENANCE','OPEN_REQUESTS','REQUEST_BACKLOG','SUPPLIER_WATCH','SURVEY_SUMMARY','RESIDENT_BALANCE','RESIDENT_REQUESTS','RESIDENT_NOTICES')");

            migrationBuilder.AddCheckConstraint(
                name: "chk_assistant_audit_entries_request_kind",
                table: "assistant_audit_entries",
                sql: "request_kind IN ('PROMPT','FREE_TEXT','DRAFT','RESIDENT_PROMPT','RESIDENT_FREE')");

            // FAZ 3 gate: the AI assistant consent type exists but stays disabled and has no text until the KVKK review is done.
            migrationBuilder.Sql("""
                INSERT INTO consent_types (id, code, name_tr, name_en, kind, is_system, feature, sort_order, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled) VALUES
                  ('6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4a04', 'AI_ASSISTANT', 'Yapay zekâ asistanı açık rızası', 'AI assistant consent', 'EXPLICIT_CONSENT', true, 'AI_ASSISTANT', 40, 1, now(), 'system', now(), 'system', false, false)
                ON CONFLICT (code) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM assistant_audit_entries WHERE request_kind IN ('RESIDENT_PROMPT','RESIDENT_FREE'))
                       OR EXISTS (SELECT 1 FROM assistant_prompts WHERE (tool_key LIKE 'RESIDENT_%' OR category = 'RESIDENT') AND modified_by <> 'migration')
                       OR EXISTS (SELECT 1 FROM consent_types t WHERE t.feature = 'AI_ASSISTANT' AND (t.is_enabled OR EXISTS (SELECT 1 FROM consent_texts x WHERE x.consent_type_id = t.id))) THEN
                        RAISE EXCEPTION 'Resident assistant use, prompts or an enabled AI_ASSISTANT consent type exist; rollback would lose them';
                    END IF;
                    DELETE FROM consent_types WHERE feature = 'AI_ASSISTANT';
                END $$;
                """);
            migrationBuilder.DropCheckConstraint(
                name: "chk_consent_types_feature",
                table: "consent_types");

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
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000021"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000022"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000023"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000024"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000025"));

            migrationBuilder.DeleteData(
                table: "assistant_prompts",
                keyColumn: "id",
                keyValue: new Guid("a1a55150-0001-4000-8000-000000000026"));

            migrationBuilder.AddCheckConstraint(
                name: "chk_consent_types_feature",
                table: "consent_types",
                sql: "feature IS NULL OR (is_system AND feature IN ('PROFILE_PHOTO','REQUEST_PHOTO'))");

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
    }
}
