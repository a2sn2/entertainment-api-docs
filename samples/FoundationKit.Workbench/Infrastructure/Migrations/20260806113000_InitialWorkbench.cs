using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoundationKit.Workbench.Infrastructure.Migrations;

[DbContext(typeof(WorkbenchDbContext))]
[Migration("20260806113000_InitialWorkbench")]
public partial class InitialWorkbench : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BuildBriefs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProjectName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                ProjectType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Audience = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Goal = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                SelectedCapabilityIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Priorities = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BuildBriefs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BuildBriefs_CreatedUtc",
            table: "BuildBriefs",
            column: "CreatedUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BuildBriefs");
    }
}
