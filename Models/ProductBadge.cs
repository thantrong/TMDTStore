using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class ProductBadge
{
    public string ProductId { get; set; } = null!;

    public string Label { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
