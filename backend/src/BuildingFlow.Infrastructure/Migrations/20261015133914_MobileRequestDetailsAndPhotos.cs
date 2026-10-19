using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MobileRequestDetailsAndPhotos : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "photo_id",
                table: "mobile_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "mobile_requests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "NORMAL");

            migrationBuilder.CreateTable(
                name: "mobile_request_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    object_key = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
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
                    table.UniqueConstraint("AK_mobile_request_photos_owner_user_id_property_id_id", x => new { x.owner_user_id, x.property_id, x.id });
                    table.CheckConstraint("ck_request_photo_size", "size BETWEEN 1 AND 262144");
                    table.CheckConstraint("ck_request_photo_state", "state IN ('STAGED','CURRENT','RETIRED','PURGED')");
                    table.ForeignKey(
                        name: "FK_mobile_request_photos_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mobile_requests_owner_user_id_property_id_photo_id",
                table: "mobile_requests",
                columns: new[] { "owner_user_id", "property_id", "photo_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_mobile_request_category",
                table: "mobile_requests",
                sql: "category IN ('FAULT','CLEANING','SECURITY','COMMON_AREA','SUGGESTION','OTHER')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_mobile_request_priority",
                table: "mobile_requests",
                sql: "priority IN ('LOW','NORMAL','HIGH')");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_request_photos_owner_user_id_operation_id",
                table: "mobile_request_photos",
                columns: new[] { "owner_user_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mobile_request_photos_property_id",
                table: "mobile_request_photos",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_request_photos_state_created_at",
                table: "mobile_request_photos",
                columns: new[] { "state", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_mobile_requests_mobile_request_photos_owner_user_id_propert~",
                table: "mobile_requests",
                columns: new[] { "owner_user_id", "property_id", "photo_id" },
                principalTable: "mobile_request_photos",
                principalColumns: new[] { "owner_user_id", "property_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM mobile_request_photos) OR EXISTS (SELECT 1 FROM mobile_requests WHERE category <> 'OTHER' OR priority <> 'NORMAL' OR photo_id IS NOT NULL) THEN RAISE EXCEPTION 'Cannot remove nonempty request photo/details history'; END IF; END $$;");
            migrationBuilder.DropForeignKey(
                name: "FK_mobile_requests_mobile_request_photos_owner_user_id_propert~",
                table: "mobile_requests");

            migrationBuilder.DropTable(
                name: "mobile_request_photos");

            migrationBuilder.DropIndex(
                name: "IX_mobile_requests_owner_user_id_property_id_photo_id",
                table: "mobile_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mobile_request_category",
                table: "mobile_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mobile_request_priority",
                table: "mobile_requests");

            migrationBuilder.DropColumn(
                name: "category",
                table: "mobile_requests");

            migrationBuilder.DropColumn(
                name: "photo_id",
                table: "mobile_requests");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "mobile_requests");
        }
    }
}
