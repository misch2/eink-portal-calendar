using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddThemesContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "themes",
                columns: new[] { "Id", "DisplayName", "FileName", "HasCustomConfig", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Default", "Default", false, true, 0 },
                    { 2, "Portal Style Calendar with Icons", "PortalStyleCalendarWithIcons", true, true, 100 },
                    { 3, "Google Fit Weight with Calendar and Icons", "GoogleFitWeightWithCalendarAndIcons", true, true, 200 },
                    { 4, "Weather", "WeatherForecast", false, true, 300 },
                    { 5, "Multi-day Calendar", "MultidayCalendar", true, true, 400 },
                    { 6, "XKCD", "XKCD", false, true, 500 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
