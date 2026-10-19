using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ManagementAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "management_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_management_assignments", x => x.id);
                    table.UniqueConstraint("AK_management_assignments_tenant_id_property_id_id", x => new { x.tenant_id, x.property_id, x.id });
                    table.CheckConstraint("ck_management_closure", "(end_reason IS NULL OR (end_date IS NOT NULL AND length(btrim(end_reason)) BETWEEN 1 AND 500)) AND ((cancelled_at IS NULL AND cancellation_reason IS NULL) OR (cancelled_at IS NOT NULL AND cancellation_reason IS NOT NULL AND length(btrim(cancellation_reason)) BETWEEN 1 AND 500))");
                    table.CheckConstraint("ck_management_dates", "start_date>DATE '0001-01-01' AND (end_date IS NULL OR end_date>=start_date)");
                    table.CheckConstraint("ck_management_role", "role IN ('SITE_ADMIN','MANAGER','ACCOUNTANT')");
                    table.ForeignKey(
                        name: "FK_management_assignments_person_property_scopes_tenant_id_pro~",
                        columns: x => new { x.tenant_id, x.property_id },
                        principalTable: "person_property_scopes",
                        principalColumns: new[] { "tenant_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_management_assignments_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "management_assignment_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_management_assignment_events", x => x.id);
                    table.CheckConstraint("ck_management_event", "action IN ('CREATED','ENDED','CANCELLED') AND length(btrim(reason)) BETWEEN 1 AND 500 AND applied_version>0");
                    table.ForeignKey(
                        name: "FK_management_assignment_events_management_assignments_tenant_~",
                        columns: x => new { x.tenant_id, x.property_id, x.assignment_id },
                        principalTable: "management_assignments",
                        principalColumns: new[] { "tenant_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "management_assignment_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_management_assignment_operations", x => x.id);
                    table.CheckConstraint("ck_management_operation", "applied_version>0 AND length(fingerprint)=64");
                    table.ForeignKey(
                        name: "FK_management_assignment_operations_management_assignments_ten~",
                        columns: x => new { x.tenant_id, x.property_id, x.assignment_id },
                        principalTable: "management_assignments",
                        principalColumns: new[] { "tenant_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_management_assignment_events_tenant_id_property_id_assignme~",
                table: "management_assignment_events",
                columns: new[] { "tenant_id", "property_id", "assignment_id", "applied_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_management_assignment_operations_tenant_id_actor_issuer_act~",
                table: "management_assignment_operations",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_management_assignment_operations_tenant_id_property_id_assi~",
                table: "management_assignment_operations",
                columns: new[] { "tenant_id", "property_id", "assignment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_management_assignments_tenant_id_person_id",
                table: "management_assignments",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_management_assignments_tenant_id_property_id_person_id_role",
                table: "management_assignments",
                columns: new[] { "tenant_id", "property_id", "person_id", "role" },
                unique: true,
                filter: "is_deleted=false AND cancelled_at IS NULL AND end_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_management_assignments_tenant_id_property_id_start_date",
                table: "management_assignments",
                columns: new[] { "tenant_id", "property_id", "start_date" });
            migrationBuilder.Sql("""
CREATE FUNCTION bf_management_assignment_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM 1 FROM person_tenants WHERE id=NEW.tenant_id FOR UPDATE;
    PERFORM 1 FROM properties WHERE id=NEW.property_id FOR UPDATE;
    IF TG_OP='UPDATE' THEN
        IF (NEW.tenant_id,NEW.property_id,NEW.person_id,NEW.role,NEW.start_date) IS DISTINCT FROM (OLD.tenant_id,OLD.property_id,OLD.person_id,OLD.role,OLD.start_date)
            OR (OLD.end_date IS NOT NULL AND (NEW.end_date,NEW.end_reason) IS DISTINCT FROM (OLD.end_date,OLD.end_reason))
            OR (OLD.cancelled_at IS NOT NULL AND (NEW.cancelled_at,NEW.cancellation_reason,NEW.end_date,NEW.end_reason) IS DISTINCT FROM (OLD.cancelled_at,OLD.cancellation_reason,OLD.end_date,OLD.end_reason)) THEN
            RAISE EXCEPTION 'Management history is immutable' USING ERRCODE='23514';
        END IF;
    ELSE
        IF NOT EXISTS (SELECT 1 FROM persons WHERE tenant_id=NEW.tenant_id AND id=NEW.person_id AND NOT is_deleted AND is_enabled AND kind='NATURAL') THEN
            RAISE EXCEPTION 'Management person is unavailable' USING ERRCODE='23514';
        END IF;
    END IF;
    IF NOT NEW.is_deleted AND NEW.cancelled_at IS NULL AND EXISTS (
        SELECT 1 FROM management_assignments a WHERE a.tenant_id=NEW.tenant_id AND a.property_id=NEW.property_id AND a.person_id=NEW.person_id AND a.role=NEW.role AND a.id<>NEW.id AND NOT a.is_deleted AND a.cancelled_at IS NULL
        AND (NEW.end_date IS NULL OR a.start_date<=NEW.end_date) AND (a.end_date IS NULL OR a.end_date>=NEW.start_date)) THEN
        RAISE EXCEPTION 'Management periods overlap' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_management_assignment_guard BEFORE INSERT OR UPDATE ON management_assignments FOR EACH ROW EXECUTE FUNCTION bf_management_assignment_guard();
CREATE FUNCTION bf_management_audit_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'Management audit is immutable' USING ERRCODE='23514'; END $$;
CREATE TRIGGER trg_management_events_immutable BEFORE UPDATE OR DELETE ON management_assignment_events FOR EACH ROW EXECUTE FUNCTION bf_management_audit_immutable();
CREATE TRIGGER trg_management_operations_immutable BEFORE UPDATE OR DELETE ON management_assignment_operations FOR EACH ROW EXECUTE FUNCTION bf_management_audit_immutable();
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$ BEGIN
IF EXISTS(SELECT 1 FROM management_assignments) OR EXISTS(SELECT 1 FROM management_assignment_events) OR EXISTS(SELECT 1 FROM management_assignment_operations)
THEN RAISE EXCEPTION 'Management rollback would lose history'; END IF;
END $$;
DROP TRIGGER trg_management_assignment_guard ON management_assignments;
DROP FUNCTION bf_management_assignment_guard();
DROP TRIGGER trg_management_events_immutable ON management_assignment_events;
DROP TRIGGER trg_management_operations_immutable ON management_assignment_operations;
DROP FUNCTION bf_management_audit_immutable();
""");

            migrationBuilder.DropTable(
                name: "management_assignment_events");

            migrationBuilder.DropTable(
                name: "management_assignment_operations");

            migrationBuilder.DropTable(
                name: "management_assignments");
        }
    }
}
