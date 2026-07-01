using TMDTStore.Models;

namespace TMDTStore.ViewModels;

public class HomeViewModel
{
    public List<Product> FeaturedProducts { get; set; } = new();
    public List<Product> NewProducts { get; set; } = new();
    public List<Product> BestSellers { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
}
