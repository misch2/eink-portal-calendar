using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddColorTypeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "display_types",
                columns: new[] { "code", "name" },
                values: new object[,]
                {
                    { "3C", "3 Color" },
                    { "BW", "Black and White" }
                });

            migrationBuilder.InsertData(
                table: "epd_colors",
                columns: new[] { "code", "epd_preview_hex_value", "hex_value", "name" },
                values: new object[,]
                {
                    { "black", "111111", "000000", "Black" },
                    { "red", "aa0000", "FF0000", "Red" },
                    { "white", "dddddd", "FFFFFF", "White" },
                    { "yellow", "dddd00", "FFFF00", "Yellow" }
                });

            migrationBuilder.InsertData(
                table: "color_variants",
                columns: new[] { "Code", "display_type_code", "name" },
                values: new object[,]
                {
                    { "BW", "BW", "Black and White" },
                    { "BWR", "3C", "Black, White, Red" },
                    { "BWY", "3C", "Black, White, Yellow" }
                });

            migrationBuilder.InsertData(
                table: "color_palette_links",
                columns: new[] { "id", "color_variant_code", "epd_color_code" },
                values: new object[,]
                {
                    { 1, "BW", "black" },
                    { 2, "BW", "white" },
                    { 3, "BWY", "black" },
                    { 4, "BWY", "white" },
                    { 5, "BWY", "yellow" },
                    { 6, "BWR", "black" },
                    { 7, "BWR", "white" },
                    { 8, "BWR", "red" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BW");

            migrationBuilder.DeleteData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BWR");

            migrationBuilder.DeleteData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BWY");

            migrationBuilder.DeleteData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "black");

            migrationBuilder.DeleteData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "red");

            migrationBuilder.DeleteData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "white");

            migrationBuilder.DeleteData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "yellow");

            migrationBuilder.DeleteData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "3C");

            migrationBuilder.DeleteData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "BW");
        }
    }
}
