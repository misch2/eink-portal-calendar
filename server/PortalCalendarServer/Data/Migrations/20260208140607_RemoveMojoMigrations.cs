using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMojoMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mojo_migrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mojo_migrations",
                columns: table => new
                {
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mojo_migrations", x => x.name);
                });
        }
    }
}
