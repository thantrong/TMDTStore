namespace TMDTStore.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels;

[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Account
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        return View(user);
    }

    // GET: /Account/Profile
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        var model = new ProfileViewModels
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
        };
        return View(model);
    }

    // POST: /Account/Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModels model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.AvatarUrl = model.AvatarUrl;

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

    // POST: /Account/ChangePassword
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
