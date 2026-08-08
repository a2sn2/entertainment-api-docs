using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madar.Infrastructure.Migrations;

[DbContext(typeof(MadarDbContext))]
[Migration("20260808093000_InitialMadar")]
public sealed class InitialMadar : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "audit");
        migrationBuilder.EnsureSchema(name: "identity");
        migrationBuilder.EnsureSchema(name: "madar");

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
            name: "AuditEvents",
            schema: "audit",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                SubjectType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                SubjectId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Outcome = table.Column<int>(type: "int", nullable: false),
                ActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                TenantId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Source = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                ReasonCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                AttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEvents", item => item.Id);
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
            name: "Cases",
            schema: "madar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                CaseType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ResolvedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ClosedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cases", item => item.Id);
                table.ForeignKey(
                    name: "FK_Cases_Users_AssignedToUserId",
                    column: item => item.AssignedToUserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Cases_Users_CreatedByUserId",
                    column: item => item.CreatedByUserId,
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

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_ActorId_OccurredAtUtc",
            schema: "audit",
            table: "AuditEvents",
            columns: new[] { "ActorId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_SubjectType_SubjectId_OccurredAtUtc",
            schema: "audit",
            table: "AuditEvents",
            columns: new[] { "SubjectType", "SubjectId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Cases_AssignedToUserId_Status_UpdatedUtc",
            schema: "madar",
            table: "Cases",
            columns: new[] { "AssignedToUserId", "Status", "UpdatedUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Cases_CreatedByUserId_CreatedUtc",
            schema: "madar",
            table: "Cases",
            columns: new[] { "CreatedByUserId", "CreatedUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Cases_Status_Priority_CreatedUtc",
            schema: "madar",
            table: "Cases",
            columns: new[] { "Status", "Priority", "CreatedUtc" });

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
        migrationBuilder.DropTable(name: "AuditEvents", schema: "audit");
        migrationBuilder.DropTable(name: "Cases", schema: "madar");
        migrationBuilder.DropTable(name: "RoleClaims", schema: "identity");
        migrationBuilder.DropTable(name: "UserClaims", schema: "identity");
        migrationBuilder.DropTable(name: "UserLogins", schema: "identity");
        migrationBuilder.DropTable(name: "UserRoles", schema: "identity");
        migrationBuilder.DropTable(name: "UserTokens", schema: "identity");
        migrationBuilder.DropTable(name: "Roles", schema: "identity");
        migrationBuilder.DropTable(name: "Users", schema: "identity");
    }
}
