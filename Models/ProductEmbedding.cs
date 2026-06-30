using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class ProductEmbedding
{
    public int Id { get; set; }

    public string? ProductId { get; set; }

    public string? ContentChunk { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product? Product { get; set; }
}
