using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePreviewColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "blue",
                column: "epd_preview_hex_value",
                value: "5080b8");

            migrationBuilder.UpdateData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "green",
                column: "epd_preview_hex_value",
                value: "608050");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "blue",
                column: "epd_preview_hex_value",
                value: "0000aa");

            migrationBuilder.UpdateData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "green",
                column: "epd_preview_hex_value",
                value: "00aa00");
        }
    }
}
