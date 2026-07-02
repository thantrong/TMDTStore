using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDTStore.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "product_variants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerCode",
                table: "product_variants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "product_variants",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "product_variants",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "ManufacturerCode",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "product_variants");
        }
    }
}
