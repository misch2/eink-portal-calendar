using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateThemeIdFromConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE displays 
                SET theme_id = (
                    SELECT CAST(value AS INTEGER) FROM config c
                    WHERE c.name = 'theme_id' AND c.display_id = displays.id
                )
                WHERE theme_id IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
