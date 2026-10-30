using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DefinitionValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "definition_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    list_key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name_tr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    icon_key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    tone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
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
                    table.PrimaryKey("PK_definition_values", x => x.id);
                    table.CheckConstraint("chk_definition_values_sort_order", "sort_order BETWEEN 0 AND 999");
                });

            migrationBuilder.CreateIndex(
                name: "uidx_definition_values_list_key_code",
                table: "definition_values",
                columns: new[] { "list_key", "code" },
                unique: true,
                filter: "NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS(SELECT 1 FROM definition_values) THEN RAISE EXCEPTION 'Definition values exist; rollback would lose administrator choices'; END IF; END $$;");
            migrationBuilder.DropTable(
                name: "definition_values");
        }
    }
}
