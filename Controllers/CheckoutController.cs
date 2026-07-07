using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Services.Cart;
using TMDTStore.Services.Banking;

namespace TMDTStore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly StoreDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IVietQrService _vietQr;

    public CheckoutController(ICartService cart, StoreDbContext context, UserManager<User> userManager, IVietQrService vietQr)
    {
        _cart = cart;
        _context = context;
        _userManager = userManager;
        _vietQr = vietQr;
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

        if (paymentMethod == "Banking")
            return RedirectToAction("Payment", "Order", new { id = order.Id });

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Đặt hàng thành công!";
        return RedirectToAction("Success", "Order", new { id = order.Id });
    }
}
