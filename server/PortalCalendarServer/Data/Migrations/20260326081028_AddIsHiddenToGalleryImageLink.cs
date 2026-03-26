using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsHiddenToGalleryImageLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_hidden",
                table: "gallery_images");

            migrationBuilder.AddColumn<bool>(
                name: "is_hidden",
                table: "gallery_image_galleries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_hidden",
                table: "gallery_image_galleries");

            migrationBuilder.AddColumn<bool>(
                name: "is_hidden",
                table: "gallery_images",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
