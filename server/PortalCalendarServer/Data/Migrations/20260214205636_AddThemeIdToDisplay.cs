using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeIdToDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "theme_id",
                table: "displays",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_displays_theme_id",
                table: "displays",
                column: "theme_id");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_themes_theme_id",
                table: "displays",
                column: "theme_id",
                principalTable: "themes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_themes_theme_id",
                table: "displays");

            migrationBuilder.DropIndex(
                name: "IX_displays_theme_id",
                table: "displays");

            migrationBuilder.DropColumn(
                name: "theme_id",
                table: "displays");
        }
    }
}
