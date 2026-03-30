using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColorOverridesToPaletteLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "epd_preview_hex_value_override",
                table: "color_palette_links",
                type: "VARCHAR",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hex_value_override",
                table: "color_palette_links",
                type: "VARCHAR",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 6,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 7,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 8,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 9,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 10,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 11,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 12,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 13,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 14,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 15,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 16,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 17,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "color_palette_links",
                keyColumn: "id",
                keyValue: 18,
                columns: new[] { "epd_preview_hex_value_override", "hex_value_override" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "epd_preview_hex_value_override",
                table: "color_palette_links");

            migrationBuilder.DropColumn(
                name: "hex_value_override",
                table: "color_palette_links");
        }
    }
}
