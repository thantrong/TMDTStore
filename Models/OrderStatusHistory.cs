using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class OrderStatusHistory
{
    public int Id { get; set; }

    public string OrderId { get; set; } = null!;

    public string? Reason { get; set; }

    public string? ChangedByUserId { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual Order Order { get; set; } = null!;
}
