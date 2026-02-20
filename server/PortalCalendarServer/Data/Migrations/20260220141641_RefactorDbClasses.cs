using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDbClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_themes_theme_id",
                table: "displays");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "themes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "themes",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "themes",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "HasCustomConfig",
                table: "themes",
                newName: "has_custom_config");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "themes",
                newName: "file_name");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "themes",
                newName: "display_name");

            migrationBuilder.RenameIndex(
                name: "IX_themes_FileName",
                table: "themes",
                newName: "IX_themes_file_name");

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "themes",
                type: "VARCHAR",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "themes",
                type: "VARCHAR",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_themes_theme_id",
                table: "displays",
                column: "theme_id",
                principalTable: "themes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_themes_theme_id",
                table: "displays");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "themes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "themes",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "themes",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "has_custom_config",
                table: "themes",
                newName: "HasCustomConfig");

            migrationBuilder.RenameColumn(
                name: "file_name",
                table: "themes",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "display_name",
                table: "themes",
                newName: "DisplayName");

            migrationBuilder.RenameIndex(
                name: "IX_themes_file_name",
                table: "themes",
                newName: "IX_themes_FileName");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "themes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "VARCHAR");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "themes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "VARCHAR");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_themes_theme_id",
                table: "displays",
                column: "theme_id",
                principalTable: "themes",
                principalColumn: "Id");
        }
    }
}
