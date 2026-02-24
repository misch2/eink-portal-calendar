using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class MakeWeatherThemeConfigurable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "themes",
                keyColumn: "id",
                keyValue: 4,
                column: "has_custom_config",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "themes",
                keyColumn: "id",
                keyValue: 4,
                column: "has_custom_config",
                value: false);
        }
    }
}
