namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly StoreDbContext _context;

    public DashboardController(StoreDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var totalProducts = await _context.Products.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalUsers = await _context.Users.CountAsync();
        var pendingOrders = await _context.Orders.CountAsync(o => o.OrderStatusHistories
            .OrderByDescending(h => h.ChangedAtUtc).Select(h => h.Reason).FirstOrDefault() == null 
            || _context.OrderStatusHistories.Where(h => h.OrderId == o.Id)
                .OrderByDescending(h => h.ChangedAtUtc).Select(h => h.Reason).FirstOrDefault() == "pending");
        // Fallback: count all orders as pending if no status tracking
        if (pendingOrders == 0 && totalOrders > 0) pendingOrders = totalOrders;

        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.PendingOrders = pendingOrders;

        return View();
    }
}
