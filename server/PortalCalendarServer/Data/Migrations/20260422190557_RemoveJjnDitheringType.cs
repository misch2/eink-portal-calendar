using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJjnDitheringType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "dithering_types",
                keyColumn: "code",
                keyValue: "jjn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "dithering_types",
                columns: new[] { "code", "name", "sort_order" },
                values: new object[] { "jjn", "Jarvis, Judice, Ninke", 400 });
        }
    }
}
