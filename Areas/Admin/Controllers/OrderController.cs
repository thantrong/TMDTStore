namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels.Order;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class OrderController : Controller
{
    private readonly StoreDbContext _context;

    public OrderController(StoreDbContext context)
    {
        _context = context;
    }

    // GET: /Admin/Order
    [HttpGet]
    public async Task<IActionResult> Index(OrderListViewModel model)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .AsQueryable();

        // Search theo tên / SĐT
        if (!string.IsNullOrEmpty(model.SearchQuery))
        {
            var keyword = model.SearchQuery.ToLower();
            query = query.Where(o =>
                o.FullName.ToLower().Contains(keyword) ||
                o.Phone.Contains(keyword) ||
                o.Id.ToLower().Contains(keyword));
        }

        // Filter theo trạng thái
        if (!string.IsNullOrEmpty(model.StatusFilter))
        {
            query = query.Where(o => o.Status == model.StatusFilter);
        }

        model.TotalItems = await query.CountAsync();

        model.Orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();

        return View(model);
    }

    // GET: /Admin/Order/Details/{id}
    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.Voucher)
            .Include(o => o.OrderStatusHistories.OrderByDescending(h => h.ChangedAtUtc))
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    // POST: /Admin/Order/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string id, string status, string? reason)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        // Validate status transition
        var validTransitions = new Dictionary<string, string[]>
        {
            { "Pending", new[] { "Confirmed", "Cancelled" } },
            { "WaitingPayment", new[] { "Confirmed", "Cancelled" } },
            { "Confirmed", new[] { "Shipping", "Cancelled" } },
            { "Shipping", new[] { "Delivered", "Cancelled" } },
            { "Delivered", Array.Empty<string>() },
            { "Cancelled", Array.Empty<string>() }
        };

        if (!validTransitions.ContainsKey(order.Status) ||
            !validTransitions[order.Status].Contains(status))
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = $"Không thể chuyển từ '{order.Status}' sang '{status}'.";
            return RedirectToAction("Details", new { id });
        }

        order.Status = status;

        _context.Set<OrderStatusHistory>().Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = status,
            Reason = reason ?? string.Empty,
            ChangedByUserId = null, // admin
            ChangedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"Đã cập nhật trạng thái đơn hàng thành '{status}'.";
        return RedirectToAction("Details", new { id });
    }
}
