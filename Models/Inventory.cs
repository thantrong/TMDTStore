using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class Inventory
{
    public string ProductId { get; set; } = null!;

    public int StockQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
