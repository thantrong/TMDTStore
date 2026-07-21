using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Services.Cart;

namespace TMDTStore.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly StoreDbContext _context;

    public CartController(ICartService cart, StoreDbContext context)
    {
        _cart = cart;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.SuggestedProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.ProductVariants)
            .Where(p => p.IsActive == true)
            .OrderByDescending(p => p.RatingAvg)
            .Take(8)
            .ToListAsync();
        return View();
    }

    // POST: /Cart/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(string productId, string? variantId, string name, string? variantName, string? imageUrl, decimal price, decimal? listPrice, int quantity, int maxQuantity)
    {
        if (quantity <= 0) quantity = 1;
        _cart.AddItem(productId, variantId, name, variantName, imageUrl, price, listPrice, quantity, maxQuantity);
        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Đã thêm vào giỏ hàng.";
        return RedirectToAction("Details", "Product", new { id = productId });
    }

    // POST: /Cart/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(string productId, string? variantId, int quantity)
    {
        _cart.UpdateQuantity(productId, variantId, quantity);
        return RedirectToAction("Index");
    }

    // POST: /Cart/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(string productId, string? variantId)
    {
        _cart.RemoveItem(productId, variantId);
        TempData["ToastType"] = "info";
        TempData["ToastMessage"] = "Đã xoá khỏi giỏ hàng.";
        return RedirectToAction("Index");
    }

    // POST: /Cart/BuyNow
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BuyNow(string productId, string? variantId, string name, string? variantName, string? imageUrl, decimal price, decimal? listPrice, int quantity, int maxQuantity)
    {
        if (quantity <= 0) quantity = 1;
        _cart.AddItem(productId, variantId, name, variantName, imageUrl, price, listPrice, quantity, maxQuantity);
        return RedirectToAction("Index", "Checkout");
    }

    // POST: /Cart/Clear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _cart.Clear();
        TempData["ToastType"] = "info";
        TempData["ToastMessage"] = "Đã xoá giỏ hàng.";
        return RedirectToAction("Index");
    }
}
