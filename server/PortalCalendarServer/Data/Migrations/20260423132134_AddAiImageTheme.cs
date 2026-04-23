using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiImageTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "themes",
                columns: new[] { "id", "display_name", "file_name", "has_custom_config", "is_active", "sort_order" },
                values: new object[] { 11, "AI Generated Image", "AiImage", true, true, 850 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "id",
                keyValue: 11);
        }
    }
}
