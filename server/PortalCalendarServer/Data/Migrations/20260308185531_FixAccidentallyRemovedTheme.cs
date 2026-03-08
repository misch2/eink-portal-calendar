using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class FixAccidentallyRemovedTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT OR IGNORE INTO themes (id, file_name, display_name, has_custom_config, sort_order, is_active, is_default) VALUES (1, 'Default', 'Default', 0, 0, 1, 1);
                INSERT OR IGNORE INTO themes (id, file_name, display_name, has_custom_config, sort_order, is_active, is_default) VALUES (2, 'PortalStyleCalendarWithIcons', 'Portal Style Calendar with Icons', 1, 100, 1, 0);
                INSERT OR IGNORE INTO themes (id, file_name, display_name, has_custom_config, sort_order, is_active, is_default) VALUES (3, 'GoogleFitWeightWithCalendarAndIcons', 'Google Fit Weight with Calendar and Icons', 1, 200, 1, 0);
                INSERT OR IGNORE INTO themes (id, file_name, display_name, has_custom_config, sort_order, is_active, is_default) VALUES (4, 'WeatherForecast', 'Weather', 1, 300, 1, 0);
                INSERT OR IGNORE INTO themes (id, file_name, display_name, has_custom_config, sort_order, is_active, is_default) VALUES (5, 'MultidayCalendar', 'Multi-day Calendar', 1, 400, 1, 0);
                INSERT OR IGNORE INTO themes (id, file_name, display_name, has_custom_config, sort_order, is_active, is_default) VALUES (6, 'XKCD', 'XKCD', 0, 500, 1, 0);
                INSERT OR IGNORE INTO themes (id, file_name, display_name, has_custom_config, sort_order, is_active, is_default) VALUES (7, 'Test', 'Test - Color Wheel', 0, 10000, 1, 0);
                INSERT OR IGNORE INTO themes (id, file_name, display_name, has_custom_config, sort_order, is_active, is_default) VALUES (8, 'WebImage', 'Image from web', 1, 600, 1, 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
