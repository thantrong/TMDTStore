using Microsoft.AspNetCore.Mvc;
using TMDTStore.Services.Cart;

namespace TMDTStore.ViewComponents;

public class CartBadgeViewComponent : ViewComponent
{
    private readonly ICartService _cart;

    public CartBadgeViewComponent(ICartService cart)
    {
        _cart = cart;
    }

    public IViewComponentResult Invoke()
    {
        var count = _cart.GetCartCount();
        return View(count);
    }
}
