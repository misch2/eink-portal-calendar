using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddColorVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "color_palette_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    color_variant_code = table.Column<string>(type: "VARCHAR", nullable: false),
                    epd_color_code = table.Column<string>(type: "VARCHAR", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_color_palette_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "color_variants",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "VARCHAR", nullable: false),
                    display_type_code = table.Column<string>(type: "VARCHAR", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_color_variants", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "display_types",
                columns: table => new
                {
                    code = table.Column<string>(type: "VARCHAR", nullable: false),
                    name = table.Column<string>(type: "VARCHAR", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_display_types", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "epd_colors",
                columns: table => new
                {
                    code = table.Column<string>(type: "VARCHAR", nullable: false),
                    name = table.Column<string>(type: "VARCHAR", nullable: false),
                    hex_value = table.Column<string>(type: "VARCHAR", nullable: false),
                    epd_preview_hex_value = table.Column<string>(type: "VARCHAR", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_epd_colors", x => x.code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_color_palette_links_color_variant_code_epd_color_code",
                table: "color_palette_links",
                columns: new[] { "color_variant_code", "epd_color_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_color_variants_display_type_code_name",
                table: "color_variants",
                columns: new[] { "display_type_code", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_display_types_name",
                table: "display_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_epd_colors_name",
                table: "epd_colors",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "color_palette_links");

            migrationBuilder.DropTable(
                name: "color_variants");

            migrationBuilder.DropTable(
                name: "display_types");

            migrationBuilder.DropTable(
                name: "epd_colors");
        }
    }
}
