using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParentIdToDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "displays",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_displays_parent_id",
                table: "displays",
                column: "parent_id");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_displays_parent_id",
                table: "displays",
                column: "parent_id",
                principalTable: "displays",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Set existing non-default displays to inherit from the "Default" display (ID=0)
            migrationBuilder.Sql("UPDATE displays SET parent_id = 0 WHERE id != 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_displays_parent_id",
                table: "displays");

            migrationBuilder.DropIndex(
                name: "IX_displays_parent_id",
                table: "displays");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "displays");
        }
    }
}
