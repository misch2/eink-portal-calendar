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
            // INSERT OR IGNORE lets SQLite skip the insert if id=1 already exists
            // (the broken C# if-guard previously used migrationBuilder.Sql() as a bool,
            // which never worked and always caused a duplicate key on fresh databases)
            migrationBuilder.Sql(
                "INSERT OR IGNORE INTO \"themes\" (\"id\", \"display_name\", \"file_name\", \"has_custom_config\", \"is_active\", \"is_default\", \"sort_order\") " +
                "VALUES (1, 'Default', 'Default', 0, 1, 1, 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
