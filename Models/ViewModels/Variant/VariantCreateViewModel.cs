using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TMDTStore.Models.ViewModels.Variant;

public class VariantCreateViewModel
{
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
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Thuộc tính")]
    public string? Attributes { get; set; }

    [Display(Name = "Mô tả biến thể")]
    public string? Description { get; set; }

    // Hidden, set from route
    public string ProductId { get; set; } = null!;
}
