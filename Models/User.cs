using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
namespace TMDTStore.Models;

public partial class User : IdentityUser
{

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public bool? IsActive { get; set; }

    public string? EmailConfirmationToken { get; set; }

    public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

}
