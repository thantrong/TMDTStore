namespace TMDTStore.Models.ViewModels.Product;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TMDTStore.Models;
public class ProductCreatViewModels
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = null!;

    [Display(Name = "Mô tả ngắn")]
    [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự.")]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    [Display(Name = "Thông số kỹ thuật")]
    public string? TechnicalSpecs { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải là số dương.")]
    [Display(Name = "Giá bán")]
    public decimal Price { get; set; }

    [Display(Name = "Giá niêm yết")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá niêm yết phải là số dương.")]
    public decimal? ListPrice { get; set; }

    [Display(Name = "Giá khuyến mãi")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá khuyến mãi phải là số dương.")]
    public decimal? SalePrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho.")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải là số nguyên dương.")]
    [Display(Name = "Tồn kho")]
    public int Quantity { get; set; }

    [StringLength(100, ErrorMessage = "Tên thương hiệu không được vượt quá 100 ký tự.")]
    public string? BrandName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public string CategoryId { get; set; } = null!;

    [Display(Name = "Bảo hành (tháng)")]
    [Range(0, 120, ErrorMessage = "Bảo hành từ 0-120 tháng.")]
    public int? WarrantyMonths { get; set; }

    [Display(Name = "Đổi trả (ngày)")]
    [Range(0, 365, ErrorMessage = "Đổi trả từ 0-365 ngày.")]
    public int? ReturnDays { get; set; }

    [Display(Name = "Ảnh sản phẩm")]
    public List<IFormFile>? ImageFile { get; set; }

    public List<ProductBadge> ProductBadges { get; set; } = new List<ProductBadge>();
    public List<string> SelectedBadgeIds { get; set; } = new List<string>();
    public List<Category> Categories { get; set; } = new List<Category>();
}
