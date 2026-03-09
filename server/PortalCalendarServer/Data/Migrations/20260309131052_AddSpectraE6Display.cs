using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddSpectraE6Display : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "display_types",
                columns: new[] { "code", "name", "num_colors", "sort_order" },
                values: new object[] { "6C", "6 Colors", 6, 400 });

            migrationBuilder.InsertData(
                table: "epd_colors",
                columns: new[] { "code", "epd_preview_hex_value", "hex_value", "name" },
                values: new object[,]
                {
                    { "blue", "0000aa", "0000FF", "Blue" },
                    { "green", "00aa00", "00FF00", "Green" }
                });

            migrationBuilder.InsertData(
                table: "color_variants",
                columns: new[] { "Code", "display_type_code", "name", "sort_order" },
                values: new object[] { "SpectraE6", "6C", "Spectra E6 (Black, White, Red, Yellow, Blue, Green)", 4000 });

            migrationBuilder.InsertData(
                table: "color_palette_links",
                columns: new[] { "id", "color_variant_code", "epd_color_code" },
                values: new object[,]
                {
                    { 13, "SpectraE6", "black" },
                    { 14, "SpectraE6", "white" },
                    { 15, "SpectraE6", "red" },
                    { 16, "SpectraE6", "yellow" },
                    { 17, "SpectraE6", "blue" },
                    { 18, "SpectraE6", "green" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "SpectraE6");

            migrationBuilder.DeleteData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "blue");

            migrationBuilder.DeleteData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "green");

            migrationBuilder.DeleteData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "6C");
        }
    }
}
