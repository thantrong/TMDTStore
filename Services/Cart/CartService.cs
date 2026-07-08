using System.Text.Json;
using TMDTStore.Models;

namespace TMDTStore.Services.Cart;

public interface ICartService
{
    List<CartItem> GetCart();
    CartItem? AddItem(string productId, string? variantId, string name, string? variantName, string? imageUrl, decimal price, decimal? listPrice, int quantity, int maxQuantity);
    void UpdateQuantity(string productId, string? variantId, int quantity);
    void RemoveItem(string productId, string? variantId);
    int GetCartCount();
    decimal GetCartTotal();
    void Clear();
}

public class CartService : ICartService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string SessionKey = "Cart";

    public CartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession Session => _httpContextAccessor.HttpContext!.Session;

    public List<CartItem> GetCart()
    {
        var data = Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(data))
            return new List<CartItem>();
        try { return JsonSerializer.Deserialize<List<CartItem>>(data) ?? new(); }
        catch { return new List<CartItem>(); }
    }

    private void SaveCart(List<CartItem> items)
    {
        Session.SetString(SessionKey, JsonSerializer.Serialize(items));
    }

    public CartItem? AddItem(string productId, string? variantId, string name, string? variantName, string? imageUrl, decimal price, decimal? listPrice, int quantity, int maxQuantity)
    {
        var cart = GetCart();
        var key = variantId ?? productId;

        var existing = cart.FirstOrDefault(i => (i.VariantId ?? i.ProductId) == key);
        if (existing != null)
        {
            var newQty = existing.Quantity + quantity;
            if (newQty > maxQuantity)
                newQty = maxQuantity;
            existing.Quantity = newQty;
            existing.MaxQuantity = maxQuantity;
            SaveCart(cart);
            return existing;
        }

        var item = new CartItem
        {
            ProductId = productId,
            VariantId = variantId,
            Name = name,
            VariantName = variantName,
            ImageUrl = imageUrl,
            Price = price,
            ListPrice = listPrice,
            Quantity = Math.Min(quantity, maxQuantity),
            MaxQuantity = maxQuantity
        };
        cart.Add(item);
        SaveCart(cart);
        return item;
    }

    public void UpdateQuantity(string productId, string? variantId, int quantity)
    {
        var cart = GetCart();
        var key = variantId ?? productId;
        var item = cart.FirstOrDefault(i => (i.VariantId ?? i.ProductId) == key);
        if (item == null) return;

        if (quantity <= 0)
        {
            cart.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(quantity, item.MaxQuantity);
        }
        SaveCart(cart);
    }

    public void RemoveItem(string productId, string? variantId)
    {
        var cart = GetCart();
        var key = variantId ?? productId;
        cart.RemoveAll(i => (i.VariantId ?? i.ProductId) == key);
        SaveCart(cart);
    }

    public int GetCartCount()
    {
        return GetCart().Sum(i => i.Quantity);
    }

    public decimal GetCartTotal()
    {
        return GetCart().Sum(i => i.Price * i.Quantity);
    }

    public void Clear()
    {
        Session.Remove(SessionKey);
    }
}
