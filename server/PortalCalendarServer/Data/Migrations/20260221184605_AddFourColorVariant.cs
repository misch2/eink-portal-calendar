using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddFourColorVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "3C",
                column: "name",
                value: "3 Colors");

            migrationBuilder.InsertData(
                table: "display_types",
                columns: new[] { "code", "name", "num_colors", "sort_order" },
                values: new object[] { "4C", "4 Colors", 4, 300 });

            migrationBuilder.InsertData(
                table: "color_variants",
                columns: new[] { "Code", "display_type_code", "name", "sort_order" },
                values: new object[] { "BWRY", "4C", "Black, White, Red, Yellow", 3000 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BWRY");

            migrationBuilder.DeleteData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "4C");

            migrationBuilder.UpdateData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "3C",
                column: "name",
                value: "3 Color");
        }
    }
}
