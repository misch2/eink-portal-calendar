using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class ExtendColorData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "num_colors",
                table: "display_types",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "3C",
                column: "num_colors",
                value: 3);

            migrationBuilder.UpdateData(
                table: "display_types",
                keyColumn: "code",
                keyValue: "BW",
                column: "num_colors",
                value: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "num_colors",
                table: "display_types");
        }
    }
}
