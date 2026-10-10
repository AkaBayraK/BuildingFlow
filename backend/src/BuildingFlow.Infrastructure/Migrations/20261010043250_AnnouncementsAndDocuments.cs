using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnnouncementsAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "announcements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_announcements", x => x.id);
                    table.UniqueConstraint("AK_announcements_property_id_id", x => new { x.property_id, x.id });
                    table.CheckConstraint("ck_announcements_content", "length(trim(title)) > 0 AND length(trim(content)) > 0 AND audience IN ('ALL_MEMBERS','MANAGEMENT')");
                    table.CheckConstraint("ck_announcements_publication", "(status = 'DRAFT' AND published_at IS NULL) OR (status IN ('PUBLISHED','ARCHIVED') AND published_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_announcements_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stored_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    object_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_stored_documents", x => x.id);
                    table.CheckConstraint("ck_documents_file", "size BETWEEN 1 AND 10485760 AND content_type IN ('application/pdf','image/jpeg','image/png') AND audience IN ('ALL_MEMBERS','MANAGEMENT') AND length(sha256) = 64 AND length(request_fingerprint) = 64 AND length(trim(file_name)) > 0");
                    table.ForeignKey(
                        name: "FK_stored_documents_announcements_property_id_announcement_id",
                        columns: x => new { x.property_id, x.announcement_id },
                        principalTable: "announcements",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stored_documents_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_property_id_status_created_at",
                table: "announcements",
                columns: new[] { "property_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stored_documents_object_key",
                table: "stored_documents",
                column: "object_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stored_documents_property_id_announcement_id_created_at",
                table: "stored_documents",
                columns: new[] { "property_id", "announcement_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stored_documents_property_id_request_id",
                table: "stored_documents",
                columns: new[] { "property_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM announcements) OR EXISTS (SELECT 1 FROM stored_documents) THEN
                    RAISE EXCEPTION 'Announcement/document history cannot be removed by rollback.';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "stored_documents");

            migrationBuilder.DropTable(
                name: "announcements");
        }
    }
}
