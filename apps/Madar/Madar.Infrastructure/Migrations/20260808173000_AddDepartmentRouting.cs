using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madar.Infrastructure.Migrations;

[DbContext(typeof(MadarDbContext))]
[Migration("20260808173000_AddDepartmentRouting")]
public sealed class AddDepartmentRouting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Departments",
            schema: "madar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Departments", item => item.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Departments_Code",
            schema: "madar",
            table: "Departments",
            column: "Code",
            unique: true);

        migrationBuilder.CreateTable(
            name: "DepartmentMemberships",
            schema: "madar",
            columns: table => new
            {
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JoinedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_DepartmentMemberships",
                    item => new { item.DepartmentId, item.UserId });
                table.ForeignKey(
                    name: "FK_DepartmentMemberships_Departments_DepartmentId",
                    column: item => item.DepartmentId,
                    principalSchema: "madar",
                    principalTable: "Departments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DepartmentMemberships_Users_UserId",
                    column: item => item.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DepartmentMemberships_UserId_DepartmentId",
            schema: "madar",
            table: "DepartmentMemberships",
            columns: new[] { "UserId", "DepartmentId" });

        migrationBuilder.AddColumn<Guid>(
            name: "DepartmentId",
            schema: "madar",
            table: "Cases",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "RoutedUtc",
            schema: "madar",
            table: "Cases",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Cases_DepartmentId_Status_UpdatedUtc",
            schema: "madar",
            table: "Cases",
            columns: new[] { "DepartmentId", "Status", "UpdatedUtc" });

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Departments_DepartmentId",
            schema: "madar",
            table: "Cases",
            column: "DepartmentId",
            principalSchema: "madar",
            principalTable: "Departments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Cases_Departments_DepartmentId",
            schema: "madar",
            table: "Cases");

        migrationBuilder.DropIndex(
            name: "IX_Cases_DepartmentId_Status_UpdatedUtc",
            schema: "madar",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "DepartmentId",
            schema: "madar",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "RoutedUtc",
            schema: "madar",
            table: "Cases");

        migrationBuilder.DropTable(
            name: "DepartmentMemberships",
            schema: "madar");

        migrationBuilder.DropTable(
            name: "Departments",
            schema: "madar");
    }
}
