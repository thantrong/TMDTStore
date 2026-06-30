namespace TMDTStore.Controllers;

using Microsoft.AspNetCore.Mvc;
using TMDTStore.Models;
using Microsoft.AspNetCore.Identity;
using TMDTStore.Models.ViewModels;
using TMDTStore.Services.Email;
using System.Threading.Tasks;

public class AuthController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IEmailService _emailService;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
    }
    public IActionResult Login() => View();
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModels model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }
        TempData["ToastType"] = "error";
        TempData["ToastMessage"] = "Email hoặc mật khẩu không đúng. Vui lòng thử lại.";
        return View(model);
    }
    public IActionResult Register() => View();
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModels model)
    {
        if (!ModelState.IsValid) return View(model);

        var NewUser = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var result = await _userManager.CreateAsync(NewUser, model.Password);
        if (result.Succeeded)
        {
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login", "Auth");
        }
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // Handle logout logic here
        await _signInManager.SignOutAsync();
        TempData["ToastType"] = "info";
        TempData["ToastMessage"] = "Bạn đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult ForgotPassword() => View();
    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModels model)
    {
        if (!ModelState.IsValid) return View(model);
        var CheckUser = _userManager.FindByEmailAsync(model.Email).Result;
        if (CheckUser != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(CheckUser);
            var resetLink = Url.Action("ResetPassword", "Auth", new { token, email = model.Email }, Request.Scheme);

            // Send the reset link via email
            await _emailService.SendEmailAsync(
            model.Email,
            "Đặt lại mật khẩu - TMDTStore",
            $"""
            <h2>Xin chào {CheckUser.FullName},</h2>
            <p>Bạn vừa yêu cầu đặt lại mật khẩu.</p>
            <p>Click vào link bên dưới để đặt lại mật khẩu:</p>
            <p><a href="{resetLink}" style="display:inline-block;padding:12px 24px;background:#4f46e5;color:white;text-decoration:none;border-radius:6px;">
            Đặt lại mật khẩu</a></p>
            <p>Link này hết hạn sau 24 giờ.</p>
            <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
            """);
        }
        return RedirectToAction("ForgotPasswordConfirmation", "Auth");
    }

    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? token, string? email)
    {
        if (token == null || email == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var model = new ResetPasswordViewModels
        {
            Token = token,
            Email = email
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModels model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return RedirectToAction("ResetPasswordConfirmation", "Auth");
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction("ResetPasswordConfirmation", "Auth");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    public IActionResult ResetPasswordConfirmation() => View();
}
