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

    // POST: /Checkout/ApplyVoucher
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> ApplyVoucher(string code)
    {
        var items = _cart.GetCart();
        var total = _cart.GetCartTotal();

        if (string.IsNullOrWhiteSpace(code))
        {
            return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
        }

        var voucher = await _context.Vouchers
            .FirstOrDefaultAsync(v => v.Code == code.ToUpper() && v.IsActive == true);

        if (voucher == null)
        {
            return Json(new { success = false, message = "Mã giảm giá không hợp lệ." });
        }

        // Kiểm tra hạn sử dụng
        var now = DateTime.UtcNow;
        if (now < voucher.StartDate.ToUniversalTime())
        {
            return Json(new { success = false, message = $"Mã giảm giá có hiệu lực từ {voucher.StartDate:dd/MM/yyyy}." });
        }
        if (now > voucher.EndDate.ToUniversalTime())
        {
            return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });
        }

        // Kiểm tra lượt dùng
        if (voucher.UsageLimit.HasValue && (voucher.UsedCount ?? 0) >= voucher.UsageLimit.Value)
        {
            return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });
        }

        // Kiểm tra đơn tối thiểu
        if (voucher.MinOrderValue.HasValue && total < voucher.MinOrderValue.Value)
        {
            return Json(new { success = false, message = $"Đơn hàng tối thiểu {voucher.MinOrderValue.Value:#,###}₫ để áp dụng mã này." });
        }

        // Tính discount
        decimal discountAmount = 0;
        if (voucher.DiscountType == "fixed")
        {
            discountAmount = voucher.DiscountValue;
        }
        else // percentage
        {
            discountAmount = total * voucher.DiscountValue / 100;
            if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount.Value)
            {
                discountAmount = voucher.MaxDiscountAmount.Value;
            }
        }

        return Json(new
        {
            success = true,
            message = $"Đã áp dụng mã {voucher.Code}!",
            discountAmount = discountAmount,
            discountFormatted = discountAmount.ToString("#,###") + "₫"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string fullName, string phone, string address, string? note, string paymentMethod, string? voucherCode)
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
        var cartTotal = _cart.GetCartTotal();
        var discountAmount = 0m;

        // Xử lý voucher nếu có
        Voucher? appliedVoucher = null;
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            appliedVoucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == voucherCode.ToUpper() && v.IsActive == true);

            if (appliedVoucher != null)
            {
                var now = DateTime.UtcNow;
                var valid = true;
                if (now < appliedVoucher.StartDate.ToUniversalTime()) valid = false;
                else if (now > appliedVoucher.EndDate.ToUniversalTime()) valid = false;
                else if (appliedVoucher.UsageLimit.HasValue && (appliedVoucher.UsedCount ?? 0) >= appliedVoucher.UsageLimit.Value) valid = false;
                else if (appliedVoucher.MinOrderValue.HasValue && cartTotal < appliedVoucher.MinOrderValue.Value) valid = false;

                if (valid)
                {
                    if (appliedVoucher.DiscountType == "fixed")
                    {
                        discountAmount = appliedVoucher.DiscountValue;
                    }
                    else // percentage
                    {
                        discountAmount = cartTotal * appliedVoucher.DiscountValue / 100;
                        if (appliedVoucher.MaxDiscountAmount.HasValue && discountAmount > appliedVoucher.MaxDiscountAmount.Value)
                            discountAmount = appliedVoucher.MaxDiscountAmount.Value;
                    }

                    appliedVoucher.UsedCount = (appliedVoucher.UsedCount ?? 0) + 1;
                }
            }
        }

        var order = new Order
        {
            Id = null!,
            UserId = user?.Id,
            FullName = fullName,
            Phone = phone,
            Address = address,
            Note = note,
            TotalPrice = cartTotal - discountAmount,
            DiscountAmount = discountAmount > 0 ? discountAmount : null,
            VoucherId = appliedVoucher?.Id,
            Status = paymentMethod == "Banking" ? "WaitingPayment" : "Pending",
            PaymentMethod = paymentMethod,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = paymentMethod == "Banking" ? DateTime.UtcNow.AddMinutes(15) : null
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

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
                $"<tr><td style=\"padding:8px;border-bottom:1px solid #eee;\">{i.Name}{(string.IsNullOrEmpty(i.VariantName) ? "" : $"<br><span style=\"color:#999;font-size:12px;\">{i.VariantName}</span>")}</td><td style=\"padding:8px;border-bottom:1px solid #eee;text-align:center\">{i.Quantity}</td><td style=\"padding:8px;border-bottom:1px solid #eee;text-align:right\">{i.Price.ToString("#,###")}₫</td><td style=\"padding:8px;border-bottom:1px solid #eee;text-align:right\"><b>{(i.Price * i.Quantity).ToString("#,###")}₫</b></td></tr>"
            ));

            var emailHtml = "<!DOCTYPE html>" +
            "<html><head><meta charset=\"utf-8\"></head>" +
            "<body style=\"margin:0;padding:0;background:#f4f7f6;font-family:Arial,Helvetica,sans-serif;\">" +
            "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\" style=\"padding:30px 15px;\">" +
            "<table width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;width:100%;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);\">" +
            // Header
            "<tr><td style=\"background:linear-gradient(135deg,#0033CC,#1A4BFF);padding:35px 30px;text-align:center;\">" +
            "<h1 style=\"color:#fff;margin:0 0 5px;font-size:24px;\">📦 Đặt hàng thành công!</h1>" +
            "<p style=\"color:#a6c1ff;margin:0;font-size:14px;\">Mã đơn: <b style=\"color:#fff;\">#" + order.Id + "</b></p>" +
            "</td></tr>" +
            // Body
            "<tr><td style=\"padding:30px;\">" +
            "<p style=\"font-size:15px;color:#333;margin:0 0 15px;\">Xin chào <b>" + user.FullName + "</b>,</p>" +
            "<p style=\"font-size:15px;color:#333;margin:0 0 20px;\">Cảm ơn bạn đã đặt hàng tại <b>TVT PC</b>. Đơn hàng của bạn đã được tiếp nhận và đang được xử lý.</p>" +
            // Info box
            "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f0f4ff;border-radius:12px;margin:0 0 20px;\"><tr><td style=\"padding:20px;\">" +
            "<p style=\"margin:3px 0;font-size:14px;color:#555;\">🔢 Mã đơn: <b>#" + order.Id + "</b></p>" +
            "<p style=\"margin:3px 0;font-size:14px;color:#555;\">📌 Trạng thái: <b>" + statusLabel + "</b></p>" +
            "<p style=\"margin:3px 0;font-size:14px;color:#555;\">💳 Thanh toán: <b>" + paymentLabel + "</b></p>" +
            "<p style=\"margin:3px 0;font-size:14px;color:#555;\">📅 Ngày đặt: <b>" + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "</b></p>" +
            "</td></tr></table>" +
            // Products table
            "<h3 style=\"font-size:15px;color:#333;margin:0 0 10px;\">🛒 Sản phẩm đã đặt</h3>" +
            "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;font-size:14px;\">" +
            "<thead><tr style=\"background:#f8fafc;\">" +
            "<th style=\"padding:10px;text-align:left;border-bottom:2px solid #e5e7eb;\">Sản phẩm</th>" +
            "<th style=\"padding:10px;text-align:center;border-bottom:2px solid #e5e7eb;width:50px;\">SL</th>" +
            "<th style=\"padding:10px;text-align:right;border-bottom:2px solid #e5e7eb;\">Đơn giá</th>" +
            "<th style=\"padding:10px;text-align:right;border-bottom:2px solid #e5e7eb;\">Thành tiền</th>" +
            "</tr></thead><tbody>" + itemsHtml + "</tbody>" +
            "<tfoot><tr>" +
            "<td colspan=\"3\" style=\"padding:12px 10px;text-align:right;font-weight:bold;\">Tổng cộng:</td>" +
            "<td style=\"padding:12px 10px;text-align:right;font-weight:bold;font-size:18px;color:#dc2626;\">" + order.TotalPrice.ToString("#,###") + "₫</td>" +
            "</tr></tfoot></table>" +
            "<hr style=\"border:none;border-top:1px solid #e5e7eb;margin:20px 0;\">" +
            // Shipping address
            "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td style=\"font-size:14px;color:#555;\">" +
            "<b>📍 Địa chỉ giao hàng:</b><br>" +
            fullName + "<br>" + phone + "<br>" + address +
            "</td></tr></table>" +
            (string.IsNullOrEmpty(note) ? "" : "<p style=\"font-size:14px;color:#555;margin:10px 0 0;\"><b>📝 Ghi chú:</b> " + note + "</p>") +
            "<hr style=\"border:none;border-top:1px solid #e5e7eb;margin:20px 0;\">" +
            "<p style=\"font-size:13px;color:#999;text-align:center;margin:0;\">TVT PC - Linh kiện máy tính chất lượng cao<br>Mọi thắc mắc vui lòng liên hệ qua email này.</p>" +
            "</td></tr></table>" +
            "</td></tr></table>" +
            "</body></html>";

            await _emailService.SendEmailAsync(user!.Email!, $"📦 TVT PC - Xác nhận đơn hàng #{order.Id}", emailHtml);
        }
        catch { /* Email lỗi không ảnh hưởng đến đơn hàng */ }

        if (paymentMethod == "Banking")
            return RedirectToAction("Payment", "Order", new { id = order.Id });

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Đặt hàng thành công!";
        return RedirectToAction("Success", "Order", new { id = order.Id });
    }
}
