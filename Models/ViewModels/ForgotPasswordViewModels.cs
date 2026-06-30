namespace TMDTStore.Models.ViewModels;

using System.ComponentModel.DataAnnotations;
public class ForgotPasswordViewModels
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = null!;
}
