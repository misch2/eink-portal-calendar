using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddColorVariantsLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color_variant",
                table: "displays",
                type: "VARCHAR",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_displays_color_variant",
                table: "displays",
                column: "color_variant");

            migrationBuilder.CreateIndex(
                name: "IX_color_palette_links_epd_color_code",
                table: "color_palette_links",
                column: "epd_color_code");

            migrationBuilder.AddForeignKey(
                name: "FK_color_palette_links_color_variants_color_variant_code",
                table: "color_palette_links",
                column: "color_variant_code",
                principalTable: "color_variants",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_color_palette_links_epd_colors_epd_color_code",
                table: "color_palette_links",
                column: "epd_color_code",
                principalTable: "epd_colors",
                principalColumn: "code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_color_variants_display_types_display_type_code",
                table: "color_variants",
                column: "display_type_code",
                principalTable: "display_types",
                principalColumn: "code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays",
                column: "color_variant",
                principalTable: "color_variants",
                principalColumn: "Code",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_color_palette_links_color_variants_color_variant_code",
                table: "color_palette_links");

            migrationBuilder.DropForeignKey(
                name: "FK_color_palette_links_epd_colors_epd_color_code",
                table: "color_palette_links");

            migrationBuilder.DropForeignKey(
                name: "FK_color_variants_display_types_display_type_code",
                table: "color_variants");

            migrationBuilder.DropForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays");

            migrationBuilder.DropIndex(
                name: "IX_displays_color_variant",
                table: "displays");

            migrationBuilder.DropIndex(
                name: "IX_color_palette_links_epd_color_code",
                table: "color_palette_links");

            migrationBuilder.DropColumn(
                name: "color_variant",
                table: "displays");
        }
    }
}
