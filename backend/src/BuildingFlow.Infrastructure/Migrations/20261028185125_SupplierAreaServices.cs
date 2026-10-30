using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierAreaServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_supplier_services_tenant_id_supplier_id_id",
                table: "supplier_services",
                columns: new[] { "tenant_id", "supplier_id", "id" });

            migrationBuilder.CreateTable(
                name: "supplier_area_services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_supplier_area_services", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_area_services_supplier_management_areas_tenant_id_~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.property_id },
                        principalTable: "supplier_management_areas",
                        principalColumns: new[] { "tenant_id", "supplier_id", "property_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_area_services_supplier_services_tenant_id_supplier~",
                        columns: x => new { x.tenant_id, x.supplier_id, x.service_id },
                        principalTable: "supplier_services",
                        principalColumns: new[] { "tenant_id", "supplier_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_area_services_tenant_id_supplier_id_property_id_se~",
                table: "supplier_area_services",
                columns: new[] { "tenant_id", "supplier_id", "property_id", "service_id" },
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_area_services_tenant_id_supplier_id_service_id",
                table: "supplier_area_services",
                columns: new[] { "tenant_id", "supplier_id", "service_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM supplier_area_services) THEN
                        RAISE EXCEPTION 'Supplier area service history exists; rollback would lose data';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "supplier_area_services");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_supplier_services_tenant_id_supplier_id_id",
                table: "supplier_services");
        }
    }
}
