using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWakeupJitterDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set wakeup_jitter_seconds to 30 on the default display (id=0)
            // so all displays inherit it unless they override it explicitly.
            migrationBuilder.Sql(@"
                INSERT INTO config (name, value, display_id)
                VALUES ('wakeup_jitter_seconds', '30', 0)
                ON CONFLICT (name, display_id) DO UPDATE SET value = '30'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
