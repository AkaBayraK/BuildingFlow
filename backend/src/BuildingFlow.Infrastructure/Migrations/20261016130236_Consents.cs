using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Consents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consent_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name_tr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_en = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    feature = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
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
                    table.PrimaryKey("PK_consent_types", x => x.id);
                    table.CheckConstraint("chk_consent_types_code", "code ~ '^[A-Z][A-Z0-9_]{1,39}$'");
                    table.CheckConstraint("chk_consent_types_feature", "feature IS NULL OR (is_system AND feature IN ('PROFILE_PHOTO','REQUEST_PHOTO'))");
                    table.CheckConstraint("chk_consent_types_kind", "kind IN ('INFORMATION','EXPLICIT_CONSENT')");
                    table.CheckConstraint("chk_consent_types_sort_order", "sort_order BETWEEN 0 AND 10000");
                });

            migrationBuilder.CreateTable(
                name: "consent_texts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    body_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    requires_reconsent = table.Column<bool>(type: "boolean", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_consent_texts", x => x.id);
                    table.UniqueConstraint("ak_consent_texts_id_type", x => new { x.id, x.consent_type_id });
                    table.CheckConstraint("chk_consent_texts_body", "length(btrim(body)) > 0 AND length(btrim(title)) > 0");
                    table.CheckConstraint("chk_consent_texts_hashes", "body_sha256 ~ '^[0-9a-f]{64}$' AND fingerprint ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("chk_consent_texts_version", "text_version > 0");
                    table.ForeignKey(
                        name: "FK_consent_texts_consent_types_consent_type_id",
                        column: x => x.consent_type_id,
                        principalTable: "consent_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approver_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    consent_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_text_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_version = table.Column<int>(type: "integer", nullable: false),
                    body_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    app_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evidence_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    revoke_operation_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_user_consents", x => x.id);
                    table.CheckConstraint("chk_user_consents_channel", "channel IN ('MOBILE','WEB')");
                    table.CheckConstraint("chk_user_consents_dates", "text_version > 0 AND (revoked_at IS NULL OR revoked_at >= approved_at)");
                    table.CheckConstraint("chk_user_consents_hashes", "body_sha256 ~ '^[0-9a-f]{64}$' AND fingerprint ~ '^[0-9a-f]{64}$' AND evidence_sha256 ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("chk_user_consents_revoke_reason", "revoke_reason IS NULL OR revoke_reason IN ('WITHDRAWN','SUPERSEDED')");
                    table.CheckConstraint("chk_user_consents_revoked", "(revoked_at IS NULL) = (revoke_reason IS NULL) AND (revoke_operation_id IS NULL OR revoked_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_user_consents_consent_types_consent_type_id",
                        column: x => x.consent_type_id,
                        principalTable: "consent_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_consents_text_type",
                        columns: x => new { x.consent_text_id, x.consent_type_id },
                        principalTable: "consent_texts",
                        principalColumns: new[] { "id", "consent_type_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uidx_consent_texts_type_operation",
                table: "consent_texts",
                columns: new[] { "consent_type_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_consent_texts_type_version",
                table: "consent_texts",
                columns: new[] { "consent_type_id", "text_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_consent_types_code",
                table: "consent_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_consent_types_feature",
                table: "consent_types",
                column: "feature",
                unique: true,
                filter: "feature IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_user_consents_type_approved_at",
                table: "user_consents",
                columns: new[] { "consent_type_id", "approved_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_consent_text_id_consent_type_id",
                table: "user_consents",
                columns: new[] { "consent_text_id", "consent_type_id" });

            migrationBuilder.CreateIndex(
                name: "uidx_user_consents_active",
                table: "user_consents",
                columns: new[] { "owner_user_id", "consent_type_id" },
                unique: true,
                filter: "revoked_at IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uidx_user_consents_operation",
                table: "user_consents",
                columns: new[] { "owner_user_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_user_consents_revoke_operation",
                table: "user_consents",
                columns: new[] { "owner_user_id", "revoke_operation_id" },
                unique: true,
                filter: "revoke_operation_id IS NOT NULL");

            // System types gate code paths (KVKK notice, photo uploads); their codes and features never change.
            migrationBuilder.Sql("""
                INSERT INTO consent_types (id, code, name_tr, name_en, kind, is_system, feature, sort_order, version, created_at, created_by, modified_at, modified_by, is_deleted, is_enabled) VALUES
                  ('6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4a01', 'KVKK_INFORMATION', 'KVKK aydınlatma metni', 'Privacy notice (KVKK)', 'INFORMATION', true, NULL, 10, 1, now(), 'system', now(), 'system', false, true),
                  ('6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4a02', 'PROFILE_PHOTO', 'Profil fotoğrafı açık rızası', 'Profile photo consent', 'EXPLICIT_CONSENT', true, 'PROFILE_PHOTO', 20, 1, now(), 'system', now(), 'system', false, true),
                  ('6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4a03', 'REQUEST_PHOTO', 'Talep fotoğrafı açık rızası', 'Request photo consent', 'EXPLICIT_CONSENT', true, 'REQUEST_PHOTO', 30, 1, now(), 'system', now(), 'system', false, true)
                ON CONFLICT (code) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM user_consents) OR EXISTS (SELECT 1 FROM consent_texts) THEN
                    RAISE EXCEPTION 'Consent texts or consent history exist; refusing a lossy downgrade.';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "user_consents");

            migrationBuilder.DropTable(
                name: "consent_texts");

            migrationBuilder.DropTable(
                name: "consent_types");
        }
    }
}
