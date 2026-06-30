namespace TMDTStore.Models.ViewModels.Product;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TMDTStore.Models;
public class ProductEditViewModels
{
    public string Id { get; set; } = null!;
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = null!;
    [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm.")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải là một số dương.")]
    public decimal Price { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho.")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải là một số nguyên dương.")]
    public int InventoryQuantity { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn danh mục sản phẩm.")]
    public string CategoryId { get; set; } = null!;
    public IFormFile? ImageFile { get; set; }
    public string? ExistingImageUrl { get; set; }
    public string? Description { get; set; }
    [StringLength(100, ErrorMessage = "Tên thương hiệu không được vượt quá 100 ký tự.")]
    public string? BrandName { get; set; }
    public List<string>? SelectedBadgeIds { get; set; }
    public List<ProductBadge>? AvailableBadges { get; set; }
    public List<Category> Categories { get; set; } = new();
}
