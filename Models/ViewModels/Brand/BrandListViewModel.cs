using TMDTStore.Models;

namespace TMDTStore.Models.ViewModels.Brand;

public class BrandListViewModel
{
    public string? SearchQuery { get; set; }
    public List<TMDTStore.Models.Brand> Brands { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
