using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddWebImageTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "themes",
                columns: new[] { "id", "display_name", "file_name", "has_custom_config", "is_active", "sort_order" },
                values: new object[] { 8, "Image from web", "WebImage", true, true, 600 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "id",
                keyValue: 8);
        }
    }
}
