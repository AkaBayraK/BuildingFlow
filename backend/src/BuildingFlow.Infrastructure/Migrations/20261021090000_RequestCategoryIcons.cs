using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <summary>Icon request types: elevator, water, electricity, heating and renovation join the category list.</summary>
    [DbContext(typeof(BuildingFlowDbContext))]
    [Migration("20261021090000_RequestCategoryIcons")]
    public partial class RequestCategoryIcons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "ck_mobile_request_category", table: "mobile_requests");
            migrationBuilder.AddCheckConstraint(
                name: "ck_mobile_request_category",
                table: "mobile_requests",
                sql: "category IN ('ELEVATOR','WATER','ELECTRICITY','HEATING','RENOVATION','FAULT','CLEANING','SECURITY','COMMON_AREA','SUGGESTION','OTHER')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // New types fold back to the closest old one so the narrower constraint can be restored.
            migrationBuilder.Sql("UPDATE mobile_requests SET category='FAULT' WHERE category IN ('ELEVATOR','WATER','ELECTRICITY','HEATING','RENOVATION')");
            migrationBuilder.DropCheckConstraint(name: "ck_mobile_request_category", table: "mobile_requests");
            migrationBuilder.AddCheckConstraint(
                name: "ck_mobile_request_category",
                table: "mobile_requests",
                sql: "category IN ('FAULT','CLEANING','SECURITY','COMMON_AREA','SUGGESTION','OTHER')");
        }
    }
}
