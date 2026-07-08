namespace TMDTStore.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels;
using TMDTStore.Services.Cloudinary;
using TMDTStore.Services.Email;
[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly StoreDbContext _context;
    private readonly IEmailService _emailService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ICloudinaryService cloudinaryService, StoreDbContext context, IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _cloudinaryService = cloudinaryService;
        _context = context;
        _emailService = emailService;
    }

    // GET: /Account
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        // Thống kê
        var userId = user.Id;
        ViewBag.TotalOrders = await _context.Orders.CountAsync(o => o.UserId == userId && o.Status != "Cancelled");
        ViewBag.TotalReviews = await _context.Reviews.CountAsync(r => r.UserId == userId);

        // Đơn hàng gần đây (5 đơn)
        ViewBag.RecentOrders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        // Đánh giá gần đây (5 đánh giá)
        ViewBag.RecentReviews = await _context.Reviews
            .Include(r => r.Product)
            .Where(r => r.UserId == userId && r.ParentId == null)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View(user);
    }


    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        ViewBag.CurrentAvatar = user.AvatarUrl;

        var model = new ProfileViewModels
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
        };
        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModels model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        ViewBag.CurrentAvatar = user.AvatarUrl;

        if (!ModelState.IsValid)
        {
            ViewBag.ToastType = "error";
            ViewBag.ToastMessage = "Vui lòng kiểm tra lại thông tin.";
            return View(model);
        }

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

        // Upload avatar mới nếu có
        if (model.AvatarImg != null && model.AvatarImg.Length > 0)
        {
            try
            {
                var avatarUrl = await _cloudinaryService.UploadImageAsync(model.AvatarImg, "avatar");
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    user.AvatarUrl = avatarUrl;
                }
            }
            catch (Exception)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không thể tải ảnh lên. Vui lòng thử lại.";
                return View(model);
            }
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // GET: /Account/ChangePassword
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModels model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // POST: /Account/TestMail
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestMail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Vui lòng nhập địa chỉ email.";
            return RedirectToAction("Index");
        }

        try
        {
            await _emailService.SendEmailAsync(
                email,
                "📧 TVT PC - Thư kiểm tra kết nối email",
                $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="utf-8"></head>
                <body style="font-family: Arial, sans-serif; background: #f4f7f6; padding: 40px 20px;">
                    <div style="max-width: 600px; margin: 0 auto; background: #fff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);">
                        <div style="background: linear-gradient(135deg, #0033CC, #1A4BFF); padding: 30px; text-align: center;">
                            <h1 style="color: #fff; margin: 0; font-size: 24px;">📧 Kiểm tra kết nối Email</h1>
                        </div>
                        <div style="padding: 30px;">
                            <p style="font-size: 16px; color: #333; line-height: 1.6;">Xin chào,</p>
                            <p style="font-size: 16px; color: #333; line-height: 1.6;">
                                Đây là email kiểm tra từ hệ thống <strong>TVT PC - Linh kiện máy tính</strong>.
                            </p>
                            <p style="font-size: 16px; color: #333; line-height: 1.6;">
                                Nếu bạn nhận được email này, hệ thống gửi mail đã hoạt động <strong style="color: #16a34a;">✅ thành công</strong>!
                            </p>
                            <div style="background: #f0f4ff; border-radius: 12px; padding: 20px; margin: 20px 0;">
                                <p style="margin: 5px 0; font-size: 14px; color: #555;">
                                    📅 Thời gian gửi: <strong>{DateTime.Now:dd/MM/yyyy HH:mm:ss}</strong>
                                </p>
                                <p style="margin: 5px 0; font-size: 14px; color: #555;">
                                    📬 Đến: <strong>{email}</strong>
                                </p>
                            </div>
                            <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;">
                            <p style="font-size: 13px; color: #999;">
                                TVT PC - Linh kiện máy tính chất lượng cao<br>
                                Email này được gửi tự động từ hệ thống, vui lòng không trả lời.
                            </p>
                        </div>
                    </div>
                </body>
                </html>
                """);

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Email kiểm tra đã được gửi đến {email}! Vui lòng kiểm tra hộp thư (cả Spam).";
        }
        catch (Exception ex)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = $"Gửi email thất bại: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
