using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsGroupOnlyToDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_group_only",
                table: "displays",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // The original "default" display (ID=0) is a group-only display
            migrationBuilder.Sql(
                """
                UPDATE "displays" SET "is_group_only" = 1 WHERE "id" = 0
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_group_only",
                table: "displays");
        }
    }
}
