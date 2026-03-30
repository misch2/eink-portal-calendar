using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOverrideColor2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 21,
                column: "epd_preview_hex_value_override",
                value: "c00000");

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 22,
                column: "epd_preview_hex_value_override",
                value: "f09d00");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 21,
                column: "epd_preview_hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 22,
                column: "epd_preview_hex_value_override",
                value: "ff9d00");
        }
    }
}
