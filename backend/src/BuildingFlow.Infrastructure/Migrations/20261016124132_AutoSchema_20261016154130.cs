using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutoSchema_20261016154130 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "person_property_scopes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_person_property_scopes", x => x.id);
                    table.UniqueConstraint("AK_person_property_scopes_tenant_id_property_id", x => new { x.tenant_id, x.property_id });
                    table.ForeignKey(
                        name: "FK_person_property_scopes_person_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "person_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_person_property_scopes_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "legacy_person_maps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_version = table.Column<long>(type: "bigint", nullable: false),
                    local_profile = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_legacy_person_maps", x => x.id);
                    table.CheckConstraint("ck_legacy_person_version", "source_version>0");
                    table.ForeignKey(
                        name: "FK_legacy_person_maps_people_property_id_legacy_person_id",
                        columns: x => new { x.property_id, x.legacy_person_id },
                        principalTable: "people",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_legacy_person_maps_person_property_scopes_tenant_id_propert~",
                        columns: x => new { x.tenant_id, x.property_id },
                        principalTable: "person_property_scopes",
                        principalColumns: new[] { "tenant_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_legacy_person_maps_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_ownerships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary_contact = table.Column<bool>(type: "boolean", nullable: false),
                    share_numerator = table.Column<long>(type: "bigint", nullable: true),
                    share_denominator = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_unit_ownerships", x => x.id);
                    table.UniqueConstraint("AK_unit_ownerships_tenant_id_property_id_unit_id_id", x => new { x.tenant_id, x.property_id, x.unit_id, x.id });
                    table.CheckConstraint("ck_ownership_share", "(share_numerator IS NULL AND share_denominator IS NULL) OR (share_numerator IS NOT NULL AND share_denominator IS NOT NULL AND share_numerator>0 AND share_denominator>0 AND share_numerator<=share_denominator)");
                    table.CheckConstraint("ck_unit_ownerships_dates", "start_date > DATE '0001-01-01' AND (end_date IS NULL OR end_date>=start_date)");
                    table.ForeignKey(
                        name: "FK_unit_ownerships_person_property_scopes_tenant_id_property_id",
                        columns: x => new { x.tenant_id, x.property_id },
                        principalTable: "person_property_scopes",
                        principalColumns: new[] { "tenant_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_ownerships_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_ownerships_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_residencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary_contact = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_unit_residencies", x => x.id);
                    table.UniqueConstraint("AK_unit_residencies_tenant_id_property_id_unit_id_id", x => new { x.tenant_id, x.property_id, x.unit_id, x.id });
                    table.CheckConstraint("ck_unit_residencies_dates", "start_date > DATE '0001-01-01' AND (end_date IS NULL OR end_date>=start_date)");
                    table.ForeignKey(
                        name: "FK_unit_residencies_person_property_scopes_tenant_id_property_~",
                        columns: x => new { x.tenant_id, x.property_id },
                        principalTable: "person_property_scopes",
                        principalColumns: new[] { "tenant_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_residencies_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_residencies_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_tenancies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary_contact = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_unit_tenancies", x => x.id);
                    table.UniqueConstraint("AK_unit_tenancies_tenant_id_property_id_unit_id_id", x => new { x.tenant_id, x.property_id, x.unit_id, x.id });
                    table.CheckConstraint("ck_unit_tenancies_dates", "start_date > DATE '0001-01-01' AND (end_date IS NULL OR end_date>=start_date)");
                    table.ForeignKey(
                        name: "FK_unit_tenancies_person_property_scopes_tenant_id_property_id",
                        columns: x => new { x.tenant_id, x.property_id },
                        principalTable: "person_property_scopes",
                        principalColumns: new[] { "tenant_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_tenancies_persons_tenant_id_person_id",
                        columns: x => new { x.tenant_id, x.person_id },
                        principalTable: "persons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_tenancies_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "legacy_occupancy_maps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_occupancy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_version = table.Column<long>(type: "bigint", nullable: false),
                    ownership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenancy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    residency_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_legacy_occupancy_maps", x => x.id);
                    table.CheckConstraint("ck_legacy_occupancy_target", "source_version>0 AND num_nonnulls(ownership_id,tenancy_id,residency_id)=1");
                    table.ForeignKey(
                        name: "FK_legacy_occupancy_maps_unit_occupancies_property_id_unit_id_~",
                        columns: x => new { x.property_id, x.unit_id, x.legacy_occupancy_id },
                        principalTable: "unit_occupancies",
                        principalColumns: new[] { "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_legacy_occupancy_maps_unit_ownerships_tenant_id_property_id~",
                        columns: x => new { x.tenant_id, x.property_id, x.unit_id, x.ownership_id },
                        principalTable: "unit_ownerships",
                        principalColumns: new[] { "tenant_id", "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_legacy_occupancy_maps_unit_residencies_tenant_id_property_i~",
                        columns: x => new { x.tenant_id, x.property_id, x.unit_id, x.residency_id },
                        principalTable: "unit_residencies",
                        principalColumns: new[] { "tenant_id", "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_legacy_occupancy_maps_unit_tenancies_tenant_id_property_id_~",
                        columns: x => new { x.tenant_id, x.property_id, x.unit_id, x.tenancy_id },
                        principalTable: "unit_tenancies",
                        principalColumns: new[] { "tenant_id", "property_id", "unit_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_legacy_occupancy_maps_legacy_occupancy_id",
                table: "legacy_occupancy_maps",
                column: "legacy_occupancy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legacy_occupancy_maps_property_id_unit_id_legacy_occupancy_~",
                table: "legacy_occupancy_maps",
                columns: new[] { "property_id", "unit_id", "legacy_occupancy_id" });

            migrationBuilder.CreateIndex(
                name: "IX_legacy_occupancy_maps_tenant_id_property_id_unit_id_ownersh~",
                table: "legacy_occupancy_maps",
                columns: new[] { "tenant_id", "property_id", "unit_id", "ownership_id" });

            migrationBuilder.CreateIndex(
                name: "IX_legacy_occupancy_maps_tenant_id_property_id_unit_id_residen~",
                table: "legacy_occupancy_maps",
                columns: new[] { "tenant_id", "property_id", "unit_id", "residency_id" });

            migrationBuilder.CreateIndex(
                name: "IX_legacy_occupancy_maps_tenant_id_property_id_unit_id_tenancy~",
                table: "legacy_occupancy_maps",
                columns: new[] { "tenant_id", "property_id", "unit_id", "tenancy_id" });

            migrationBuilder.CreateIndex(
                name: "IX_legacy_person_maps_legacy_person_id",
                table: "legacy_person_maps",
                column: "legacy_person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legacy_person_maps_person_id",
                table: "legacy_person_maps",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legacy_person_maps_property_id_legacy_person_id",
                table: "legacy_person_maps",
                columns: new[] { "property_id", "legacy_person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_legacy_person_maps_tenant_id_person_id",
                table: "legacy_person_maps",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_legacy_person_maps_tenant_id_property_id",
                table: "legacy_person_maps",
                columns: new[] { "tenant_id", "property_id" });

            migrationBuilder.CreateIndex(
                name: "IX_person_property_scopes_property_id",
                table: "person_property_scopes",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unit_ownerships_property_id_unit_id",
                table: "unit_ownerships",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_ownerships_tenant_id_person_id",
                table: "unit_ownerships",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_ownerships_tenant_id_property_id_unit_id_person_id",
                table: "unit_ownerships",
                columns: new[] { "tenant_id", "property_id", "unit_id", "person_id" },
                unique: true,
                filter: "is_deleted=false AND end_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_unit_residencies_property_id_unit_id",
                table: "unit_residencies",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_residencies_tenant_id_person_id",
                table: "unit_residencies",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_residencies_tenant_id_property_id_unit_id_person_id",
                table: "unit_residencies",
                columns: new[] { "tenant_id", "property_id", "unit_id", "person_id" },
                unique: true,
                filter: "is_deleted=false AND end_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_unit_tenancies_property_id_unit_id",
                table: "unit_tenancies",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_tenancies_tenant_id_person_id",
                table: "unit_tenancies",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_tenancies_tenant_id_property_id_unit_id_person_id",
                table: "unit_tenancies",
                columns: new[] { "tenant_id", "property_id", "unit_id", "person_id" },
                unique: true,
                filter: "is_deleted=false AND end_date IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legacy_occupancy_maps");

            migrationBuilder.DropTable(
                name: "legacy_person_maps");

            migrationBuilder.DropTable(
                name: "unit_ownerships");

            migrationBuilder.DropTable(
                name: "unit_residencies");

            migrationBuilder.DropTable(
                name: "unit_tenancies");

            migrationBuilder.DropTable(
                name: "person_property_scopes");
        }
    }
}
