using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MobileAccountProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mobile_profile_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_mobile_profile_photos", x => x.id);
                    table.UniqueConstraint("AK_mobile_profile_photos_owner_user_id_id", x => new { x.owner_user_id, x.id });
                    table.CheckConstraint("ck_profile_photo_size", "size BETWEEN 1 AND 262144");
                    table.CheckConstraint("ck_profile_photo_state", "state IN ('STAGED','CURRENT','RETIRED','PURGED')");
                });

            migrationBuilder.CreateTable(
                name: "mobile_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    first_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    last_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_mobile_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_mobile_profiles_mobile_profile_photos_owner_user_id_photo_id",
                        columns: x => new { x.owner_user_id, x.photo_id },
                        principalTable: "mobile_profile_photos",
                        principalColumns: new[] { "owner_user_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mobile_profile_photos_owner_user_id_operation_id",
                table: "mobile_profile_photos",
                columns: new[] { "owner_user_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mobile_profile_photos_state_created_at",
                table: "mobile_profile_photos",
                columns: new[] { "state", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_mobile_profiles_owner_user_id",
                table: "mobile_profiles",
                column: "owner_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mobile_profiles_owner_user_id_photo_id",
                table: "mobile_profiles",
                columns: new[] { "owner_user_id", "photo_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM mobile_profiles) OR EXISTS (SELECT 1 FROM mobile_profile_photos) THEN
                    RAISE EXCEPTION 'Cannot drop nonempty mobile profile tables';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "mobile_profiles");

            migrationBuilder.DropTable(
                name: "mobile_profile_photos");
        }
    }
}
