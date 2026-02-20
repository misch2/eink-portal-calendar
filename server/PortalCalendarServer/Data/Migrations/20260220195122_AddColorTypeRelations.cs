using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddColorTypeRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays");

            migrationBuilder.CreateIndex(
                name: "IX_displays_displaytype",
                table: "displays",
                column: "displaytype");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays",
                column: "color_variant",
                principalTable: "color_variants",
                principalColumn: "Code",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_displays_display_types_displaytype",
                table: "displays",
                column: "displaytype",
                principalTable: "display_types",
                principalColumn: "code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays");

            migrationBuilder.DropForeignKey(
                name: "FK_displays_display_types_displaytype",
                table: "displays");

            migrationBuilder.DropIndex(
                name: "IX_displays_displaytype",
                table: "displays");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays",
                column: "color_variant",
                principalTable: "color_variants",
                principalColumn: "Code");
        }
    }
}
