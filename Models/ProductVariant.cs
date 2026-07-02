using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TMDTStore.Models;

public partial class ProductVariant
{
    public string Id { get; set; } = null!;

    [Display(Name = "Sản phẩm")]
    public string ProductId { get; set; } = null!;

    [Display(Name = "Mã SKU")]
    public string Sku { get; set; } = null!;

    [Display(Name = "Tên biến thể")]
    [Required(ErrorMessage = "Vui lòng nhập tên biến thể.")]
    public string Name { get; set; } = null!;

    [Display(Name = "Giá bán")]
    [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải là số dương.")]
    public decimal Price { get; set; }

    [Display(Name = "Giá niêm yết")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá niêm yết phải là số dương.")]
    public decimal? ListPrice { get; set; }

    [Display(Name = "Tồn kho")]
    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải là số nguyên dương.")]
    public int StockQuantity { get; set; }

    [Display(Name = "Ảnh biến thể")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Album ảnh biến thể")]
    public string? ImageUrls { get; set; }

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

    [Display(Name = "Mã nhà sản xuất (MPN)")]
    public string? ManufacturerCode { get; set; }

    [Display(Name = "Thuộc tính")]
    public string? Attributes { get; set; }

    [Display(Name = "Thứ tự")]
    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự phải là số nguyên dương.")]
    public int SortOrder { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "Sản phẩm")]
    public virtual Product Product { get; set; } = null!;
}
