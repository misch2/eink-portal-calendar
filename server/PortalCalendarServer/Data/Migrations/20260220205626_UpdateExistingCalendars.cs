using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingCalendars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE displays
                SET color_variant='BW'
                WHERE color_variant IS NULL
                    AND display_type='BW'
            ");
            migrationBuilder.Sql(@"
                UPDATE displays
                SET color_variant='BWR'
                WHERE color_variant IS NULL
                    AND display_type='3C'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
