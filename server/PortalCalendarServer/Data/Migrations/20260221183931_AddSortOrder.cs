using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "display_types",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "color_variants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BW",
                column: "sort_order",
                value: 1000);

            migrationBuilder.UpdateData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BWR",
                column: "sort_order",
                value: 2010);

            migrationBuilder.UpdateData(
                table: "color_variants",
                keyColumn: "Code",
                keyValue: "BWY",
                column: "sort_order",
                value: 2000);

            migrationBuilder.UpdateData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "3C",
                column: "sort_order",
                value: 200);

            migrationBuilder.UpdateData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "BW",
                column: "sort_order",
                value: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "display_types");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "color_variants");
        }
    }
}
