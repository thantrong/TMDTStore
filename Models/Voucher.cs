using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class Voucher
{
    public string Id { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string DiscountType { get; set; } = null!; // "fixed" or "percentage"

    public decimal DiscountValue { get; set; }

    public decimal? MinOrderValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? UsageLimit { get; set; }

    public int? UsedCount { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
