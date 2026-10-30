using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierWorkOrderNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_work_order_revision_valid",
                table: "supplier_work_order_revisions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_work_order_revision_valid",
                table: "supplier_work_order_revisions",
                sql: "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','EDIT','ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED','CANCELLED','PHOTO','NOTE')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM supplier_work_order_revisions WHERE action='NOTE') THEN RAISE EXCEPTION 'Work order notes exist; rollback would lose history'; END IF; END $$;");
            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_work_order_revision_valid",
                table: "supplier_work_order_revisions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_work_order_revision_valid",
                table: "supplier_work_order_revisions",
                sql: "applied_version > 0 AND length(fingerprint) = 64 AND action IN ('CREATE','EDIT','ASSIGNED','ACCEPTED','IN_PROGRESS','COMPLETED','VERIFIED','CLOSED','REJECTED','CANCELLED','PHOTO')");
        }
    }
}
