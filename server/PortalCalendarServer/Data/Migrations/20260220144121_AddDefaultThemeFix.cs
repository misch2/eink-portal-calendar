using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultThemeFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // do this only if the default theme doesn't exist, otherwise it will cause a duplicate key error
            if (!migrationBuilder.Sql("SELECT COUNT(*) FROM themes WHERE id = 1").Equals(0))
            {

                migrationBuilder.InsertData(
                table: "themes",
                columns: new[] { "id", "display_name", "file_name", "has_custom_config", "is_active", "is_default", "sort_order" },
                values: new object[] { 1, "Default", "Default", false, true, true, 0 });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
