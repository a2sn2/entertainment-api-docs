using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madar.Infrastructure.Migrations;

[DbContext(typeof(MadarDbContext))]
[Migration("20260808180000_AddDepartmentAdministration")]
public sealed class AddDepartmentAdministration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset?>(
            name: "UpdatedUtc",
            schema: "madar",
            table: "Departments",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE [madar].[Departments] SET [UpdatedUtc] = [CreatedUtc] WHERE [UpdatedUtc] IS NULL;");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "UpdatedUtc",
            schema: "madar",
            table: "Departments",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "UpdatedUtc",
            schema: "madar",
            table: "Departments");
    }
}
