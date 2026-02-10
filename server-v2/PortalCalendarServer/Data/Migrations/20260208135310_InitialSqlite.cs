using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cache",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    creator = table.Column<string>(type: "VARCHAR(255)", nullable: false),
                    key = table.Column<string>(type: "VARCHAR(255)", nullable: false),
                    created_at = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "0"),
                    expires_at = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "0"),
                    data = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "displays",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    mac = table.Column<string>(type: "VARCHAR", nullable: false),
                    name = table.Column<string>(type: "VARCHAR", nullable: false),
                    width = table.Column<int>(type: "INTEGER", nullable: false),
                    height = table.Column<int>(type: "INTEGER", nullable: false),
                    rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    colortype = table.Column<string>(type: "VARCHAR", nullable: false),
                    gamma = table.Column<double>(type: "NUMERIC(4,2)", nullable: true),
                    border_top = table.Column<int>(type: "INTEGER", nullable: false),
                    border_right = table.Column<int>(type: "INTEGER", nullable: false),
                    border_bottom = table.Column<int>(type: "INTEGER", nullable: false),
                    border_left = table.Column<int>(type: "INTEGER", nullable: false),
                    firmware = table.Column<string>(type: "VARCHAR", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_displays", x => x.id);
                });

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

            migrationBuilder.CreateTable(
                name: "config",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "VARCHAR", nullable: false),
                    value = table.Column<string>(type: "VARCHAR", nullable: true),
                    display_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_config_displays_display_id",
                        column: x => x.display_id,
                        principalTable: "displays",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "cache_creator_key",
                table: "cache",
                columns: new[] { "creator", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "cache_expires_at",
                table: "cache",
                columns: new[] { "expires_at", "creator" });

            migrationBuilder.CreateIndex(
                name: "config_name_display",
                table: "config",
                columns: new[] { "name", "display_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_config_display_id",
                table: "config",
                column: "display_id");

            migrationBuilder.CreateIndex(
                name: "IX_displays_mac",
                table: "displays",
                column: "mac",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_displays_name",
                table: "displays",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cache");

            migrationBuilder.DropTable(
                name: "config");

            migrationBuilder.DropTable(
                name: "mojo_migrations");

            migrationBuilder.DropTable(
                name: "displays");
        }
    }
}
