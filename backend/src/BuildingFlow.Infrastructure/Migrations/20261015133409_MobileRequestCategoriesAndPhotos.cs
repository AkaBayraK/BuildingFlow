using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MobileRequestCategoriesAndPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "mobile_requests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "OTHER");

            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "mobile_requests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "NORMAL");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_mobile_requests_property_id_id",
                table: "mobile_requests",
                columns: new[] { "property_id", "id" });

            migrationBuilder.CreateTable(
                name: "mobile_request_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    object_key = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    state = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                    table.PrimaryKey("PK_mobile_request_photos", x => x.id);
                    table.CheckConstraint("ck_request_photo_size", "size BETWEEN 1 AND 262144");
                    table.CheckConstraint("ck_request_photo_state", "state IN ('STAGED','CURRENT','RETIRED','PURGED')");
                    table.ForeignKey(
                        name: "FK_mobile_request_photos_mobile_requests_property_id_request_id",
                        columns: x => new { x.property_id, x.request_id },
                        principalTable: "mobile_requests",
                        principalColumns: new[] { "property_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mobile_request_photos_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_mobile_requests_category",
                table: "mobile_requests",
                sql: "category IN ('PLUMBING','ELECTRICAL','HEATING','ELEVATOR','CLEANING','SECURITY','OTHER')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_mobile_requests_priority",
                table: "mobile_requests",
                sql: "priority IN ('LOW','NORMAL','HIGH','URGENT')");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_request_photos_owner_user_id_operation_id",
                table: "mobile_request_photos",
                columns: new[] { "owner_user_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mobile_request_photos_property_id_request_id",
                table: "mobile_request_photos",
                columns: new[] { "property_id", "request_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mobile_request_photos_request_id_state",
                table: "mobile_request_photos",
                columns: new[] { "request_id", "state" });

            migrationBuilder.CreateIndex(
                name: "IX_mobile_request_photos_state_created_at",
                table: "mobile_request_photos",
                columns: new[] { "state", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down would lose photo metadata (and orphan private objects) or reset chosen categories.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM mobile_request_photos) OR EXISTS (SELECT 1 FROM mobile_requests WHERE category <> 'OTHER' OR priority <> 'NORMAL') THEN
                    RAISE EXCEPTION 'Cannot drop nonempty mobile request photo or category data';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "mobile_request_photos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_mobile_requests_property_id_id",
                table: "mobile_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mobile_requests_category",
                table: "mobile_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mobile_requests_priority",
                table: "mobile_requests");

            migrationBuilder.DropColumn(
                name: "category",
                table: "mobile_requests");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "mobile_requests");
        }
    }
}
