using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumineux.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    session = table.Column<int>(type: "int", nullable: false),
                    member = table.Column<int>(type: "int", nullable: false),
                    arrival_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    origin_antenna = table.Column<int>(type: "int", nullable: true),
                    client_operation_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updatedt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updatedby = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendances_antennas_origin_antenna",
                        column: x => x.origin_antenna,
                        principalTable: "antennas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendances_attendance_sessions_session",
                        column: x => x.session,
                        principalTable: "attendance_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendances_members_member",
                        column: x => x.member,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendances_member",
                table: "attendances",
                column: "member");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_origin_antenna",
                table: "attendances",
                column: "origin_antenna");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_session_client_operation_id",
                table: "attendances",
                columns: new[] { "session", "client_operation_id" },
                unique: true,
                filter: "client_operation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_session_member",
                table: "attendances",
                columns: new[] { "session", "member" },
                unique: true,
                filter: "status = 'Valid'");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_session_status",
                table: "attendances",
                columns: new[] { "session", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendances");
        }
    }
}
