using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class SeedUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed default admin user with hashed password for "changeme"
            migrationBuilder.Sql(
                """
                INSERT INTO "users" ("username", "password_hash")
                VALUES ('admin', 'AQIDBAUGBwgJCgsMDQ4PEA7Cs33HcuaLSH5oRxU4/fJTEZHvHHI2WO4xx+kUFO4j')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
