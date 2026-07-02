using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TMDTStore.Models.ViewModels.Variant;

public class VariantEditViewModel
{
    public string Id { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập tên biến thể.")]
    [Display(Name = "Tên biến thể")]
    public string Name { get; set; } = null!;

    [Display(Name = "Mã SKU")]
    public string? Sku { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải là số dương.")]
    [Display(Name = "Giá bán")]
    public decimal Price { get; set; }

    [Display(Name = "Giá niêm yết")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá niêm yết phải là số dương.")]
    public decimal? ListPrice { get; set; }

    [Display(Name = "Tồn kho")]
    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải là số nguyên dương.")]
    public int StockQuantity { get; set; }

    [Display(Name = "Thứ tự")]
    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự phải là số nguyên dương.")]
    public int SortOrder { get; set; }

    [Display(Name = "Ảnh biến thể")]
    public List<IFormFile>? ImageFiles { get; set; }

    [Display(Name = "Album ảnh hiện tại")]
    public string? ExistingImageUrls { get; set; }

    [Display(Name = "Thuộc tính")]
    public string? Attributes { get; set; }

    [Display(Name = "Mô tả biến thể")]
    public string? Description { get; set; }

    [Display(Name = "Giá khuyến mãi")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá khuyến mãi phải là số dương.")]
    public decimal? SalePrice { get; set; }

    [Display(Name = "Trọng lượng (g)")]
    [Range(0, 100000, ErrorMessage = "Trọng lượng từ 0-100.000g.")]
    public int? Weight { get; set; }

    [Display(Name = "Mã vạch")]
    public string? Barcode { get; set; }

    [Display(Name = "Mã nhà sản xuất")]
    public string? ManufacturerCode { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; }

    public string ProductId { get; set; } = null!;
}
