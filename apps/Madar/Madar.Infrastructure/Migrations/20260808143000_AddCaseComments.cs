using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madar.Infrastructure.Migrations;

[DbContext(typeof(MadarDbContext))]
[Migration("20260808143000_AddCaseComments")]
public sealed class AddCaseComments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CaseComments",
            schema: "madar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseComments", item => item.Id);
                table.ForeignKey(
                    name: "FK_CaseComments_Cases_CaseId",
                    column: item => item.CaseId,
                    principalSchema: "madar",
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CaseComments_Users_AuthorUserId",
                    column: item => item.AuthorUserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CaseComments_AuthorUserId",
            schema: "madar",
            table: "CaseComments",
            column: "AuthorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_CaseComments_CaseId_CreatedUtc_Id",
            schema: "madar",
            table: "CaseComments",
            columns: new[] { "CaseId", "CreatedUtc", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CaseComments",
            schema: "madar");
    }
}
