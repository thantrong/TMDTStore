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
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModels model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && !user.EmailConfirmed)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Vui lòng xác nhận email trước khi đăng nhập. Kiểm tra hộp thư (cả Spam).";
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            // Chuyển hướng về ReturnUrl nếu có
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home", new { area = "" });
        }
        TempData["ToastType"] = "error";
        TempData["ToastMessage"] = "Email hoặc mật khẩu không đúng. Vui lòng thử lại.";
        if (result.IsLockedOut)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng thử lại sau.";
        }
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
            // Gửi email xác nhận
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(NewUser);
                // Tạo link xác nhận hoạt động cho cả local development và production
                var confirmLink = Url.Action("ConfirmEmail", "Auth",
                    new { userId = NewUser.Id, token }, Request.Scheme);

                // Nếu đang chạy local, sử dụng localhost:5001
                if (Request.Scheme == "http" && Request.Host.Value.Contains("localhost"))
                {
                    confirmLink = $"http://localhost:5001/Auth/ConfirmEmail?userId={NewUser.Id}&token={token}";
                }

                await _emailService.SendEmailAsync(
                    model.Email,
                    "Xác nhận email - TVT PC",
                    $"<h2>Xin chào {model.FullName},</h2>" +
                    $"<p>Cảm ơn bạn đã đăng ký tài khoản tại <b>TVT PC</b>.</p>" +
                    $"<p>Vui lòng click vào nút bên dưới để xác nhận email:</p>" +
                    $"<p style='text-align:center;margin:30px 0;'>" +
                    $"<a href='{confirmLink}' " +
                    $"style='display:inline-block;padding:14px 32px;background:#0033CC;color:white;text-decoration:none;border-radius:8px;font-weight:bold;font-size:16px;'>" +
                    $"Xác nhận email</a></p>" +
                    $"<p>Link này hết hạn sau 24 giờ.</p>" +
                    $"<p>Nếu bạn không đăng ký, vui lòng bỏ qua email này.</p>");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi gửi email nhưng không ảnh hưởng đến đăng ký
                System.Diagnostics.Debug.WriteLine($"Lỗi gửi email xác nhận: {ex.Message}");
                // Hiển thị thông báo cho người dùng
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản. Nếu không nhận được email, vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Login", "Auth", new { area = "" });
            }

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.";
            return RedirectToAction("Login", "Auth", new { area = "" });
        }
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Link xác nhận không hợp lệ.";
            return RedirectToAction("Login");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Người dùng không tồn tại.";
            return RedirectToAction("Login");
        }

        if (user.EmailConfirmed)
        {
            ViewBag.Message = "Email của bạn đã được xác nhận trước đó.";
            return View();
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            ViewBag.Message = "Xác nhận email thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return View();
        }

        TempData["ToastType"] = "error";
        TempData["ToastMessage"] = "Xác nhận email thất bại. Link có thể đã hết hạn.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["ToastType"] = "info";
        TempData["ToastMessage"] = "Bạn đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    public IActionResult ForgotPassword() => View();
    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModels model)
    {
        if (!ModelState.IsValid) return View(model);
        var CheckUser = await _userManager.FindByEmailAsync(model.Email);
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
        return RedirectToAction("ForgotPasswordConfirmation", "Auth", new { area = "" });
    }

    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? token, string? email)
    {
        if (token == null || email == null)
        {
            return RedirectToAction("Login", "Auth", new { area = "" });
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
            return RedirectToAction("ResetPasswordConfirmation", "Auth", new { area = "" });
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction("ResetPasswordConfirmation", "Auth", new { area = "" });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    public IActionResult ResetPasswordConfirmation() => View();
}
