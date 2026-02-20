using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class RenamePropertyInDisplayClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays",
                column: "color_variant",
                principalTable: "color_variants",
                principalColumn: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_color_variants_color_variant",
                table: "displays",
                column: "color_variant",
                principalTable: "color_variants",
                principalColumn: "Code",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
