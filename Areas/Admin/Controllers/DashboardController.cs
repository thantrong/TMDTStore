namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class DashboardController : Controller
{
    private readonly StoreDbContext _context;

    public DashboardController(StoreDbContext context)
    {
        _context = context;
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private static DateTime ToUtcKind(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Index()
    {
        var vnTz = GetVietnamTimeZone();
        var utcNow = DateTime.UtcNow;
        var vnNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, vnTz);
        var todayStartVn = vnNow.Date;
        var monthStartVn = new DateTime(vnNow.Year, vnNow.Month, 1);
        var sevenDaysStartVn = todayStartVn.AddDays(-6);

        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStartVn, vnTz);
        var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthStartVn, vnTz);
        var sevenDaysAgoUtc = TimeZoneInfo.ConvertTimeToUtc(sevenDaysStartVn, vnTz);

        var totalProducts = await _context.Products.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalUsers = await _context.Users.CountAsync();
        var pendingOrders = await _context.Orders.CountAsync(o =>
            o.Status == "Pending" || o.Status == "WaitingPayment");

        var lowStockVariants = await _context.ProductVariants
            .CountAsync(v => v.StockQuantity > 0 && v.StockQuantity <= 5 && v.IsActive);
        var outOfStock = await _context.ProductVariants
            .CountAsync(v => v.StockQuantity <= 0 && v.IsActive);

        // Doanh thu tháng: query riêng toàn tháng (không lấy từ cửa sổ 7 ngày)
        var monthRevenue = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == "Delivered" && o.CreatedAt >= monthStartUtc)
            .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

        // Đơn 7 ngày gần nhất (theo giờ VN)
        var allRecentOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= sevenDaysAgoUtc)
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

        var olderStatuses = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt < sevenDaysAgoUtc)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Top bán chạy: chỉ đơn đã giao
        var topProducts = await (
            from oi in _context.OrderItems.AsNoTracking()
            join o in _context.Orders.AsNoTracking() on oi.OrderId equals o.Id
            where o.Status == "Delivered"
            group oi by new { oi.ProductId, oi.Name, oi.ImageUrl } into g
            select new TopProductItem
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.Name,
                ImageUrl = g.Key.ImageUrl,
                TotalSold = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(5)
            .ToListAsync();

        var waitingOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == "Pending" || o.Status == "WaitingPayment")
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        // Đơn gần đây: không giới hạn 7 ngày
        var recentOrders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(8)
            .ToListAsync();

        var todayRevenue = allRecentOrders
            .Where(o => o.Status == "Delivered"
                        && o.CreatedAt.HasValue
                        && ToUtcKind(o.CreatedAt.Value) >= todayStartUtc)
            .Sum(o => o.TotalPrice);

        var ordersToday = allRecentOrders.Count(o =>
            o.CreatedAt.HasValue && ToUtcKind(o.CreatedAt.Value) >= todayStartUtc);

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

        var dailyRevenue = new List<DailyRevenueItem>();
        decimal maxRev = 0m;
        for (var i = 6; i >= 0; i--)
        {
            var dayStartVn = todayStartVn.AddDays(-i);
            var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartVn, vnTz);
            var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartVn.AddDays(1), vnTz);

            var dayOrders = allRecentOrders
                .Where(o => o.CreatedAt.HasValue
                            && ToUtcKind(o.CreatedAt.Value) >= dayStartUtc
                            && ToUtcKind(o.CreatedAt.Value) < dayEndUtc)
                .ToList();

            var rev = dayOrders.Where(o => o.Status == "Delivered").Sum(o => o.TotalPrice);
            var deliveredCount = dayOrders.Count(o => o.Status == "Delivered");
            var orderCount = dayOrders.Count;

            if (rev > maxRev) maxRev = rev;

            dailyRevenue.Add(new DailyRevenueItem
            {
                Date = dayStartVn.ToString("dd/MM"),
                Revenue = rev,
                DeliveredCount = deliveredCount,
                OrderCount = orderCount
            });
        }

        if (maxRev == 0m) maxRev = 1m;

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
        ViewBag.LocalNow = vnNow;

        return View();
    }
}
