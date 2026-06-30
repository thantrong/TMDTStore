namespace TMDTStore.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels;
using TMDTStore.Services.Cloudinary;
[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ICloudinaryService _cloudinaryService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ICloudinaryService cloudinaryService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _cloudinaryService = cloudinaryService;
    }

    // GET: /Account
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

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

        if (!ModelState.IsValid){
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
}
