using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNonPreviewColorOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hex_value_override",
                table: "color_palette_links");

            migrationBuilder.InsertData(
                table: "color_variants",
                columns: new[] { "Code", "display_type_code", "name", "sort_order" },
                values: new object[] { "BWRY-GDEM075F52", "4C", "Black, White, Red, Yellow - GDEM075F52", 3100 });

            migrationBuilder.InsertData(
                table: "color_palette_links",
                columns: new[] { "id", "color_variant_code", "epd_color_code", "epd_preview_hex_value_override" },
                values: new object[,]
                {
                    { 19, "BWRY-GDEM075F52", "black", null },
                    { 20, "BWRY-GDEM075F52", "white", null },
                    { 21, "BWRY-GDEM075F52", "red", null },
                    { 22, "BWRY-GDEM075F52", "yellow", "ff9d00" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BWRY-GDEM075F52");

            migrationBuilder.AddColumn<string>(
                name: "hex_value_override",
                table: "color_palette_links",
                type: "VARCHAR",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 1,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 2,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 3,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 4,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 5,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 6,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 7,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 8,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 9,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 10,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 11,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 12,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 13,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 14,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 15,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 16,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 17,
                column: "hex_value_override",
                value: null);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 18,
                column: "hex_value_override",
                value: null);
        }
    }
}
