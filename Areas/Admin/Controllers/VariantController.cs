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

    [HttpGet]
    public async Task<IActionResult> Index(string productId, string? search, int page = 1)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound();

        var query = _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var kw = search.ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(kw) || v.Sku.ToLower().Contains(kw));
        }

        var totalItems = await query.CountAsync();
        var pageSize = 10;
        var variants = await query
            .OrderBy(v => v.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Product = product;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Search = search;
        return View(variants);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound();

        ViewBag.Product = product;
        return View(new VariantCreateViewModel { ProductId = productId });
    }

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

        // Tự động sinh SKU 10 chữ số nếu để trống
        if (string.IsNullOrWhiteSpace(model.Sku))
        {
            variant.Sku = await GenerateSequentialSku();
        }
        else
        {
            variant.Sku = model.Sku;
        }

        // Upload images nếu có
        var uploadedUrls = new List<string>();
        if (model.ImageFiles != null && model.ImageFiles.Count > 0)
        {
            try
            {
                foreach (var image in model.ImageFiles)
                {
                    if (image.Length > 0)
                    {
                        var url = await _cloudinaryService.UploadImageAsync(image, "variants");
                        if (!string.IsNullOrEmpty(url))
                            uploadedUrls.Add(url);
                    }
                }
                if (uploadedUrls.Count > 0)
                {
                    variant.ImageUrl = uploadedUrls.First();
                    variant.ImageUrls = System.Text.Json.JsonSerializer.Serialize(uploadedUrls);
                }
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
            IsActive = variant.IsActive,
            ExistingImageUrls = variant.ImageUrls ?? (variant.ImageUrl != null ? "[\"" + variant.ImageUrl + "\"]" : "[]")
        };

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
        // Giữ nguyên SKU cũ nếu để trống
        variant.Sku = string.IsNullOrWhiteSpace(model.Sku) ? variant.Sku : model.Sku;
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

        // Upload images mới nếu có — giữ ảnh cũ + thêm ảnh mới
        var existingUrls = new List<string>();
        if (!string.IsNullOrEmpty(variant.ImageUrls))
        {
            try { existingUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(variant.ImageUrls) ?? new(); } catch { }
        }
        if (!string.IsNullOrEmpty(variant.ImageUrl) && !existingUrls.Contains(variant.ImageUrl))
            existingUrls.Insert(0, variant.ImageUrl);

        if (model.ImageFiles != null && model.ImageFiles.Count > 0)
        {
            try
            {
                foreach (var image in model.ImageFiles)
                {
                    if (image.Length > 0)
                    {
                        var url = await _cloudinaryService.UploadImageAsync(image, "variants");
                        if (!string.IsNullOrEmpty(url))
                            existingUrls.Add(url);
                    }
                }
                if (existingUrls.Count > 0)
                {
                    variant.ImageUrl = existingUrls.First();
                    variant.ImageUrls = System.Text.Json.JsonSerializer.Serialize(existingUrls);
                }
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

    private async Task<string> GenerateSequentialSku()
    {
        var prefix = DateTime.Now.ToString("yyMMdd"); // VD: 260702
        var todayCount = await _context.ProductVariants.CountAsync(v => v.Sku.StartsWith(prefix));
        var seq = (todayCount + 1).ToString("D3"); // 001, 002...
        var variantDigit = "0";
        return $"{prefix}{seq}{variantDigit}"; // 10 chữ số: YYMMDDSSSV
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
