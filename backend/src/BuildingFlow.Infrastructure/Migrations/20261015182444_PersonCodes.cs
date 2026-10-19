using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersonCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "person_code_hash",
                table: "people",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "person_code_hint",
                table: "people",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "person_code_issued_at",
                table: "people",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uidx_people_person_code_hash",
                table: "people",
                column: "person_code_hash",
                unique: true,
                filter: "person_code_hash IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "chk_people_person_code",
                table: "people",
                sql: "(person_code_hash IS NULL AND person_code_hint IS NULL AND person_code_issued_at IS NULL) OR (person_code_hash ~ '^[0-9a-f]{64}$' AND person_code_hint ~ '^[0-9]{2}$' AND person_code_issued_at IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uidx_people_person_code_hash",
                table: "people");

            migrationBuilder.DropCheckConstraint(
                name: "chk_people_person_code",
                table: "people");

            migrationBuilder.DropColumn(
                name: "person_code_hash",
                table: "people");

            migrationBuilder.DropColumn(
                name: "person_code_hint",
                table: "people");

            migrationBuilder.DropColumn(
                name: "person_code_issued_at",
                table: "people");
        }
    }
}
