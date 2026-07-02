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
            // Không set Id — database tự sinh theo sequence VAR_001, VAR_002...
            ProductId = model.ProductId,
            Name = model.Name,
            Price = model.Price,
            ListPrice = model.ListPrice,
            SalePrice = model.SalePrice,
            StockQuantity = model.StockQuantity,
            Weight = model.Weight,
            Barcode = model.Barcode,
            ManufacturerCode = model.ManufacturerCode,
            SortOrder = model.SortOrder,
            Attributes = model.Attributes,
            Description = model.Description,
            ImageUrl = null,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Tự động sinh SKU nếu để trống hoặc đảm bảo không trùng
        if (string.IsNullOrWhiteSpace(model.Sku) && !string.IsNullOrWhiteSpace(model.Name))
        {
            var rawSku = RemoveVietnameseAccents(model.Name.ToUpperInvariant())
                .Replace(" ", "-");
            variant.Sku = await GenerateUniqueSku(rawSku);
        }
        else
        {
            variant.Sku = await GenerateUniqueSku(TruncateSku(model.Sku!));
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
            SalePrice = variant.SalePrice,
            StockQuantity = variant.StockQuantity,
            Weight = variant.Weight,
            Barcode = variant.Barcode,
            ManufacturerCode = variant.ManufacturerCode,
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

        // Đảm bảo SKU không trùng (bỏ qua chính nó)
        var newSku = TruncateSku(model.Sku ?? RemoveVietnameseAccents(model.Name.ToUpperInvariant()).Replace(" ", "-"));
        if (newSku != variant.Sku)
        {
            variant.Sku = await GenerateUniqueSku(newSku);
        }
        variant.Price = model.Price;
        variant.ListPrice = model.ListPrice;
        variant.SalePrice = model.SalePrice;
        variant.StockQuantity = model.StockQuantity;
        variant.Weight = model.Weight;
        variant.Barcode = model.Barcode;
        variant.ManufacturerCode = model.ManufacturerCode;
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

    private static string TruncateSku(string sku, int maxLength = 20)
    {
        if (string.IsNullOrEmpty(sku)) return sku;
        return sku.Length <= maxLength ? sku : sku[..maxLength];
    }

    private async Task<string> GenerateUniqueSku(string baseSku)
    {
        if (string.IsNullOrWhiteSpace(baseSku)) return baseSku;

        // Kiểm tra tồn tại
        var exists = await _context.ProductVariants.AnyAsync(v => v.Sku == baseSku);
        if (!exists) return baseSku;

        // Thêm hậu tố số cho đến khi unique
        for (int i = 1; i <= 999; i++)
        {
            var candidate = TruncateSku($"{baseSku}-{i}");
            exists = await _context.ProductVariants.AnyAsync(v => v.Sku == candidate);
            if (!exists) return candidate;
        }

        // Throw nếu không thể sinh unique (rất hiếm)
        throw new Exception("Không thể sinh SKU duy nhất. Vui lòng nhập tay.");
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
