using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madar.Infrastructure.Migrations;

[DbContext(typeof(MadarDbContext))]
[Migration("20260808110000_AddMadarSla")]
public sealed class AddMadarSla : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "EscalatedUtc",
            schema: "madar",
            table: "Cases",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "SlaBreachedUtc",
            schema: "madar",
            table: "Cases",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "SlaTargetUtc",
            schema: "madar",
            table: "Cases",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Cases_SlaBreachedUtc_ResolvedUtc_SlaTargetUtc",
            schema: "madar",
            table: "Cases",
            columns: new[] { "SlaBreachedUtc", "ResolvedUtc", "SlaTargetUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Cases_SlaBreachedUtc_ResolvedUtc_SlaTargetUtc",
            schema: "madar",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "EscalatedUtc",
            schema: "madar",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "SlaBreachedUtc",
            schema: "madar",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "SlaTargetUtc",
            schema: "madar",
            table: "Cases");
    }
}
