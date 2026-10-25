using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountPhoneOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "phone_otp_challenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code_mac = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    protected_payload = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resend_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_phone_otp_challenges", x => x.id);
                    table.CheckConstraint("ck_phone_otp_attempts", "attempts BETWEEN 0 AND 5");
                    table.CheckConstraint("ck_phone_otp_lifetime", "expires_at > created_at");
                    table.CheckConstraint("ck_phone_otp_status", "status IN ('RESERVED','ACCEPTED','UNKNOWN','FAILED','VERIFIED','CANCELLED','EXPIRED','LOCKED')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_phone_otp_challenges_ip_hash_created_at",
                table: "phone_otp_challenges",
                columns: new[] { "ip_hash", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_phone_otp_challenges_owner_user_id_created_at",
                table: "phone_otp_challenges",
                columns: new[] { "owner_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_phone_otp_challenges_target_hash_created_at",
                table: "phone_otp_challenges",
                columns: new[] { "target_hash", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM phone_otp_challenges) THEN RAISE EXCEPTION 'OTP audit history must be preserved.'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "phone_otp_challenges");
        }
    }
}
