using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOverrideColor3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "color_variants",
                columns: new[] { "Code", "display_type_code", "name", "sort_order" },
                values: new object[] { "BWR-cheap", "3C", "Black, White, Red - cheap with bad contrast", 2020 });

            migrationBuilder.InsertData(
                table: "color_palette_links",
                columns: new[] { "id", "color_variant_code", "epd_color_code", "epd_preview_hex_value_override" },
                values: new object[,]
                {
                    { 23, "BWR-cheap", "black", "333" },
                    { 24, "BWR-cheap", "white", "ddd" },
                    { 25, "BWR-cheap", "red", "822" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BWR-cheap");
        }
    }
}
