using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class InvertGammaValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE displays SET gamma = 1.0 / gamma WHERE gamma IS NOT NULL AND gamma != 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE displays SET gamma = 1.0 / gamma WHERE gamma IS NOT NULL AND gamma != 0");
        }
    }
}
