using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoundationKit.Workbench.Infrastructure.Migrations;

[DbContext(typeof(WorkbenchDbContext))]
[Migration("20260806164000_DualPortalWorkflow")]
public partial class DualPortalWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "BuildBriefs",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Submitted");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedUtc",
            table: "BuildBriefs",
            type: "datetimeoffset",
            nullable: false,
            defaultValue: default(DateTimeOffset));

        migrationBuilder.Sql(
            "UPDATE [BuildBriefs] SET [UpdatedUtc] = [CreatedUtc] WHERE [UpdatedUtc] = '0001-01-01T00:00:00.0000000+00:00';");

        migrationBuilder.CreateTable(
            name: "AdminReviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BuildBriefId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ReviewedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                ReviewedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminReviews", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdminReviews_BuildBriefs_BuildBriefId",
                    column: x => x.BuildBriefId,
                    principalTable: "BuildBriefs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BuildBriefs_Status",
            table: "BuildBriefs",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_AdminReviews_BuildBriefId",
            table: "AdminReviews",
            column: "BuildBriefId");

        migrationBuilder.CreateIndex(
            name: "IX_AdminReviews_ReviewedUtc",
            table: "AdminReviews",
            column: "ReviewedUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminReviews");

        migrationBuilder.DropIndex(
            name: "IX_BuildBriefs_Status",
            table: "BuildBriefs");

        migrationBuilder.DropColumn(
            name: "Status",
            table: "BuildBriefs");

        migrationBuilder.DropColumn(
            name: "UpdatedUtc",
            table: "BuildBriefs");
    }
}
