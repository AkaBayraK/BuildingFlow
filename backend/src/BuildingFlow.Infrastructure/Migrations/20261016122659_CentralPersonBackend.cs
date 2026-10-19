using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CentralPersonBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "person_tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_person_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person_user_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_person_user_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    citizenship = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    identity_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
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
                    table.PrimaryKey("PK_persons", x => x.id);
                    table.UniqueConstraint("AK_persons_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_central_person_profile", "length(btrim(first_name)) BETWEEN 1 AND 100 AND (kind='LEGAL' OR length(btrim(last_name)) BETWEEN 1 AND 100) AND kind IN ('NATURAL','LEGAL') AND citizenship IN ('DOMESTIC','FOREIGN') AND country_code ~ '^[A-Z]{2}$' AND length(btrim(identity_number)) BETWEEN 1 AND 50 AND ((citizenship='DOMESTIC' AND country_code='TR' AND identity_number ~ CASE WHEN kind='NATURAL' THEN '^[1-9][0-9]{10}$' ELSE '^[0-9]{10}$' END) OR (citizenship='FOREIGN' AND country_code<>'TR' AND (kind='LEGAL' OR identity_number ~ '^[A-Za-z0-9-]{6,30}$'))) AND (kind='NATURAL' OR (birth_date IS NULL AND gender IS NULL)) AND (gender IS NULL OR gender IN ('FEMALE','MALE','OTHER','UNSPECIFIED')) AND (birth_date IS NULL OR birth_date>=DATE '1900-01-01')");
                    table.ForeignKey(
                        name: "FK_persons_person_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "person_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_tenant_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_person_tenant_memberships", x => x.id);
                    table.UniqueConstraint("AK_person_tenant_memberships_tenant_id_user_account_id", x => new { x.tenant_id, x.user_account_id });
                    table.ForeignKey(
                        name: "FK_person_tenant_memberships_person_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "person_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_tenant_memberships_person_user_accounts_user_account~",
                        column: x => x.user_account_id,
                        principalTable: "person_user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_operation_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_version = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_person_operation_receipts", x => x.id);
                    table.CheckConstraint("ck_person_receipt_version", "applied_version>0");
                    table.ForeignKey(
                        name: "FK_person_operation_receipts_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_account_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_person_account_links", x => x.id);
                    table.CheckConstraint("ck_person_link_dates", "ended_at IS NULL OR ended_at>=linked_at");
                    table.ForeignKey(
                        name: "FK_person_account_links_person_tenant_memberships_tenant_id_us~",
                        columns: x => new { x.tenant_id, x.user_account_id },
                        principalTable: "person_tenant_memberships",
                        principalColumns: new[] { "tenant_id", "user_account_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_account_links_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_person_account_links_tenant_id_person_id",
                table: "person_account_links",
                columns: new[] { "tenant_id", "person_id" },
                unique: true,
                filter: "is_deleted=false AND is_enabled=true AND ended_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_person_account_links_tenant_id_user_account_id",
                table: "person_account_links",
                columns: new[] { "tenant_id", "user_account_id" },
                unique: true,
                filter: "is_deleted=false AND is_enabled=true AND ended_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_person_operation_receipts_tenant_id_actor_issuer_actor_subj~",
                table: "person_operation_receipts",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_operation_receipts_tenant_id_person_id",
                table: "person_operation_receipts",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_person_tenant_memberships_user_account_id",
                table: "person_tenant_memberships",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_person_user_accounts_issuer_subject",
                table: "person_user_accounts",
                columns: new[] { "issuer", "subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persons_tenant_id_last_name_first_name",
                table: "persons",
                columns: new[] { "tenant_id", "last_name", "first_name" });
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_central_person_receipt_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Person operation receipts are immutable'; END $$;
                CREATE TRIGGER central_person_receipt_immutable BEFORE UPDATE OR DELETE ON person_operation_receipts
                FOR EACH ROW EXECUTE FUNCTION reject_central_person_receipt_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS(SELECT 1 FROM persons) OR EXISTS(SELECT 1 FROM person_tenants) OR
                     EXISTS(SELECT 1 FROM person_user_accounts) OR EXISTS(SELECT 1 FROM person_tenant_memberships) OR
                     EXISTS(SELECT 1 FROM person_account_links) OR EXISTS(SELECT 1 FROM person_operation_receipts)
                  THEN RAISE EXCEPTION 'Central person history prevents rollback'; END IF;
                END $$;
                DROP TRIGGER central_person_receipt_immutable ON person_operation_receipts;
                DROP FUNCTION reject_central_person_receipt_mutation();
                """);
            migrationBuilder.DropTable(
                name: "person_account_links");

            migrationBuilder.DropTable(
                name: "person_operation_receipts");

            migrationBuilder.DropTable(
                name: "person_tenant_memberships");

            migrationBuilder.DropTable(
                name: "persons");

            migrationBuilder.DropTable(
                name: "person_user_accounts");

            migrationBuilder.DropTable(
                name: "person_tenants");
        }
    }
}
