namespace TMDTStore.Models.ViewModels;

using System.ComponentModel.DataAnnotations;

public class ProfileViewModels
{
    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Display(Name = "Số điện thoại")]
    [DataType(DataType.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? AvatarImg { get; set; }
}