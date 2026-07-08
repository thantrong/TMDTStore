using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Services.Cart;
using TMDTStore.Services.Banking;
using TMDTStore.Services.Email;

namespace TMDTStore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly StoreDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IVietQrService _vietQr;
    private readonly IEmailService _emailService;

    public CheckoutController(ICartService cart, StoreDbContext context, UserManager<User> userManager, IVietQrService vietQr, IEmailService emailService)
    {
        _cart = cart;
        _context = context;
        _userManager = userManager;
        _vietQr = vietQr;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var items = _cart.GetCart();
        if (items.Count == 0)
            return RedirectToAction("Index", "Cart");

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string fullName, string phone, string address, string? note, string paymentMethod)
    {
        var items = _cart.GetCart();
        if (items.Count == 0)
            return RedirectToAction("Index", "Cart");

        // Kiểm tra tồn kho
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.VariantId))
            {
                var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                if (variant == null || variant.StockQuantity < item.Quantity)
                {
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = $"Sản phẩm {item.Name} ({item.VariantName}) không đủ hàng.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
                if (inventory == null || inventory.StockQuantity < item.Quantity)
                {
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = $"Sản phẩm {item.Name} không đủ hàng.";
                    return RedirectToAction("Index");
                }
            }
        }

        var user = await _userManager.GetUserAsync(User);
        var order = new Order
        {
            Id = null!, // DB auto-generate
            UserId = user?.Id,
            FullName = fullName,
            Phone = phone,
            Address = address,
            Note = note,
            TotalPrice = _cart.GetCartTotal(),
            Status = paymentMethod == "Banking" ? "WaitingPayment" : "Pending",
            PaymentMethod = paymentMethod,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = paymentMethod == "Banking" ? DateTime.UtcNow.AddMinutes(15) : null
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(); // để lấy Id

        // Trừ tồn kho + tạo OrderItems
        foreach (var item in items)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                Name = item.Name,
                VariantName = item.VariantName,
                ImageUrl = item.ImageUrl,
                Quantity = item.Quantity,
                UnitPrice = item.Price
            });

            // Trừ tồn kho
            if (!string.IsNullOrEmpty(item.VariantId))
            {
                var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                if (variant != null) variant.StockQuantity -= item.Quantity;
            }
            else
            {
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
                if (inventory != null) inventory.StockQuantity -= item.Quantity;
            }
        }

        // Ghi lịch sử trạng thái
        _context.Set<OrderStatusHistory>().Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = order.Status,
            ChangedByUserId = user?.Id,
            ChangedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Xoá giỏ hàng
        _cart.Clear();

        // Gửi email xác nhận đơn hàng (bất đồng bộ, không ảnh hưởng nếu lỗi)
        try
        {
            var paymentLabel = paymentMethod == "Banking" ? "Chuyển khoản (VietQR)" : "Thanh toán khi nhận hàng (COD)";
            var statusLabel = paymentMethod == "Banking" ? "Chờ thanh toán" : "Chờ xử lý";
            var itemsHtml = string.Join("", items.Select(i =>
                $"<tr><td style='padding:8px;border-bottom:1px solid #eee;'>{i.Name}{(string.IsNullOrEmpty(i.VariantName) ? "" : $"<br><small style='color:#999;'>{i.VariantName}</small>")}</td><td style='padding:8px;border-bottom:1px solid #eee;text-align:center'>{i.Quantity}</td><td style='padding:8px;border-bottom:1px solid #eee;text-align:right'>{i.Price.ToString("#,###")}₫</td><td style='padding:8px;border-bottom:1px solid #eee;text-align:right'><strong>{(i.Price * i.Quantity).ToString("#,###")}₫</strong></td></tr>"
            ));

            await _emailService.SendEmailAsync(
                user!.Email!,
                $"📦 TVT PC - Xác nhận đơn hàng #{order.Id}",
                $"""
                <!DOCTYPE html>
                <html><head><meta charset="utf-8"></head>
                <body style="font-family:Arial,sans-serif;background:#f4f7f6;padding:30px 20px;">
                    <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);">
                        <div style="background:linear-gradient(135deg,#0033CC,#1A4BFF);padding:30px;text-align:center;">
                            <h1 style="color:#fff;margin:0;font-size:22px;">📦 Đặt hàng thành công!</h1>
                            <p style="color:#a6c1ff;margin:8px 0 0;">Mã đơn: <strong style="color:#fff;">#{order.Id}</strong></p>
                        </div>
                        <div style="padding:30px;">
                            <p style="font-size:15px;color:#333;">Xin chào <strong>{user.FullName}</strong>,</p>
                            <p style="font-size:15px;color:#333;">Cảm ơn bạn đã đặt hàng tại <strong>TVT PC</strong>. Đơn hàng của bạn đã được tiếp nhận và đang được xử lý.</p>
                            <div style="background:#f0f4ff;border-radius:12px;padding:20px;margin:20px 0;">
                                <p style="margin:5px 0;font-size:14px;color:#555;">🔢 Mã đơn: <strong>#{order.Id}</strong></p>
                                <p style="margin:5px 0;font-size:14px;color:#555;">📌 Trạng thái: <strong>{statusLabel}</strong></p>
                                <p style="margin:5px 0;font-size:14px;color:#555;">💳 Thanh toán: <strong>{paymentLabel}</strong></p>
                                <p style="margin:5px 0;font-size:14px;color:#555;">📅 Ngày đặt: <strong>{DateTime.Now:dd/MM/yyyy HH:mm}</strong></p>
                            </div>
                            <h3 style="font-size:15px;color:#333;margin:20px 0 10px;">🛒 Sản phẩm đã đặt</h3>
                            <table style="width:100%;border-collapse:collapse;font-size:14px;">
                                <thead><tr style="background:#f8fafc;"><th style="padding:8px;text-align:left;border-bottom:2px solid #e5e7eb;">Sản phẩm</th><th style="padding:8px;text-align:center;border-bottom:2px solid #e5e7eb;">SL</th><th style="padding:8px;text-align:right;border-bottom:2px solid #e5e7eb;">Đơn giá</th><th style="padding:8px;text-align:right;border-bottom:2px solid #e5e7eb;">Thành tiền</th></tr></thead>
                                <tbody>{itemsHtml}</tbody>
                                <tfoot>
                                    <tr><td colspan="3" style="padding:12px 8px;text-align:right;font-weight:bold;">Tổng cộng:</td><td style="padding:12px 8px;text-align:right;font-weight:bold;font-size:16px;color:#dc2626;">{order.TotalPrice.ToString("#,###")}₫</td></tr>
                                </tfoot>
                            </table>
                            <hr style="border:none;border-top:1px solid #e5e7eb;margin:20px 0;">
                            <p style="font-size:14px;color:#555;"><strong>📍 Địa chỉ giao hàng:</strong><br>{fullName}<br>{phone}<br>{address}</p>
                            {(!string.IsNullOrEmpty(note) ? $"<p style='font-size:14px;color:#555;'><strong>📝 Ghi chú:</strong> {note}</p>" : "")}
                            <hr style="border:none;border-top:1px solid #e5e7eb;margin:20px 0;">
                            <p style="font-size:13px;color:#999;text-align:center;">
                                TVT PC - Linh kiện máy tính chất lượng cao<br>
                                Mọi thắc mắc vui lòng liên hệ qua email này.
                            </p>
                        </div>
                    </div>
                </body>
                </html>
                """);
        }
        catch { /* Email lỗi không ảnh hưởng đến đơn hàng */ }

        if (paymentMethod == "Banking")
            return RedirectToAction("Payment", "Order", new { id = order.Id });

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Đặt hàng thành công!";
        return RedirectToAction("Success", "Order", new { id = order.Id });
    }
}
