using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Services.Banking;

namespace TMDTStore.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly StoreDbContext _context;
    private readonly IVietQrService _vietQr;

    public OrderController(StoreDbContext context, IVietQrService vietQr)
    {
        _context = context;
        _vietQr = vietQr;
    }

    [HttpGet]
    public async Task<IActionResult> Success(string id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Payment(string id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        if (order.Status != "WaitingPayment")
            return RedirectToAction("Success", new { id });

        ViewBag.QrImageUrl = _vietQr.GenerateQrImageUrl(order.Id, order.TotalPrice);
        ViewBag.Content = _vietQr.GenerateContent(order.Id);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(string id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();
        if (order.Status != "WaitingPayment")
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Đơn hàng không ở trạng thái chờ thanh toán.";
            return RedirectToAction("Success", new { id });
        }

        order.Status = "Confirmed";
        order.ExpiresAt = null;

        _context.Set<OrderStatusHistory>().Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = "Confirmed",
            Reason = "Khách xác nhận đã thanh toán",
            ChangedByUserId = null,
            ChangedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Xác nhận thanh toán thành công!";
        return RedirectToAction("Success", new { id });
    }
}
