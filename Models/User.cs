using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class User
{
    public string Id { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public bool? IsActive { get; set; }

    public bool EmailConfirmed { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public string? EmailConfirmationToken { get; set; }

    public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
