using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParkingRetentionRedaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "personal_data_redacted_at",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "personal_data_redacted_at",
                table: "guest_vehicles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM vehicles WHERE personal_data_redacted_at IS NOT NULL)
                     OR EXISTS (SELECT 1 FROM guest_vehicles WHERE personal_data_redacted_at IS NOT NULL) THEN
                    RAISE EXCEPTION 'Parking retention history exists; preserve redaction metadata.';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropColumn(
                name: "personal_data_redacted_at",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "personal_data_redacted_at",
                table: "guest_vehicles");
        }
    }
}
