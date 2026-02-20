using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class RenameColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_display_types_displaytype",
                table: "displays");

            migrationBuilder.RenameColumn(
                name: "displaytype",
                table: "displays",
                newName: "display_type");

            migrationBuilder.RenameIndex(
                name: "IX_displays_displaytype",
                table: "displays",
                newName: "IX_displays_display_type");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_display_types_display_type",
                table: "displays",
                column: "display_type",
                principalTable: "display_types",
                principalColumn: "code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_display_types_display_type",
                table: "displays");

            migrationBuilder.RenameColumn(
                name: "display_type",
                table: "displays",
                newName: "displaytype");

            migrationBuilder.RenameIndex(
                name: "IX_displays_display_type",
                table: "displays",
                newName: "IX_displays_displaytype");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_display_types_displaytype",
                table: "displays",
                column: "displaytype",
                principalTable: "display_types",
                principalColumn: "code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
