using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddOrangeColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "epd_colors",
                columns: new[] { "code", "epd_preview_hex_value", "hex_value", "name" },
                values: new object[] { "orange", "cc8400", "FFA500", "Orange" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "epd_colors",
                keyColumn: "code",
                keyValue: "orange");
        }
    }
}
