using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations.Session
{
    /// <inheritdoc />
    public partial class InitialSessionStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_sessions",
                columns: table => new
                {
                    key = table.Column<string>(type: "VARCHAR(128)", nullable: false),
                    value = table.Column<byte[]>(type: "BLOB", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_sessions", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_expires_at",
                table: "auth_sessions",
                column: "expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_sessions");
        }
    }
}
