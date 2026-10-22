using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LeaseContractExclusivityAndOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lease_contract_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_lease_contract_operations", x => x.id);
                    table.CheckConstraint("ck_lease_contract_operation", "action IN ('CREATE','ADD_PARTY','ATTACH','END') AND applied_version>0 AND length(fingerprint)=64");
                    table.ForeignKey(
                        name: "FK_lease_contract_operations_lease_contracts_tenant_id_propert~",
                        columns: x => new { x.tenant_id, x.property_id, x.unit_id, x.contract_id },
                        principalTable: "lease_contracts",
                        principalColumns: new[] { "tenant_id", "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lease_contract_operations_tenant_id_actor_issuer_actor_subj~",
                table: "lease_contract_operations",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lease_contract_operations_tenant_id_property_id_unit_id_con~",
                table: "lease_contract_operations",
                columns: new[] { "tenant_id", "property_id", "unit_id", "contract_id" });
            // One lease per unit and period (user decision 22.10.2026). Existing overlaps stop the upgrade instead of being kept silently.
            migrationBuilder.Sql("""
DO $$ BEGIN
IF EXISTS (SELECT 1 FROM lease_contracts a JOIN lease_contracts b ON a.tenant_id=b.tenant_id AND a.property_id=b.property_id AND a.unit_id=b.unit_id AND a.id<b.id
    WHERE NOT a.is_deleted AND NOT b.is_deleted AND (a.end_date IS NULL OR b.start_date<=a.end_date) AND (b.end_date IS NULL OR a.start_date<=b.end_date))
THEN RAISE EXCEPTION 'Overlapping lease contracts must be resolved before upgrade'; END IF;
END $$;
CREATE FUNCTION bf_lease_overlap_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.is_deleted THEN RETURN NEW; END IF;
    PERFORM 1 FROM units WHERE property_id=NEW.property_id AND id=NEW.unit_id FOR UPDATE;
    IF EXISTS (SELECT 1 FROM lease_contracts c WHERE c.tenant_id=NEW.tenant_id AND c.property_id=NEW.property_id AND c.unit_id=NEW.unit_id AND c.id<>NEW.id AND NOT c.is_deleted
        AND (NEW.end_date IS NULL OR c.start_date<=NEW.end_date) AND (c.end_date IS NULL OR c.end_date>=NEW.start_date)) THEN
        RAISE EXCEPTION 'Overlapping lease contract' USING ERRCODE='23P01';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_lease_overlap_guard AFTER INSERT OR UPDATE ON lease_contracts FOR EACH ROW EXECUTE FUNCTION bf_lease_overlap_guard();
CREATE FUNCTION lease_contract_operation_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'Lease contract operation receipts are immutable' USING ERRCODE='23514'; END $$;
CREATE TRIGGER lease_contract_operation_immutable BEFORE UPDATE OR DELETE ON lease_contract_operations FOR EACH ROW EXECUTE FUNCTION lease_contract_operation_immutable();
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$ BEGIN IF EXISTS(SELECT 1 FROM lease_contract_operations) THEN RAISE EXCEPTION 'Cannot remove populated lease operation history'; END IF; END $$;
DROP TRIGGER lease_contract_operation_immutable ON lease_contract_operations;
DROP FUNCTION lease_contract_operation_immutable();
DROP TRIGGER trg_lease_overlap_guard ON lease_contracts;
DROP FUNCTION bf_lease_overlap_guard();
""");
            migrationBuilder.DropTable(
                name: "lease_contract_operations");
        }
    }
}
