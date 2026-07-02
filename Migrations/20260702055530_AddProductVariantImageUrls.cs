using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDTStore.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrls",
                table: "product_variants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "product_variants");
        }
    }
}
