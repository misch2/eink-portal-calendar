using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddColorsToThePalette : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "color_palette_links",
                columns: new[] { "id", "color_variant_code", "epd_color_code" },
                values: new object[,]
                {
                    { 9, "BWRY", "black" },
                    { 10, "BWRY", "white" },
                    { 11, "BWRY", "red" },
                    { 12, "BWRY", "yellow" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 12);
        }
    }
}
