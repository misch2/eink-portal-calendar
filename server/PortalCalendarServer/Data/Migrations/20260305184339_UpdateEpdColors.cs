using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEpdColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "yellow",
                column: "epd_preview_hex_value",
                value: "c0a010");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "yellow",
                column: "epd_preview_hex_value",
                value: "dddd00");
        }
    }
}
