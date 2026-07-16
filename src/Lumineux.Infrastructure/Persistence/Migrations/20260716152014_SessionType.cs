using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumineux.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SessionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "session_type",
                table: "attendance_sessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "AntennaMeeting");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "session_type",
                table: "attendance_sessions");
        }
    }
}
