using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumineux.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Authentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_attempts",
                table: "member_accounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_login_at",
                table: "member_accounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lockout_until",
                table: "member_accounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "member_permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    member = table.Column<int>(type: "int", nullable: false),
                    permission = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_member_permissions_members_member",
                        column: x => x.member,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_member_permissions_member_permission",
                table: "member_permissions",
                columns: new[] { "member", "permission" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "member_permissions");

            migrationBuilder.DropColumn(
                name: "failed_attempts",
                table: "member_accounts");

            migrationBuilder.DropColumn(
                name: "last_login_at",
                table: "member_accounts");

            migrationBuilder.DropColumn(
                name: "lockout_until",
                table: "member_accounts");
        }
    }
}
