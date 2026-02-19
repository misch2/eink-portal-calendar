using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConfigWithThemeIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "default" -> theme_id = 1
            migrationBuilder.Sql(@"
                INSERT INTO config (name, value, display_id)
                SELECT 'theme_id', '1', display_id FROM config c0
                WHERE c0.name = 'theme' AND c0.value = 'default'
                AND EXISTS (
                    SELECT 1 FROM displays d WHERE d.id = c0.display_id
                )   
                AND NOT EXISTS (
                    SELECT 1 FROM config c1
                    WHERE c1.name = 'theme_id' AND c1.display_id = c0.display_id
                )
            ");

            // "01 portal_style_calendar_with_icons" -> theme_id = 2
            migrationBuilder.Sql(@"
                INSERT INTO config (name, value, display_id)
                SELECT 'theme_id', '2', display_id FROM config c0
                WHERE c0.name = 'theme' AND c0.value = '01 portal_style_calendar_with_icons'
                AND EXISTS (
                    SELECT 1 FROM displays d WHERE d.id = c0.display_id
                )   
                AND NOT EXISTS (
                    SELECT 1 FROM config c1
                    WHERE c1.name = 'theme_id' AND c1.display_id = c0.display_id
                )
            ");

            // "02 googlefit_weight_with_calendar_and_icons" -> theme_id = 3
            migrationBuilder.Sql(@"
                INSERT INTO config (name, value, display_id)
                SELECT 'theme_id', '3', display_id FROM config c0
                WHERE c0.name = 'theme' AND c0.value = '02 googlefit_weight_with_calendar_and_icons'
                AND EXISTS (
                    SELECT 1 FROM displays d WHERE d.id = c0.display_id
                )   
                AND NOT EXISTS (
                    SELECT 1 FROM config c1
                    WHERE c1.name = 'theme_id' AND c1.display_id = c0.display_id
                )
            ");

            // "03 weather" -> theme_id = 4
            migrationBuilder.Sql(@"
                INSERT INTO config (name, value, display_id)
                SELECT 'theme_id', '4', display_id FROM config c0
                WHERE c0.name = 'theme' AND c0.value = '03 weather'
                AND EXISTS (
                    SELECT 1 FROM displays d WHERE d.id = c0.display_id
                )   
                AND NOT EXISTS (
                    SELECT 1 FROM config c1
                    WHERE c1.name = 'theme_id' AND c1.display_id = c0.display_id
                )
            ");

            // "04 multiday_calendar" -> theme_id = 5
            migrationBuilder.Sql(@"
                INSERT INTO config (name, value, display_id)
                SELECT 'theme_id', '5', display_id FROM config c0
                WHERE c0.name = 'theme' AND c0.value = '04 multiday_calendar'
                AND EXISTS (
                    SELECT 1 FROM displays d WHERE d.id = c0.display_id
                )   
                AND NOT EXISTS (
                    SELECT 1 FROM config c1
                    WHERE c1.name = 'theme_id' AND c1.display_id = c0.display_id
                )
            ");

            // "06 xkcd" -> theme_id = 6
            migrationBuilder.Sql(@"
                INSERT INTO config (name, value, display_id)
                SELECT 'theme_id', '6', display_id FROM config c0
                WHERE c0.name = 'theme' AND c0.value = '06 xkcd'
                AND NOT EXISTS (
                    SELECT 1 FROM config c1
                    WHERE c1.name = 'theme_id' AND c1.display_id = c0.display_id
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
