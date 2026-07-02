using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDTStore.Migrations
{
    /// <inheritdoc />
    public partial class FixVariantImageUrlsColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrls",
                table: "product_variants",
                newName: "image_urls");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "image_urls",
                table: "product_variants",
                newName: "ImageUrls");
        }
    }
}
