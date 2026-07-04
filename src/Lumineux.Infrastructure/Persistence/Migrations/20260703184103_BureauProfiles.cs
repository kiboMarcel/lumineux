using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumineux.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BureauProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bureau_profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    name_normalized = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bureau_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bureau_profile_permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    bureau_profile = table.Column<int>(type: "int", nullable: false),
                    permission = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bureau_profile_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bureau_profile_permissions_bureau_profiles_bureau_profile",
                        column: x => x.bureau_profile,
                        principalTable: "bureau_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "member_bureau_profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    member = table.Column<int>(type: "int", nullable: false),
                    bureau_profile = table.Column<int>(type: "int", nullable: false),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_bureau_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_member_bureau_profiles_bureau_profiles_bureau_profile",
                        column: x => x.bureau_profile,
                        principalTable: "bureau_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_member_bureau_profiles_members_member",
                        column: x => x.member,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bureau_profile_permissions_bureau_profile_permission",
                table: "bureau_profile_permissions",
                columns: new[] { "bureau_profile", "permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bureau_profiles_name_normalized",
                table: "bureau_profiles",
                column: "name_normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_bureau_profiles_bureau_profile",
                table: "member_bureau_profiles",
                column: "bureau_profile");

            migrationBuilder.CreateIndex(
                name: "IX_member_bureau_profiles_member_bureau_profile",
                table: "member_bureau_profiles",
                columns: new[] { "member", "bureau_profile" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bureau_profile_permissions");

            migrationBuilder.DropTable(
                name: "member_bureau_profiles");

            migrationBuilder.DropTable(
                name: "bureau_profiles");
        }
    }
}
