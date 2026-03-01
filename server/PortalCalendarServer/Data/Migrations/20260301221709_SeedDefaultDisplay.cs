using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // INSERT OR IGNORE lets SQLite skip the insert if id=1 already exists
            // (the broken C# if-guard previously used migrationBuilder.Sql() as a bool,
            // which never worked and always caused a duplicate key on fresh databases)
            migrationBuilder.Sql(
                """
                INSERT OR IGNORE INTO "displays"
                    ("id", "name", "mac", "width", "height", "rotation",
                     "border_top", "border_right", "border_bottom", "border_left",
                     "firmware", "gamma",
                     "theme_id", "display_type", "color_variant", "dithering_type_code",
                     "rendered_at", "render_errors")
                VALUES
                    (0, 'Default settings', NULL, 0, 0, 0,
                     0, 0, 0, 0,
                     'Default', 1.8,
                     NULL, NULL, NULL, NULL,
                     NULL, NULL)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
