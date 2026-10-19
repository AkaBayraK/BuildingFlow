using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WebsiteDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "website_domains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_name = table.Column<string>(type: "citext", maxLength: 253, nullable: false),
                    kind = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    verification_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tls_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_website_domains", x => x.id);
                    table.CheckConstraint("ck_website_domain_state", "status IN ('PENDING','VERIFIED','DISABLED') AND kind IN ('CUSTOM','PLATFORM_SUBDOMAIN') AND verification_token_hash ~ '^[0-9a-f]{64}$' AND (NOT is_primary OR status='VERIFIED') AND (status<>'VERIFIED' OR (verified_at IS NOT NULL AND tls_checked_at IS NOT NULL AND last_checked_at IS NOT NULL AND (kind<>'CUSTOM' OR approved_at IS NOT NULL)))");
                    table.ForeignKey(
                        name: "FK_website_domains_website_sites_property_id_site_id",
                        columns: x => new { x.property_id, x.site_id },
                        principalTable: "website_sites",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_website_domains_host_name",
                table: "website_domains",
                column: "host_name",
                unique: true,
                filter: "is_deleted=false");

            migrationBuilder.CreateIndex(
                name: "IX_website_domains_property_id_site_id",
                table: "website_domains",
                columns: new[] { "property_id", "site_id" });

            migrationBuilder.CreateIndex(
                name: "IX_website_domains_site_id",
                table: "website_domains",
                column: "site_id",
                unique: true,
                filter: "is_deleted=false AND is_primary=true");

            migrationBuilder.CreateIndex(
                name: "IX_website_domains_site_id_request_id",
                table: "website_domains",
                columns: new[] { "site_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $guard$ BEGIN IF EXISTS(SELECT 1 FROM website_domains) THEN RAISE EXCEPTION 'WebsiteDomains contains ownership history'; END IF; END $guard$;");
            migrationBuilder.DropTable(
                name: "website_domains");
        }
    }
}
