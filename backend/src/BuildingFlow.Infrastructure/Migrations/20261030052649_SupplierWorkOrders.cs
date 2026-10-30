using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierWorkOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER supplier_work_order_draft_immutable ON supplier_work_orders;");
            migrationBuilder.DropIndex(
                name: "IX_supplier_work_orders_quote_id",
                table: "supplier_work_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_work_order_draft",
                table: "supplier_work_orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "quote_id",
                table: "supplier_work_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "inquiry_id",
                table: "supplier_work_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "currency",
                table: "supplier_work_orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "supplier_work_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "accepted_at",
                table: "supplier_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "assigned_at",
                table: "supplier_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "supplier_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "supplier_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "completion_note",
                table: "supplier_work_orders",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "decision_reason",
                table: "supplier_work_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "supplier_work_orders",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "due_at",
                table: "supplier_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "maintenance_plan_id",
                table: "supplier_work_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "request_id",
                table: "supplier_work_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "supplier_work_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "QUOTE");

            migrationBuilder.AddColumn<string>(
                name: "source_snapshot",
                table: "supplier_work_orders",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "source_version",
                table: "supplier_work_orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "started_at",
                table: "supplier_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "supplier_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_supplier_work_orders_tenant_id_supplier_id_property_id_id",
                table: "supplier_work_orders",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "id" });

            migrationBuilder.CreateTable(
                name: "supplier_work_order_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    caption = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_supplier_work_order_photos", x => x.id);
                    table.CheckConstraint("ck_supplier_work_order_photo_caption", "length(trim(caption)) > 0");
                    table.ForeignKey(
                        name: "FK_supplier_work_order_photos_supplier_documents_tenant_id_sup~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.document_id },
                        principalTable: "supplier_documents",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_work_order_photos_supplier_work_orders_tenant_id_s~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.work_order_id },
                        principalTable: "supplier_work_orders",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_work_order_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_supplier_work_order_revisions", x => x.id);
                    table.CheckConstraint("ck_supplier_work_order_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','EDIT','ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED','CANCELLED','PHOTO')");
                    table.ForeignKey(
                        name: "FK_supplier_work_order_revisions_supplier_work_orders_tenant_i~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.work_order_id },
                        principalTable: "supplier_work_orders",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_maintenance_plan_id",
                table: "supplier_work_orders",
                column: "maintenance_plan_id",
                unique: true,
                filter: "maintenance_plan_id IS NOT NULL AND status NOT IN ('CLOSED','REJECTED','CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_property_id_maintenance_plan_id",
                table: "supplier_work_orders",
                columns: new[] { "property_id", "maintenance_plan_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_property_id_request_id",
                table: "supplier_work_orders",
                columns: new[] { "property_id", "request_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_quote_id",
                table: "supplier_work_orders",
                column: "quote_id",
                unique: true,
                filter: "quote_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_request_id",
                table: "supplier_work_orders",
                column: "request_id",
                unique: true,
                filter: "request_id IS NOT NULL AND status NOT IN ('CLOSED','REJECTED','CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_tenant_id_supplier_id_property_id_stat~",
                table: "supplier_work_orders",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "status", "created_at" });

            // Existing phase05 work orders are quote drafts; copy their actual inquiry scope without changing audit/version.
            migrationBuilder.Sql("""
                UPDATE supplier_work_orders w SET source='QUOTE', description=i.description, source_snapshot=i.description
                FROM supplier_inquiries i WHERE i.id=w.inquiry_id AND i.tenant_id=w.tenant_id AND i.property_id=w.property_id;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_work_order_lifecycle",
                table: "supplier_work_orders",
                sql: "(status NOT IN ('ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED') OR assigned_at IS NOT NULL AND due_at IS NOT NULL) AND (status NOT IN ('ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED') OR accepted_at IS NOT NULL) AND (status NOT IN ('IN_PROGRESS','COMPLETED','VERIFIED','CLOSED') OR started_at IS NOT NULL) AND (status NOT IN ('COMPLETED','VERIFIED','CLOSED') OR completed_at IS NOT NULL AND completion_note IS NOT NULL AND length(trim(completion_note)) > 0) AND (status NOT IN ('VERIFIED','CLOSED') OR verified_at IS NOT NULL) AND (status <> 'CLOSED' OR closed_at IS NOT NULL) AND (status NOT IN ('REJECTED','CANCELLED') OR decision_reason IS NOT NULL AND length(trim(decision_reason)) > 0) AND (assigned_at IS NULL OR due_at IS NOT NULL AND due_at > assigned_at) AND (accepted_at IS NULL OR assigned_at IS NOT NULL AND accepted_at >= assigned_at) AND (started_at IS NULL OR accepted_at IS NOT NULL AND started_at >= accepted_at) AND (completed_at IS NULL OR started_at IS NOT NULL AND completed_at >= started_at) AND (verified_at IS NULL OR completed_at IS NOT NULL AND verified_at >= completed_at) AND (closed_at IS NULL OR verified_at IS NOT NULL AND closed_at >= verified_at)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_work_order_source",
                table: "supplier_work_orders",
                sql: "(source = 'QUOTE' AND quote_id IS NOT NULL AND inquiry_id IS NOT NULL AND request_id IS NULL AND maintenance_plan_id IS NULL AND source_version IS NULL AND amount IS NOT NULL AND currency IS NOT NULL) OR (source = 'REQUEST' AND request_id IS NOT NULL AND source_version IS NOT NULL AND source_version > 0 AND inquiry_id IS NULL AND quote_id IS NULL AND maintenance_plan_id IS NULL AND amount IS NULL AND currency IS NULL) OR (source = 'MAINTENANCE' AND maintenance_plan_id IS NOT NULL AND source_version IS NOT NULL AND source_version > 0 AND inquiry_id IS NULL AND quote_id IS NULL AND request_id IS NULL AND amount IS NULL AND currency IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_work_order_valid",
                table: "supplier_work_orders",
                sql: "length(trim(title)) > 0 AND length(trim(description)) > 0 AND length(trim(source_snapshot)) > 0 AND (amount IS NULL OR amount > 0 AND amount <= 99999999.99) AND (currency IS NULL OR currency IN ('TRY','USD','EUR')) AND status IN ('DRAFT','ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED','CANCELLED') AND (due_at IS NULL OR due_at >= TIMESTAMPTZ '2000-01-01' AND due_at < TIMESTAMPTZ '2101-01-01')");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_order_photos_tenant_id_supplier_id_property_~1",
                table: "supplier_work_order_photos",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "work_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_order_photos_tenant_id_supplier_id_property_i~",
                table: "supplier_work_order_photos",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "document_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_order_photos_work_order_id_document_id",
                table: "supplier_work_order_photos",
                columns: new[] { "work_order_id", "document_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_order_revisions_tenant_id_actor_issuer_actor_~",
                table: "supplier_work_order_revisions",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_order_revisions_tenant_id_supplier_id_propert~",
                table: "supplier_work_order_revisions",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "work_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_order_revisions_work_order_id_applied_version",
                table: "supplier_work_order_revisions",
                columns: new[] { "work_order_id", "applied_version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_work_orders_maintenance_plans_property_id_maintena~",
                table: "supplier_work_orders",
                columns: new[] { "property_id", "maintenance_plan_id" },
                principalTable: "maintenance_plans",
                principalColumns: new[] { "property_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_work_orders_mobile_requests_property_id_request_id",
                table: "supplier_work_orders",
                columns: new[] { "property_id", "request_id" },
                principalTable: "mobile_requests",
                principalColumns: new[] { "property_id", "id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                CREATE FUNCTION guard_supplier_work_order_history() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Work order history is protected' USING ERRCODE='23514'; END IF;
                    IF NEW.is_deleted OR NOT NEW.is_enabled OR
                       (NEW.tenant_id,NEW.supplier_id,NEW.property_id,NEW.area_service_id,NEW.inquiry_id,NEW.quote_id,NEW.request_id,NEW.maintenance_plan_id,NEW.source,NEW.source_version,NEW.source_snapshot,NEW.title,NEW.amount,NEW.currency)
                       IS DISTINCT FROM (OLD.tenant_id,OLD.supplier_id,OLD.property_id,OLD.area_service_id,OLD.inquiry_id,OLD.quote_id,OLD.request_id,OLD.maintenance_plan_id,OLD.source,OLD.source_version,OLD.source_snapshot,OLD.title,OLD.amount,OLD.currency) OR
                       (OLD.status <> 'DRAFT' AND (NEW.description,NEW.due_at) IS DISTINCT FROM (OLD.description,OLD.due_at)) THEN
                        RAISE EXCEPTION 'Work order scope and history are immutable' USING ERRCODE='23514';
                    END IF;
                    IF NEW.status <> OLD.status AND NOT (
                        (OLD.status='DRAFT' AND NEW.status IN ('ASSIGNED','CANCELLED')) OR
                        (OLD.status='ASSIGNED' AND NEW.status IN ('ACCEPTED','REJECTED','CANCELLED')) OR
                        (OLD.status='ACCEPTED' AND NEW.status IN ('IN_PROGRESS','CANCELLED')) OR
                        (OLD.status='IN_PROGRESS' AND NEW.status IN ('COMPLETED','CANCELLED')) OR
                        (OLD.status='COMPLETED' AND NEW.status IN ('VERIFIED','CANCELLED')) OR
                        (OLD.status='VERIFIED' AND NEW.status='CLOSED')
                    ) THEN RAISE EXCEPTION 'Invalid work order transition' USING ERRCODE='23514'; END IF;
                    IF (NEW.assigned_at IS DISTINCT FROM OLD.assigned_at AND NOT (OLD.status='DRAFT' AND NEW.status='ASSIGNED')) OR
                       (NEW.accepted_at IS DISTINCT FROM OLD.accepted_at AND NOT (OLD.status='ASSIGNED' AND NEW.status='ACCEPTED')) OR
                       (NEW.started_at IS DISTINCT FROM OLD.started_at AND NOT (OLD.status='ACCEPTED' AND NEW.status='IN_PROGRESS')) OR
                       ((NEW.completed_at,NEW.completion_note) IS DISTINCT FROM (OLD.completed_at,OLD.completion_note) AND NOT (OLD.status='IN_PROGRESS' AND NEW.status='COMPLETED')) OR
                       (NEW.verified_at IS DISTINCT FROM OLD.verified_at AND NOT (OLD.status='COMPLETED' AND NEW.status='VERIFIED')) OR
                       (NEW.closed_at IS DISTINCT FROM OLD.closed_at AND NOT (OLD.status='VERIFIED' AND NEW.status='CLOSED')) OR
                       (NEW.decision_reason IS DISTINCT FROM OLD.decision_reason AND NOT (OLD.status<>NEW.status AND NEW.status IN ('REJECTED','CANCELLED'))) THEN
                        RAISE EXCEPTION 'Work order timeline cannot be rewritten' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_work_order_history_guard BEFORE UPDATE OR DELETE ON supplier_work_orders FOR EACH ROW EXECUTE FUNCTION guard_supplier_work_order_history();
                CREATE TRIGGER supplier_work_order_revision_immutable BEFORE UPDATE OR DELETE ON supplier_work_order_revisions FOR EACH ROW EXECUTE FUNCTION reject_supplier_inquiry_revision_mutation();
                CREATE TRIGGER supplier_work_order_photo_immutable BEFORE UPDATE OR DELETE ON supplier_work_order_photos FOR EACH ROW EXECUTE FUNCTION reject_supplier_inquiry_revision_mutation();
                CREATE FUNCTION guard_supplier_work_order_photo() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NOT EXISTS(SELECT 1 FROM supplier_work_orders w WHERE w.id=NEW.work_order_id AND w.tenant_id=NEW.tenant_id AND w.supplier_id=NEW.supplier_id AND w.property_id=NEW.property_id AND w.status IN ('DRAFT','ASSIGNED','ACCEPTED','IN_PROGRESS')) OR
                       (SELECT count(*) FROM supplier_work_order_photos p WHERE p.work_order_id=NEW.work_order_id)>=10 OR
                       NOT EXISTS(SELECT 1 FROM supplier_documents d JOIN stored_documents f ON f.id=d.document_id AND f.property_id=d.property_id
                           WHERE d.id=NEW.document_id AND d.tenant_id=NEW.tenant_id AND d.supplier_id=NEW.supplier_id AND d.property_id=NEW.property_id
                           AND NOT d.is_deleted AND d.is_enabled AND NOT f.is_deleted AND f.is_enabled AND f.is_supplier_only AND f.audience='MANAGEMENT' AND f.content_type IN ('image/jpeg','image/png')) THEN
                        RAISE EXCEPTION 'Invalid work order photo scope or limit' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_work_order_photo_guard BEFORE INSERT ON supplier_work_order_photos FOR EACH ROW EXECUTE FUNCTION guard_supplier_work_order_photo();
                CREATE FUNCTION protect_supplier_work_order_photo_document() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF (TG_TABLE_NAME='supplier_documents' AND EXISTS(SELECT 1 FROM supplier_work_order_photos p WHERE p.document_id=OLD.id)) OR
                       (TG_TABLE_NAME='stored_documents' AND EXISTS(SELECT 1 FROM supplier_work_order_photos p JOIN supplier_documents d ON d.id=p.document_id AND d.tenant_id=p.tenant_id AND d.supplier_id=p.supplier_id AND d.property_id=p.property_id WHERE d.document_id=OLD.id AND d.property_id=OLD.property_id)) THEN
                        RAISE EXCEPTION 'Work order photo evidence is immutable' USING ERRCODE='23514';
                    END IF;
                    RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
                END $$;
                CREATE TRIGGER supplier_work_order_document_guard BEFORE UPDATE OR DELETE ON supplier_documents FOR EACH ROW EXECUTE FUNCTION protect_supplier_work_order_photo_document();
                CREATE TRIGGER supplier_work_order_file_guard BEFORE UPDATE OR DELETE ON stored_documents FOR EACH ROW EXECUTE FUNCTION protect_supplier_work_order_photo_document();
                CREATE OR REPLACE FUNCTION guard_supplier_work_order_quote() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW.source='QUOTE' AND NOT EXISTS(SELECT 1 FROM supplier_quotes q
                        JOIN supplier_inquiries i ON i.id=q.inquiry_id AND i.tenant_id=q.tenant_id AND i.property_id=q.property_id
                        JOIN supplier_invitations v ON v.id=q.invitation_id AND v.tenant_id=q.tenant_id AND v.property_id=q.property_id AND v.inquiry_id=q.inquiry_id AND v.supplier_id=q.supplier_id
                        WHERE q.id=NEW.quote_id AND q.tenant_id=NEW.tenant_id AND q.property_id=NEW.property_id AND q.inquiry_id=NEW.inquiry_id AND q.supplier_id=NEW.supplier_id
                        AND q.status='APPROVED' AND i.status='AWARDED' AND v.area_service_id=NEW.area_service_id AND q.gross_amount=NEW.amount AND q.currency=NEW.currency AND i.title=NEW.title)
                    THEN RAISE EXCEPTION 'Work order must preserve the approved quote snapshot' USING ERRCODE='23514'; END IF;
                    RETURN NEW;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM supplier_work_order_revisions) OR EXISTS(SELECT 1 FROM supplier_work_order_photos) OR
                       EXISTS(SELECT 1 FROM supplier_work_orders w LEFT JOIN supplier_inquiries i ON i.id=w.inquiry_id AND i.tenant_id=w.tenant_id AND i.property_id=w.property_id
                           WHERE w.source<>'QUOTE' OR w.status<>'DRAFT' OR w.quote_id IS NULL OR w.inquiry_id IS NULL OR w.due_at IS NOT NULL OR
                                 w.assigned_at IS NOT NULL OR w.accepted_at IS NOT NULL OR w.started_at IS NOT NULL OR w.completed_at IS NOT NULL OR w.verified_at IS NOT NULL OR w.closed_at IS NOT NULL OR w.completion_note IS NOT NULL OR w.decision_reason IS NOT NULL OR
                                 w.description IS DISTINCT FROM i.description OR w.source_snapshot IS DISTINCT FROM i.description) THEN
                        RAISE EXCEPTION 'Work order history exists; rollback would lose data';
                    END IF;
                END $$;
                DROP TRIGGER supplier_work_order_history_guard ON supplier_work_orders;
                DROP TRIGGER supplier_work_order_revision_immutable ON supplier_work_order_revisions;
                DROP TRIGGER supplier_work_order_photo_immutable ON supplier_work_order_photos;
                DROP TRIGGER supplier_work_order_photo_guard ON supplier_work_order_photos;
                DROP TRIGGER supplier_work_order_document_guard ON supplier_documents;
                DROP TRIGGER supplier_work_order_file_guard ON stored_documents;
                DROP FUNCTION guard_supplier_work_order_history();
                DROP FUNCTION guard_supplier_work_order_photo();
                DROP FUNCTION protect_supplier_work_order_photo_document();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_work_orders_maintenance_plans_property_id_maintena~",
                table: "supplier_work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_work_orders_mobile_requests_property_id_request_id",
                table: "supplier_work_orders");

            migrationBuilder.DropTable(
                name: "supplier_work_order_photos");

            migrationBuilder.DropTable(
                name: "supplier_work_order_revisions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_supplier_work_orders_tenant_id_supplier_id_property_id_id",
                table: "supplier_work_orders");

            migrationBuilder.DropIndex(
                name: "IX_supplier_work_orders_maintenance_plan_id",
                table: "supplier_work_orders");

            migrationBuilder.DropIndex(
                name: "IX_supplier_work_orders_property_id_maintenance_plan_id",
                table: "supplier_work_orders");

            migrationBuilder.DropIndex(
                name: "IX_supplier_work_orders_property_id_request_id",
                table: "supplier_work_orders");

            migrationBuilder.DropIndex(
                name: "IX_supplier_work_orders_quote_id",
                table: "supplier_work_orders");

            migrationBuilder.DropIndex(
                name: "IX_supplier_work_orders_request_id",
                table: "supplier_work_orders");

            migrationBuilder.DropIndex(
                name: "IX_supplier_work_orders_tenant_id_supplier_id_property_id_stat~",
                table: "supplier_work_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_work_order_lifecycle",
                table: "supplier_work_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_work_order_source",
                table: "supplier_work_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_work_order_valid",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "accepted_at",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "completion_note",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "decision_reason",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "description",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "due_at",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "maintenance_plan_id",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "source",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "source_snapshot",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "source_version",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "supplier_work_orders");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "supplier_work_orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "quote_id",
                table: "supplier_work_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "inquiry_id",
                table: "supplier_work_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "currency",
                table: "supplier_work_orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "supplier_work_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_quote_id",
                table: "supplier_work_orders",
                column: "quote_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_work_order_draft",
                table: "supplier_work_orders",
                sql: "status = 'DRAFT' AND amount > 0 AND amount <= 99999999.99 AND currency IN ('TRY','USD','EUR') AND length(trim(title)) > 0");
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION guard_supplier_work_order_quote() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NOT EXISTS(SELECT 1 FROM supplier_quotes q
                        JOIN supplier_inquiries i ON i.id=q.inquiry_id AND i.tenant_id=q.tenant_id AND i.property_id=q.property_id
                        JOIN supplier_invitations v ON v.id=q.invitation_id AND v.tenant_id=q.tenant_id AND v.property_id=q.property_id AND v.inquiry_id=q.inquiry_id AND v.supplier_id=q.supplier_id
                        WHERE q.id=NEW.quote_id AND q.tenant_id=NEW.tenant_id AND q.property_id=NEW.property_id AND q.inquiry_id=NEW.inquiry_id AND q.supplier_id=NEW.supplier_id
                        AND q.status='APPROVED' AND i.status='AWARDED' AND v.area_service_id=NEW.area_service_id AND q.gross_amount=NEW.amount AND q.currency=NEW.currency AND i.title=NEW.title)
                    THEN RAISE EXCEPTION 'Work order must preserve the approved quote snapshot' USING ERRCODE='23514'; END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_work_order_draft_immutable BEFORE UPDATE OR DELETE ON supplier_work_orders FOR EACH ROW EXECUTE FUNCTION reject_supplier_inquiry_revision_mutation();
                """);
        }
    }
}
