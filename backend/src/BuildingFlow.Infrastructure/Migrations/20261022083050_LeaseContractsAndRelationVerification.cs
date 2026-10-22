using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LeaseContractsAndRelationVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "evidence_document_id",
                table: "unit_tenancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "lease_contract_id",
                table: "unit_tenancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "unit_tenancies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "LEGACY");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "unit_tenancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by_account_id",
                table: "unit_tenancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "evidence_document_id",
                table: "unit_residencies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "unit_residencies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "LEGACY");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "unit_residencies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by_account_id",
                table: "unit_residencies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "evidence_document_id",
                table: "unit_ownerships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "unit_ownerships",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "LEGACY");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "unit_ownerships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by_account_id",
                table: "unit_ownerships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_stored_documents_property_id_id",
                table: "stored_documents",
                columns: new[] { "property_id", "id" });

            migrationBuilder.CreateTable(
                name: "lease_contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    contract_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    verification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verified_by_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    evidence_document_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lease_contracts", x => x.id);
                    table.UniqueConstraint("AK_lease_contracts_tenant_id_property_id_unit_id_id", x => new { x.tenant_id, x.property_id, x.unit_id, x.id });
                    table.CheckConstraint("ck_lease_contracts_dates", "start_date > DATE '0001-01-01' AND (end_date IS NULL OR end_date>=start_date)");
                    table.CheckConstraint("ck_lease_contracts_reference", "contract_reference IS NULL OR length(trim(contract_reference)) > 0");
                    table.CheckConstraint("ck_lease_contracts_verification", "(verification_status IN ('LEGACY','PENDING') AND verified_by_account_id IS NULL AND verified_at IS NULL) OR (verification_status IN ('VERIFIED','REJECTED') AND verified_by_account_id IS NOT NULL AND verified_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_lease_contracts_person_property_scopes_tenant_id_property_id",
                        columns: x => new { x.tenant_id, x.property_id },
                        principalTable: "person_property_scopes",
                        principalColumns: new[] { "tenant_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lease_contracts_person_user_accounts_verified_by_account_id",
                        column: x => x.verified_by_account_id,
                        principalTable: "person_user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lease_contracts_stored_documents_property_id_evidence_docum~",
                        columns: x => new { x.property_id, x.evidence_document_id },
                        principalTable: "stored_documents",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lease_contracts_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_unit_tenancies_property_id_evidence_document_id",
                table: "unit_tenancies",
                columns: new[] { "property_id", "evidence_document_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_tenancies_tenant_id_property_id_unit_id_lease_contract~",
                table: "unit_tenancies",
                columns: new[] { "tenant_id", "property_id", "unit_id", "lease_contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_tenancies_verified_by_account_id",
                table: "unit_tenancies",
                column: "verified_by_account_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_unit_tenancies_verification",
                table: "unit_tenancies",
                sql: "(verification_status IN ('LEGACY','PENDING') AND verified_by_account_id IS NULL AND verified_at IS NULL) OR (verification_status IN ('VERIFIED','REJECTED') AND verified_by_account_id IS NOT NULL AND verified_at IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_unit_residencies_property_id_evidence_document_id",
                table: "unit_residencies",
                columns: new[] { "property_id", "evidence_document_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_residencies_verified_by_account_id",
                table: "unit_residencies",
                column: "verified_by_account_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_unit_residencies_verification",
                table: "unit_residencies",
                sql: "(verification_status IN ('LEGACY','PENDING') AND verified_by_account_id IS NULL AND verified_at IS NULL) OR (verification_status IN ('VERIFIED','REJECTED') AND verified_by_account_id IS NOT NULL AND verified_at IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_unit_ownerships_property_id_evidence_document_id",
                table: "unit_ownerships",
                columns: new[] { "property_id", "evidence_document_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_ownerships_verified_by_account_id",
                table: "unit_ownerships",
                column: "verified_by_account_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_unit_ownerships_verification",
                table: "unit_ownerships",
                sql: "(verification_status IN ('LEGACY','PENDING') AND verified_by_account_id IS NULL AND verified_at IS NULL) OR (verification_status IN ('VERIFIED','REJECTED') AND verified_by_account_id IS NOT NULL AND verified_at IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_property_id_evidence_document_id",
                table: "lease_contracts",
                columns: new[] { "property_id", "evidence_document_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_property_id_unit_id",
                table: "lease_contracts",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_tenant_id_property_id_unit_id_start_date",
                table: "lease_contracts",
                columns: new[] { "tenant_id", "property_id", "unit_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_verified_by_account_id",
                table: "lease_contracts",
                column: "verified_by_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_unit_ownerships_person_user_accounts_verified_by_account_id",
                table: "unit_ownerships",
                column: "verified_by_account_id",
                principalTable: "person_user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unit_ownerships_stored_documents_property_id_evidence_docum~",
                table: "unit_ownerships",
                columns: new[] { "property_id", "evidence_document_id" },
                principalTable: "stored_documents",
                principalColumns: new[] { "property_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unit_residencies_person_user_accounts_verified_by_account_id",
                table: "unit_residencies",
                column: "verified_by_account_id",
                principalTable: "person_user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unit_residencies_stored_documents_property_id_evidence_docu~",
                table: "unit_residencies",
                columns: new[] { "property_id", "evidence_document_id" },
                principalTable: "stored_documents",
                principalColumns: new[] { "property_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unit_tenancies_lease_contracts_tenant_id_property_id_unit_i~",
                table: "unit_tenancies",
                columns: new[] { "tenant_id", "property_id", "unit_id", "lease_contract_id" },
                principalTable: "lease_contracts",
                principalColumns: new[] { "tenant_id", "property_id", "unit_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unit_tenancies_person_user_accounts_verified_by_account_id",
                table: "unit_tenancies",
                column: "verified_by_account_id",
                principalTable: "person_user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_unit_tenancies_stored_documents_property_id_evidence_docume~",
                table: "unit_tenancies",
                columns: new[] { "property_id", "evidence_document_id" },
                principalTable: "stored_documents",
                principalColumns: new[] { "property_id", "id" },
                onDelete: ReferentialAction.Restrict);
            // Existing rows become LEGACY via the column default: no verifier or verification time is invented.
            migrationBuilder.Sql("""
CREATE FUNCTION bf_verification_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.verification_status IN ('VERIFIED','REJECTED') AND (NEW.verification_status,NEW.verified_by_account_id,NEW.verified_at,NEW.evidence_document_id)
        IS DISTINCT FROM (OLD.verification_status,OLD.verified_by_account_id,OLD.verified_at,OLD.evidence_document_id)
        OR NEW.verification_status IN ('LEGACY','PENDING') AND NEW.verification_status IS DISTINCT FROM OLD.verification_status THEN
        RAISE EXCEPTION 'Verification decision is immutable' USING ERRCODE='23514';
    END IF;
    IF TG_TABLE_NAME='unit_tenancies' THEN
        IF OLD.lease_contract_id IS NOT NULL AND NEW.lease_contract_id IS DISTINCT FROM OLD.lease_contract_id THEN
            RAISE EXCEPTION 'Lease party is immutable' USING ERRCODE='23514';
        END IF;
    ELSIF TG_TABLE_NAME='lease_contracts' THEN
        IF (NEW.tenant_id,NEW.property_id,NEW.unit_id,NEW.start_date,NEW.contract_reference) IS DISTINCT FROM (OLD.tenant_id,OLD.property_id,OLD.unit_id,OLD.start_date,OLD.contract_reference)
            OR (OLD.end_date IS NOT NULL AND NEW.end_date IS DISTINCT FROM OLD.end_date) THEN
            RAISE EXCEPTION 'Lease history is immutable' USING ERRCODE='23514';
        END IF;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_ownership_verification_guard BEFORE UPDATE ON unit_ownerships FOR EACH ROW EXECUTE FUNCTION bf_verification_guard();
CREATE TRIGGER trg_tenancy_verification_guard BEFORE UPDATE ON unit_tenancies FOR EACH ROW EXECUTE FUNCTION bf_verification_guard();
CREATE TRIGGER trg_residency_verification_guard BEFORE UPDATE ON unit_residencies FOR EACH ROW EXECUTE FUNCTION bf_verification_guard();
CREATE TRIGGER trg_lease_verification_guard BEFORE UPDATE ON lease_contracts FOR EACH ROW EXECUTE FUNCTION bf_verification_guard();
CREATE FUNCTION bf_lease_party_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE contract record;
BEGIN
    IF TG_TABLE_NAME='unit_tenancies' THEN
        IF NEW.lease_contract_id IS NULL OR NEW.is_deleted THEN RETURN NEW; END IF;
        SELECT start_date,end_date,is_deleted INTO contract FROM lease_contracts WHERE id=NEW.lease_contract_id FOR SHARE;
        IF contract.is_deleted OR NEW.start_date<contract.start_date OR (contract.end_date IS NOT NULL AND (NEW.end_date IS NULL OR NEW.end_date>contract.end_date)) THEN
            RAISE EXCEPTION 'Lease party is outside the contract' USING ERRCODE='23514';
        END IF;
    ELSIF EXISTS (SELECT 1 FROM unit_tenancies t WHERE t.lease_contract_id=NEW.id AND NOT t.is_deleted
        AND (NEW.is_deleted OR t.start_date<NEW.start_date OR (NEW.end_date IS NOT NULL AND (t.end_date IS NULL OR t.end_date>NEW.end_date)))) THEN
        RAISE EXCEPTION 'Lease party is outside the contract' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE CONSTRAINT TRIGGER trg_lease_party_guard AFTER INSERT OR UPDATE ON unit_tenancies DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bf_lease_party_guard();
CREATE CONSTRAINT TRIGGER trg_lease_contract_guard AFTER UPDATE ON lease_contracts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bf_lease_party_guard();
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$ BEGIN
IF EXISTS(SELECT 1 FROM lease_contracts) OR EXISTS(SELECT 1 FROM unit_tenancies WHERE lease_contract_id IS NOT NULL)
 OR EXISTS(SELECT 1 FROM unit_ownerships WHERE verification_status<>'LEGACY' OR evidence_document_id IS NOT NULL)
 OR EXISTS(SELECT 1 FROM unit_tenancies WHERE verification_status<>'LEGACY' OR evidence_document_id IS NOT NULL)
 OR EXISTS(SELECT 1 FROM unit_residencies WHERE verification_status<>'LEGACY' OR evidence_document_id IS NOT NULL)
THEN RAISE EXCEPTION 'Lease or verification rollback would lose history'; END IF;
END $$;
DROP TRIGGER trg_lease_contract_guard ON lease_contracts;
DROP TRIGGER trg_lease_party_guard ON unit_tenancies;
DROP FUNCTION bf_lease_party_guard();
DROP TRIGGER trg_lease_verification_guard ON lease_contracts;
DROP TRIGGER trg_residency_verification_guard ON unit_residencies;
DROP TRIGGER trg_tenancy_verification_guard ON unit_tenancies;
DROP TRIGGER trg_ownership_verification_guard ON unit_ownerships;
DROP FUNCTION bf_verification_guard();
""");
            migrationBuilder.DropForeignKey(
                name: "FK_unit_ownerships_person_user_accounts_verified_by_account_id",
                table: "unit_ownerships");

            migrationBuilder.DropForeignKey(
                name: "FK_unit_ownerships_stored_documents_property_id_evidence_docum~",
                table: "unit_ownerships");

            migrationBuilder.DropForeignKey(
                name: "FK_unit_residencies_person_user_accounts_verified_by_account_id",
                table: "unit_residencies");

            migrationBuilder.DropForeignKey(
                name: "FK_unit_residencies_stored_documents_property_id_evidence_docu~",
                table: "unit_residencies");

            migrationBuilder.DropForeignKey(
                name: "FK_unit_tenancies_lease_contracts_tenant_id_property_id_unit_i~",
                table: "unit_tenancies");

            migrationBuilder.DropForeignKey(
                name: "FK_unit_tenancies_person_user_accounts_verified_by_account_id",
                table: "unit_tenancies");

            migrationBuilder.DropForeignKey(
                name: "FK_unit_tenancies_stored_documents_property_id_evidence_docume~",
                table: "unit_tenancies");

            migrationBuilder.DropTable(
                name: "lease_contracts");

            migrationBuilder.DropIndex(
                name: "IX_unit_tenancies_property_id_evidence_document_id",
                table: "unit_tenancies");

            migrationBuilder.DropIndex(
                name: "IX_unit_tenancies_tenant_id_property_id_unit_id_lease_contract~",
                table: "unit_tenancies");

            migrationBuilder.DropIndex(
                name: "IX_unit_tenancies_verified_by_account_id",
                table: "unit_tenancies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_unit_tenancies_verification",
                table: "unit_tenancies");

            migrationBuilder.DropIndex(
                name: "IX_unit_residencies_property_id_evidence_document_id",
                table: "unit_residencies");

            migrationBuilder.DropIndex(
                name: "IX_unit_residencies_verified_by_account_id",
                table: "unit_residencies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_unit_residencies_verification",
                table: "unit_residencies");

            migrationBuilder.DropIndex(
                name: "IX_unit_ownerships_property_id_evidence_document_id",
                table: "unit_ownerships");

            migrationBuilder.DropIndex(
                name: "IX_unit_ownerships_verified_by_account_id",
                table: "unit_ownerships");

            migrationBuilder.DropCheckConstraint(
                name: "ck_unit_ownerships_verification",
                table: "unit_ownerships");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_stored_documents_property_id_id",
                table: "stored_documents");

            migrationBuilder.DropColumn(
                name: "evidence_document_id",
                table: "unit_tenancies");

            migrationBuilder.DropColumn(
                name: "lease_contract_id",
                table: "unit_tenancies");

            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "unit_tenancies");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "unit_tenancies");

            migrationBuilder.DropColumn(
                name: "verified_by_account_id",
                table: "unit_tenancies");

            migrationBuilder.DropColumn(
                name: "evidence_document_id",
                table: "unit_residencies");

            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "unit_residencies");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "unit_residencies");

            migrationBuilder.DropColumn(
                name: "verified_by_account_id",
                table: "unit_residencies");

            migrationBuilder.DropColumn(
                name: "evidence_document_id",
                table: "unit_ownerships");

            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "unit_ownerships");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "unit_ownerships");

            migrationBuilder.DropColumn(
                name: "verified_by_account_id",
                table: "unit_ownerships");
        }
    }
}
