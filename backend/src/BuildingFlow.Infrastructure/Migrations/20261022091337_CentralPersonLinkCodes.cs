using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CentralPersonLinkCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "person_link_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hint = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    issued_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    used_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_person_link_codes", x => x.id);
                    table.CheckConstraint("ck_person_link_codes_state", "length(code_hash)=64 AND length(hint)=2 AND NOT (used_at IS NOT NULL AND revoked_at IS NOT NULL) AND (used_at IS NULL) = (used_by_account_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_person_link_codes_person_user_accounts_issued_by_account_id",
                        column: x => x.issued_by_account_id,
                        principalTable: "person_user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_link_codes_person_user_accounts_used_by_account_id",
                        column: x => x.used_by_account_id,
                        principalTable: "person_user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_link_codes_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_person_link_codes_code_hash",
                table: "person_link_codes",
                column: "code_hash",
                unique: true,
                filter: "is_deleted=false AND used_at IS NULL AND revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_person_link_codes_issued_by_account_id",
                table: "person_link_codes",
                column: "issued_by_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_person_link_codes_tenant_id_person_id",
                table: "person_link_codes",
                columns: new[] { "tenant_id", "person_id" },
                unique: true,
                filter: "is_deleted=false AND used_at IS NULL AND revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_person_link_codes_used_by_account_id",
                table: "person_link_codes",
                column: "used_by_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Issued and used codes are the audit trail of account links; refuse to drop them.
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM person_link_codes) THEN RAISE EXCEPTION 'Cannot remove populated person link code history'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "person_link_codes");
        }
    }
}
