using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountPhoneVerificationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_phone_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    verified_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    verified_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pending_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    pending_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
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
                    table.PrimaryKey("PK_account_phone_verifications", x => x.id);
                    table.CheckConstraint("ck_account_phone_pending", "(pending_phone IS NULL) = (pending_key IS NULL)");
                    table.CheckConstraint("ck_account_phone_proof", "(verified_phone IS NULL) = (verified_key IS NULL) AND (verified_key IS NULL) = (verified_at IS NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_phone_verifications_owner_user_id",
                table: "account_phone_verifications",
                column: "owner_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM account_phone_verifications) THEN
                    RAISE EXCEPTION 'Account contact history must be preserved.';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "account_phone_verifications");
        }
    }
}
