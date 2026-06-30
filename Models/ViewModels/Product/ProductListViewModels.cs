namespace TMDTStore.Models.ViewModels.Product;
using System.ComponentModel.DataAnnotations;
using TMDTStore.Models;
public class ProductListViewModels
{
    // Filter
    public string? CategoryId { get; set; } // Lọc theo danh mục
    public string? SearchQuery { get; set; } // Lọc theo tên sản phẩm
    public decimal? MinPrice { get; set; } // Lọc theo giá tối thiểu
    public decimal? MaxPrice { get; set; } // Lọc theo giá tối đa
    public string? BrandName { get; set; } // Lọc theo tên thương hiệu
    public string? SortBy { get; set; } // "price_asc", "price_desc", "newest", "rating"

    // Danh sách danh mục cho sidebar filter
    public List<Category> Categories { get; set; } = new();

    // Danh sách sản phẩm
    public List<Product> Products { get; set; } = new();

    // Phân trang
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
