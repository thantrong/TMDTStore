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
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // ---- Tổng quan ----
        var totalProducts = await _context.Products.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalUsers = await _context.Users.CountAsync();
        var pendingOrders = await _context.Orders.CountAsync(o =>
            o.Status == "Pending" || o.Status == "WaitingPayment");

        // ---- Doanh thu ----
        var todayRevenue = await _context.Orders
            .Where(o => o.Status == "Delivered" && o.CreatedAt >= todayStart)
            .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

        var monthRevenue = await _context.Orders
            .Where(o => o.Status == "Delivered" && o.CreatedAt >= monthStart)
            .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

        var ordersToday = await _context.Orders
            .CountAsync(o => o.CreatedAt >= todayStart);

        // ---- Tồn kho thấp ----
        var lowStockVariants = await _context.ProductVariants
            .CountAsync(v => v.StockQuantity > 0 && v.StockQuantity <= 5 && v.IsActive);
        var outOfStock = await _context.ProductVariants
            .CountAsync(v => v.StockQuantity <= 0 && v.IsActive);

        // ---- Đơn hàng gần đây (7 đơn) ----
        var recentOrders = await _context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(7)
            .ToListAsync();

        // ---- Thống kê theo trạng thái ----
        var statusCounts = await _context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var totalOrdersForStats = statusCounts.Sum(s => s.Count);

        // ---- Top sản phẩm bán chạy ----
        var topProducts = await _context.OrderItems
            .GroupBy(oi => new { oi.ProductId, oi.Name, oi.ImageUrl })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.ImageUrl,
                TotalSold = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(5)
            .ToListAsync();

        // ---- Đơn chờ xử lý gần đây (5 đơn) ----
        var waitingOrders = await _context.Orders
            .Where(o => o.Status == "Pending" || o.Status == "WaitingPayment")
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.PendingOrders = pendingOrders;
        ViewBag.TodayRevenue = todayRevenue;
        ViewBag.MonthRevenue = monthRevenue;
        ViewBag.OrdersToday = ordersToday;
        ViewBag.LowStockVariants = lowStockVariants;
        ViewBag.OutOfStock = outOfStock;
        ViewBag.RecentOrders = recentOrders;
        ViewBag.StatusCounts = statusCounts;
        ViewBag.TotalOrdersForStats = totalOrdersForStats > 0 ? totalOrdersForStats : 1;
        ViewBag.TopProducts = topProducts;
        ViewBag.WaitingOrders = waitingOrders;

        return View();
    }
}
