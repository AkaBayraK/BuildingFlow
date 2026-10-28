using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParkingRequestIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows get distinct server-generated ids so the unique replay index can be created.
            migrationBuilder.Sql("ALTER TABLE vehicles ADD COLUMN request_id uuid; UPDATE vehicles SET request_id = gen_random_uuid(); ALTER TABLE vehicles ALTER COLUMN request_id SET NOT NULL;");

            migrationBuilder.Sql("ALTER TABLE guest_vehicles ADD COLUMN request_id uuid; UPDATE guest_vehicles SET request_id = gen_random_uuid(); ALTER TABLE guest_vehicles ALTER COLUMN request_id SET NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_property_id_request_id",
                table: "vehicles",
                columns: new[] { "property_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guest_vehicles_property_id_request_id",
                table: "guest_vehicles",
                columns: new[] { "property_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vehicles_property_id_request_id",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_guest_vehicles_property_id_request_id",
                table: "guest_vehicles");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "guest_vehicles");
        }
    }
}
