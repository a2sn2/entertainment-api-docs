using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madar.Infrastructure.Migrations;

[DbContext(typeof(MadarDbContext))]
[Migration("20260808155000_AddCaseApprovals")]
public sealed class AddCaseApprovals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CaseApprovals",
            schema: "madar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequestedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                DecidedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DecisionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseApprovals", item => item.Id);
                table.ForeignKey(
                    name: "FK_CaseApprovals_Cases_CaseId",
                    column: item => item.CaseId,
                    principalSchema: "madar",
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CaseApprovals_Users_RequestedByUserId",
                    column: item => item.RequestedByUserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CaseApprovals_Users_ReviewedByUserId",
                    column: item => item.ReviewedByUserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CaseApprovals_CaseId_RequestedUtc_Id",
            schema: "madar",
            table: "CaseApprovals",
            columns: new[] { "CaseId", "RequestedUtc", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_CaseApprovals_RequestedByUserId",
            schema: "madar",
            table: "CaseApprovals",
            column: "RequestedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_CaseApprovals_ReviewedByUserId",
            schema: "madar",
            table: "CaseApprovals",
            column: "ReviewedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CaseApprovals",
            schema: "madar");
    }
}
