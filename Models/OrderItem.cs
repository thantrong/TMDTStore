using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class OrderItem
{
    public string OrderId { get; set; } = null!;

    public string ProductId { get; set; } = null!;

    public string VariantId { get; set; } = "";

    public string Name { get; set; } = null!;

    public string? VariantName { get; set; }

    public string? ImageUrl { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
