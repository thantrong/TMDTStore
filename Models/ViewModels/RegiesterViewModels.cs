namespace TMDTStore.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
public class RegisterViewModels
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [DataType(DataType.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = null!;
    
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = null!;
}
