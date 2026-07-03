using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumineux.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MemberRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "members",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "birth_city",
                table: "members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "birth_date",
                table: "members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "birth_place",
                table: "members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "civility",
                table: "members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "district",
                table: "members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "members",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "entry_date",
                table: "members",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "members",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "introducer",
                table: "members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mobile",
                table: "members",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "nationality",
                table: "members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference",
                table: "members",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    label = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "civilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_civilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    label_country = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    label_nationality = table.Column<string>(type: "nvarchar(210)", maxLength: 210, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    label = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_districts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "member_accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    member = table.Column<int>(type: "int", nullable: false),
                    login_id = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    must_change_password = table.Column<bool>(type: "bit", nullable: false),
                    activation_state = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_member_accounts_members_member",
                        column: x => x.member,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_members_birth_city",
                table: "members",
                column: "birth_city");

            migrationBuilder.CreateIndex(
                name: "IX_members_birth_place",
                table: "members",
                column: "birth_place");

            migrationBuilder.CreateIndex(
                name: "IX_members_civility",
                table: "members",
                column: "civility");

            migrationBuilder.CreateIndex(
                name: "IX_members_district",
                table: "members",
                column: "district");

            migrationBuilder.CreateIndex(
                name: "IX_members_email",
                table: "members",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL AND status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_members_first_name",
                table: "members",
                column: "first_name");

            migrationBuilder.CreateIndex(
                name: "IX_members_introducer",
                table: "members",
                column: "introducer");

            migrationBuilder.CreateIndex(
                name: "IX_members_last_name",
                table: "members",
                column: "last_name");

            migrationBuilder.CreateIndex(
                name: "IX_members_mobile",
                table: "members",
                column: "mobile",
                unique: true,
                filter: "mobile IS NOT NULL AND status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_members_nationality",
                table: "members",
                column: "nationality");

            migrationBuilder.CreateIndex(
                name: "IX_members_reference",
                table: "members",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_accounts_login_id",
                table: "member_accounts",
                column: "login_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_accounts_member",
                table: "member_accounts",
                column: "member",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_members_cities_birth_city",
                table: "members",
                column: "birth_city",
                principalTable: "cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_members_cities_birth_place",
                table: "members",
                column: "birth_place",
                principalTable: "cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_members_civilities_civility",
                table: "members",
                column: "civility",
                principalTable: "civilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_members_countries_nationality",
                table: "members",
                column: "nationality",
                principalTable: "countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_members_districts_district",
                table: "members",
                column: "district",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_members_members_introducer",
                table: "members",
                column: "introducer",
                principalTable: "members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_members_cities_birth_city",
                table: "members");

            migrationBuilder.DropForeignKey(
                name: "FK_members_cities_birth_place",
                table: "members");

            migrationBuilder.DropForeignKey(
                name: "FK_members_civilities_civility",
                table: "members");

            migrationBuilder.DropForeignKey(
                name: "FK_members_countries_nationality",
                table: "members");

            migrationBuilder.DropForeignKey(
                name: "FK_members_districts_district",
                table: "members");

            migrationBuilder.DropForeignKey(
                name: "FK_members_members_introducer",
                table: "members");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "civilities");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropTable(
                name: "member_accounts");

            migrationBuilder.DropIndex(
                name: "IX_members_birth_city",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_birth_place",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_civility",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_district",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_email",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_first_name",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_introducer",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_last_name",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_mobile",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_nationality",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_reference",
                table: "members");

            migrationBuilder.DropColumn(
                name: "address",
                table: "members");

            migrationBuilder.DropColumn(
                name: "birth_city",
                table: "members");

            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "members");

            migrationBuilder.DropColumn(
                name: "birth_place",
                table: "members");

            migrationBuilder.DropColumn(
                name: "civility",
                table: "members");

            migrationBuilder.DropColumn(
                name: "district",
                table: "members");

            migrationBuilder.DropColumn(
                name: "email",
                table: "members");

            migrationBuilder.DropColumn(
                name: "entry_date",
                table: "members");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "members");

            migrationBuilder.DropColumn(
                name: "introducer",
                table: "members");

            migrationBuilder.DropColumn(
                name: "mobile",
                table: "members");

            migrationBuilder.DropColumn(
                name: "nationality",
                table: "members");

            migrationBuilder.DropColumn(
                name: "reference",
                table: "members");
        }
    }
}
