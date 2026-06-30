using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class Review
{
    public string Id { get; set; } = null!;

    public string? ProductId { get; set; }

    public string? UserId { get; set; }

    public string? ParentId { get; set; }

    public short? Rating { get; set; }

    public string Comment { get; set; } = null!;

    public bool? IsStaffReply { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Review> InverseParent { get; set; } = new List<Review>();

    public virtual Review? Parent { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User? User { get; set; }
}
