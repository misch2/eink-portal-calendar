using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddObjectOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_admin",
                table: "users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hide_from_other_users",
                table: "galleries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "owner_id",
                table: "galleries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "hide_from_other_users",
                table: "displays",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "owner_id",
                table: "displays",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_galleries_owner_id",
                table: "galleries",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_displays_owner_id",
                table: "displays",
                column: "owner_id");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_users_owner_id",
                table: "displays",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_galleries_users_owner_id",
                table: "galleries",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Set the first user as admin and owner of all existing displays and galleries
            migrationBuilder.Sql("""
                UPDATE users SET is_admin = 1 WHERE id = (SELECT MIN(id) FROM users);
                UPDATE displays SET owner_id = (SELECT MIN(id) FROM users) WHERE owner_id IS NULL;
                UPDATE galleries SET owner_id = (SELECT MIN(id) FROM users) WHERE owner_id IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_users_owner_id",
                table: "displays");

            migrationBuilder.DropForeignKey(
                name: "FK_galleries_users_owner_id",
                table: "galleries");

            migrationBuilder.DropIndex(
                name: "IX_galleries_owner_id",
                table: "galleries");

            migrationBuilder.DropIndex(
                name: "IX_displays_owner_id",
                table: "displays");

            migrationBuilder.DropColumn(
                name: "is_admin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "hide_from_other_users",
                table: "galleries");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "galleries");

            migrationBuilder.DropColumn(
                name: "hide_from_other_users",
                table: "displays");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "displays");
        }
    }
}
