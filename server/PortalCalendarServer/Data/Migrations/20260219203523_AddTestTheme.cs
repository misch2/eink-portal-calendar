using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddTestTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "themes",
                columns: new[] { "Id", "DisplayName", "FileName", "HasCustomConfig", "IsActive", "SortOrder" },
                values: new object[] { 7, "Test - Color Wheel", "Test", false, true, 10000 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
