using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WebsiteMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "hero_media_id",
                table: "website_sites",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "logo_media_id",
                table: "website_sites",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "website_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    object_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    size = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    alt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rights_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rights_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    contains_people = table.Column<bool>(type: "boolean", nullable: false),
                    people_consent_confirmed = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_website_media", x => x.id);
                    table.UniqueConstraint("AK_website_media_property_id_site_id_id", x => new { x.property_id, x.site_id, x.id });
                    table.CheckConstraint("ck_website_media_values", "content_type IN ('image/jpeg','image/png') AND size BETWEEN 1 AND 5242880 AND width BETWEEN 1 AND 4096 AND height BETWEEN 1 AND 4096 AND sha256 ~ '^[0-9a-f]{64}$' AND fingerprint ~ '^[0-9a-f]{64}$' AND length(trim(alt_text))>0 AND rights_basis IN ('OWN_WORK','LICENSED','PERMISSION') AND (rights_basis='OWN_WORK' OR length(trim(rights_note))>0 AND rights_note IS NOT NULL) AND (NOT contains_people OR people_consent_confirmed)");
                    table.ForeignKey(
                        name: "FK_website_media_website_sites_property_id_site_id",
                        columns: x => new { x.property_id, x.site_id },
                        principalTable: "website_sites",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_website_sites_property_id_id_hero_media_id",
                table: "website_sites",
                columns: new[] { "property_id", "id", "hero_media_id" });

            migrationBuilder.CreateIndex(
                name: "IX_website_sites_property_id_id_logo_media_id",
                table: "website_sites",
                columns: new[] { "property_id", "id", "logo_media_id" });

            migrationBuilder.CreateIndex(
                name: "IX_website_media_site_id_request_id",
                table: "website_media",
                columns: new[] { "site_id", "request_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_website_sites_website_media_property_id_id_hero_media_id",
                table: "website_sites",
                columns: new[] { "property_id", "id", "hero_media_id" },
                principalTable: "website_media",
                principalColumns: new[] { "property_id", "site_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_website_sites_website_media_property_id_id_logo_media_id",
                table: "website_sites",
                columns: new[] { "property_id", "id", "logo_media_id" },
                principalTable: "website_media",
                principalColumns: new[] { "property_id", "site_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM website_media) THEN RAISE EXCEPTION 'Website media rollback would lose data.'; END IF; END $$;");
            migrationBuilder.DropForeignKey(
                name: "FK_website_sites_website_media_property_id_id_hero_media_id",
                table: "website_sites");

            migrationBuilder.DropForeignKey(
                name: "FK_website_sites_website_media_property_id_id_logo_media_id",
                table: "website_sites");

            migrationBuilder.DropTable(
                name: "website_media");

            migrationBuilder.DropIndex(
                name: "IX_website_sites_property_id_id_hero_media_id",
                table: "website_sites");

            migrationBuilder.DropIndex(
                name: "IX_website_sites_property_id_id_logo_media_id",
                table: "website_sites");

            migrationBuilder.DropColumn(
                name: "hero_media_id",
                table: "website_sites");

            migrationBuilder.DropColumn(
                name: "logo_media_id",
                table: "website_sites");
        }
    }
}
