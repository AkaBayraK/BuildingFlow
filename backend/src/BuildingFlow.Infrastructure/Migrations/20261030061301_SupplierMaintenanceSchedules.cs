using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierMaintenanceSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_holidays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_supplier_holidays", x => x.id);
                    table.CheckConstraint("ck_supplier_holiday_valid", "length(trim(name)) > 0 AND date >= DATE '2000-01-01' AND date <= DATE '2100-12-31'");
                    table.ForeignKey(
                        name: "FK_supplier_holidays_person_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "person_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_maintenance_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reminder_days = table.Column<int>(type: "integer", nullable: false),
                    non_working_day_rule = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    end_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_supplier_maintenance_schedules", x => x.id);
                    table.UniqueConstraint("AK_supplier_maintenance_schedules_tenant_id_supplier_id_proper~", x => new { x.tenant_id, x.supplier_id, x.property_id, x.id });
                    table.CheckConstraint("ck_supplier_maintenance_schedule_valid", "length(trim(title)) > 0 AND type IN ('MAINTENANCE','CLEANING','INSPECTION','DRILL','OTHER') AND frequency IN ('WEEKLY','MONTHLY','QUARTERLY','SEMIANNUAL','YEARLY') AND non_working_day_rule IN ('KEEP','NEXT_WORKING_DAY','PREVIOUS_WORKING_DAY') AND start_date >= DATE '2000-01-01' AND end_date <= DATE '2100-12-31' AND end_date >= start_date AND end_date - start_date <= 731 AND reminder_days BETWEEN 0 AND 60 AND status IN ('ACTIVE','ENDED') AND (status <> 'ENDED' OR end_reason IS NOT NULL AND length(trim(end_reason)) > 0)");
                    table.ForeignKey(
                        name: "FK_supplier_maintenance_schedules_supplier_area_services_tenan~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.area_service_id },
                        principalTable: "supplier_area_services",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_maintenance_schedules_supplier_contracts_tenant_id~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.contract_id },
                        principalTable: "supplier_contracts",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_maintenance_occurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    nominal_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_date = table.Column<DateOnly>(type: "date", nullable: false),
                    maintenance_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    completed_on = table.Column<DateOnly>(type: "date", nullable: true),
                    delay_days = table.Column<int>(type: "integer", nullable: true),
                    completion_note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    skip_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_supplier_maintenance_occurrences", x => x.id);
                    table.UniqueConstraint("AK_supplier_maintenance_occurrences_tenant_id_supplier_id_prop~", x => new { x.tenant_id, x.supplier_id, x.property_id, x.id });
                    table.CheckConstraint("ck_supplier_maintenance_occurrence_valid", "sequence > 0 AND status IN ('PLANNED','COMPLETED','SKIPPED') AND (status = 'COMPLETED') = (completed_on IS NOT NULL AND delay_days IS NOT NULL AND delay_days >= 0 AND completion_note IS NOT NULL AND length(trim(completion_note)) > 0) AND (status = 'SKIPPED') = (skip_reason IS NOT NULL AND length(trim(skip_reason)) > 0) AND (work_order_id IS NULL OR status = 'COMPLETED')");
                    table.ForeignKey(
                        name: "FK_supplier_maintenance_occurrences_maintenance_plans_property~",
                        columns: x => new { x.property_id, x.maintenance_plan_id },
                        principalTable: "maintenance_plans",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_maintenance_occurrences_supplier_maintenance_sched~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.schedule_id },
                        principalTable: "supplier_maintenance_schedules",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_maintenance_occurrences_supplier_work_orders_tenan~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.work_order_id },
                        principalTable: "supplier_work_orders",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_maintenance_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrence_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_supplier_maintenance_revisions", x => x.id);
                    table.CheckConstraint("ck_supplier_maintenance_revision_valid", "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','END','COMPLETE','SKIP') AND (occurrence_id IS NULL) = (action IN ('CREATE','END'))");
                    table.ForeignKey(
                        name: "FK_supplier_maintenance_revisions_supplier_maintenance_occurre~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.occurrence_id },
                        principalTable: "supplier_maintenance_occurrences",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_maintenance_revisions_supplier_maintenance_schedul~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id, x.schedule_id },
                        principalTable: "supplier_maintenance_schedules",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_holidays_tenant_id_date",
                table: "supplier_holidays",
                columns: new[] { "tenant_id", "date" },
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_occurrences_maintenance_plan_id",
                table: "supplier_maintenance_occurrences",
                column: "maintenance_plan_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_occurrences_property_id_maintenance_pl~",
                table: "supplier_maintenance_occurrences",
                columns: new[] { "property_id", "maintenance_plan_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_occurrences_schedule_id_sequence",
                table: "supplier_maintenance_occurrences",
                columns: new[] { "schedule_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_occurrences_tenant_id_property_id_plan~",
                table: "supplier_maintenance_occurrences",
                columns: new[] { "tenant_id", "property_id", "planned_date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_occurrences_tenant_id_supplier_id_pro~1",
                table: "supplier_maintenance_occurrences",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "work_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_occurrences_tenant_id_supplier_id_prop~",
                table: "supplier_maintenance_occurrences",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "schedule_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_revisions_schedule_id_created_at",
                table: "supplier_maintenance_revisions",
                columns: new[] { "schedule_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_revisions_tenant_id_actor_issuer_actor~",
                table: "supplier_maintenance_revisions",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_revisions_tenant_id_supplier_id_prope~1",
                table: "supplier_maintenance_revisions",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "schedule_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_revisions_tenant_id_supplier_id_proper~",
                table: "supplier_maintenance_revisions",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "occurrence_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_schedules_tenant_id_supplier_id_prope~1",
                table: "supplier_maintenance_schedules",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_schedules_tenant_id_supplier_id_prope~2",
                table: "supplier_maintenance_schedules",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "status", "start_date" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_maintenance_schedules_tenant_id_supplier_id_proper~",
                table: "supplier_maintenance_schedules",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "area_service_id" });
            migrationBuilder.Sql("""
                CREATE FUNCTION guard_supplier_maintenance_schedule() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP='DELETE' OR NEW.is_deleted OR NOT NEW.is_enabled OR
                       (NEW.tenant_id,NEW.supplier_id,NEW.property_id,NEW.area_service_id,NEW.contract_id,NEW.title,NEW.description,NEW.type,NEW.frequency,NEW.start_date,NEW.end_date,NEW.reminder_days,NEW.non_working_day_rule)
                       IS DISTINCT FROM (OLD.tenant_id,OLD.supplier_id,OLD.property_id,OLD.area_service_id,OLD.contract_id,OLD.title,OLD.description,OLD.type,OLD.frequency,OLD.start_date,OLD.end_date,OLD.reminder_days,OLD.non_working_day_rule) OR
                       ((NEW.status,NEW.end_reason) IS DISTINCT FROM (OLD.status,OLD.end_reason) AND NOT (OLD.status='ACTIVE' AND NEW.status='ENDED')) THEN
                        RAISE EXCEPTION 'Maintenance schedule history is immutable' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_maintenance_schedule_guard BEFORE UPDATE OR DELETE ON supplier_maintenance_schedules FOR EACH ROW EXECUTE FUNCTION guard_supplier_maintenance_schedule();
                CREATE FUNCTION guard_supplier_maintenance_occurrence() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP='DELETE' OR NEW.is_deleted OR NOT NEW.is_enabled OR
                       (NEW.tenant_id,NEW.supplier_id,NEW.property_id,NEW.schedule_id,NEW.sequence,NEW.nominal_date,NEW.planned_date,NEW.maintenance_plan_id)
                       IS DISTINCT FROM (OLD.tenant_id,OLD.supplier_id,OLD.property_id,OLD.schedule_id,OLD.sequence,OLD.nominal_date,OLD.planned_date,OLD.maintenance_plan_id) OR
                       ((NEW.status,NEW.completed_on,NEW.delay_days,NEW.completion_note,NEW.work_order_id,NEW.skip_reason) IS DISTINCT FROM (OLD.status,OLD.completed_on,OLD.delay_days,OLD.completion_note,OLD.work_order_id,OLD.skip_reason)
                        AND NOT (OLD.status='PLANNED' AND NEW.status IN ('COMPLETED','SKIPPED'))) THEN
                        RAISE EXCEPTION 'Maintenance occurrence evidence is immutable' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_maintenance_occurrence_guard BEFORE UPDATE OR DELETE ON supplier_maintenance_occurrences FOR EACH ROW EXECUTE FUNCTION guard_supplier_maintenance_occurrence();
                CREATE TRIGGER supplier_maintenance_revision_immutable BEFORE UPDATE OR DELETE ON supplier_maintenance_revisions FOR EACH ROW EXECUTE FUNCTION reject_supplier_inquiry_revision_mutation();
                CREATE FUNCTION guard_supplier_holiday() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP='DELETE' OR (NEW.tenant_id,NEW.date,NEW.name) IS DISTINCT FROM (OLD.tenant_id,OLD.date,OLD.name) OR (OLD.is_deleted AND NOT NEW.is_deleted) THEN
                        RAISE EXCEPTION 'Holiday entries can only be retired' USING ERRCODE='23514';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER supplier_holiday_guard BEFORE UPDATE OR DELETE ON supplier_holidays FOR EACH ROW EXECUTE FUNCTION guard_supplier_holiday();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM supplier_maintenance_schedules) OR EXISTS(SELECT 1 FROM supplier_holidays) THEN
                        RAISE EXCEPTION 'Maintenance schedules or holidays exist; rollback would lose data';
                    END IF;
                END $$;
                DROP FUNCTION guard_supplier_maintenance_schedule() CASCADE;
                DROP FUNCTION guard_supplier_maintenance_occurrence() CASCADE;
                DROP FUNCTION guard_supplier_holiday() CASCADE;
                DROP TRIGGER supplier_maintenance_revision_immutable ON supplier_maintenance_revisions;
                """);
            migrationBuilder.DropTable(
                name: "supplier_holidays");

            migrationBuilder.DropTable(
                name: "supplier_maintenance_revisions");

            migrationBuilder.DropTable(
                name: "supplier_maintenance_occurrences");

            migrationBuilder.DropTable(
                name: "supplier_maintenance_schedules");
        }
    }
}
