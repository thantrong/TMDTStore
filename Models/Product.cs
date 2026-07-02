using System;
using System.Collections.Generic;

namespace TMDTStore.Models;

public partial class Product
{
    public string Id { get; set; } = null!;

    public string? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public decimal? Price { get; set; }

    public decimal? ListPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public string? TechnicalSpecs { get; set; }

    public string? ReviewSummary { get; set; }

    public DateTime? ReviewSummaryUpdatedAt { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImageUrls { get; set; }

    public List<string>? BadgeLabels { get; set; }

    public string? BrandId { get; set; }

    public string? BrandName { get; set; }

    public decimal RatingAvg { get; set; }

    public int RatingCount { get; set; }

    public int? WarrantyMonths { get; set; }

    public int? ReturnDays { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual Inventory? Inventory { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductBadge> ProductBadges { get; set; } = new List<ProductBadge>();

    public virtual ICollection<ProductEmbedding> ProductEmbeddings { get; set; } = new List<ProductEmbedding>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
