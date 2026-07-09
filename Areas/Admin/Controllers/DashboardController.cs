namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class DashboardController : Controller
{
    private readonly StoreDbContext _context;

    public DashboardController(StoreDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var sevenDaysAgo = todayStart.AddDays(-6);

        // ==== Chạy song song các query độc lập ====
        var totalProductsTask = _context.Products.CountAsync();
        var totalOrdersTask = _context.Orders.CountAsync();
        var totalUsersTask = _context.Users.CountAsync();
        var pendingOrdersTask = _context.Orders.CountAsync(o =>
            o.Status == "Pending" || o.Status == "WaitingPayment");

        var lowStockTask = _context.ProductVariants
            .CountAsync(v => v.StockQuantity > 0 && v.StockQuantity <= 5 && v.IsActive);
        var outOfStockTask = _context.ProductVariants
            .CountAsync(v => v.StockQuantity <= 0 && v.IsActive);

        // 1 query lấy đơn 7 ngày gần nhất — tính toán mọi thứ từ memory
        var recentOrdersTask = _context.Orders
            .Where(o => o.CreatedAt >= sevenDaysAgo)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalPrice,
                o.CreatedAt,
                o.FullName,
                o.PaymentMethod,
                o.ShippingFee,
                o.DiscountAmount
            })
            .ToListAsync();

        // Thống kê trạng thái cho đơn cũ hơn 7 ngày
        var olderStatusesTask = _context.Orders
            .Where(o => o.CreatedAt < sevenDaysAgo)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Top sản phẩm bán chạy
        var topProductsTask = _context.OrderItems
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

        // Đơn chờ xử lý
        var waitingOrdersTask = _context.Orders
            .Where(o => o.Status == "Pending" || o.Status == "WaitingPayment")
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        await Task.WhenAll(
            totalProductsTask, totalOrdersTask, totalUsersTask, pendingOrdersTask,
            lowStockTask, outOfStockTask, recentOrdersTask, olderStatusesTask,
            topProductsTask, waitingOrdersTask
        );

        // ---- Lấy kết quả ----
        var totalProducts = totalProductsTask.Result;
        var totalOrders = totalOrdersTask.Result;
        var totalUsers = totalUsersTask.Result;
        var pendingOrders = pendingOrdersTask.Result;
        var lowStockVariants = lowStockTask.Result;
        var outOfStock = outOfStockTask.Result;
        var allRecentOrders = recentOrdersTask.Result;
        var olderStatuses = olderStatusesTask.Result;
        var topProducts = topProductsTask.Result;
        var waitingOrders = waitingOrdersTask.Result;

        // ---- Tính toán trên memory (0 query thêm) ----

        // Doanh thu hôm nay / tháng
        var todayRevenue = allRecentOrders
            .Where(o => o.Status == "Delivered" && o.CreatedAt >= todayStart)
            .Sum(o => o.TotalPrice);
        var monthRevenue = allRecentOrders
            .Where(o => o.Status == "Delivered" && o.CreatedAt >= monthStart)
            .Sum(o => o.TotalPrice);
        var ordersToday = allRecentOrders.Count(o => o.CreatedAt >= todayStart);

        // Thống kê trạng thái — gộp recent + older
        var statusDict = allRecentOrders
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());
        foreach (var s in olderStatuses)
        {
            if (statusDict.ContainsKey(s.Status))
                statusDict[s.Status] += s.Count;
            else
                statusDict[s.Status] = s.Count;
        }
        var totalOrdersForStats = statusDict.Values.Sum();

        // Doanh thu 7 ngày — tính trên memory
        var dailyRevenue = new List<dynamic>();
        decimal maxRev = 0;
        for (int i = 6; i >= 0; i--)
        {
            var dayStart = now.Date.AddDays(-i);
            var dayEnd = dayStart.AddDays(1);
            var rev = allRecentOrders
                .Where(o => o.Status == "Delivered" && o.CreatedAt >= dayStart && o.CreatedAt < dayEnd)
                .Sum(o => o.TotalPrice);
            var count = allRecentOrders.Count(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd);
            if (rev > maxRev) maxRev = rev;
            dailyRevenue.Add(new { Date = dayStart.ToString("dd/MM"), Revenue = rev, Count = count });
        }
        if (maxRev == 0) maxRev = 1m;

        // 7 đơn gần đây nhất
        var recentOrders = allRecentOrders
            .OrderByDescending(o => o.CreatedAt)
            .Take(7)
            .Select(o => new Order
            {
                Id = o.Id,
                Status = o.Status,
                FullName = o.FullName,
                PaymentMethod = o.PaymentMethod,
                CreatedAt = o.CreatedAt,
                TotalPrice = o.TotalPrice,
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount
            })
            .ToList();

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
        ViewBag.StatusCounts = statusDict;
        ViewBag.TotalOrdersForStats = totalOrdersForStats > 0 ? totalOrdersForStats : 1;
        ViewBag.TopProducts = topProducts;
        ViewBag.WaitingOrders = waitingOrders;
        ViewBag.DailyRevenue = dailyRevenue;
        ViewBag.MaxRevenue = maxRev;

        return View();
    }
}
