using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_inquiries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    deadline = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_supplier_inquiries", x => x.id);
                    table.UniqueConstraint("AK_supplier_inquiries_tenant_id_property_id_id", x => new { x.tenant_id, x.property_id, x.id });
                    table.CheckConstraint("ck_supplier_inquiry_valid", "length(trim(title)) > 0 AND length(trim(description)) > 0 AND deadline BETWEEN DATE '2000-01-01' AND DATE '2100-12-31' AND status IN ('OPEN','AWARDED','CANCELLED') AND (status <> 'CANCELLED' OR cancellation_reason IS NOT NULL AND length(trim(cancellation_reason)) > 0)");
                    table.ForeignKey(
                        name: "FK_supplier_inquiries_person_property_scopes_tenant_id_propert~",
                        columns: x => new { x.tenant_id, x.property_id },
                        principalTable: "person_property_scopes",
                        principalColumns: new[] { "tenant_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_inquiries_supplier_categories_tenant_id_category_id",
                        columns: x => new { x.tenant_id, x.category_id },
                        principalTable: "supplier_categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_inquiry_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inquiry_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_supplier_inquiry_revisions", x => x.id);
                    table.CheckConstraint("ck_supplier_inquiry_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','QUOTE','APPROVE','REJECT','CANCEL','CONVERT')");
                    table.ForeignKey(
                        name: "FK_supplier_inquiry_revisions_supplier_inquiries_tenant_id_pro~",
                        columns: x => new { x.tenant_id, x.property_id, x.inquiry_id },
                        principalTable: "supplier_inquiries",
                        principalColumns: new[] { "tenant_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inquiry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_service_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_supplier_invitations", x => x.id);
                    table.UniqueConstraint("AK_supplier_invitations_tenant_id_property_id_inquiry_id_suppl~", x => new { x.tenant_id, x.property_id, x.inquiry_id, x.supplier_id, x.id });
                    table.ForeignKey(
                        name: "FK_supplier_invitations_supplier_area_services_tenant_id_suppl~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.area_service_id },
                        principalTable: "supplier_area_services",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invitations_supplier_inquiries_tenant_id_property_~",
                        columns: x => new { x.tenant_id, x.property_id, x.inquiry_id },
                        principalTable: "supplier_inquiries",
                        principalColumns: new[] { "tenant_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inquiry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    entered_by_issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    entered_by_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decision_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_supplier_quotes", x => x.id);
                    table.UniqueConstraint("AK_supplier_quotes_tenant_id_property_id_inquiry_id_supplier_i~", x => new { x.tenant_id, x.property_id, x.inquiry_id, x.supplier_id, x.id });
                    table.CheckConstraint("ck_supplier_quote_valid", "net_amount > 0 AND gross_amount <= 99999999.99 AND vat_rate BETWEEN 0 AND 100 AND vat_amount = round(net_amount * vat_rate / 100, 2) AND gross_amount = net_amount + vat_amount AND currency IN ('TRY','USD','EUR') AND valid_until BETWEEN DATE '2000-01-01' AND DATE '2100-12-31' AND length(trim(notes)) > 0 AND status IN ('PENDING','APPROVED','REJECTED') AND (status = 'PENDING' AND decision_reason IS NULL OR status <> 'PENDING' AND decision_reason IS NOT NULL AND length(trim(decision_reason)) > 0)");
                    table.ForeignKey(
                        name: "FK_supplier_quotes_supplier_inquiries_tenant_id_property_id_in~",
                        columns: x => new { x.tenant_id, x.property_id, x.inquiry_id },
                        principalTable: "supplier_inquiries",
                        principalColumns: new[] { "tenant_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_quotes_supplier_invitations_tenant_id_property_id_~",
                        columns: x => new { x.tenant_id, x.property_id, x.inquiry_id, x.supplier_id, x.invitation_id },
                        principalTable: "supplier_invitations",
                        principalColumns: new[] { "tenant_id", "property_id", "inquiry_id", "supplier_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_work_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inquiry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_supplier_work_orders", x => x.id);
                    table.CheckConstraint("ck_supplier_work_order_draft", "status = 'DRAFT' AND amount > 0 AND amount <= 99999999.99 AND currency IN ('TRY','USD','EUR') AND length(trim(title)) > 0");
                    table.ForeignKey(
                        name: "FK_supplier_work_orders_supplier_area_services_tenant_id_suppl~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.area_service_id },
                        principalTable: "supplier_area_services",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_work_orders_supplier_inquiries_tenant_id_property_~",
                        columns: x => new { x.tenant_id, x.property_id, x.inquiry_id },
                        principalTable: "supplier_inquiries",
                        principalColumns: new[] { "tenant_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_work_orders_supplier_quotes_tenant_id_property_id_~",
                        columns: x => new { x.tenant_id, x.property_id, x.inquiry_id, x.supplier_id, x.quote_id },
                        principalTable: "supplier_quotes",
                        principalColumns: new[] { "tenant_id", "property_id", "inquiry_id", "supplier_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_inquiries_tenant_id_category_id",
                table: "supplier_inquiries",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_inquiries_tenant_id_property_id_created_at",
                table: "supplier_inquiries",
                columns: new[] { "tenant_id", "property_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_inquiry_revisions_inquiry_id_applied_version",
                table: "supplier_inquiry_revisions",
                columns: new[] { "inquiry_id", "applied_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_inquiry_revisions_tenant_id_actor_issuer_actor_sub~",
                table: "supplier_inquiry_revisions",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_inquiry_revisions_tenant_id_property_id_inquiry_id",
                table: "supplier_inquiry_revisions",
                columns: new[] { "tenant_id", "property_id", "inquiry_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invitations_inquiry_id_supplier_id",
                table: "supplier_invitations",
                columns: new[] { "inquiry_id", "supplier_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invitations_tenant_id_supplier_id_property_id_area~",
                table: "supplier_invitations",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "area_service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_quotes_inquiry_id",
                table: "supplier_quotes",
                column: "inquiry_id",
                unique: true,
                filter: "status = 'APPROVED'");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_quotes_invitation_id",
                table: "supplier_quotes",
                column: "invitation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_quotes_tenant_id_property_id_inquiry_id_supplier_i~",
                table: "supplier_quotes",
                columns: new[] { "tenant_id", "property_id", "inquiry_id", "supplier_id", "invitation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_quote_id",
                table: "supplier_work_orders",
                column: "quote_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_tenant_id_property_id_inquiry_id_suppl~",
                table: "supplier_work_orders",
                columns: new[] { "tenant_id", "property_id", "inquiry_id", "supplier_id", "quote_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_work_orders_tenant_id_supplier_id_property_id_area~",
                table: "supplier_work_orders",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "area_service_id" });
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_supplier_inquiry_revision_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Supplier inquiry revisions are immutable' USING ERRCODE='23514'; END $$;
                CREATE TRIGGER supplier_inquiry_revision_immutable BEFORE UPDATE OR DELETE ON supplier_inquiry_revisions
                FOR EACH ROW EXECUTE FUNCTION reject_supplier_inquiry_revision_mutation();

                CREATE FUNCTION guard_supplier_invitation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP <> 'INSERT' THEN RAISE EXCEPTION 'Supplier invitations are immutable' USING ERRCODE='23514'; END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM supplier_inquiries i
                        JOIN supplier_area_services a ON a.id=NEW.area_service_id AND a.tenant_id=NEW.tenant_id AND a.property_id=NEW.property_id AND a.supplier_id=NEW.supplier_id
                        JOIN supplier_services s ON s.id=a.service_id AND s.tenant_id=a.tenant_id AND s.supplier_id=a.supplier_id
                        WHERE i.id=NEW.inquiry_id AND i.tenant_id=NEW.tenant_id AND i.property_id=NEW.property_id AND i.category_id=s.category_id
                    ) THEN RAISE EXCEPTION 'Supplier invitation category mismatch' USING ERRCODE='23514'; END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_invitation_guard BEFORE INSERT OR UPDATE OR DELETE ON supplier_invitations
                FOR EACH ROW EXECUTE FUNCTION guard_supplier_invitation();

                CREATE FUNCTION guard_supplier_quote() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Supplier quote history is protected' USING ERRCODE='23514'; END IF;
                    IF NEW.is_deleted OR NOT NEW.is_enabled OR
                       (NEW.tenant_id,NEW.property_id,NEW.inquiry_id,NEW.supplier_id,NEW.invitation_id,NEW.entered_by_issuer,NEW.entered_by_subject)
                       IS DISTINCT FROM (OLD.tenant_id,OLD.property_id,OLD.inquiry_id,OLD.supplier_id,OLD.invitation_id,OLD.entered_by_issuer,OLD.entered_by_subject) OR
                       (OLD.status <> 'PENDING' AND (NEW.net_amount,NEW.vat_rate,NEW.vat_amount,NEW.gross_amount,NEW.currency,NEW.valid_until,NEW.notes,NEW.status,NEW.decision_reason)
                       IS DISTINCT FROM (OLD.net_amount,OLD.vat_rate,OLD.vat_amount,OLD.gross_amount,OLD.currency,OLD.valid_until,OLD.notes,OLD.status,OLD.decision_reason)) THEN
                        RAISE EXCEPTION 'Supplier quote history is protected' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_quote_guard BEFORE UPDATE OR DELETE ON supplier_quotes FOR EACH ROW EXECUTE FUNCTION guard_supplier_quote();

                CREATE FUNCTION guard_supplier_inquiry() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Supplier inquiry history is protected' USING ERRCODE='23514'; END IF;
                    IF NEW.is_deleted OR NOT NEW.is_enabled OR
                       (NEW.tenant_id,NEW.property_id,NEW.category_id,NEW.title,NEW.description,NEW.deadline)
                       IS DISTINCT FROM (OLD.tenant_id,OLD.property_id,OLD.category_id,OLD.title,OLD.description,OLD.deadline) OR
                       (OLD.status <> 'OPEN' AND (NEW.status,NEW.cancellation_reason) IS DISTINCT FROM (OLD.status,OLD.cancellation_reason)) THEN
                        RAISE EXCEPTION 'Supplier inquiry history is protected' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_inquiry_guard BEFORE UPDATE OR DELETE ON supplier_inquiries FOR EACH ROW EXECUTE FUNCTION guard_supplier_inquiry();
                CREATE FUNCTION guard_supplier_work_order_quote() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM supplier_quotes q
                        JOIN supplier_inquiries i ON i.id=q.inquiry_id AND i.tenant_id=q.tenant_id AND i.property_id=q.property_id
                        JOIN supplier_invitations v ON v.id=q.invitation_id AND v.tenant_id=q.tenant_id AND v.property_id=q.property_id AND v.inquiry_id=q.inquiry_id AND v.supplier_id=q.supplier_id
                        WHERE q.id=NEW.quote_id AND q.tenant_id=NEW.tenant_id AND q.property_id=NEW.property_id AND q.inquiry_id=NEW.inquiry_id AND q.supplier_id=NEW.supplier_id
                        AND q.status='APPROVED' AND i.status='AWARDED' AND v.area_service_id=NEW.area_service_id AND q.gross_amount=NEW.amount AND q.currency=NEW.currency AND i.title=NEW.title)
                    THEN RAISE EXCEPTION 'Work order must preserve the approved quote snapshot' USING ERRCODE='23514'; END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_work_order_quote_guard BEFORE INSERT ON supplier_work_orders FOR EACH ROW EXECUTE FUNCTION guard_supplier_work_order_quote();
                CREATE TRIGGER supplier_work_order_draft_immutable BEFORE UPDATE OR DELETE ON supplier_work_orders FOR EACH ROW EXECUTE FUNCTION reject_supplier_inquiry_revision_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM supplier_inquiries) OR EXISTS(SELECT 1 FROM supplier_invitations) OR
                       EXISTS(SELECT 1 FROM supplier_quotes) OR EXISTS(SELECT 1 FROM supplier_work_orders) OR EXISTS(SELECT 1 FROM supplier_inquiry_revisions) THEN
                        RAISE EXCEPTION 'Supplier inquiry history exists; rollback would lose data';
                    END IF;
                END $$;
                DROP TRIGGER supplier_work_order_quote_guard ON supplier_work_orders;
                DROP FUNCTION guard_supplier_work_order_quote();
                DROP TRIGGER supplier_work_order_draft_immutable ON supplier_work_orders;
                DROP TRIGGER supplier_inquiry_revision_immutable ON supplier_inquiry_revisions;
                DROP TRIGGER supplier_invitation_guard ON supplier_invitations;
                DROP TRIGGER supplier_quote_guard ON supplier_quotes;
                DROP TRIGGER supplier_inquiry_guard ON supplier_inquiries;
                DROP FUNCTION guard_supplier_invitation();
                DROP FUNCTION guard_supplier_quote();
                DROP FUNCTION guard_supplier_inquiry();
                DROP FUNCTION reject_supplier_inquiry_revision_mutation();
                """);
            migrationBuilder.DropTable(
                name: "supplier_inquiry_revisions");

            migrationBuilder.DropTable(
                name: "supplier_work_orders");

            migrationBuilder.DropTable(
                name: "supplier_quotes");

            migrationBuilder.DropTable(
                name: "supplier_invitations");

            migrationBuilder.DropTable(
                name: "supplier_inquiries");
        }
    }
}
