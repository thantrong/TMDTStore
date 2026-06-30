namespace TMDTStore.Models.ViewModels.Product;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TMDTStore.Models;
public class ProductCreatViewModels
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string BrandName { get; set; } = null!;
    public string CategoryId { get; set; } = null!;
    public List<IFormFile>? ImageFile { get; set; }
    public List<ProductBadge> ProductBadges { get; set; } = new List<ProductBadge>();
    public List<string> SelectedBadgeIds { get; set; } = new List<string>();
    public List<Category> Categories { get; set; } = new List<Category>();
}
