namespace TMDTStore.Models.ViewModels.Product;

using System.ComponentModel.DataAnnotations;
using TMDTStore.Models;
public class ProductListViewModels
{
    // Filter
    public string? CategoryId { get; set; } // Lọc theo danh mục
    public string? CategorySlug { get; set; } // Slug cho route {category}
    public string? SearchQuery { get; set; } // Lọc theo tên sản phẩm
    public decimal? MinPrice { get; set; } // Lọc theo giá tối thiểu
    public decimal? MaxPrice { get; set; } // Lọc theo giá tối đa
    public string? BrandName { get; set; } // Lọc theo tên thương hiệu
    public string? SortBy { get; set; } // "price_asc", "price_desc", "newest", "rating"
    /// <summary>true = chỉ sản phẩm đang khuyến mãi (có SalePrice hợp lệ).</summary>
    public bool OnSale { get; set; }

    // Danh sách danh mục cho sidebar filter
    public List<Category> Categories { get; set; } = new();

    // Danh sách sản phẩm
    public List<Product> Products { get; set; } = new();

    // Phân trang
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }

    public PagingInfo PagingInfo => new()
    {
        CurrentPage = Page,
        ItemsPerPage = PageSize,
        TotalItems = TotalItems
    };
}
