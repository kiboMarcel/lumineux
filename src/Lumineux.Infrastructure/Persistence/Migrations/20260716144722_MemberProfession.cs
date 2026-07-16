using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumineux.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MemberProfession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "profession",
                table: "members",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profession",
                table: "members");
        }
    }
}
