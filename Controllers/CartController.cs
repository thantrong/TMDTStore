using Microsoft.AspNetCore.Mvc;
using TMDTStore.Services.Cart;

namespace TMDTStore.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart)
    {
        _cart = cart;
    }

    // GET: /Cart
    [HttpGet]
    public IActionResult Index()
    {
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
