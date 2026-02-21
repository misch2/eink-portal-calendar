using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDitherings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dithering_type_code",
                table: "displays",
                type: "VARCHAR",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dithering_types",
                columns: table => new
                {
                    code = table.Column<string>(type: "VARCHAR", nullable: false),
                    name = table.Column<string>(type: "VARCHAR", nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dithering_types", x => x.code);
                });

            migrationBuilder.InsertData(
                table: "dithering_types",
                columns: new[] { "code", "name", "sort_order" },
                values: new object[,]
                {
                    { "", "None", 100 },
                    { "at", "Atkinson", 300 },
                    { "fs", "Floyd-Steinberg", 200 },
                    { "jjn", "Jarvis, Judice, Ninke", 400 },
                    { "st", "Stucki", 500 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_displays_dithering_type_code",
                table: "displays",
                column: "dithering_type_code");

            migrationBuilder.AddForeignKey(
                name: "FK_displays_dithering_types_dithering_type_code",
                table: "displays",
                column: "dithering_type_code",
                principalTable: "dithering_types",
                principalColumn: "code",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_displays_dithering_types_dithering_type_code",
                table: "displays");

            migrationBuilder.DropTable(
                name: "dithering_types");

            migrationBuilder.DropIndex(
                name: "IX_displays_dithering_type_code",
                table: "displays");

            migrationBuilder.DropColumn(
                name: "dithering_type_code",
                table: "displays");
        }
    }
}
