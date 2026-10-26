using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SmsCommercialConsents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sms_legal_senders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mersis_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    contact_information = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    opt_out_keyword = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    opt_out_short_code = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    is_opt_out_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_iys_integration_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    iys_brand_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_sms_legal_senders", x => x.id);
                    table.CheckConstraint("chk_sms_legal_senders_iys", "NOT is_iys_integration_enabled OR iys_brand_code IS NOT NULL");
                    table.CheckConstraint("chk_sms_legal_senders_mersis", "mersis_number IS NULL OR mersis_number ~ '^[0-9]{16}$'");
                    table.ForeignKey(
                        name: "FK_sms_legal_senders_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_consent_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    recipient = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_recipient_verified = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    text_version = table.Column<int>(type: "integer", nullable: true),
                    text_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    iys_status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    iys_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    iys_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    iys_error_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_commercial_consent_records", x => x.id);
                    table.CheckConstraint("chk_commercial_consent_records_approval", "status <> 'APPROVED' OR (is_recipient_verified AND text_version > 0 AND text_sha256 IS NOT NULL AND source NOT IN ('SMS_REPLY', 'PROVIDER_CALLBACK'))");
                    table.CheckConstraint("chk_commercial_consent_records_iys", "(iys_status = 'SYNCED') = (iys_reference IS NOT NULL AND iys_synced_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_commercial_consent_records_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_consent_records_sms_legal_senders_sender_id",
                        column: x => x.sender_id,
                        principalTable: "sms_legal_senders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_consent_records_person_id",
                table: "commercial_consent_records",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "idx_commercial_consent_records_iys_status",
                table: "commercial_consent_records",
                column: "iys_status",
                filter: "iys_status IN ('PENDING', 'FAILED')");

            migrationBuilder.CreateIndex(
                name: "idx_commercial_consent_records_sender_id_recipient",
                table: "commercial_consent_records",
                columns: new[] { "sender_id", "channel", "recipient", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "uidx_commercial_consent_records_operation_id",
                table: "commercial_consent_records",
                column: "operation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uidx_sms_legal_senders_platform",
                table: "sms_legal_senders",
                column: "is_deleted",
                unique: true,
                filter: "is_deleted = false AND property_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uidx_sms_legal_senders_property_id",
                table: "sms_legal_senders",
                column: "property_id",
                unique: true,
                filter: "is_deleted = false AND property_id IS NOT NULL");

            migrationBuilder.Sql("""
                CREATE FUNCTION guard_commercial_consent_record() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
                  IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'Commercial consent history is append-only'; END IF;
                  IF (NEW.sender_id, NEW.property_id, NEW.channel, NEW.recipient, NEW.is_recipient_verified, NEW.status, NEW.source, NEW.occurred_at,
                      NEW.owner_user_id, NEW.person_id, NEW.text_version, NEW.text_sha256, NEW.operation_id, NEW.evidence_sha256, NEW.is_deleted, NEW.created_at, NEW.created_by)
                     IS DISTINCT FROM (OLD.sender_id, OLD.property_id, OLD.channel, OLD.recipient, OLD.is_recipient_verified, OLD.status, OLD.source, OLD.occurred_at,
                      OLD.owner_user_id, OLD.person_id, OLD.text_version, OLD.text_sha256, OLD.operation_id, OLD.evidence_sha256, OLD.is_deleted, OLD.created_at, OLD.created_by)
                  THEN RAISE EXCEPTION 'Commercial consent decision is immutable'; END IF;
                  RETURN NEW; END $$;
                CREATE TRIGGER commercial_consent_record_immutable BEFORE UPDATE OR DELETE ON commercial_consent_records FOR EACH ROW EXECUTE FUNCTION guard_commercial_consent_record();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM commercial_consent_records) OR EXISTS (SELECT 1 FROM sms_legal_senders) THEN RAISE EXCEPTION 'Commercial consent history must be preserved.'; END IF; END $$;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS commercial_consent_record_immutable ON commercial_consent_records; DROP FUNCTION IF EXISTS guard_commercial_consent_record();");
            migrationBuilder.DropTable(
                name: "commercial_consent_records");

            migrationBuilder.DropTable(
                name: "sms_legal_senders");
        }
    }
}
