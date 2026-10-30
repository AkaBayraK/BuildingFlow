using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierDocumentsContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "previous_document_id",
                table: "supplier_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "revision_number",
                table: "supplier_documents",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "is_supplier_only",
                table: "stored_documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM supplier_documents d JOIN stored_documents f ON f.id=d.document_id AND f.property_id=d.property_id
                               WHERE f.audience <> 'MANAGEMENT' OR f.announcement_id IS NOT NULL) THEN
                        RAISE EXCEPTION 'Existing supplier documents require an explicit privacy migration';
                    END IF;
                END $$;
                UPDATE stored_documents f SET is_supplier_only=true
                WHERE EXISTS(SELECT 1 FROM supplier_documents d WHERE d.document_id=f.id AND d.property_id=f.property_id);
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_supplier_documents_tenant_id_supplier_id_property_id_id",
                table: "supplier_documents",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_supplier_area_services_tenant_id_supplier_id_property_id_id",
                table: "supplier_area_services",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "id" });

            migrationBuilder.CreateTable(
                name: "supplier_contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    previous_contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    reminder_days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_supplier_contracts", x => x.id);
                    table.UniqueConstraint("AK_supplier_contracts_tenant_id_supplier_id_property_id_id", x => new { x.tenant_id, x.supplier_id, x.property_id, x.id });
                    table.CheckConstraint("ck_supplier_contract_valid", "length(trim(title)) > 0 AND start_date >= DATE '2000-01-01' AND end_date <= DATE '2100-12-31' AND end_date >= start_date AND amount > 0 AND amount <= 99999999.99 AND currency = 'TRY' AND reminder_days BETWEEN 0 AND 365 AND status IN ('DRAFT','ACTIVE','CANCELLED') AND (status <> 'CANCELLED' OR cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0)");
                    table.ForeignKey(
                        name: "FK_supplier_contracts_supplier_area_services_tenant_id_supplie~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.area_service_id },
                        principalTable: "supplier_area_services",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_contracts_supplier_contracts_tenant_id_supplier_id~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.previous_contract_id },
                        principalTable: "supplier_contracts",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_contracts_supplier_documents_tenant_id_supplier_id~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.document_id },
                        principalTable: "supplier_documents",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_contract_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_supplier_contract_revisions", x => x.id);
                    table.CheckConstraint("ck_supplier_contract_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','EDIT','ACTIVATE','CANCEL')");
                    table.ForeignKey(
                        name: "FK_supplier_contract_revisions_supplier_contracts_tenant_id_su~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.contract_id },
                        principalTable: "supplier_contracts",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_documents_previous_document_id",
                table: "supplier_documents",
                column: "previous_document_id",
                unique: true,
                filter: "previous_document_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_documents_tenant_id_supplier_id_property_id_previo~",
                table: "supplier_documents",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "previous_document_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_document_revision",
                table: "supplier_documents",
                sql: "(previous_document_id IS NULL AND revision_number = 1) OR (previous_document_id IS NOT NULL AND revision_number > 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documents_supplier_private",
                table: "stored_documents",
                sql: "NOT is_supplier_only OR (audience = 'MANAGEMENT' AND announcement_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contract_revisions_contract_id_applied_version",
                table: "supplier_contract_revisions",
                columns: new[] { "contract_id", "applied_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contract_revisions_tenant_id_actor_issuer_actor_su~",
                table: "supplier_contract_revisions",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contract_revisions_tenant_id_supplier_id_property_~",
                table: "supplier_contract_revisions",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contracts_previous_contract_id",
                table: "supplier_contracts",
                column: "previous_contract_id",
                unique: true,
                filter: "previous_contract_id IS NOT NULL AND NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contracts_tenant_id_supplier_id_property_id_area_s~",
                table: "supplier_contracts",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "area_service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contracts_tenant_id_supplier_id_property_id_docume~",
                table: "supplier_contracts",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "document_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contracts_tenant_id_supplier_id_property_id_end_da~",
                table: "supplier_contracts",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contracts_tenant_id_supplier_id_property_id_previo~",
                table: "supplier_contracts",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "previous_contract_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_documents_supplier_documents_tenant_id_supplier_id~",
                table: "supplier_documents",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "previous_document_id" },
                principalTable: "supplier_documents",
                principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_supplier_contract_revision_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Supplier contract revisions are immutable' USING ERRCODE='23514'; END $$;
                CREATE TRIGGER supplier_contract_revision_immutable BEFORE UPDATE OR DELETE ON supplier_contract_revisions
                FOR EACH ROW EXECUTE FUNCTION reject_supplier_contract_revision_mutation();
                CREATE FUNCTION guard_supplier_contract_history() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW.is_deleted OR NOT NEW.is_enabled OR
                       (NEW.tenant_id,NEW.supplier_id,NEW.property_id,NEW.area_service_id,NEW.previous_contract_id)
                       IS DISTINCT FROM (OLD.tenant_id,OLD.supplier_id,OLD.property_id,OLD.area_service_id,OLD.previous_contract_id) OR
                       (OLD.status <> 'DRAFT' AND (NEW.title,NEW.start_date,NEW.end_date,NEW.amount,NEW.currency,NEW.reminder_days,NEW.document_id)
                        IS DISTINCT FROM (OLD.title,OLD.start_date,OLD.end_date,OLD.amount,OLD.currency,OLD.reminder_days,OLD.document_id)) OR
                       (OLD.status='CANCELLED' AND (NEW.status,NEW.cancellation_reason) IS DISTINCT FROM (OLD.status,OLD.cancellation_reason)) OR
                       (OLD.status='ACTIVE' AND NEW.status NOT IN ('ACTIVE','CANCELLED')) THEN
                        RAISE EXCEPTION 'Supplier contract history is immutable' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_contract_history_guard BEFORE UPDATE ON supplier_contracts
                FOR EACH ROW EXECUTE FUNCTION guard_supplier_contract_history();
                CREATE FUNCTION guard_supplier_document_link() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NOT EXISTS(SELECT 1 FROM stored_documents f WHERE f.id=NEW.document_id AND f.property_id=NEW.property_id
                                  AND f.is_supplier_only AND f.audience='MANAGEMENT' AND f.announcement_id IS NULL) THEN
                        RAISE EXCEPTION 'Supplier document must remain private' USING ERRCODE='23514';
                    END IF;
                    IF TG_OP='UPDATE' THEN
                        IF (NEW.tenant_id,NEW.supplier_id,NEW.property_id,NEW.document_id,NEW.previous_document_id,NEW.revision_number,NEW.expires_on)
                           IS DISTINCT FROM (OLD.tenant_id,OLD.supplier_id,OLD.property_id,OLD.document_id,OLD.previous_document_id,OLD.revision_number,OLD.expires_on) OR
                           (NEW.is_deleted AND (OLD.previous_document_id IS NOT NULL OR
                             EXISTS(SELECT 1 FROM supplier_documents n WHERE n.previous_document_id=OLD.id) OR
                             EXISTS(SELECT 1 FROM supplier_contracts c WHERE c.document_id=OLD.id))) THEN
                            RAISE EXCEPTION 'Supplier document history is protected' USING ERRCODE='23514';
                        END IF;
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_document_link_guard BEFORE INSERT OR UPDATE ON supplier_documents
                FOR EACH ROW EXECUTE FUNCTION guard_supplier_document_link();
                CREATE FUNCTION guard_supplier_stored_document() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF OLD.is_supplier_only AND (NOT NEW.is_supplier_only OR NEW.audience<>'MANAGEMENT' OR NEW.announcement_id IS NOT NULL) THEN
                        RAISE EXCEPTION 'Supplier document privacy cannot be widened' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_stored_document_guard BEFORE UPDATE ON stored_documents
                FOR EACH ROW EXECUTE FUNCTION guard_supplier_stored_document();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM supplier_contracts) OR EXISTS(SELECT 1 FROM supplier_contract_revisions) OR
                       EXISTS(SELECT 1 FROM supplier_documents) OR EXISTS(SELECT 1 FROM stored_documents WHERE is_supplier_only) THEN
                        RAISE EXCEPTION 'Supplier documents or contracts exist; rollback would lose history or privacy';
                    END IF;
                END $$;
                DROP TRIGGER supplier_stored_document_guard ON stored_documents;
                DROP FUNCTION guard_supplier_stored_document();
                DROP TRIGGER supplier_document_link_guard ON supplier_documents;
                DROP FUNCTION guard_supplier_document_link();
                DROP TRIGGER supplier_contract_history_guard ON supplier_contracts;
                DROP FUNCTION guard_supplier_contract_history();
                DROP TRIGGER supplier_contract_revision_immutable ON supplier_contract_revisions;
                DROP FUNCTION reject_supplier_contract_revision_mutation();
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_supplier_documents_supplier_documents_tenant_id_supplier_id~",
                table: "supplier_documents");

            migrationBuilder.DropTable(
                name: "supplier_contract_revisions");

            migrationBuilder.DropTable(
                name: "supplier_contracts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_supplier_documents_tenant_id_supplier_id_property_id_id",
                table: "supplier_documents");

            migrationBuilder.DropIndex(
                name: "IX_supplier_documents_previous_document_id",
                table: "supplier_documents");

            migrationBuilder.DropIndex(
                name: "IX_supplier_documents_tenant_id_supplier_id_property_id_previo~",
                table: "supplier_documents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_document_revision",
                table: "supplier_documents");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_supplier_area_services_tenant_id_supplier_id_property_id_id",
                table: "supplier_area_services");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documents_supplier_private",
                table: "stored_documents");

            migrationBuilder.DropColumn(
                name: "previous_document_id",
                table: "supplier_documents");

            migrationBuilder.DropColumn(
                name: "revision_number",
                table: "supplier_documents");

            migrationBuilder.DropColumn(
                name: "is_supplier_only",
                table: "stored_documents");
        }
    }
}
