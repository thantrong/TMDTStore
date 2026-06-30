namespace TMDTStore.Models.ViewModels;

using System.ComponentModel.DataAnnotations;

public class ProfileViewModels
{
    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = null!;

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [DataType(DataType.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Ảnh đại diện (URL)")]
    [Url(ErrorMessage = "URL không hợp lệ.")]
    public string? AvatarUrl { get; set; }
}
