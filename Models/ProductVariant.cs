using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class ProductVariant
{
    public string Id { get; set; } = null!;

    public string ProductId { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal? ListPrice { get; set; }

    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public string? Attributes { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
