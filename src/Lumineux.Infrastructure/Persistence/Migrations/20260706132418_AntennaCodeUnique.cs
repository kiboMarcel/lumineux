using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumineux.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AntennaCodeUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_antennas_code",
                table: "antennas",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_antennas_code",
                table: "antennas");
        }
    }
}
