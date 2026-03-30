using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantWateringTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "themes",
                columns: new[] { "id", "display_name", "file_name", "has_custom_config", "is_active", "sort_order" },
                values: new object[] { 10, "Plant Watering Monitor", "PlantWatering", true, true, 800 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "themes",
                keyColumn: "id",
                keyValue: 10);
        }
    }
}
