using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCalendarServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class GalleryManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the new primary_folder column (with a temporary default so existing rows are valid)
            migrationBuilder.AddColumn<string>(
                name: "primary_folder",
                table: "gallery_images",
                type: "VARCHAR",
                nullable: false,
                defaultValue: "");

            // 2. Copy gallery_id values into primary_folder as strings
            migrationBuilder.Sql(
                "UPDATE gallery_images SET primary_folder = CAST(gallery_id AS TEXT)");

            // 3. Create the many-to-many join table
            migrationBuilder.CreateTable(
                name: "gallery_image_galleries",
                columns: table => new
                {
                    gallery_id = table.Column<int>(type: "INTEGER", nullable: false),
                    gallery_image_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gallery_image_galleries", x => new { x.gallery_id, x.gallery_image_id });
                    table.ForeignKey(
                        name: "FK_gallery_image_galleries_galleries_gallery_id",
                        column: x => x.gallery_id,
                        principalTable: "galleries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gallery_image_galleries_gallery_images_gallery_image_id",
                        column: x => x.gallery_image_id,
                        principalTable: "gallery_images",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gallery_image_galleries_gallery_image_id",
                table: "gallery_image_galleries",
                column: "gallery_image_id");

            // 4. Populate the join table from existing gallery_id relationships
            migrationBuilder.Sql(
                "INSERT INTO gallery_image_galleries (gallery_id, gallery_image_id) SELECT gallery_id, id FROM gallery_images");

            // 5. Now drop the old foreign key, index, and column
            migrationBuilder.DropForeignKey(
                name: "FK_gallery_images_galleries_gallery_id",
                table: "gallery_images");

            migrationBuilder.DropIndex(
                name: "IX_gallery_images_gallery_id",
                table: "gallery_images");

            migrationBuilder.DropColumn(
                name: "gallery_id",
                table: "gallery_images");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Re-add the gallery_id column
            migrationBuilder.AddColumn<int>(
                name: "gallery_id",
                table: "gallery_images",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // 2. Restore gallery_id from the join table (pick any one gallery per image)
            migrationBuilder.Sql(@"
                UPDATE gallery_images
                SET gallery_id = (
                    SELECT gallery_id
                    FROM gallery_image_galleries
                    WHERE gallery_image_galleries.gallery_image_id = gallery_images.id
                    LIMIT 1
                )");

            // 3. Re-create the index and FK
            migrationBuilder.CreateIndex(
                name: "IX_gallery_images_gallery_id",
                table: "gallery_images",
                column: "gallery_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gallery_images_galleries_gallery_id",
                table: "gallery_images",
                column: "gallery_id",
                principalTable: "galleries",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // 4. Drop the join table and primary_folder column
            migrationBuilder.DropTable(
                name: "gallery_image_galleries");

            migrationBuilder.DropColumn(
                name: "primary_folder",
                table: "gallery_images");
        }
    }
}
