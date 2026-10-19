using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WebsiteCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "website_sites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_key = table.Column<string>(type: "citext", maxLength: 63, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_demo = table.Column<bool>(type: "boolean", nullable: false),
                    template = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    primary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    accent_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
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
                    table.PrimaryKey("PK_website_sites", x => x.id);
                    table.UniqueConstraint("AK_website_sites_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_website_sites_values", "status IN ('DRAFT','PUBLISHED','SUSPENDED') AND template = 'PREMIUM_RESIDENCE' AND primary_color ~ '^#[0-9A-F]{6}$' AND accent_color ~ '^#[0-9A-F]{6}$' AND site_key ~ '^[a-z0-9]([a-z0-9-]{1,61}[a-z0-9])$' AND length(trim(display_name)) > 0");
                    table.ForeignKey(
                        name: "FK_website_sites_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "website_pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    menu_label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    show_in_menu = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    seo_title = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    seo_description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
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
                    table.PrimaryKey("PK_website_pages", x => x.id);
                    table.UniqueConstraint("AK_website_pages_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_website_pages_values", "kind IN ('HOME','ABOUT','UNITS','AMENITIES','NOTICES','GALLERY','CONTACT','PRIVACY','COOKIES','CUSTOM') AND sort_order BETWEEN 0 AND 1000 AND ((kind = 'HOME' AND slug = '') OR (kind <> 'HOME' AND slug ~ '^[a-z0-9]([a-z0-9-]{0,58}[a-z0-9])?$')) AND length(trim(title)) > 0");
                    table.ForeignKey(
                        name: "FK_website_pages_website_sites_property_id_site_id",
                        columns: x => new { x.property_id, x.site_id },
                        principalTable: "website_sites",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "website_publications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    snapshot = table.Column<string>(type: "text", nullable: false),
                    snapshot_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_publication_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_website_publications", x => x.id);
                    table.CheckConstraint("ck_website_publications_values", "number > 0 AND snapshot_sha256 ~ '^[0-9a-f]{64}$' AND length(snapshot) > 0");
                    table.ForeignKey(
                        name: "FK_website_publications_website_publications_source_publicatio~",
                        column: x => x.source_publication_id,
                        principalTable: "website_publications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_website_publications_website_sites_property_id_site_id",
                        columns: x => new { x.property_id, x.site_id },
                        principalTable: "website_sites",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "website_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_website_sections", x => x.id);
                    table.CheckConstraint("ck_website_sections_values", "type IN ('HERO','FEATURES','LISTINGS','NOTICES','AMENITIES','GALLERY','STATS','CONTACT','LOGIN_CARD','RICH_TEXT','VIDEO_LINK') AND sort_order BETWEEN 0 AND 1000 AND jsonb_typeof(data) = 'object' AND length(data::text) <= 16384");
                    table.ForeignKey(
                        name: "FK_website_sections_website_pages_property_id_page_id",
                        columns: x => new { x.property_id, x.page_id },
                        principalTable: "website_pages",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_website_pages_property_id_site_id",
                table: "website_pages",
                columns: new[] { "property_id", "site_id" });

            migrationBuilder.CreateIndex(
                name: "IX_website_pages_site_id_required_kind",
                table: "website_pages",
                columns: new[] { "site_id", "kind" },
                unique: true,
                filter: "is_deleted = false AND kind IN ('HOME','PRIVACY','COOKIES')");

            migrationBuilder.CreateIndex(
                name: "IX_website_pages_site_id_slug",
                table: "website_pages",
                columns: new[] { "site_id", "slug" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_website_pages_site_id_sort_order",
                table: "website_pages",
                columns: new[] { "site_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_website_publications_property_id_site_id",
                table: "website_publications",
                columns: new[] { "property_id", "site_id" });

            migrationBuilder.CreateIndex(
                name: "IX_website_publications_site_id_active",
                table: "website_publications",
                column: "site_id",
                unique: true,
                filter: "is_active = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_website_publications_site_id_number",
                table: "website_publications",
                columns: new[] { "site_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_website_publications_site_id_request_id",
                table: "website_publications",
                columns: new[] { "site_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_website_publications_source_publication_id",
                table: "website_publications",
                column: "source_publication_id");

            migrationBuilder.CreateIndex(
                name: "IX_website_sections_page_id_sort_order",
                table: "website_sections",
                columns: new[] { "page_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_website_sections_property_id_page_id",
                table: "website_sections",
                columns: new[] { "property_id", "page_id" });

            migrationBuilder.CreateIndex(
                name: "IX_website_sites_property_id",
                table: "website_sites",
                column: "property_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_website_sites_request_id",
                table: "website_sites",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_website_sites_site_key",
                table: "website_sites",
                column: "site_key",
                unique: true,
                filter: "is_deleted = false");
            migrationBuilder.Sql("""
CREATE FUNCTION bf_website_publication_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Website publications are immutable' USING ERRCODE='23514'; END IF;
    IF (NEW.property_id,NEW.site_id,NEW.request_id,NEW.number,NEW.snapshot,NEW.snapshot_sha256,NEW.published_at,NEW.note,NEW.source_publication_id,NEW.created_at,NEW.created_by,NEW.is_deleted)
        IS DISTINCT FROM (OLD.property_id,OLD.site_id,OLD.request_id,OLD.number,OLD.snapshot,OLD.snapshot_sha256,OLD.published_at,OLD.note,OLD.source_publication_id,OLD.created_at,OLD.created_by,OLD.is_deleted)
        OR (NEW.is_active AND NOT OLD.is_active) THEN
        RAISE EXCEPTION 'Website publications are immutable' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_website_publication_guard BEFORE UPDATE OR DELETE ON website_publications FOR EACH ROW EXECUTE FUNCTION bf_website_publication_guard();
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$ BEGIN
IF EXISTS(SELECT 1 FROM website_sites) OR EXISTS(SELECT 1 FROM website_publications)
THEN RAISE EXCEPTION 'Website rollback would lose site content or publication history'; END IF;
END $$;
DROP TRIGGER trg_website_publication_guard ON website_publications;
DROP FUNCTION bf_website_publication_guard();
""");

            migrationBuilder.DropTable(
                name: "website_publications");

            migrationBuilder.DropTable(
                name: "website_sections");

            migrationBuilder.DropTable(
                name: "website_pages");

            migrationBuilder.DropTable(
                name: "website_sites");
        }
    }
}
