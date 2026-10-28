using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommonAreaPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "common_area_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    common_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rights_confirmed = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_common_area_photos", x => x.id);
                    table.CheckConstraint("ck_common_area_photo_rights", "rights_confirmed = true");
                    table.ForeignKey(
                        name: "FK_common_area_photos_common_areas_property_id_common_area_id",
                        columns: x => new { x.property_id, x.common_area_id },
                        principalTable: "common_areas",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_common_area_photos_stored_documents_property_id_document_id",
                        columns: x => new { x.property_id, x.document_id },
                        principalTable: "stored_documents",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_common_area_photos_property_id_common_area_id_document_id",
                table: "common_area_photos",
                columns: new[] { "property_id", "common_area_id", "document_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_common_area_photos_property_id_document_id",
                table: "common_area_photos",
                columns: new[] { "property_id", "document_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                  IF EXISTS (SELECT 1 FROM common_area_photos) THEN
                    RAISE EXCEPTION 'Common area photo history exists; use a forward migration.';
                  END IF;
                END $guard$;
                """);
            migrationBuilder.DropTable(
                name: "common_area_photos");
        }
    }
}
