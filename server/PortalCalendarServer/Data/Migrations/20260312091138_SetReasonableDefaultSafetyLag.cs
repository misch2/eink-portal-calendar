using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class SetReasonableDefaultSafetyLag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set alive_check_safety_lag_minutes to 5 on the default display (id=0)
            // if it is currently missing or set to 0.
            migrationBuilder.Sql(@"
                INSERT INTO config (name, value, display_id)
                SELECT 'alive_check_safety_lag_minutes', '5', 0
                WHERE NOT EXISTS (
                    SELECT 1 FROM config
                    WHERE name = 'alive_check_safety_lag_minutes'
                      AND display_id = 0
                      AND value IS NOT NULL
                      AND value <> ''
                      AND value <> '0'
                )
                ON CONFLICT (name, display_id) DO UPDATE SET value = '5'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
