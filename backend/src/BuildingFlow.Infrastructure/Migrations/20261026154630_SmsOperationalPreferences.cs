using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SmsOperationalPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sms_operational_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recipient = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_operational_enabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_sms_operational_preferences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_sms_operational_preferences_recipient",
                table: "sms_operational_preferences",
                column: "recipient",
                filter: "is_deleted = false AND NOT is_operational_enabled");

            migrationBuilder.CreateIndex(
                name: "uidx_sms_operational_preferences_owner_user_id",
                table: "sms_operational_preferences",
                column: "owner_user_id",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM sms_operational_preferences) THEN RAISE EXCEPTION 'SMS preferences must be preserved.'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "sms_operational_preferences");
        }
    }
}
