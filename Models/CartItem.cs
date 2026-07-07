namespace TMDTStore.Models;

public class CartItem
{
    public string ProductId { get; set; } = null!;
    public string? VariantId { get; set; }
    public string Name { get; set; } = null!;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int MaxQuantity { get; set; } // Tồn kho tối đa cho phép
}
