using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels.Variant;
using TMDTStore.Services.Cloudinary;

namespace TMDTStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VariantController : Controller
{
    private readonly StoreDbContext _context;
    private readonly ICloudinaryService _cloudinaryService;

    public VariantController(StoreDbContext context, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    // GET: /Admin/Variant/Index/{productId}
    [HttpGet]
    public async Task<IActionResult> Index(string productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound();

        var variants = await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.SortOrder)
            .ToListAsync();

        ViewBag.Product = product;
        return View(variants);
    }

    // GET: /Admin/Variant/Create/{productId}
    [HttpGet]
    public async Task<IActionResult> Create(string productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound();

        ViewBag.Product = product;
        return View(new VariantCreateViewModel { ProductId = productId });
    }

    // POST: /Admin/Variant/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VariantCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            ViewBag.Product = product;
            return View(model);
        }

        // Map ViewModel → Entity
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid().ToString("N"),
            ProductId = model.ProductId,
            Name = model.Name,
            Price = model.Price,
            ListPrice = model.ListPrice,
            StockQuantity = model.StockQuantity,
            SortOrder = model.SortOrder,
            Attributes = model.Attributes,
            Description = model.Description,
            ImageUrl = null,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Tự động sinh SKU nếu để trống
        if (string.IsNullOrWhiteSpace(model.Sku) && !string.IsNullOrWhiteSpace(model.Name))
        {
            variant.Sku = RemoveVietnameseAccents(model.Name.ToUpperInvariant())
                .Replace(" ", "-");
        }
        else
        {
            variant.Sku = model.Sku!;
        }

        // Upload image nếu có
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            try
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(model.ImageFile, "variants");
                if (!string.IsNullOrEmpty(imageUrl))
                    variant.ImageUrl = imageUrl;
            }
            catch (Exception)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không thể tải ảnh lên.";
                return View(model);
            }
        }

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Biến thể đã được tạo.";
        return RedirectToAction("Index", new { productId = model.ProductId });
    }

    // GET: /Admin/Variant/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (variant == null) return NotFound();

        var model = new VariantEditViewModel
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Name = variant.Name,
            Sku = variant.Sku,
            Price = variant.Price,
            ListPrice = variant.ListPrice,
            StockQuantity = variant.StockQuantity,
            SortOrder = variant.SortOrder,
            Attributes = variant.Attributes,
            Description = variant.Description,
            IsActive = variant.IsActive
        };

        ViewBag.ExistingImageUrl = variant.ImageUrl;
        return View(model);
    }

    // POST: /Admin/Variant/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, VariantEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var variant = await _context.ProductVariants.FindAsync(id);
        if (variant == null) return NotFound();

        variant.Name = model.Name;
        variant.Sku = model.Sku ?? RemoveVietnameseAccents(model.Name.ToUpperInvariant()).Replace(" ", "-");
        variant.Price = model.Price;
        variant.ListPrice = model.ListPrice;
        variant.StockQuantity = model.StockQuantity;
        variant.Description = model.Description;
        variant.Attributes = model.Attributes;
        variant.SortOrder = model.SortOrder;
        variant.IsActive = model.IsActive;

        // Upload image mới nếu có
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            try
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(model.ImageFile, "variants");
                if (!string.IsNullOrEmpty(imageUrl))
                    variant.ImageUrl = imageUrl;
            }
            catch (Exception)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không thể tải ảnh lên.";
                return View(model);
            }
        }

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Biến thể đã được cập nhật.";
        return RedirectToAction("Index", new { productId = variant.ProductId });
    }

    private static string RemoveVietnameseAccents(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // Thay Đ/đ trước vì không bị ảnh hưởng bởi FormD
        text = text.Replace("Đ", "D").Replace("đ", "d");
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
        return new string(chars).Normalize(System.Text.NormalizationForm.FormC);
    }

    // POST: /Admin/Variant/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var variant = await _context.ProductVariants.FindAsync(id);
        if (variant == null) return NotFound();

        var productId = variant.ProductId;
        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Biến thể đã được xoá.";
        return RedirectToAction("Index", new { productId });
    }
}
