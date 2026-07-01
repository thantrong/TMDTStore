namespace TMDTStore.Models.ViewModels.Category;
using System.ComponentModel.DataAnnotations;
using TMDTStore.Models;
public class CategoryCreateViewModels
{
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = null!;

    [Display(Name = "Danh mục cha")]
    public string? ParentId { get; set; }
    public string? Slug { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();
    public virtual Category? Parent { get; set; }
}