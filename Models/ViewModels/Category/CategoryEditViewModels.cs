namespace TMDTStore.Models.ViewModels.Category;

using System.ComponentModel.DataAnnotations;

public class CategoryEditViewModels
{
    public string Id { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = null!;

    [Display(Name = "Danh mục cha")]
    public string? ParentId { get; set; }
}
