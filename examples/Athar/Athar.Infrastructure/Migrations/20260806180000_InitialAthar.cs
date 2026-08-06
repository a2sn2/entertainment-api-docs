using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Athar.Infrastructure.Migrations;

[DbContext(typeof(AtharDbContext))]
[Migration("20260806180000_InitialAthar")]
public sealed class InitialAthar : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "athar");
        migrationBuilder.EnsureSchema(name: "identity");

        migrationBuilder.CreateTable(
            name: "Roles",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", item => item.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                AccessFailedCount = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", item => item.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditEntries",
            schema: "athar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEntries", item => item.Id);
            });

        migrationBuilder.CreateTable(
            name: "RoleClaims",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoleClaims", item => item.Id);
                table.ForeignKey(
                    name: "FK_RoleClaims_Roles_RoleId",
                    column: item => item.RoleId,
                    principalSchema: "identity",
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Initiatives",
            schema: "athar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                Summary = table.Column<string>(type: "nvarchar(1800)", maxLength: 1800, nullable: false),
                Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                RequestedBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                TargetBeneficiaries = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Initiatives", item => item.Id);
                table.ForeignKey(
                    name: "FK_Initiatives_Users_OwnerUserId",
                    column: item => item.OwnerUserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserClaims",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserClaims", item => item.Id);
                table.ForeignKey(
                    name: "FK_UserClaims_Users_UserId",
                    column: item => item.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserLogins",
            schema: "identity",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserLogins", item => new { item.LoginProvider, item.ProviderKey });
                table.ForeignKey(
                    name: "FK_UserLogins_Users_UserId",
                    column: item => item.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            schema: "identity",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", item => new { item.UserId, item.RoleId });
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: item => item.RoleId,
                    principalSchema: "identity",
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: item => item.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserTokens",
            schema: "identity",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserTokens", item => new { item.UserId, item.LoginProvider, item.Name });
                table.ForeignKey(
                    name: "FK_UserTokens_Users_UserId",
                    column: item => item.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "InitiativeReviews",
            schema: "athar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InitiativeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Decision = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                ReviewedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InitiativeReviews", item => item.Id);
                table.ForeignKey(
                    name: "FK_InitiativeReviews_Initiatives_InitiativeId",
                    column: item => item.InitiativeId,
                    principalSchema: "athar",
                    principalTable: "Initiatives",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_InitiativeReviews_Users_ReviewerUserId",
                    column: item => item.ReviewerUserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditEntries_EntityType_EntityId_CreatedUtc",
            schema: "athar",
            table: "AuditEntries",
            columns: new[] { "EntityType", "EntityId", "CreatedUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_InitiativeReviews_InitiativeId_ReviewedUtc",
            schema: "athar",
            table: "InitiativeReviews",
            columns: new[] { "InitiativeId", "ReviewedUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_InitiativeReviews_ReviewerUserId",
            schema: "athar",
            table: "InitiativeReviews",
            column: "ReviewerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Initiatives_OwnerUserId_ClientRequestId",
            schema: "athar",
            table: "Initiatives",
            columns: new[] { "OwnerUserId", "ClientRequestId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Initiatives_Status_CreatedUtc",
            schema: "athar",
            table: "Initiatives",
            columns: new[] { "Status", "CreatedUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_RoleClaims_RoleId",
            schema: "identity",
            table: "RoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            schema: "identity",
            table: "Roles",
            column: "NormalizedName",
            unique: true,
            filter: "[NormalizedName] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_UserClaims_UserId",
            schema: "identity",
            table: "UserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserLogins_UserId",
            schema: "identity",
            table: "UserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            schema: "identity",
            table: "UserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            schema: "identity",
            table: "Users",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            schema: "identity",
            table: "Users",
            column: "NormalizedUserName",
            unique: true,
            filter: "[NormalizedUserName] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditEntries", schema: "athar");
        migrationBuilder.DropTable(name: "InitiativeReviews", schema: "athar");
        migrationBuilder.DropTable(name: "RoleClaims", schema: "identity");
        migrationBuilder.DropTable(name: "UserClaims", schema: "identity");
        migrationBuilder.DropTable(name: "UserLogins", schema: "identity");
        migrationBuilder.DropTable(name: "UserRoles", schema: "identity");
        migrationBuilder.DropTable(name: "UserTokens", schema: "identity");
        migrationBuilder.DropTable(name: "Initiatives", schema: "athar");
        migrationBuilder.DropTable(name: "Roles", schema: "identity");
        migrationBuilder.DropTable(name: "Users", schema: "identity");
    }
}
