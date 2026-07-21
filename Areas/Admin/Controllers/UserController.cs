namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly StoreDbContext _context;

    public UserController(UserManager<User> userManager, RoleManager<Role> roleManager, StoreDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? role, int page = 1)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var kw = search.ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(kw) ||
                u.Email!.ToLower().Contains(kw) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
        }

        // Lọc theo vai trò — cần join bảng AspNetUserRoles
        if (!string.IsNullOrEmpty(role))
        {
            var roleObj = await _roleManager.FindByNameAsync(role);
            if (roleObj != null)
            {
                var userIdsInRole = await _context.Set<IdentityUserRole<string>>()
                    .Where(ur => ur.RoleId == roleObj.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
        }

        var totalItems = await query.CountAsync();
        var pageSize = 15;
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Lấy role cho từng user
        var userRoles = new Dictionary<string, List<string>>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles.ToList();
        }

        ViewBag.UserRoles = userRoles;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Search = search;
        ViewBag.RoleFilter = role;
        ViewBag.AllRoles = await _roleManager.Roles.ToListAsync();

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = roles;

        // Thống kê
        ViewBag.TotalOrders = await _context.Orders.CountAsync(o => o.UserId == id);
        ViewBag.TotalReviews = await _context.Reviews.CountAsync(r => r.UserId == id);
        ViewBag.TotalSpent = await _context.Orders
            .Where(o => o.UserId == id && o.Status == "Delivered")
            .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

        // Đơn hàng gần đây
        ViewBag.RecentOrders = await _context.Orders
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View(user);
    }

    // POST: /Admin/User/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Không cho khoá chính mình
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == id)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Bạn không thể khoá tài khoản của chính mình.";
            return RedirectToAction("Index");
        }

        user.IsActive = !(user.IsActive ?? true);

        if (user.IsActive == false)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else
        {
            user.LockoutEnd = null;
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Đã {(user.IsActive == true ? "kích hoạt" : "khoá")} tài khoản {user.Email}.";
        }
        else
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Không thể cập nhật trạng thái.";
        }

        return RedirectToAction("Index");
    }

    // POST: /Admin/User/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Mật khẩu phải có ít nhất 6 ký tự.";
            return RedirectToAction("Details", new { id });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Đã reset mật khẩu cho {user.Email}.";
        }
        else
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = $"Lỗi: {string.Join(", ", result.Errors.Select(e => e.Description))}";
        }

        return RedirectToAction("Details", new { id });
    }
}
