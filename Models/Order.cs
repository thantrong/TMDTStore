using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class Order
{
    public string Id { get; set; } = null!;

    public string? UserId { get; set; }

    public decimal TotalPrice { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? VoucherId { get; set; }

    public decimal? DiscountAmount { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual User? User { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
