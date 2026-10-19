using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PropertyListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "property_listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    room_layout = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    gross_area = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    net_area = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    floor_label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    building_label = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    features = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    publish_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consent_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    consent_object_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    consent_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    consent_content_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    consent_size = table.Column<int>(type: "integer", nullable: true),
                    agent_license_no = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    compliance_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    verification_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_property_listings", x => x.id);
                    table.UniqueConstraint("AK_property_listings_property_id_id", x => new { x.property_id, x.id });
                    table.UniqueConstraint("AK_property_listings_property_id_site_id_id", x => new { x.property_id, x.site_id, x.id });
                    table.CheckConstraint("ck_property_listing_values", "kind IN ('SALE','RENT') AND price>0 AND gross_area>0 AND net_area>0 AND net_area<=gross_area AND room_layout ~ '^[0-9]{1,2}\\+[0-9]$' AND status IN ('DRAFT','PENDING_APPROVAL','APPROVED','PUBLISHED','WITHDRAWN','EXPIRED') AND (status <> 'PUBLISHED' OR (publish_until IS NOT NULL AND verification_reference IS NOT NULL AND consent_object_key IS NOT NULL AND compliance_note IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_property_listings_units_property_id_unit_id",
                        columns: x => new { x.property_id, x.unit_id },
                        principalTable: "units",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_property_listings_website_sites_property_id_site_id",
                        columns: x => new { x.property_id, x.site_id },
                        principalTable: "website_sites",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "listing_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    listing_version = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_listing_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_listing_events_property_listings_property_id_listing_id",
                        columns: x => new { x.property_id, x.listing_id },
                        principalTable: "property_listings",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "listing_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_listing_photos", x => x.id);
                    table.CheckConstraint("ck_listing_photo_order", "sort_order BETWEEN 0 AND 19");
                    table.ForeignKey(
                        name: "FK_listing_photos_property_listings_property_id_site_id_listin~",
                        columns: x => new { x.property_id, x.site_id, x.listing_id },
                        principalTable: "property_listings",
                        principalColumns: new[] { "property_id", "site_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_photos_website_media_property_id_site_id_media_id",
                        columns: x => new { x.property_id, x.site_id, x.media_id },
                        principalTable: "website_media",
                        principalColumns: new[] { "property_id", "site_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "website_inquiries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: true),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    privacy_notice_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    retain_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                    table.PrimaryKey("PK_website_inquiries", x => x.id);
                    table.CheckConstraint("ck_website_inquiry_status", "status IN ('NEW','HANDLED','SPAM')");
                    table.ForeignKey(
                        name: "FK_website_inquiries_property_listings_property_id_site_id_lis~",
                        columns: x => new { x.property_id, x.site_id, x.listing_id },
                        principalTable: "property_listings",
                        principalColumns: new[] { "property_id", "site_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_website_inquiries_website_sites_property_id_site_id",
                        columns: x => new { x.property_id, x.site_id },
                        principalTable: "website_sites",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listing_events_property_id_listing_id",
                table: "listing_events",
                columns: new[] { "property_id", "listing_id" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_photos_listing_id_media_id",
                table: "listing_photos",
                columns: new[] { "listing_id", "media_id" },
                unique: true,
                filter: "is_deleted=false");

            migrationBuilder.CreateIndex(
                name: "IX_listing_photos_listing_id_sort_order",
                table: "listing_photos",
                columns: new[] { "listing_id", "sort_order" },
                unique: true,
                filter: "is_deleted=false");

            migrationBuilder.CreateIndex(
                name: "IX_listing_photos_property_id_site_id_listing_id",
                table: "listing_photos",
                columns: new[] { "property_id", "site_id", "listing_id" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_photos_property_id_site_id_media_id",
                table: "listing_photos",
                columns: new[] { "property_id", "site_id", "media_id" });

            migrationBuilder.CreateIndex(
                name: "IX_property_listings_property_id_unit_id",
                table: "property_listings",
                columns: new[] { "property_id", "unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_property_listings_site_id_request_id",
                table: "property_listings",
                columns: new[] { "site_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_website_inquiries_ip_hash_created_at",
                table: "website_inquiries",
                columns: new[] { "ip_hash", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_website_inquiries_property_id_site_id_listing_id",
                table: "website_inquiries",
                columns: new[] { "property_id", "site_id", "listing_id" });

            migrationBuilder.CreateIndex(
                name: "IX_website_inquiries_site_id_created_at",
                table: "website_inquiries",
                columns: new[] { "site_id", "created_at" });
            migrationBuilder.Sql("""
                CREATE FUNCTION bf_listing_event_immutable() RETURNS trigger LANGUAGE plpgsql AS $body$
                BEGIN RAISE EXCEPTION 'Listing events are immutable'; END; $body$;
                CREATE TRIGGER tr_listing_events_immutable BEFORE UPDATE OR DELETE ON listing_events
                FOR EACH ROW EXECUTE FUNCTION bf_listing_event_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                IF EXISTS (SELECT 1 FROM property_listings) OR EXISTS (SELECT 1 FROM listing_photos)
                   OR EXISTS (SELECT 1 FROM listing_events) OR EXISTS (SELECT 1 FROM website_inquiries)
                THEN RAISE EXCEPTION 'PropertyListings contains history; rollback would destroy data'; END IF;
                END $guard$;
                DROP TRIGGER tr_listing_events_immutable ON listing_events;
                DROP FUNCTION bf_listing_event_immutable();
                """);
            migrationBuilder.DropTable(
                name: "listing_events");

            migrationBuilder.DropTable(
                name: "listing_photos");

            migrationBuilder.DropTable(
                name: "website_inquiries");

            migrationBuilder.DropTable(
                name: "property_listings");
        }
    }
}
