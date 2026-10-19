using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersonCenterUnitOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_unit_tenancies_tenant_id_property_id_person_id_id",
                table: "unit_tenancies",
                columns: new[] { "tenant_id", "property_id", "person_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_unit_residencies_tenant_id_property_id_person_id_id",
                table: "unit_residencies",
                columns: new[] { "tenant_id", "property_id", "person_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_unit_ownerships_tenant_id_property_id_person_id_id",
                table: "unit_ownerships",
                columns: new[] { "tenant_id", "property_id", "person_id", "id" });

            migrationBuilder.CreateTable(
                name: "person_unit_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ownership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenancy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    residency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_person_unit_operations", x => x.id);
                    table.CheckConstraint("ck_person_unit_operation", "((kind='OWNER' AND ownership_id IS NOT NULL AND tenancy_id IS NULL AND residency_id IS NULL) OR (kind='TENANT' AND tenancy_id IS NOT NULL AND ownership_id IS NULL AND residency_id IS NULL) OR (kind='RESIDENT' AND residency_id IS NOT NULL AND ownership_id IS NULL AND tenancy_id IS NULL)) AND applied_version>0 AND length(fingerprint)=64");
                    table.ForeignKey(
                        name: "FK_person_unit_operations_person_property_scopes_tenant_id_pro~",
                        columns: x => new { x.tenant_id, x.property_id },
                        principalTable: "person_property_scopes",
                        principalColumns: new[] { "tenant_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_unit_operations_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_unit_operations_unit_ownerships_tenant_id_property_i~",
                        columns: x => new { x.tenant_id, x.property_id, x.person_id, x.ownership_id },
                        principalTable: "unit_ownerships",
                        principalColumns: new[] { "tenant_id", "property_id", "person_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_unit_operations_unit_residencies_tenant_id_property_~",
                        columns: x => new { x.tenant_id, x.property_id, x.person_id, x.residency_id },
                        principalTable: "unit_residencies",
                        principalColumns: new[] { "tenant_id", "property_id", "person_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_unit_operations_unit_tenancies_tenant_id_property_id~",
                        columns: x => new { x.tenant_id, x.property_id, x.person_id, x.tenancy_id },
                        principalTable: "unit_tenancies",
                        principalColumns: new[] { "tenant_id", "property_id", "person_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_person_unit_operations_tenant_id_actor_issuer_actor_subject~",
                table: "person_unit_operations",
                columns: new[] { "tenant_id", "actor_issuer", "actor_subject", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_unit_operations_tenant_id_person_id",
                table: "person_unit_operations",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_person_unit_operations_tenant_id_property_id_person_id_owne~",
                table: "person_unit_operations",
                columns: new[] { "tenant_id", "property_id", "person_id", "ownership_id" });

            migrationBuilder.CreateIndex(
                name: "IX_person_unit_operations_tenant_id_property_id_person_id_resi~",
                table: "person_unit_operations",
                columns: new[] { "tenant_id", "property_id", "person_id", "residency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_person_unit_operations_tenant_id_property_id_person_id_tena~",
                table: "person_unit_operations",
                columns: new[] { "tenant_id", "property_id", "person_id", "tenancy_id" });
            migrationBuilder.Sql("""
                CREATE FUNCTION person_unit_operation_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Person unit operation receipts are immutable' USING ERRCODE='23514'; END $$;
                CREATE TRIGGER person_unit_operation_immutable BEFORE UPDATE OR DELETE ON person_unit_operations
                  FOR EACH ROW EXECUTE FUNCTION person_unit_operation_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN IF EXISTS(SELECT 1 FROM person_unit_operations) THEN
                  RAISE EXCEPTION 'Cannot remove populated person unit operation history';
                END IF; END $$;
                DROP TRIGGER person_unit_operation_immutable ON person_unit_operations;
                DROP FUNCTION person_unit_operation_immutable();
                """);
            migrationBuilder.DropTable(
                name: "person_unit_operations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_unit_tenancies_tenant_id_property_id_person_id_id",
                table: "unit_tenancies");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_unit_residencies_tenant_id_property_id_person_id_id",
                table: "unit_residencies");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_unit_ownerships_tenant_id_property_id_person_id_id",
                table: "unit_ownerships");
        }
    }
}
